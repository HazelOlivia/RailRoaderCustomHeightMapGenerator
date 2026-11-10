using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class Texture16BitLoader
{
    public static Texture2D LoadR16Png(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found at: {filePath}");
            return null;
        }

        byte[] fileBytes = File.ReadAllBytes(filePath);

        // Validate PNG Signature
        if (fileBytes.Length < 8 ||
            fileBytes[0] != 137 || fileBytes[1] != 80 || fileBytes[2] != 78 || fileBytes[3] != 71)
        {
            Debug.LogError("Not a valid PNG file.");
            return null;
        }

        int width = 0;
        int height = 0;
        int index = 8;

        using (MemoryStream idatMS = new MemoryStream())
        {
            while (index < fileBytes.Length - 4)
            {
                int chunkSize = (fileBytes[index] << 24) | (fileBytes[index + 1] << 16) | (fileBytes[index + 2] << 8) | fileBytes[index + 3];
                index += 4;

                string chunkType = "" + (char)fileBytes[index] + (char)fileBytes[index + 1] + (char)fileBytes[index + 2] + (char)fileBytes[index + 3];
                index += 4;

                if (chunkType == "IHDR")
                {
                    width = (fileBytes[index] << 24) | (fileBytes[index + 1] << 16) | (fileBytes[index + 2] << 8) | fileBytes[index + 3];
                    height = (fileBytes[index + 4] << 24) | (fileBytes[index + 5] << 16) | (fileBytes[index + 6] << 8) | fileBytes[index + 7];

                    int bitDepth = fileBytes[index + 8];
                    int colorType = fileBytes[index + 9];

                    if (bitDepth != 16 || colorType != 0)
                    {
                        Debug.LogError($"Texture16BitLoader only supports Grayscale 16-bit PNGs. This file is ColorType:{colorType}, BitDepth:{bitDepth}");
                        return null;
                    }
                }
                else if (chunkType == "IDAT")
                {
                    idatMS.Write(fileBytes, index, chunkSize);
                }
                else if (chunkType == "IEND")
                {
                    break;
                }

                index += chunkSize + 4; // Skip data + CRC
            }

            if (width <= 0 || height <= 0 || idatMS.Length == 0)
            {
                Debug.LogError("Failed to parse valid PNG metadata or IDAT chunks.");
                return null;
            }

            byte[] compressedData = idatMS.ToArray();
            byte[] uncompressedData = DecompressAndFilterPNG(compressedData, width, height);

            if (uncompressedData == null) return null;

            // Create true uncompressed R16 Texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.R16, false, true);
            texture.LoadRawTextureData(uncompressedData);
            texture.Apply();
            return texture;
        }
    }

    private static byte[] DecompressAndFilterPNG(byte[] compressed, int width, int height)
    {
        // Skip 2-byte ZLIB header (0x78 0x9C) so .NET DeflateStream can parse the raw bits
        using (MemoryStream ms = new MemoryStream(compressed, 2, compressed.Length - 2))
        using (DeflateStream deflate = new DeflateStream(ms, CompressionMode.Decompress))
        using (MemoryStream output = new MemoryStream())
        {
            deflate.CopyTo(output);
            byte[] decompressedBytes = output.ToArray();

            int bpp = 2; // 2 bytes per pixel for 16-bit grayscale
            int stride = (width * bpp) + 1; // 1 filter byte + row data bytes
            byte[] rawTextureBytes = new byte[width * height * bpp];

            byte[] prevRowBytes = new byte[width * bpp];
            byte[] currRowBytes = new byte[width * bpp];

            for (int y = 0; y < height; y++)
            {
                int srcRowOffset = y * stride;
                if (srcRowOffset >= decompressedBytes.Length) break;

                byte filterType = decompressedBytes[srcRowOffset];

                // 1. Reconstruct current row bytes using the PNG specification filters
                for (int i = 0; i < width * bpp; i++)
                {
                    byte rawByte = decompressedBytes[srcRowOffset + 1 + i];

                    byte a = (i >= bpp) ? currRowBytes[i - bpp] : (byte)0;
                    byte b = (y > 0) ? prevRowBytes[i] : (byte)0;
                    byte c = (y > 0 && i >= bpp) ? prevRowBytes[i - bpp] : (byte)0;

                    byte reconByte = 0;
                    switch (filterType)
                    {
                        case 0: // None
                            reconByte = rawByte;
                            break;
                        case 1: // Sub
                            reconByte = (byte)(rawByte + a);
                            break;
                        case 2: // Up
                            reconByte = (byte)(rawByte + b);
                            break;
                        case 3: // Average
                            reconByte = (byte)(rawByte + ((a + b) / 2));
                            break;
                        case 4: // Paeth
                            reconByte = (byte)(rawByte + PaethPredictor(a, b, c));
                            break;
                        default:
                            reconByte = rawByte;
                            break;
                    }
                    currRowBytes[i] = reconByte;
                }

                // 2. Flip Y (PNG is top-to-bottom, Unity is bottom-to-top) 
                // and swap Endianness from Big-Endian to Little-Endian
                int targetY = height - 1 - y;
                int destRowOffset = targetY * width * bpp;

                for (int x = 0; x < width * bpp; x += 2)
                {
                    byte msb = currRowBytes[x];     // PNG high byte
                    byte lsb = currRowBytes[x + 1]; // PNG low byte

                    rawTextureBytes[destRowOffset + x] = lsb;     // Unity expects Low Byte first
                    rawTextureBytes[destRowOffset + x + 1] = msb; // Unity expects High Byte second
                }

                // Swap rows for next loop iteration
                byte[] temp = prevRowBytes;
                prevRowBytes = currRowBytes;
                currRowBytes = temp;
            }

            return rawTextureBytes;
        }
    }

    private static byte PaethPredictor(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);

        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }
}
