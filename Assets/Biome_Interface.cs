using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Biome_Interface : MonoBehaviour
{
    public main main;
    public Chunk_manager chunk_Manager;

    // Unknown Water Bare Balast Dirt Gravel Grass Shrub Trees Wetland Crops Buit Ice
    public int[] IDs;
    public Color[] Colors;

    public GameObject[] Bomes_GameObjects;

    public Button[] Pipette_buttons;
    public Image[] Disolay_Colors;
    public Text[] Text_Hex;
    public Text[] Text_R;
    public Text[] Text_G;
    public Text[] Text_B;
    public Text[] Text_Name;
    public Text[] Text_ID;

    public GameObject Pipette_Cursor;

    public bool Pipetting;
    public int Pipetting_id;

    private void Start()
    {
        main = GetComponent<main>();
        chunk_Manager = GetComponent<Chunk_manager>();

        Pipette_buttons = new Button[IDs.Length];
        Disolay_Colors = new Image[IDs.Length];
        Text_Hex = new Text[IDs.Length];
        Text_R = new Text[IDs.Length];
        Text_G = new Text[IDs.Length];
        Text_B = new Text[IDs.Length];
        Text_Name = new Text[IDs.Length];
        Text_ID = new Text[IDs.Length];

        for (int i = 0; i < IDs.Length; i++)
        {
            Pipette_buttons[i] = Bomes_GameObjects[i].transform.Find("Button").GetComponent<Button>();
            int id = IDs[i];
            Pipette_buttons[i].onClick.AddListener(() => Pipet_Button(id));
            Disolay_Colors[i] = Bomes_GameObjects[i].transform.Find("Color").GetComponent<Image>();
            Disolay_Colors[i].color = Colors[i];
            Text_Hex[i] = Bomes_GameObjects[i].transform.Find("Color r g b").transform.Find("Text Hex").GetComponent<Text>();
            Text_Hex[i].text = ToHtmlStringRGB(Colors[i]);
            Text_R[i] = Bomes_GameObjects[i].transform.Find("Color r g b").transform.Find("Text R").GetComponent<Text>();
            Text_R[i].text = ((int)(Colors[i].r * 255f)).ToString();
            Text_G[i] = Bomes_GameObjects[i].transform.Find("Color r g b").transform.Find("Text G").GetComponent<Text>();
            Text_G[i].text = ((int)(Colors[i].g * 255f)).ToString();
            Text_B[i] = Bomes_GameObjects[i].transform.Find("Color r g b").transform.Find("Text B").GetComponent<Text>();
            Text_B[i].text = ((int)(Colors[i].b * 255f)).ToString();
            Text_Name[i] = Bomes_GameObjects[i].transform.Find("Name").transform.Find("Text").GetComponent<Text>();
            Text_Name[i].text = Bomes_GameObjects[i].name;
            Text_ID[i] = Bomes_GameObjects[i].transform.Find("ID").transform.Find("Text").GetComponent<Text>();
            Text_ID[i].text = IDs[i].ToString();
        }
    }

    private void Update()
    {
        Pipette_Cursor.SetActive(Pipetting);
        Pipette_Cursor.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if(Input.GetMouseButtonDown(0) && Pipetting && !chunk_Manager.UI_colider())
        {
            int i = Pipetting_id;
            Colors[i] = main.FetchPixel_Biome((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition));
            Disolay_Colors[i].color = Colors[i];
            Text_Hex[i].text = ToHtmlStringRGB(Colors[i]);
            Text_R[i].text = ((int)(Colors[i].r * 255f)).ToString();
            Text_G[i].text = ((int)(Colors[i].g * 255f)).ToString();
            Text_B[i].text = ((int)(Colors[i].b * 255f)).ToString();

            Pipetting = false;
            chunk_Manager.selecting_chunks = false;
        }
    }

    public string ToHtmlStringRGB(Color color)
    {
        string Hex = "";

        Hex += ((int)(color.r * 255f)).ToString("X2");
        Hex += ((int)(color.g * 255f)).ToString("X2");
        Hex += ((int)(color.b * 255f)).ToString("X2");

        return Hex;
    }

    public void Pipet_Button(int id)
    {
        for (int i = 0; i < IDs.Length; i++)
        {
            if (IDs[i] == id)
            {
                Pipetting_id = i;
                Pipetting = true;
                break;
            }
        }
    }
}
