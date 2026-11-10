using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using SFB;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class Chunk_manager : MonoBehaviour
{
    public main main;
    public Biome_Interface biome_Interface;
    public Dictionary<Vector2Int, Chunk> SelectedChunks = new Dictionary<Vector2Int, Chunk>();
    public Vector2Int grab;
    public bool selecting_chunks;

    public Toggle toggle_slider;
    public Slider slider_in_toggle;

    public GameObject Chunk_cord_label;

    [SerializeReference] RectTransform[] UI_Zones;

    public GameObject chunk_fab;

    // Start is called before the first frame update
    void Start()
    {
        GameObject newChunkObject = Instantiate(chunk_fab, Vector3.zero, Quaternion.identity);
        Chunk newChunk = newChunkObject.GetComponent<Chunk>();
        SelectedChunks.Add(Vector2Int.zero, newChunk);
    }

    // Update is called once per frame
    void Update()
    {
        if (!biome_Interface.Pipetting && !UI_colider())
        {
            Vector2 wordpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = new Vector2Int(
            Mathf.FloorToInt(wordpos.x / 500f),
            Mathf.FloorToInt(wordpos.y / 500f));
            Chunk_cord_label.GetComponent<Text>().text = $"({gridPos.x},{gridPos.y})";
            Chunk_cord_label.transform.position = (Vector2)gridPos * 500 + (new Vector2Int(250, 250));
            if (Input.GetMouseButtonDown(0))
            {
                grab = gridPos;
                selecting_chunks = true;
            }
            if (Input.GetMouseButtonUp(0) && selecting_chunks)
            {
                Vector2Int min = new Vector2Int(Mathf.Min(grab.x, gridPos.x), Mathf.Min(grab.y, gridPos.y));
                Vector2Int max = new Vector2Int(Mathf.Max(grab.x, gridPos.x), Mathf.Max(grab.y, gridPos.y));

                for (int ix = min.x; ix <= max.x; ix++)
                {
                    for (int iy = min.y; iy <= max.y; iy++)
                    {
                        Vector2Int gridPos_recursive = new Vector2Int(ix,iy);

                        if (SelectedChunks.TryGetValue(gridPos_recursive, out Chunk existingChunk))
                        {
                            Destroy(existingChunk.gameObject);
                            SelectedChunks.Remove(gridPos_recursive);

                            Debug.Log($"Removed chunk at: {gridPos_recursive}. Total selected: {SelectedChunks.Count}");
                        }
                        else
                        {
                            // The chunk doesn't exist -> Spawn the prefab at the centered grid position
                            Vector3 spawnPosition = new Vector3(gridPos_recursive.x, gridPos_recursive.y, 0f) * 500;
                            GameObject newChunkObject = Instantiate(chunk_fab, spawnPosition, Quaternion.identity);

                            // Get the Chunk component attached to the spawned prefab
                            Chunk newChunk = newChunkObject.GetComponent<Chunk>();

                            // Store it in the dictionary using the grid position as the key
                            SelectedChunks.Add(gridPos_recursive, newChunk);

                            Debug.Log($"Added chunk at: {gridPos_recursive}. Total selected: {SelectedChunks.Count}");
                        }
                    }
                }
            }
        }
    }
    public bool UI_colider()
    {
        for (int x = 0; x < UI_Zones.Length; x++)
        {
            try
            {
                if (UI_Zones[x].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(UI_Zones[x], Input.mousePosition, Camera.main)) return true;
            }
            catch (System.Exception)
            {
                Debug.Log("Error");
                throw;
            }
        }
        return false;
    }

    public void Toggle_the_Slider() //these are the worst names known to gog I know stop reminding me
    {
        if (toggle_slider.isOn) slider_in_toggle.value = 1;
        else slider_in_toggle.value = 0;
    }

    public void generate_TerrainJSON()
    {
        string path = Path.Combine(main.Directory, "Terrain.json");

        string json_data = File.ReadAllText(path);
        Terrain_File data = JsonUtility.FromJson<Terrain_File>(json_data);

        data.tiles = new List<Terrain_File.tile_class>();
        // Get all .riff files from the target directory
        string[] riffFiles = Directory.GetFiles(main.Directory, "*.riff");

        // Regular expression to safely parse positive and negative integers from the name format: tile_XXX_YYY.riff
        Regex regex = new Regex(@"tile_(-?\d+)_(-?\d+)\.riff");

        foreach (string file in riffFiles)
        {
            string fileName = Path.GetFileName(file);
            Match match = regex.Match(fileName);

            if (match.Success)
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);

                int min = 0;
                int max = 0;

                try
                {
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(fs))
                    {
                        // Basic sanity check for RIFF header
                        string riffHeader = new string(reader.ReadChars(4));
                        if (riffHeader != "RIFF") continue;

                        // Skip RIFF size (4 bytes) + Form Type "RRT0" (4 bytes)
                        fs.Seek(8, SeekOrigin.Current);

                        bool foundRhgi = false;

                        // Walk through chunks to find RHGI safely
                        while (fs.Position < fs.Length)
                        {
                            string chunkId = new string(reader.ReadChars(4));
                            int chunkSize = reader.ReadInt32();

                            if (chunkId == "RHGI")
                            {
                                int version = reader.ReadInt32(); // Skip / read Version (01 00 00 00)
                                min = reader.ReadInt32();         // Read minHeight (4 bytes)
                                max = reader.ReadInt32();         // Read maxHeight (4 bytes)
                                foundRhgi = true;
                                break;
                            }
                            else
                            {
                                // Skip past this chunk's payload to get to the next chunk header
                                // (Adding +1 & ~1 ensures alignment if your writer pads odd-sized chunks)
                                int alignedSize = (chunkSize + 1) & ~1;
                                fs.Seek(alignedSize, SeekOrigin.Current);
                            }
                        }

                        if (!foundRhgi)
                        {
                            Debug.LogWarning($"RHGI chunk not found in file: {fileName}");
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error reading structural binary data from {fileName}: {e.Message}");
                    continue;
                }

                // Create and populate the tile entry
                Terrain_File.tile_class newTile = new Terrain_File.tile_class
                {
                    x = x,
                    y = y,
                    min = min,
                    max = max
                };

                data.tiles.Add(newTile);
            }
        }

        json_data = JsonUtility.ToJson(data);
        File.WriteAllText(path, json_data);
    }
}
