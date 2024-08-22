using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(menuName = "Skill")]
public class Skill : ScriptableObject
{
    public float skillPower;
    public float minPhysical;
    public float maxPhysical;
    public float skillLevel;
    public bool isPhysical;
    public bool isCasting;
    public float castSpeed;
    public float cooldown;
    public Sprite icon;
    public Color iconColor = new Color(1,1,1,1);
    public float manaCost;
    public float range;
    public bool autoAttack = false;
    public CC cc;
    public float ccDuration;
    public bool noDamage = false;
}
public enum CC
{
    None, 
    Root
}
