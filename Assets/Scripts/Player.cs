using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Player : NetworkBehaviour
{
    // Player Main Variables
    [Header("Main Variables")]
    public NetworkVariable<float> maxHealth = new NetworkVariable<float>(0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> health = new NetworkVariable<float>(0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> maxMana = new NetworkVariable<float>(500, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> mana = new NetworkVariable<float>(0, writePerm: NetworkVariableWritePermission.Server);

    public Stats stats;
    public PlayerClass playerClass;

    // UI Elements
    [Header("UI Elements")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject UI;
    [SerializeField] private GroupUI groupUI;
    [SerializeField] private GameObject enemyCasting;
    [SerializeField] private Slider enemyCastingSlider;

    // References
    private Movement movement;

    // Equipment
    [Header("Equipment")]
    [SerializeField] private Weapon weapon;
    [SerializeField] private Weapon offhand;
    private bool offHandStrike = false;

    // Skills
    //      UI Elements
    [Header("Skills")]
    [SerializeField] private GameObject castingBar;
    [SerializeField] private Slider castingSlider;
    //      Logic Variables
    [SerializeField] private List<Skill> skillList = new List<Skill>();
    [SerializeField] private List<float> coolDowns = new List<float>();
    private Skill activeSkill;
    private float skillTimer;
    private float skillTime;
    private int skillIndex = 0;
    private float attackSpeedTimer = 0f;

    // Level
    [Header("Levels")]
    [SerializeField] List<int>  levelupValues = new List<int>();
    [SerializeField] private XPBarUI xpBarUI;
    public NetworkVariable<int> level = new NetworkVariable<int>(1, writePerm:NetworkVariableWritePermission.Server);
    private NetworkVariable<int> xp = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);


    // Selected Enemy
    //      UI Elements
    [Header("Selected Enemy")]
    [SerializeField] private GameObject selectedEnemyParent;
    private SelectedEnemyUI selectedEnemyUI;
    [SerializeField] private GameObject damageUI;
    //      Logic Variables
    private Enemy selectedEnemy;
    public float enemyRange = 50;
    private List<Enemy> enemyList = new List<Enemy>();
    private int enemyIndex = 0;
    private float enemyIndexResetTimer = 0f;
    private NavMeshAgent agent;
    private bool waitingOnRange = false;


    //Skillbar
    [Header("Skillbar")]
    [SerializeField] private SkillBarUI skillBarUI;

    // Start is called before the first frame update
    void Awake()
    {
        selectedEnemyUI = selectedEnemyParent.GetComponent<SelectedEnemyUI>();
        agent = GetComponent<NavMeshAgent>();  
        movement = GetComponent<Movement>();
        agent.speed = movement.movementSpeed;
    }

    private void Start()
    {
        groupUI.SetPlayer(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            movement.enabled = false;
            UI.SetActive(false);
            groupUI.gameObject.SetActive(false);
            return;
        }
        transform.position = GameObject.Find("Spawn").transform.position;
        groupUI.SetPlayer(this);
        groupUI.transform.parent = null;
        groupUI.transform.localPosition = Vector3.zero;

        skillList = new List<Skill>();
        coolDowns = new List<float>();
        for (int i = 0; i < playerClass.starterSkills.Count; i++)
        {
            skillList.Add(playerClass.starterSkills[i]);
            coolDowns.Add(0);
        }
        weapon = playerClass.starterWeapon;
        offhand = playerClass.starterOffhand;   

        MaxHealthServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void MaxHealthServerRpc()
    {
        maxMana.Value = 500;
        maxHealth.Value = playerClass.hpPerLevel[level.Value - 1];
        health.Value = maxHealth.Value;
        mana.Value = maxMana.Value;
        Debug.Log(mana.Value);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        stats.Reset();
        // Calculate Stats
        CalculateStats();

        // Try To Get Selected Target
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TryGetSelectedTarget();
        }

        // Update UI Elements
        float h = (float)health.Value;
        float mh = (float)maxHealth.Value;
        hpSlider.value = Mathf.Clamp(h / mh,0f,1f);
        float m = (float)mana.Value;
        float mm = (float)maxMana.Value;
        manaSlider.value = Mathf.Clamp(m / mm,0f,1f);
        int l = level.Value;
        hpText.text = h.ToString("F1");
        manaText.text = m.ToString("F1");
        levelText.text = l.ToString();


        // Check if Selected Enemy is too far away
        float distanceToTarget = 0;
        if (selectedEnemy != null)
        {
            distanceToTarget = Vector3.Distance(transform.position, selectedEnemy.transform.position);
            if (distanceToTarget > enemyRange)
            {
                selectedEnemy.CanvasOn();
                selectedEnemy = null;
            }
        }


        // Update Selected Enemy UI
        if (selectedEnemy == null)
        {
            selectedEnemyParent.SetActive(false);
        }
        else
        {
            selectedEnemyParent.SetActive(true);
            selectedEnemyUI.SetUIElements(selectedEnemy, Mathf.FloorToInt(distanceToTarget));
            float val = selectedEnemy.GetComponent<EnemyAI>().CastingValue();
            enemyCasting.SetActive(selectedEnemy.GetComponent<EnemyAI>().IsCasting() && val > 0f);
            enemyCastingSlider.value = val;
        }


        // Reset Enemy Index
        if (enemyIndexResetTimer > 0f)
        {
            enemyIndexResetTimer -= Time.deltaTime;
            if (enemyIndexResetTimer < 0f)
            {
                enemyIndex = 0;
            }
        }

        xpBarUI.Run(xp.Value, levelupValues[level.Value - 1]);

        SkillDemo(distanceToTarget);
        skillBarUI.UpdateSkillBar(skillList, coolDowns);
    }

    private void TryGetSelectedTarget()
    {
        for(int i = enemyList.Count - 1;  i >= 0; i--)
        {
            if (enemyList[i] == null)
            {
                enemyList.RemoveAt(i);
            }
        }

        if (enemyList.Count == 0)
        {
            if (selectedEnemy != null)
            {
                selectedEnemy.CanvasOn();
            }

            selectedEnemy = null;
            return;
        }

        if (enemyList.Count <= enemyIndex)
        {
            enemyIndex = 0;
            if (selectedEnemy != null)
            {
                selectedEnemy.CanvasOn();
            }
            selectedEnemy = null;
            return;
        }
        if (selectedEnemy != null)
        {
            selectedEnemy.CanvasOn();
        }
        selectedEnemy = enemyList[enemyIndex];
        selectedEnemy.CanvasOff();
        enemyIndex++;
        enemyIndexResetTimer = 30f;
        attackSpeedTimer = 0;
        skillTimer = 0f;
    }



    private void SkillDemo(float distanceToTarget)
    {
        // Always Run Down Cooldowns
        for (int i = 0; i < coolDowns.Count; i++)
        {
            if (coolDowns[i] > 0f)
            {
                coolDowns[i] -= Time.deltaTime;
            }
        }   
        
        // Reset Attack If Timer is Running
        if (attackSpeedTimer > 0f)
        {
            attackSpeedTimer -= Time.deltaTime;
            skillBarUI.Unavailable();
            return;
        }
        else
        {
            skillBarUI.Available();
        }


        // Return if no enemy
        if (selectedEnemy == null)
        {
            activeSkill = null;
            skillTimer = 0f;
        }

        // If we're charging a skill, run timer (attack speed / cast speed)
        if (activeSkill != null)
        {
            skillTimer += Time.deltaTime;
        }

        if (movement.Moving())
        {
            agent.isStopped = true;
        }

        // If were moving, cancel
        if (movement.Moving() && activeSkill != null)
        {
            if (activeSkill.isCasting)
            {
                agent.isStopped = true;
                activeSkill = null;
                castingSlider.value = 0;
                castingBar.SetActive(false);
                skillTimer = 0;
            }
            else if (distanceToTarget > activeSkill.range)
            {
                agent.isStopped = true;
                activeSkill = null;
                castingSlider.value = 0;
                castingBar.SetActive(false);
                skillTimer = 0;
            }

        }

        // Get Inputs
        // If skills are ready, or we're charging an autoattack
        bool input = false;
        if (Input.GetKey(KeyCode.Alpha1))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 0;
                input = true;
            }          
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 1;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 1;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha3) )
        {
            if (skillTimer == 0f)
            {
                skillIndex = 2;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 2;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha4) )
        {
            if (skillTimer == 0f)
            {
                skillIndex = 3;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 3;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha5))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 4;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 4;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha6))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 5;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 5;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha7))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 6;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 6;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha8))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 7;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 7;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha7))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 8;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 8;
                    input = true;
                }
            }
        }
        if (Input.GetKey(KeyCode.Alpha8))
        {
            if (skillTimer == 0f)
            {
                skillIndex = 9;
                input = true;
            }
            else
            {
                if (activeSkill.autoAttack)
                {
                    skillIndex = 9;
                    input = true;
                }
            }
        }


        // When we have an input, We're either charging an auto attack or ready to cast anything
        if (input)
        {
            // Here is where we check for autoattacking
            if (activeSkill != null)
            {
                if (activeSkill.autoAttack && skillTimer >= skillTime * 0.6f && skillIndex != 0)
                {
                    StartCoroutine(Weaving(skillTime - skillTimer, activeSkill));
                }
            }


            if (skillList.Count >= skillIndex)
            {
                if (distanceToTarget >= skillList[skillIndex].range && coolDowns[skillIndex] <= 0f && mana.Value > skillList[skillIndex].manaCost)
                {
                    agent.SetDestination(selectedEnemy.transform.position);
                    agent.isStopped = false;
                    waitingOnRange = true;
                }
                else if (coolDowns[skillIndex] <= 0f && mana.Value > skillList[skillIndex].manaCost)
                {
                    activeSkill = skillList[skillIndex];
                    skillTime = activeSkill.isCasting ? activeSkill.castSpeed * stats.castSpeed : skillIndex == 0 ? weapon.stats.attackSpeed * stats.attackSpeed : 0.25f;
                    skillTimer = 0;
                    waitingOnRange = false;
                }
            }
        }

        if (waitingOnRange)
        {
            if (selectedEnemy == null)
            {
                agent.isStopped = true;
                waitingOnRange = false;
            }
            else
            {
                transform.forward = (selectedEnemy.transform.position - transform.position).normalized;
                if (movement.Moving())
                {
                    agent.isStopped = true;
                    waitingOnRange = false;
                }
                if (distanceToTarget < skillList[skillIndex].range)
                {
                    activeSkill = skillList[skillIndex];
                    skillTime = activeSkill.isCasting ? activeSkill.castSpeed * stats.castSpeed : skillIndex == 0 ? weapon.stats.attackSpeed * stats.attackSpeed : 0.25f;
                    skillTimer = 0;
                    waitingOnRange = false;
                    agent.isStopped = true;
                }
            }

        }



        if (activeSkill != null)
        {
            if (activeSkill.isCasting)
            {
                castingBar.SetActive(true);
                castingSlider.value = skillTimer / skillTime;
            }
                

            // Skill is about to trigger
            if (skillTimer >= skillTime)
            {
                // Reset cooldown
                coolDowns[skillIndex] = activeSkill.cooldown;

                float damage = DamageCalc(activeSkill, out bool crit);

                castingSlider.value = 0;
                castingBar.SetActive(false);
                skillTimer = 0;
                UseManaServerRpc(activeSkill.manaCost);
                attackSpeedTimer = weapon.stats.attackSpeed;
                if (offhand != null)
                {
                    attackSpeedTimer *= offhand.stats.attackSpeed;
                }
                if (!activeSkill.noDamage)
                {
                    Vector3 offset = selectedEnemy.transform.right * Random.Range(1f, -1f);
                    GameObject damageCounter = Instantiate(damageUI, selectedEnemy.transform.position + new Vector3(0f, 4.5f, 0f) + offset, Quaternion.identity);
                    damageCounter.GetComponent<DamageUI>().Setup(Mathf.FloorToInt(damage), activeSkill.isPhysical, crit, true);
                    selectedEnemy.DealDamage(damage, OwnerClientId);
                }
                if (!activeSkill.autoAttack)
                {
                    activeSkill = null;
                }
            }
        }

    }

    private void CalculateStats()
    {
        stats.attack = playerClass.stats.attack * level.Value;
        stats.accuracy = playerClass.stats.accuracy;
        stats.crit = playerClass.stats.crit; 
        stats.armour = playerClass.stats.armour;
        stats.evasion = playerClass.stats.evasion;

        stats.magicPower = playerClass.stats.magicPower* level.Value;
        stats.magicalAccuracy = playerClass.stats.magicalAccuracy;
        stats.magicResist = playerClass.stats.magicResist;
        stats.spellResist = playerClass.stats.spellResist;
        stats.spellPenetration = playerClass.stats.spellPenetration;

        stats.attackSpeed = 1f;
        stats.castSpeed = 1f;

        stats.attackModifier = 1 * playerClass.stats.attackModifier;
        stats.magicPowerModifier = 1 * playerClass.stats.magicPowerModifier;
    }
    IEnumerator Weaving(float timeLeft, Skill skill)
    {
        yield return new WaitForSeconds(timeLeft);

        float damage = DamageCalc(skill, out bool crit);

        if (selectedEnemy != null)
        {
            Vector3 offset = selectedEnemy.transform.right * Random.Range(1f, -1f);
            GameObject damageCounter = Instantiate(damageUI, selectedEnemy.transform.position + new Vector3(0f, 4.5f, 0f) + offset, Quaternion.identity);
            damageCounter.GetComponent<DamageUI>().Setup(Mathf.FloorToInt(damage), activeSkill.isPhysical, crit, true);
            selectedEnemy.DealDamage(damage, OwnerClientId);
        }

        yield return null;
    }

    public float DamageCalc(Skill skill, out bool crit)
    {
        // Calc Damage
        float damage = 0;
        crit = false;
        if (skill.isPhysical)
        {
            if (offHandStrike)
            {
                int evadeRandom = Random.Range(0, 100);
                int evadeDifference = (selectedEnemy.stats.evasion - (offhand.stats.accuracy + stats.accuracy)) / 10;

                if (evadeDifference < 0)
                {
                    evadeDifference = 100;
                }
                else if (evadeDifference > 0)
                {
                    evadeDifference = 100 - evadeDifference;
                }


                if (evadeRandom < evadeDifference)
                {
                    if (skill.autoAttack)
                    {
                        damage = offhand.stats.attack + stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                    }
                    else
                    {
                        damage = weapon.stats.attack + offhand.stats.attack + stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                    }

                    damage *= stats.attackModifier;

                    int critRandom = Random.Range(0, 200);
                    if (critRandom < offhand.stats.crit + stats.crit)
                    {
                        damage *= 2;
                        crit = true;
                    }
                }

                offHandStrike = false;

                damage -= selectedEnemy.stats.armour / 6;
                selectedEnemy.ApplyCC(skill.cc, skill.ccDuration, OwnerClientId);
            }
            else
            {

                int evadeRandom = Random.Range(0, 100);
                int evadeDifference = (selectedEnemy.stats.evasion - (weapon.stats.accuracy + stats.accuracy)) / 10;

                if (evadeDifference < 0)
                {
                    evadeDifference = 100;
                }
                else if (evadeDifference > 0)
                {
                    evadeDifference = 100 - evadeDifference;
                }


                if (evadeRandom < evadeDifference)
                {
                    if (skill.autoAttack)
                    {
                        damage = weapon.stats.attack + stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                    }
                    else
                    {
                        if (offhand == null)
                        {
                            damage = weapon.stats.attack + stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                        }
                        else
                        {
                            damage = weapon.stats.attack + offhand.stats.attack + stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                        }
                    }

                    damage *= stats.attackModifier;

                    int critRandom = Random.Range(0, 200);
                    if (critRandom < weapon.stats.crit + stats.crit)
                    {
                        damage *= 2;
                        crit = true;
                    }
                }

                if (offhand != null)
                {
                    offHandStrike = true;
                }

                damage -= selectedEnemy.stats.armour / 6;
                selectedEnemy.ApplyCC(skill.cc, skill.ccDuration, OwnerClientId);
            }
        }
        else
        {

            int magicResistRandom = Random.Range(0, 100);
            int magicResistDifference = (selectedEnemy.stats.magicResist - (weapon.stats.magicalAccuracy + stats.magicalAccuracy)) / 10;

            if (magicResistDifference < 0)
            {
                magicResistDifference = 100;
            }
            else if (magicResistDifference > 0)
            {
                magicResistDifference = 100 - magicResistDifference;
            }

            if (magicResistRandom < magicResistDifference)
            {
                damage = skill.skillPower * ((weapon.stats.magicPower + stats.magicPower) / 12);

                float fraction = Mathf.Clamp(selectedEnemy.stats.spellResist - (weapon.stats.spellPenetration + stats.spellPenetration), 0, 100) / 100f;
                float spellResist = damage * fraction;
                damage -= spellResist;
                selectedEnemy.ApplyCC(skill.cc, skill.ccDuration, OwnerClientId);
            }

        }

        return damage;
    }
    public void ApplyCC(CC cc, float duration, ulong player)
    {
        switch (cc)
        {
            case CC.None:
                break;
            case CC.Root:
                movement.StopMovement(duration);
                break;
        }
    }




    public void DealDamage(float damage, bool physical)
    {
        Vector3 offset = transform.right * Random.Range(1f, -1f);
        GameObject damageCounter = Instantiate(damageUI, transform.position + new Vector3(0f, 4.5f, 0f) + offset, Quaternion.identity);
        damageCounter.GetComponent<DamageUI>().Setup(Mathf.FloorToInt(damage), physical, false, false);

        DealDamageServerRpc(damage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DealDamageServerRpc(float damage)
    {
        health.Value -= damage;
    }

    [ServerRpc(RequireOwnership = false)]
    private void UseManaServerRpc(float m)
    {
        mana.Value -= m;
    }

    public void YieldXP(int value)
    {
        YieldXPServerRpc(value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void YieldXPServerRpc(int value)
    {
        xp.Value += value;
        if (xp.Value > levelupValues[level.Value - 1])
        {
            xp.Value -= levelupValues[level.Value - 1];
            level.Value++;
            maxHealth.Value = playerClass.hpPerLevel[level.Value - 1];
            health.Value = maxHealth.Value;
            mana.Value = maxMana.Value;
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (!enemyList.Contains(enemy))
                {
                    enemyList.Add(enemy);
                    Debug.Log("Added");
                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (enemyList.Contains(enemy))
                    enemyList.Remove(enemy);
            }
        }
    }
}


