using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item : ScriptableObject
{
    public string itemName;
    public Rarity rarity;
    public int itemId;
}
public enum Rarity
{
    Common,
    UnCommon,
    Rare,
    Epic,
    Legendary
}