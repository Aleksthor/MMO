using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TargetDummy : NetworkBehaviour
{
    private Enemy enemy;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }
        enemy.Heal(20);    
    }



}
