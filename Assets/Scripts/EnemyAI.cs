using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private AgroMode agroMode;
    [SerializeField] private int agroRange;
    [SerializeField] private List<Skill> skillList = new List<Skill>();
    [SerializeField] private List<float> cooldowns = new List<float>();
    [SerializeField] private WanderMode wanderMode;
    [SerializeField] private List<Transform> pathList = new List<Transform>();
    [SerializeField] private float wanderRange = 3f;
    [SerializeField] private float stopTime = 10f;


    [SerializeField] private GameObject damageUI;


    //References
    private NavMeshAgent agent;
    private Vector3 position;
    private Vector3 targetPosition;
    private NetworkVariable<bool> agro = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Owner);
    private Player selectedPlayer = null;
    private LayerMask playerMask;
    private Enemy enemy;
    private Skill activeSkill = null;
    private bool attackLoading = false;

    private float skillTimer = 0f;
    private float skillTime = 0f;
    private int skillIndex = 0;
    private float timeStopped = 0f;
    private float attackSpeedTimer = 0f;
    private int pathIndex = 0;
    private NetworkVariable<float> stoppedTimer = new NetworkVariable<float>(0, writePerm: NetworkVariableWritePermission.Owner);

    private bool offHandStrike = false;

    // Start is called before the first frame update
    void Start()
    {
        enemy = GetComponent<Enemy>();
        agent = GetComponent<NavMeshAgent>();
        playerMask = LayerMask.GetMask("Player");
        position = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (agro.Value && selectedPlayer != null)
        {
            // attack player
            if (activeSkill == null)
            {
                skillIndex = Random.Range(0, skillList.Count);
                activeSkill = skillList[skillIndex];
                skillTime = activeSkill.isCasting ? activeSkill.castSpeed * enemy.stats.castSpeed : skillIndex == 0 ? enemy.weapon.stats.attackSpeed * enemy.stats.attackSpeed : 0.25f;
            }
            if (Vector3.Distance(transform.position, selectedPlayer.transform.position) > activeSkill.range && !attackLoading)
            {
                if (stoppedTimer.Value > 0f)
                {
                    agent.isStopped = true;
                    stoppedTimer.Value -= Time.deltaTime;
                }
                else
                {
                    agent.SetDestination(selectedPlayer.transform.position);
                    agent.isStopped = false;
                    attackSpeedTimer = 0f;
                    skillTimer = 0f;
                }
            }

            if (attackSpeedTimer > 0f)
            {
                attackSpeedTimer -= Time.deltaTime;
                return;
            }
            if (Vector3.Distance(transform.position, selectedPlayer.transform.position) < activeSkill.range)
            {
                if (activeSkill != null && selectedPlayer != null)
                {
                    attackLoading = true;
                }
            }


            if (attackLoading)
            {
                agent.isStopped = true;
                skillTimer += Time.deltaTime;

                // Skill is about to trigger
                if (skillTimer >= skillTime)
                {
                    // Reset cooldown
                    cooldowns[skillIndex] = activeSkill.cooldown;

                    float damage = DamageCalc(activeSkill, out bool crit);
                    selectedPlayer.DealDamage(damage, activeSkill.isPhysical);

                    attackSpeedTimer = enemy.weapon.stats.attackSpeed;
                    if (enemy.offhand != null)
                    {
                        attackSpeedTimer *= enemy.offhand.stats.attackSpeed;
                    }
                    activeSkill = null;
                    skillTimer = 0;
                    attackLoading = false;

                }


            }


            return;
        }

        switch (agroMode)
        {
            case AgroMode.Passive:
                break;
            case AgroMode.Agressive:
                Collider[] colliders = Physics.OverlapSphere(transform.position, agroRange, playerMask);

                foreach (Collider collider in colliders)
                {
                    if (collider.CompareTag("Player"))
                    {
                        selectedPlayer = collider.transform.parent.GetComponent<Player>();
                        AgroServerRpc();
                        return;
                    }
                }
                break;
        }



        switch(wanderMode)
        {
            case WanderMode.Random:
                if (targetPosition == Vector3.zero)
                {
                    Vector2 unit = Random.insideUnitCircle;

                    targetPosition = position + new Vector3(unit.x,0,unit.y) * Random.Range(1f, wanderRange);
                    agent.SetDestination(targetPosition);
                }
                if (Vector3.Distance(targetPosition,transform.position) < 0.25f || !agent.hasPath)
                {
                    timeStopped += Time.deltaTime;
                    if (timeStopped > stopTime)
                    {
                        Vector2 unit = Random.insideUnitCircle;
                        timeStopped = 0f;
                        targetPosition = position + new Vector3(unit.x, 0, unit.y) * Random.Range(1f, wanderRange);
                        agent.SetDestination(targetPosition);
                    }
                }
                break;
            case WanderMode.Path:
                if (targetPosition == Vector3.zero)
                {
                    targetPosition = pathList[pathIndex].position;
                    agent.SetDestination(targetPosition);
                    pathIndex++;
                }
                if (Vector3.Distance(targetPosition, transform.position) < 0.1f)
                {
                    timeStopped += Time.deltaTime;
                    if (timeStopped > stopTime)
                    {
                        targetPosition = pathList[pathIndex].position;
                        agent.SetDestination(targetPosition);
                        pathIndex++;
                        timeStopped = 0f;
                    }
                }
                break;
            case WanderMode.Static:
                break;
        }
    }

    public void Agro(ulong player)
    {
        if (agroMode == AgroMode.None) return;
        AgroServerRpc(player);
    }

    [Rpc(SendTo.Server)]
    private void AgroServerRpc(ulong player)
    {
        agro.Value = true;
        stoppedTimer.Value = 0;
        selectedPlayer = NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<Player>();
    }

    [Rpc(SendTo.Server)]
    private void AgroServerRpc()
    {
        agro.Value = true;
    }

    public void StopMovement(float time)
    {
        StopServerRpc(time);
    }
    [Rpc(SendTo.Server)]
    private void StopServerRpc(float time)
    {
        stoppedTimer.Value = time;
        agent.isStopped = true;
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
                int evadeDifference = (selectedPlayer.stats.evasion - (enemy.offhand.stats.accuracy + enemy.stats.accuracy)) / 10;

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
                        damage = enemy.offhand.stats.attack + enemy.stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                    }
                    else
                    {
                        damage = enemy.weapon.stats.attack + enemy.offhand.stats.attack + enemy.stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                    }

                    damage *= enemy.stats.attackModifier;

                    int critRandom = Random.Range(0, 200);
                    if (critRandom < enemy.offhand.stats.crit + enemy.stats.crit)
                    {
                        damage *= 2;
                        crit = true;
                    }
                }

                offHandStrike = false;

                damage -= selectedPlayer.stats.armour / 6;
                selectedPlayer.ApplyCC(skill.cc, skill.ccDuration, OwnerClientId);
            }
            else
            {

                int evadeRandom = Random.Range(0, 100);
                int evadeDifference = (selectedPlayer.stats.evasion - (enemy.weapon.stats.accuracy + enemy.stats.accuracy)) / 10;

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
                        damage = enemy.weapon.stats.attack + enemy.stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                    }
                    else
                    {
                        if (enemy.offhand == null)
                        {
                            damage = enemy.weapon.stats.attack + enemy.stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                        }
                        else
                        {
                            damage = enemy.weapon.stats.attack + enemy.offhand.stats.attack + enemy.stats.attack + Random.Range(skill.minPhysical, skill.maxPhysical);
                        }
                    }

                    damage *= enemy.stats.attackModifier;

                    int critRandom = Random.Range(0, 200);
                    if (critRandom < enemy.weapon.stats.crit + enemy.stats.crit)
                    {
                        damage *= 2;
                        crit = true;
                    }
                }

                if (enemy.offhand != null)
                {
                    offHandStrike = true;
                }

                damage -= selectedPlayer.stats.armour / 6;
                selectedPlayer.ApplyCC(skill.cc, skill.ccDuration, OwnerClientId);
            }
        }
        else
        {

            int magicResistRandom = Random.Range(0, 100);
            int magicResistDifference = (selectedPlayer.stats.magicResist - (enemy.weapon.stats.magicalAccuracy + enemy.stats.magicalAccuracy)) / 10;

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
                damage = skill.skillPower * ((enemy.weapon.stats.magicPower + enemy.stats.magicPower) / 12);

                float fraction = Mathf.Clamp(selectedPlayer.stats.spellResist - (enemy.weapon.stats.spellPenetration + enemy.stats.spellPenetration), 0, 100) / 100f;
                float spellResist = damage * fraction;
                damage -= spellResist;
                selectedPlayer.ApplyCC(skill.cc, skill.ccDuration, OwnerClientId);
            }

        }

        return damage;
    }

    public bool IsCasting()
    {
        if (activeSkill != null)
        {
            return activeSkill.isCasting;
        }
        return false;
    }
    public float CastingValue()
    {
        return skillTimer / skillTime;
    }

}




public enum AgroMode
{
    Passive,
    Agressive,
    None
}

public enum WanderMode
{
    Random,
    Path,
    Static
}
