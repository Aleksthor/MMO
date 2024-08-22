using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DamageUI : MonoBehaviour
{
    private TextMeshProUGUI text;
    private float lifeTime = 0;

    public void Setup(int damage, bool physical, bool crit, bool player)
    {
        if (damage == 0 && physical)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            text.text = "EVADE";
        }
        else if (damage == 0 && !physical)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            text.text = "RESIST";
        }
        else
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            text.text = damage.ToString();
        }

        if (player)
        {
            if (crit)
            {
                text.color = Color.magenta;
            }
            else
            {
                text.color = Color.yellow;
            }
        }
        else
        {
            text.color = Color.red;
        }

    }


    private void Update()
    {
        lifeTime += Time.deltaTime;
        if(lifeTime > 2f)
        {
            Destroy(gameObject);
        }
    }
}
