using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixCanvasLocation : MonoBehaviour
{
    private void Start()
    {
        transform.position = Vector3.zero;
        transform.parent = null; 
    }
}
