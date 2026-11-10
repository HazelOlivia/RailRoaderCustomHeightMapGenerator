using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RIFF_generator : MonoBehaviour
{
    main main;
    Chunk_manager chunk_Manager;
    Biome_Interface Biome_Interface;

    public ComputeShader HeightComputeShader;
    public ComputeShader BiomeComputeShader;

    public UnityEngine.UI.Button button;

    void Start()
    {
        main = gameObject.GetComponent<main>();
        chunk_Manager = gameObject.GetComponent<Chunk_manager>();
        Biome_Interface = gameObject.GetComponent<Biome_Interface>();
    }

    public void generate_RIFFs()
    {
        button.gameObject.SetActive(false);
        foreach (var item in chunk_Manager.SelectedChunks)
        {
            generate_RIFF(item.Key);
        }
        chunk_Manager.generate_TerrainJSON();
        button.gameObject.SetActive(true);
    }
    public void generate_RIFF(Vector2Int key)
    {
        int HeightMapSize = 513;
        ushort[,] myDataHeight = new ushort[HeightMapSize, HeightMapSize];

        uint[] rawOutputBuffer = new uint[HeightMapSize * HeightMapSize];
        int kernel = HeightComputeShader.FindKernel("CSMain");

        HeightComputeShader.SetTexture(kernel, "HeightMapTexture", main.HeightMapTexture);
        HeightComputeShader.SetInt("Width", main.HeightMapTexture.width);
        HeightComputeShader.SetInt("Height", main.HeightMapTexture.height);
        HeightComputeShader.SetVector("HeightMapOffset", main.HeightMapOffset);
        HeightComputeShader.SetFloat("Resolution", main.resolution_height);
        HeightComputeShader.SetVector("ChunkKey", new Vector2(key.x, key.y));
        HeightComputeShader.SetInt("OutputSize", HeightMapSize);

        ComputeBuffer resultBuffer = new ComputeBuffer(rawOutputBuffer.Length, sizeof(uint));
        resultBuffer.SetData(rawOutputBuffer);
        HeightComputeShader.SetBuffer(kernel, "OutputHeightBuffer", resultBuffer);

        int threadGroups = Mathf.CeilToInt(HeightMapSize / 8f);
        HeightComputeShader.Dispatch(kernel, threadGroups, threadGroups, 1);

        resultBuffer.GetData(rawOutputBuffer);
        resultBuffer.Release();

        for (int y = 0; y < HeightMapSize; y++)
        {
            for (int x = 0; x < HeightMapSize; x++)
            {
                myDataHeight[x, y] = (ushort)rawOutputBuffer[y * HeightMapSize + x];
                //Vector2 pos = (new Vector2((float)(i0) / (float)(widthHeight - 1), (float)(i1) / (float)(heightHeight - 1)) + key) * 500;
                //myDataHeight[i0, i1] = main.FetchSubPixel_Height(pos);
            }
        }
        int BiomeMapSize = 256;
        byte[,] myDataBiome = new byte[BiomeMapSize, BiomeMapSize];

        rawOutputBuffer = new uint[BiomeMapSize * BiomeMapSize];
        kernel = BiomeComputeShader.FindKernel("CSMain");

        Vector4[] shaderColors = new Vector4[Biome_Interface.Colors.Length];
        for (int i = 0; i < Biome_Interface.Colors.Length; i++)
        {
            shaderColors[i] = new Vector4(Biome_Interface.Colors[i].r, Biome_Interface.Colors[i].g, Biome_Interface.Colors[i].b, Biome_Interface.Colors[i].a);
        }

        ComputeBuffer colorBuffer = new ComputeBuffer(shaderColors.Length, sizeof(float) * 4);
        ComputeBuffer idBuffer = new ComputeBuffer(Biome_Interface.IDs.Length, sizeof(int));

        colorBuffer.SetData(shaderColors);
        idBuffer.SetData(Biome_Interface.IDs);

        BiomeComputeShader.SetTexture(kernel, "BiomeMapTexture", main.BiomeMapTexture);
        BiomeComputeShader.SetInt("Width", main.BiomeMapTexture.width);
        BiomeComputeShader.SetInt("Height", main.BiomeMapTexture.height);
        BiomeComputeShader.SetVector("BiomeMapOffset", main.HeightMapOffset);
        BiomeComputeShader.SetFloat("Resolution", main.resolution_biome);
        BiomeComputeShader.SetVector("ChunkKey", new Vector2(key.x, key.y));
        BiomeComputeShader.SetInt("OutputSize", BiomeMapSize);

        BiomeComputeShader.SetBuffer(kernel, "InspectorColors", colorBuffer);
        BiomeComputeShader.SetBuffer(kernel, "InspectorIDs", idBuffer);
        BiomeComputeShader.SetInt("MapLength", Biome_Interface.IDs.Length);

        resultBuffer = new ComputeBuffer(rawOutputBuffer.Length, sizeof(uint));
        resultBuffer.SetData(rawOutputBuffer);
        BiomeComputeShader.SetBuffer(kernel, "OutputBiomeBuffer", resultBuffer);

        threadGroups = Mathf.CeilToInt(BiomeMapSize / 8f);
        BiomeComputeShader.Dispatch(kernel, threadGroups, threadGroups, 1);

        resultBuffer.GetData(rawOutputBuffer);
        resultBuffer.Release();
        colorBuffer.Release();
        idBuffer.Release();

        for (int y = 0; y < BiomeMapSize; y++)
        {
            for (int x = 0; x < BiomeMapSize; x++)
            {
                myDataBiome[x, y] = (byte)rawOutputBuffer[y * BiomeMapSize + x];
            }
        }
        string full_path = Path.Combine(main.Directory, $"tile_{key.x:000;-000}_{key.y:000;-000}.riff");
        RailroaderTerrainExporter.ExportTile(full_path, main.HeightMapMinHeight, main.HeightMapMaxHeight, myDataHeight, myDataBiome);
    }
}

public static class RailroaderTerrainExporter
{
    /// <summary>
    /// Exports terrain heights and biomes into a valid Railroader .riff tile.
    /// </summary>
    /// <param name="outputPath">Path to save the file (e.g., "tile_004_-012.riff")</param>
    /// <param name="minHeight">Minimum height in meters</param>
    /// <param name="maxHeight">Maximum height in meters</param>
    /// <param name="heightmap">513x513 array of heights in meters</param>
    /// <param name="biomes">256x256 array of biome codes (0-255)</param>
    public static void ExportTile(string outputPath, int minHeight, int maxHeight, ushort[,] heightmap, byte[,] biomes)
    {
        // 1. Prepare raw payload data for the chunks
        byte[] rhgiPayload = CreateRhgiPayload(minHeight, maxHeight);
        byte[] rhgtPayload = CreateRhgtPayload(minHeight, maxHeight, heightmap);
        byte[] rbioPayload = CreateRbioPayload(biomes);
        
        // 2. Calculate chunk lengths (including their headers and padding)
        Int32 rhgiTotalSize = GetChunkTotalWireSize(rhgiPayload.Length);
        Int32 rhgtTotalSize = GetChunkTotalWireSize(rhgtPayload.Length);
        Int32 rbioTotalSize = GetChunkTotalWireSize(rbioPayload.Length);

        // RIFF Container Size = Form Type (4 bytes) + Combined Total Size of all Chunk Blocks
        Int32 riffSize = 4 + rhgiTotalSize + rhgtTotalSize + rbioTotalSize;

        // 3. Write out the structural container
        using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        using (var writer = new BinaryWriter(fs, Encoding.ASCII))
        {
            // RIFF Header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(riffSize);
            writer.Write(Encoding.ASCII.GetBytes("RRT0"));

            // Write individual sub-chunks
            WriteChunk(writer, "RHGI", rhgiPayload);
            WriteChunk(writer, "RHGT", rhgtPayload);
            WriteChunk(writer, "RBIO", rbioPayload);
        }
    }

    private static byte[] CreateRhgiPayload(int minHeight, int maxHeight)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write((int)1);         // Version: 1 = includes biome raster (RBIO)
            writer.Write(minHeight);     // Min height
            writer.Write(maxHeight);     // Max height
            return ms.ToArray();
        }
    }

    private static byte[] CreateRhgtPayload(int minHeight, int maxHeight, ushort[,] heightmap)
    {
        int width = heightmap.GetLength(0);  // Expected: 513
        int height = heightmap.GetLength(1); // Expected: 513

        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(Encoding.ASCII.GetBytes("R16 ")); // 4-byte Pixel format
            writer.Write(width);
            writer.Write(height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ushort sample = heightmap[x, y];

                    writer.Write(sample);
                }
            }
            return ms.ToArray();
        }
    }

    private static byte[] CreateRbioPayload(byte[,] biomes)
    {
        int width = biomes.GetLength(0);  // Expected: 256
        int height = biomes.GetLength(1); // Expected: 256

        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            // Texture Raster Header
            writer.Write(Encoding.ASCII.GetBytes("R8  ")); // 4-byte Pixel format
            writer.Write(width);
            writer.Write(height);

            // Raster data mapping
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    writer.Write(biomes[x, y]);
                }
            }
            return ms.ToArray();
        }
    }

    private static void WriteChunk(BinaryWriter writer, string chunkId, byte[] payload)
    {
        writer.Write(Encoding.ASCII.GetBytes(chunkId));
        writer.Write((Int32)payload.Length);
        writer.Write(payload);

        // Word-alignment padding (does not count toward chunk size integer)
        if (payload.Length % 2 != 0)
        {
            writer.Write((byte)0);
        }
    }

    private static Int32 GetChunkTotalWireSize(int payloadLength)
    {
        // 4 bytes ID + 4 bytes Size integer + payload length + optional padding byte
        Int32 size = 8 + payloadLength;
        if (payloadLength % 2 != 0) size++;
        return size;
    }
}
