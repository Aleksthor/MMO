using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stats
{
    public int attack = 0;
    public int accuracy = 0;
    public int crit = 0;
    public int armour = 0;
    public int evasion = 0;


    public int magicPower = 0;
    public int magicalAccuracy = 0;
    public int magicResist = 0;
    public int spellResist = 0;
    public int spellPenetration = 0;

    public float attackSpeed = 1f;
    public float castSpeed = 1f;

    public int attackModifier = 1;
    public int magicPowerModifier = 1;

    public void Reset()
    {
        attack = 0;
        accuracy = 0;
        crit = 0;
        armour = 0;
        evasion = 0;


        magicPower = 0;
        magicalAccuracy = 0;
        magicResist = 0;
        spellResist = 0;
        spellPenetration = 0;

        attackSpeed = 1f;
        castSpeed = 1f;

        attackModifier = 1;
        magicPowerModifier = 1;
    }
}
