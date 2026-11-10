using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Popout : MonoBehaviour
{
    public Vector3 pos_in;
    public Vector3 difference;
    public bool is_out;

    // Start is called before the first frame update
    void Start()
    {
        pos_in = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Button_pressed()
    {
        is_out = !is_out;
        StartCoroutine(move());
    }

    public IEnumerator move()
    {
        yield return null;
        if (is_out) transform.localPosition = pos_in + difference;
        else transform.localPosition = pos_in;
    }
}
