using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Weapon")]
public class Weapon : Item
{

    public WeaponType type;
    public Stats stats;

}
public enum WeaponType
{
    Greatsword,
    Sword,
    Dagger,
    Bow,
    Spellbook,
    Mace,
    Staff
}