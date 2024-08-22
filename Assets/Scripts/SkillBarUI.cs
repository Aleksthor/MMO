using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    List<Image> imageList = new List<Image>();
    List<GameObject> globalCooldown = new List<GameObject>();   
    List<Slider> cooldownSliderList = new List<Slider>();   

    private void Awake()
    {
        foreach(Transform child in transform)
        {
            imageList.Add(child.Find("Icon").GetComponent<Image>());
            cooldownSliderList.Add(child.Find("Cooldown").GetComponent<Slider>());
            globalCooldown.Add(child.Find("Image").gameObject);
        }
    }

    public void UpdateSkillBar(List<Skill> skillList, List<float> cooldowns)
    {
        for(int i = 0; i < skillList.Count; i++)
        {
            imageList[i].sprite = skillList[i].icon;
            imageList[i].color = skillList[i].iconColor;
            cooldownSliderList[i].value = cooldowns[i] / skillList[i].cooldown;
        }
    }
    public void Unavailable()
    {
        for (int i = 0; i < cooldownSliderList.Count; i++)
        {
            globalCooldown[i].SetActive(true);
        }
    }
    public void Available()
    {
        for (int i = 0; i < cooldownSliderList.Count; i++)
        {
            globalCooldown[i].SetActive(false);
        }
    }
}
