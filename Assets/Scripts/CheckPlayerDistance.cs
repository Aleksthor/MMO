using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckPlayerDistance : NetworkBehaviour
{
    private EnemySpawner spawner;

    private void Awake()
    {
        spawner = GetComponent<EnemySpawner>();
    }
    private void Update()
    {
        if (!IsServer) return;

        bool close = false;
        foreach (var player in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (Vector3.Distance(transform.position, player.PlayerObject.transform.position) < 100)
            {
                close = true;
                break;
            }
        }

        if (close)
        {
            spawner.SpawnEnemyServerRpc();
        }
        else
        {
            spawner.DespawnEnemyServerRpc();
        }
    }

}