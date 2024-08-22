using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Loot : NetworkBehaviour
{
    public List<Item> items = new List<Item>();
    public NetworkVariable<bool> playerHolding = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void SetupLoot(List<Item> items)
    {
        this.items = items;
        foreach (Item item in this.items)
        {
            LoadItemsOnClientRpc(item.itemId);
        }
    }

    [ClientRpc]
    void LoadItemsOnClientRpc(int itemId)
    {
        if (IsServer) return;
        items.Add(ItemCatalogue.instance.GetItem(itemId));
    }

    public List<Item> LookAtItems()
    {
        HoldingItemsServerRpc(true);
        return items;
    }

    public void DeleteItem(int index)
    {
        items.RemoveAt(index);
    }

    public void StopLooking()
    {
        HoldingItemsServerRpc(false);
    }


    [ServerRpc]
    private void HoldingItemsServerRpc(bool holding)
    {
        playerHolding.Value = holding;
    }

}
