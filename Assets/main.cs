using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using SFB;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Windows.Forms;

public class main : MonoBehaviour
{
    public Texture2D HeightMapTexture;
    [NonSerialized] public ushort[] HeightMap_RawData; //if onspected in inspector causes ubity to freeze, don't do that
    public Texture2D BiomeMapTexture;
    public SpriteRenderer HeightMapSpriteRenderer;
    public SpriteRenderer BiomeMapSpriteRenderer;

    public string Directory = "";
    public Text Directory_label;

    public float resolution_height = 1; // m/ppx
    public float resolution_biome = 1; // m/ppx
    public InputField inputField_Resolution_Height;
    public InputField inputField_Resolution_Biome;
    public Vector2 HeightMapOffset = Vector2.zero;
    public int HeightMapMinHeight = 0;
    public int HeightMapMaxHeight = 50;

    public Vector2 grab;
    public Vector2 HeightMapPosAtGrab;

    public InputField InputFieldXOffset;
    public InputField InputFieldYOffset;

    public InputField InputFieldMin;
    public InputField InputFieldMax;

    public UnityEngine.UI.Button RIFF_button;

    // Start is called before the first frame update
    void Start()
    {
        //camera_Controller = Camera.main.GetComponent<Camera_controller>();
        inputField_Resolution_Height.SetTextWithoutNotify("1");
        inputField_Resolution_Biome.SetTextWithoutNotify("1");
        InputFieldXOffset.SetTextWithoutNotify("0");
        InputFieldYOffset.SetTextWithoutNotify("0");
        InputFieldMin.SetTextWithoutNotify("0");
        InputFieldMax.SetTextWithoutNotify("50");
    }

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Vector3 mouse_pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(1))
            {
                grab = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                HeightMapPosAtGrab = HeightMapSpriteRenderer.transform.position;
            }
            HeightMapOffset = ((Vector2)mouse_pos - grab) + HeightMapPosAtGrab;
            HeightMapSpriteRenderer.transform.position = HeightMapOffset;

            MoveHeightmapFromDrag();
        }
    }

    IEnumerator OutputRoutineOpen(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
    }
    public void OnClickOpen_HeightMap()
    {
        ExtensionFilter[] extensions = new ExtensionFilter[1];
        extensions[0].Extensions = new string[] { "png" };
        string[] paths_ = StandaloneFileBrowser.OpenFilePanel("Select HeightMap", "", extensions, false);

        if (paths_.Length > 0)
        {
            StartCoroutine(OutputRoutineOpen(new System.Uri(paths_[0]).AbsoluteUri));
        }

        HeightMapTexture = Texture16BitLoader.LoadR16Png(paths_[0]);

        if (HeightMapTexture != null)
        {
            // Extract true 16-bit data successfully now that the texture format is natively R16
            HeightMap_RawData = HeightMapTexture.GetRawTextureData<ushort>().ToArray();

            // 4. Create a Sprite using the texture
            // Rect defines what part of the texture to use (the whole thing)
            // Vector2.one * 0.5f sets the pivot point directly in the center (0.5, 0.5)
            Sprite newSprite = Sprite.Create(
                HeightMapTexture,
                new Rect(0, 0, HeightMapTexture.width, HeightMapTexture.height),
                new Vector2(0, 0),
                1/resolution_height
            );

            if (HeightMapSpriteRenderer != null)
            {
                HeightMapSpriteRenderer.sprite = newSprite;
            }
            else
            {
                Debug.LogError("Target SpriteRenderer is missing! Please assign it in the Inspector.");
            }
        }
        Can_generate_RIFF();
    }
    public void OnClickOpen_BiomeMap()
    {
        ExtensionFilter[] extensions = new ExtensionFilter[1];
        extensions[0].Extensions = new string[] { "png" };
        string[] paths_ = StandaloneFileBrowser.OpenFilePanel("Select BiomeMap", "", extensions, false);

        if (paths_.Length > 0)
        {
            StartCoroutine(OutputRoutineOpen(new System.Uri(paths_[0]).AbsoluteUri));
        }

        byte[] fileData = File.ReadAllBytes(paths_[0]);
        if (fileData.Length != 0)
        {
            BiomeMapTexture = new Texture2D(2, 2);
            if (BiomeMapTexture.LoadImage(fileData))
            {
                // 4. Create a Sprite using the texture
                // Rect defines what part of the texture to use (the whole thing)
                // Vector2.one * 0.5f sets the pivot point directly in the center (0.5, 0.5)
                Sprite newSprite = Sprite.Create(
                    BiomeMapTexture,
                    new Rect(0, 0, BiomeMapTexture.width, BiomeMapTexture.height),
                    new Vector2(0, 0),
                    1 / resolution_biome
                );

                if (BiomeMapSpriteRenderer != null)
                {
                    BiomeMapSpriteRenderer.sprite = newSprite;
                }
                else
                {
                    Debug.LogError("Target SpriteRenderer is missing! Please assign it in the Inspector.");
                }
            }
        }
        Can_generate_RIFF();
    }
    public void OnClickOpen_Directory()
    {
        string[] paths_ = StandaloneFileBrowser.OpenFolderPanel("Select Terrain", "", false);

        Directory = paths_[0];
        Directory_label.text = Directory;
        Can_generate_RIFF();
    }

    public void Can_generate_RIFF()
    {
        if(HeightMapTexture != null && BiomeMapTexture != null && Directory != "")
        {
            RIFF_button.interactable = true;
        }
    }

    public void SetResolution_Height()
    {
        try
        {
            resolution_height = float.Parse(inputField_Resolution_Height.text);
            Sprite newSprite = Sprite.Create(
                HeightMapTexture,
                new Rect(0, 0, HeightMapTexture.width, HeightMapTexture.height),
                new Vector2(0, 0),
                1/resolution_height
            );
            HeightMapSpriteRenderer.sprite = newSprite;
        }
        catch { }
    }
    public void SetResolution_Biome()
    {
        try
        {
            resolution_biome = float.Parse(inputField_Resolution_Biome.text);
            Sprite newSprite = Sprite.Create(
                BiomeMapTexture,
                new Rect(0, 0, BiomeMapTexture.width, BiomeMapTexture.height),
                new Vector2(0, 0),
                1 / resolution_biome
            );
            BiomeMapSpriteRenderer.sprite = newSprite;
        }
        catch { }
    }
    public void SetTransparency_Biome(float value)
    {
        BiomeMapSpriteRenderer.color = new Color(1, 1, 1, value);
    }

    public void OnClickClipboardSet()
    {
        Clipboard.SetText($"({HeightMapOffset.x},{HeightMapOffset.y})");
    }
    public void OnClickClipboardPaste()
    {
        string text = Clipboard.GetText();
        string[] values_text = text.Trim(new char[] { '(', ')' }).Split(new char[] { ',' });
        float[] values = new float[values_text.Length];
        for (int i = 0; i < values_text.Length; i++)
        {
            values[i] = float.Parse(values_text[i]);
        }
        HeightMapOffset.x = values[0];
        HeightMapOffset.y = values[1];

        InputFieldXOffset.SetTextWithoutNotify(HeightMapOffset.x.ToString());
        InputFieldYOffset.SetTextWithoutNotify(HeightMapOffset.y.ToString());

        HeightMapSpriteRenderer.transform.position = HeightMapOffset;
    }

    public void MoveHeightmapFromDrag()
    {
        InputFieldXOffset.SetTextWithoutNotify(HeightMapOffset.x.ToString());
        InputFieldYOffset.SetTextWithoutNotify(HeightMapOffset.y.ToString());
    }
    public void MoveHeightmapFromField()
    {
        HeightMapOffset.x = float.Parse(InputFieldXOffset.text);
        HeightMapOffset.y = float.Parse(InputFieldYOffset.text);
        HeightMapSpriteRenderer.transform.position = HeightMapOffset;
    }

    public void HeightMapMinHeightSet()
    {
        HeightMapMinHeight = int.Parse(InputFieldMin.text);
    }
    public void HeightMapMaxHeightSet()
    {
        HeightMapMaxHeight = int.Parse(InputFieldMax.text);
    }

    //public ushort FetchSubPixel_Height(Vector2 world_pos)
    //{
    //    world_pos -= HeightMapOffset;

    //    // Ensure these dimensions perfectly match your raw byte array length!
    //    int width = HeightMapTexture.width;
    //    int height = HeightMapTexture.height;

    //    Vector2 size_texture = new Vector2(width, height) * resolution;
    //    Vector2 pos0to1 = new Vector2(world_pos.x / size_texture.x, world_pos.y / size_texture.y);

    //    if (pos0to1.x < 0 || pos0to1.x >= 1 || pos0to1.y < 0 || pos0to1.y >= 1) return 0;

    //    //return (ushort)(HeightMapTexture.GetPixelBilinear(pos0to1.x, pos0to1.y).r * ushort.MaxValue);

    //    try
    //    {
    //        // Map normalized coordinates to continuous pixel coordinates
    //        float pixelX = pos0to1.x * width;
    //        float pixelY = pos0to1.y * height;

    //        // Find the top-left corner of our 4x4 sampling window
    //        int xCenter = Mathf.FloorToInt(pixelX);
    //        int yCenter = Mathf.FloorToInt(pixelY);

    //        // Interpolation weights (fractional distance from the floor pixel)
    //        float tx = pixelX - xCenter;
    //        float ty = pixelY - yCenter;

    //        // Mitchell-Netravali weights for X and Y axes
    //        float wX0 = MitchellNetravaliWeight(tx + 1.0f);
    //        float wX1 = MitchellNetravaliWeight(tx);
    //        float wX2 = MitchellNetravaliWeight(1.0f - tx);
    //        float wX3 = MitchellNetravaliWeight(2.0f - tx);

    //        float wY0 = MitchellNetravaliWeight(ty + 1.0f);
    //        float wY1 = MitchellNetravaliWeight(ty);
    //        float wY2 = MitchellNetravaliWeight(1.0f - ty);
    //        float wY3 = MitchellNetravaliWeight(2.0f - ty);

    //        float totalHeight = 0f;

    //        for (int j = -1; j <= 2; j++)
    //        {
    //            int y = Mathf.Clamp(yCenter + j, 0, height - 1);
    //            float weightY = (j == -1) ? wY0 : (j == 0) ? wY1 : (j == 1) ? wY2 : wY3;

    //            for (int i = -1; i <= 2; i++)
    //            {
    //                int x = Mathf.Clamp(xCenter + i, 0, width - 1);
    //                float weightX = (i == -1) ? wX0 : (i == 0) ? wX1 : (i == 1) ? wX2 : wX3;

    //                // Fetch raw ushort height from your array
    //                ushort pixelHeight = FetchPixel_Height(x, y, width);

    //                totalHeight += pixelHeight * weightX * weightY;
    //            }
    //        }

    //        return (ushort)Mathf.Clamp(Mathf.RoundToInt(totalHeight), 0, ushort.MaxValue);
    //    }
    //    catch
    //    {
    //        return 0;
    //    }
    //}
    //private float MitchellNetravaliWeight(float x)
    //{
    //    x = Mathf.Abs(x);
    //    float B = 1.0f / 3.0f;
    //    float C = 1.0f / 3.0f;

    //    if (x < 1.0f)
    //    {
    //        return ((12.0f - 9.0f * B - 6.0f * C) * x * x * x +
    //                (-18.0f + 12.0f * B + 6.0f * C) * x * x +
    //                (6.0f - 2.0f * B)) / 6.0f;
    //    }
    //    else if (x < 2.0f)
    //    {
    //        return ((-B - 6.0f * C) * x * x * x +
    //                (6.0f * B + 30.0f * C) * x * x +
    //                (-12.0f * B - 48.0f * C) * x +
    //                (8.0f * B + 24.0f * C)) / 6.0f;
    //    }
    //    return 0.0f;
    //}
    //public ushort FetchPixel_Height(int x, int y, int heightMapWidth)
    //{
    //    int byteIndex = (y * heightMapWidth + x);

    //    // Rebuild 16-bit ushort value
    //    ushort rawHeight = HeightMap_RawData[byteIndex];
    //    return rawHeight;
    //}

    public Color FetchPixel_Biome(Vector2 world_pos)
    {
        world_pos -= HeightMapOffset;
        Vector2 size_texture_pix = new Vector2(BiomeMapTexture.width, BiomeMapTexture.height);
        Vector2 size_texture = size_texture_pix * resolution_biome; //m = ppx * m/ppx
        Vector2 pos0to1 = new Vector2(world_pos.x / size_texture.x, world_pos.y / size_texture.y);
        if (pos0to1.x < 0 || pos0to1.x >= 1 || pos0to1.y < 0 || pos0to1.y >= 1) return Color.black;
        try
        {
            Vector2 pos_ppx = new Vector2(pos0to1.x * size_texture_pix.x, pos0to1.y * size_texture_pix.y);

            pos_ppx = new Vector2Int(Mathf.RoundToInt(pos_ppx.x), Mathf.RoundToInt(pos_ppx.y));
            Vector2Int pos_ppx_int = new Vector2Int(Mathf.RoundToInt(pos_ppx.x), Mathf.RoundToInt(pos_ppx.y));
            Color color = BiomeMapTexture.GetPixel(pos_ppx_int.x, pos_ppx_int.y);

            return color;
        }
        catch
        {
            return Color.black;
        }
    }
}
