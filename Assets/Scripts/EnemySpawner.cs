using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] GameObject enemyToSpawn;
    private GameObject spawnedEnemy;
    private bool isSpawned = false;

    private float deadTimer = 0f;
    public float respawnTimer = 300f;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        isSpawned = true;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (spawnedEnemy == null)
        {
            deadTimer += Time.deltaTime;
            if (deadTimer > respawnTimer)
            {
                deadTimer = 0f;
                isSpawned=true;
            }
            else
            {
                isSpawned = false;
            }
        }
    }
    [ServerRpc]
    public void SpawnEnemyServerRpc()
    {
        if (spawnedEnemy == null && isSpawned)
        {
            spawnedEnemy = Instantiate(enemyToSpawn, transform);
            spawnedEnemy.GetComponent<NetworkObject>().Spawn();
        }
    }

    [ServerRpc]
    public void DespawnEnemyServerRpc()
    {
        if (spawnedEnemy != null)
        {
            spawnedEnemy.GetComponent<NetworkObject>().Despawn();
            Destroy(spawnedEnemy);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
