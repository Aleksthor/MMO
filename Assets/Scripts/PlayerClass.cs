using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Class")]
public class PlayerClass : ScriptableObject
{
    [Header("Class Variables")]
    public string className;
    public List<int> hpPerLevel = new List<int>();

    [Header("Skills")]
    public List<Skill> starterSkills = new List<Skill>();

    [Header("StarterEquipment")]
    public Weapon starterWeapon;
    public Weapon starterOffhand;

    [Header("BaseStats")]
    public Stats stats;
}

