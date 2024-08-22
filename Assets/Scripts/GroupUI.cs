using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GroupUI : MonoBehaviour
{
    [SerializeField] private List<GameObject> groupBars = new List<GameObject>();
    [SerializeField] private List<Slider> healthSliders = new List<Slider>();
    [SerializeField] private List<Slider> manaSliders = new List<Slider>();
    [SerializeField] private List<TextMeshProUGUI> healthTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> manaTexts = new List<TextMeshProUGUI>();
    [SerializeField] private List<TextMeshProUGUI> levelTexts = new List<TextMeshProUGUI>();

    private Player myPlayer;


    // Update is called once per frame
    void Update()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Player");
        int i = 0;
        foreach (GameObject gameObject in gameObjects)
        {

            if (gameObject.GetComponent<Player>() != null)
            {
                Player player = gameObject.GetComponent<Player>();

                if (player != myPlayer)
                {
                    groupBars[i].SetActive(true);
                    float h = (float)player.health.Value;
                    float mh = (float)player.maxHealth.Value;
                    healthSliders[i].value = Mathf.Clamp(h / mh,0f,1f);
                    float m = (float)player.mana.Value;
                    float mm = (float)player.maxMana.Value;
                    manaSliders[i].value = Mathf.Clamp(m / mm,0f,1f);
                    int l = player.level.Value;
                    healthTexts[i].text = h.ToString("F1");
                    manaTexts[i].text = m.ToString("F1");
                    levelTexts[i].text = l.ToString();
                    i++;
                }
            }
        }
    }

    public void SetPlayer(Player p)
    {
        myPlayer = p;
    }
}
