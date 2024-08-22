using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCatalogue : MonoBehaviour
{
    public static ItemCatalogue instance;
    [SerializeField] private List<Item> items;

    private void Awake()
    {
        instance = this;
    }

    public Item GetItem(int itemId)
    {
        foreach (var item in items)
        {
            if (item.itemId == itemId) return item;
        }
        return null;
    }
}
