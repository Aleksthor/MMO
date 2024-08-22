using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectedEnemyUI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI level;
    [SerializeField] private TextMeshProUGUI enemyName;
    [SerializeField] private TextMeshProUGUI hp;
    [SerializeField] private TextMeshProUGUI rangeText;


    public bool SetUIElements(Enemy enemy, float range)
    {
        if (enemy == null) return false;
        hpSlider.value = enemy.health.Value / enemy.maxHealth;
        level.text = enemy.level.ToString();
        enemyName.text = enemy.name.ToString();
        hp.text = enemy.health.Value.ToString("F1");
        rangeText.text = range.ToString() + "m";
        return true;
    }
}
