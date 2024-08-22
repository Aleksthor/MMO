using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    public float maxHealth;
    public NetworkVariable<float> health = new NetworkVariable<float>(0, writePerm: NetworkVariableWritePermission.Owner);
    public float maxMana;
    public float mana;
    public int xpYield;
    public GameObject selectedTag;

    public List<Item> lootPoolCommon = new List<Item>();
    public List<Item> lootPoolUncommon = new List<Item>();
    public List<Item> lootPoolRare = new List<Item>();
    public List<Item> lootPoolEpic = new List<Item>();
    public List<Item> lootPoolLegendary = new List<Item>();

    public Stats stats;
    public Weapon weapon;
    public Weapon offhand;

    public int level;
    public string name;

    private EnemyAI enemyAI;
    private bool spawned = false;

    [SerializeField] private GameObject canvas;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;

    [SerializeField] private GameObject loot;

    private MeshRenderer mesh;
    private float fadeIn = 0f;


    private void Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        mesh = GetComponentInChildren<MeshRenderer>();
    }
    public override void OnNetworkSpawn() 
    {
        base.OnNetworkSpawn();
        spawned = true;
        health.Value = maxHealth;
        mesh = GetComponentInChildren<MeshRenderer>();
        mesh.materials[0].color = new Color(mesh.material.color.r, mesh.material.color.g, mesh.material.color.b, 0f);
    }


    void Update()
    {
        if (!IsSpawned) { return; }
        if (fadeIn < 2f)
        {
            fadeIn += Time.deltaTime;
            mesh.materials[0].color = new Color(mesh.material.color.r, mesh.material.color.g, mesh.material.color.b, fadeIn / 2);
        }

        // Update UI elements
        selectedTag.SetActive(!canvas.activeSelf);
        float h = health.Value;
        hpSlider.value = h / maxHealth;
        hpText.text = health.Value.ToString();
        levelText.text = level.ToString();
        nameText.text = name;
    }


    public void CanvasOn()
    {
        canvas.SetActive(true);
    }
    public void CanvasOff()
    {
        canvas.SetActive(false);
    }

    public void DealDamage(float damage, ulong player)
    {
        enemyAI.Agro(player);
        DealDamageServerRpc(damage, player);
    }

    [Rpc(SendTo.Server)]
    private void DealDamageServerRpc(float damage, ulong player)
    {
        health.Value -= damage;
        if (health.Value <= 0)
        {
            YieldXPServerRpc(player, xpYield);
            GameObject l = Instantiate(loot,transform.position, Quaternion.identity);
            l.GetComponent<NetworkObject>().Spawn();
            l.GetComponent<Loot>().SetupLoot(lootPoolCommon);
            GetComponent<NetworkObject>().Despawn(true);
            Destroy(gameObject);
        }
    }

    public void Heal(float value)
    {
        if (!spawned) return;
        HealServerRpc(value);
    }
    [Rpc(SendTo.Server)]
    private void HealServerRpc(float damage)
    {
        health.Value += damage;
        if (health.Value > maxHealth)
        {
            health.Value = maxHealth;
        }
    }




    [Rpc(SendTo.Server)]
    private void YieldXPServerRpc(ulong player, int xp)
    {
        NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Player>().YieldXP(xp);
    }


    public void ApplyCC(CC cc, float duration, ulong player)
    {
        enemyAI.Agro(player);
        switch (cc)
        {
            case CC.None:
                break;
            case CC.Root:
                enemyAI.StopMovement(duration);
                break;
        }
    }
}
