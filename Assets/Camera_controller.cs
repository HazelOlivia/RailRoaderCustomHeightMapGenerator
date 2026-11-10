using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_controller : MonoBehaviour
{
    Camera RenderCamera;
    //public Camera TerrainCamera;
    //public Material TerrainShader;
    public float wheel_distance = 1;
    public Transform Focus;
    public Vector3 grab;
    public (Vector2 min, Vector2 max) bounds = (Vector2.one * -1024, Vector2.one * 1024);

    private void Start()
    {
        RenderCamera = gameObject.GetComponent<Camera>();

        float size = get_screen_size(wheel_distance);
        Camera.main.orthographicSize = size;
        RenderCamera.orthographicSize = size;
    }
    public float get_screen_size(float value)
    {
        return Mathf.Pow(2.71828f, value);
    }

    void Update()
    {
        float delta_weel = -Input.mouseScrollDelta.y;
        if (delta_weel != 0 /*|| Input.GetMouseButton(1)*/)
        {
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl)) wheel_distance += delta_weel / 8;
            //wheel_distance = Mathf.Clamp(wheel_distance, 1, 6);
            float size = get_screen_size(wheel_distance);
            Vector3 mouse_pos_before = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Camera.main.orthographicSize = size;
            RenderCamera.orthographicSize = size;
            Vector3 mouse_pos_after = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            /*if (Input.GetMouseButtonDown(1)) grab = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Input.GetMouseButton(1)) transform.position += (Vector3)(grab - mouse_pos_after);
            else*/ transform.position += (Vector3)(mouse_pos_before - mouse_pos_after);
        }
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) move += Vector3.left;
        if (Input.GetKey(KeyCode.S)) move += Vector3.down;
        if (Input.GetKey(KeyCode.W)) move += Vector3.up;
        if (Input.GetKey(KeyCode.D)) move += Vector3.right;
        if (move != Vector3.zero)
        {
            Focus = null;
            if (move.x != 0 && move.y != 0) move *= 0.70710678118f;
            transform.position += move * Time.deltaTime * Camera.main.orthographicSize * 4;
        }
        else if(Focus)
        {
            transform.position = Focus.position + Vector3.up * 30;
        }

        if (move != Vector3.zero || delta_weel != 0)
        {
            //general.grid.transform.position = general.snap_to_grid_05of(Camera.main.transform.position) - (new Vector3(1, 0, 1) * general.grid_size);
            //general.grid.transform.position += Vector3.up * (10 - general.grid.transform.position.y);
        }

        //if (bounds.min.x > Camera.main.ScreenToWorldPoint(Vector2.zero).x) Camera.main.transform.position += new Vector3(bounds.min.x - Camera.main.ScreenToWorldPoint(Vector2.zero).x, 0);
        //if (bounds.min.y > Camera.main.ScreenToWorldPoint(Vector2.zero).y) Camera.main.transform.position += new Vector3(0, bounds.min.y - Camera.main.ScreenToWorldPoint(Vector2.zero).y);
        //if (bounds.max.x < Camera.main.ScreenToWorldPoint(new Vector2(Camera.main.scaledPixelWidth, Camera.main.scaledPixelHeight)).x) Camera.main.transform.position += new Vector3(bounds.max.x - Camera.main.ScreenToWorldPoint(new Vector2(Camera.main.scaledPixelWidth, Camera.main.scaledPixelHeight)).x, 0);
        //if (bounds.max.y < Camera.main.ScreenToWorldPoint(new Vector2(Camera.main.scaledPixelWidth, Camera.main.scaledPixelHeight)).y) Camera.main.transform.position += new Vector3(0, bounds.max.y - Camera.main.ScreenToWorldPoint(new Vector2(Camera.main.scaledPixelWidth, Camera.main.scaledPixelHeight)).y);
    }
}
