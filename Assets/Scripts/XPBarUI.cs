using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XPBarUI : MonoBehaviour
{
    [SerializeField] List<Slider> sliders = new List<Slider>();



    public void Run(int xp, int needed)
    {
        int x = 0;
        int bar = needed / 20;
        for (int i = 0; i < sliders.Count; i++)
        {
            sliders[i].value = (float)(xp - (bar*i)) / (float)bar;
        }
    }

}
