using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slider : MonoBehaviour
{
    public float value = 0f;
    private float width = 0f;
    private float height = 0f;  

    [SerializeField] private RectTransform fill;

    private void Start()
    { 
        fill = GetComponent<RectTransform>().transform.Find("Fill Area").Find("Fill").GetComponent<RectTransform>();
        if (fill == null )
        {
            Debug.Log("Null at start");
        }
        width = fill.sizeDelta.x;
        height = fill.sizeDelta.y;
    }


    private void Update()
    {
        if (fill == null)
        {
            //Debug.Log("Fill is null");
        }
        else
        {
            fill.sizeDelta = new Vector2(width * value, height);
        }
    }
}
