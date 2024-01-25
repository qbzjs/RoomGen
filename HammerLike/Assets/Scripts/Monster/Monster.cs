using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Johnson;
using UnityEngine.AI;

[Serializable]
public struct MonsterStat
{
    public float maxHp;
    public float curHp;
    public float attackPoint;
    [Space(7.5f)]
    public float walkSpd;
    public float runSpd;

    [Space(7.5f)]
    public EnvasionStat envasionStat;

    [Space(7.5f)]
    public float upperHomingSpd; //상체 회전 속도
    public float legHomingSpd; //하체 회전 속도

    public float detectionRange;  // 플레이어를 인식할 범위 설정. 원하는 값으로 조절 가능.

}

[System.Serializable]
public class DropItem
{
    public int itemID; // 아이템 ID
    public float dropChance; // 드랍 확률
}


public class Monster : MonoBehaviour
{

    private Transform playerTransform;
    // Note: 기능 구현 할 때는 접근지정자 크게 신경 안쓰고 작업함.
    // 차후 기능 작업 끝나고 나면 추가적으로 정리 예정!!
    // 불편해도 양해바랍니다!!! 스마미센!!

    public MonsterStat stat;
    public Slider healthSlider;
    [Header("State Machine")]
    public MonsterFSM fsm;

    [Space(10f)]
    [Header("Default Comps")]
    public Transform meshTr;
    public Animator animCtrl;
    public Rigidbody rd;

    [Space(10f)]
    [Header("Action Table")]
    // Note: 해당 부분은 몬스터에 맞는 액션으로 수정 필요
    public MonsterMove move;
    public MonsterAtk atk;
    public MonsterAim monsterAim;

    [Space(10f)]
    [Header("Cam Controller")]
    public CamCtrl camCtrl; // Note: 몬스터가 카메라를 직접 제어할 필요가 있을지 확인 필요

    [Header("Drop Items")]
    public List<DropItem> dropItems = new List<DropItem>(); // 드랍 아이템 목록

    [Space(10f)]
    [Header("Anim Bones")]
    public Transform headBoneTr;
    public Transform spineBoneTr;
    public Transform hpBoneTr;
    private HashSet<int> processedAttacks = new HashSet<int>();

    private bool isKnockedBack = false;
    private bool canTakeKnockBackDamage = true;

    public Transform target;
    NavMeshAgent nmAgent;
    public LineRenderer lineRenderer; // LineRenderer 참조

    public Collider attackCollider;
    private Player player;

    private void Awake()
    {
        if (!fsm)
        { fsm = GetComponent<MonsterFSM>(); }

        if (!fsm)
        {
            gameObject.AddComponent<MonsterFSM>();
        }

        if (!rd)
        {
            rd = GetComponent<Rigidbody>();
        }
        stat.curHp = stat.maxHp;
    }

    void Start()
    {
        nmAgent = GetComponent<NavMeshAgent>();
        animCtrl.SetBool("IsChasing", true);
        if (healthSlider != null)
        {
            healthSlider.maxValue = stat.maxHp;
            healthSlider.value = stat.curHp;
        }

        // LineRenderer 기본 설정
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2; // 시작점과 끝점
            lineRenderer.widthMultiplier = 0.05f; // 선의 너비
        }
    }

    void Update()
    {
        if (stat.curHp > 0)
        {
            DetectPlayer();

            if (playerTransform != null)
            {
                ChasePlayer();
            }
            else
            {
                animCtrl.SetBool("IsChasing", false);
                animCtrl.SetTrigger("tIdle");
            }
        }
        else
        {
            animCtrl.SetBool("IsChasing", false);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            stat.curHp = 0;
            Die();
        }
       

    }

    void DrawDirectionLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position); // 선의 시작점: 몬스터의 위치
            lineRenderer.SetPosition(1, transform.position + transform.forward * 5f); // 선의 끝점: 몬스터가 바라보는 방향
        }
    }

    private void LateUpdate()
    {
        Vector3 lookDir = monsterAim.Aiming();
        //Funcs.LookAtSpecificBone(spineBoneTr, eGizmoDir.Forward, lookDir, Vector3.zero);
    }

    private void FixedUpdate()
    {
        if (playerTransform != null && stat.curHp > 0)
        {
            FaceTarget(); // 플레이어를 지속적으로 바라보게 하는 메서드
        }

    }

    private void OnEnable()
    {

    }
    private void OnDisable()
    {

    }

    private void OnDestroy()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WeaponCollider"))
        {
            WeaponCollider weaponCollider = other.GetComponent<WeaponCollider>();
            if (weaponCollider != null && !processedAttacks.Contains(weaponCollider.CurrentAttackId))
            {
                // 데미지 및 넉백 처리
                PlayerAtk playerAttack = other.GetComponentInParent<PlayerAtk>();
                if (playerAttack != null)
                {
                    TakeDamage(playerAttack.attackDamage);
                    ApplyKnockback(playerAttack.transform.forward);
                    processedAttacks.Add(weaponCollider.CurrentAttackId);
                }

            }
        }
        if (other.gameObject.CompareTag("KnockBackable") && isKnockedBack && canTakeKnockBackDamage)
        {
            TakeDamage(10f); // KnockBackDamage
            canTakeKnockBackDamage = false;
            StartCoroutine(KnockBackDamageCooldown());
        }
    }

    private IEnumerator KnockBackDamageCooldown()
    {
        yield return new WaitForSeconds(1f); // 넉백 데미지 쿨다운
        canTakeKnockBackDamage = true;
    }

    private void TakeDamage(float damage)
    {
        if (stat.curHp <= 0) return; // 이미 사망한 경우 데미지를 받지 않음

        if (stat.curHp > 0)  // 몬스터가 살아있을 때만 피격 처리
        {
            stat.curHp -= damage;
            if (healthSlider != null)
            {
                healthSlider.value = stat.curHp;
                ShowHealthSlider();  // 체력 UI 슬라이더 표시
            }

            if (stat.curHp <= 0)
            {
                Die();
            }
        }
    }

    private void ApplyKnockback(Vector3 direction)
    {
        float knockbackIntensity = 30f; // 넉백 강도
        direction.y = 0; // Y축 변화 제거
        GetComponent<Rigidbody>().AddForce(direction.normalized * knockbackIntensity, ForceMode.Impulse);
        isKnockedBack = true;
        StartCoroutine(KnockBackDuration());
    }

    private IEnumerator KnockBackDuration()
    {
        yield return new WaitForSeconds(1f); // 넉백 지속 시간
        isKnockedBack = false;
    }

    private void Die()
    {
        // 몬스터 사망 처리
        // 예: gameObject.SetActive(false); 또는 Destroy(gameObject);
        animCtrl.SetBool("IsChasing", false);
        animCtrl.SetTrigger("tDead");

        // NavMeshAgent 비활성화
        if (nmAgent != null && nmAgent.isActiveAndEnabled)
        {
            nmAgent.isStopped = true;
            nmAgent.enabled = false;
        }


        DropItems();
        //Destroy(gameObject);
    }
    private void ShowHealthSlider()
    {
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            StopCoroutine("HideHealthSlider");  // 이미 진행 중인 코루틴이 있다면 중단
            StartCoroutine("HideHealthSlider");  // 새 코루틴 시작
        }
    }

    private IEnumerator HideHealthSlider()
    {
        yield return new WaitForSeconds(2f);
        if (healthSlider != null && stat.curHp > 0)  // 몬스터가 살아있을 때만 슬라이더 비활성화
        {
            healthSlider.gameObject.SetActive(false);
        }
    }
    public Player Player
    {
        get { return player; }
    }
    private void DetectPlayer()
    {
        if (stat.curHp <= 0) return; // 체력이 0 이하면 감지 중지
        if (Vector3.Distance(transform.position, target.position) <= stat.detectionRange)
        {
            playerTransform = target; // 기존 로직을 유지
            player = target.GetComponent<Player>(); // target에서 Player 컴포넌트를 가져옴

            if (player != null)
            {
                monsterAim.SetTarget(target); // MonsterAim 스크립트에도 타겟 설정
            }
        }
        else
        {
            playerTransform = null;
            player = null; // Player 참조도 해제
            monsterAim.SetTarget(null); // MonsterAim 스크립트의 타겟도 해제
        }
    }


    void ChasePlayer()
    {
        if (stat.curHp <= 0) return; // 체력이 0 이하면 추격 중지
        float distanceToTarget = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToTarget <= stat.detectionRange)
        {
            if (distanceToTarget > nmAgent.stoppingDistance)
            {
                nmAgent.SetDestination(playerTransform.position);
                animCtrl.SetBool("IsChasing", true);
            }
            else
            {
                animCtrl.SetBool("IsChasing", false);
                Attack();
            }
        }
        else
        {
            animCtrl.SetBool("IsChasing", false);
            animCtrl.SetTrigger("tIdle");
        }
    }

    // 플레이어를 바라보게 하는 메서드
    private void FaceTarget()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * stat.legHomingSpd);
    }



    void Attack()
    {
        if (stat.curHp <= 0 || animCtrl.GetBool("IsAttacking")) return; // 체력이 0 이하거나 이미 공격 중이면 공격 중지
        if (!animCtrl.GetBool("IsAttacking"))
        {
            FaceTarget();
            StartAttack();
            animCtrl.SetTrigger("tAttack");
        }
    }

    void DropItems()
    {
        ItemManager itemManager = FindObjectOfType<ItemManager>();
        foreach (DropItem dropItem in dropItems)
        {
            if (UnityEngine.Random.Range(0f, 100f) < dropItem.dropChance)
            {
                // 아이템 생성 및 드랍
                itemManager.DropItem(dropItem.itemID, transform.position);
            }
        }
    }

    void StartAttack()
    {
        animCtrl.SetBool("IsAttacking", true);
    }

    public void EndAttack()
    {
        animCtrl.SetBool("IsAttacking", false);
        if (stat.curHp > 0)  // 체력이 0 이상일 때만 tIdle 트리거를 설정
        {
            animCtrl.SetTrigger("tIdle");
        }
    }

    public void EnableAttackCollider()
    {
        attackCollider.enabled = true;
    }

    // 공격용 Collider 비활성화
    public void DisableAttackCollider()
    {
        attackCollider.enabled = false;
    }




}
