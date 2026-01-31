using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 状態の定義
public enum AllyState
{
    Chase,  // 敵を追跡
    Battle, // 攻撃
    Stun,   // ノックバック中
    Dizzy,  // 気絶（HP0）
    Follow, // プレイヤー追従
    Wander  // 待機/徘徊
}

public class AllyAI : MonoBehaviour
{
    private Rigidbody rb;
    private StatusManager statusManager;
    private Transform target;

    [Header("DefaultTarget")]
    [SerializeField] private string defaultTargetTag = "Enemy"; // デフォルトで敵を狙う

    public Transform CurrentTarget => target;

    // 💡 行動リスト
    private List<EnemyAction> attackActions = new List<EnemyAction>(); // 攻撃用
    private EnemyAction chaseAction; // 追跡用

    [Header("AI Settings")]
    [SerializeField] float attackRange = 7.0f; 
    [SerializeField] float followRange = 10.0f; // プレイヤーから離れすぎた場合の追従開始距離
    [SerializeField] float stopFollowRange = 3.0f; // 追従終了距離

    // ノックバック設定
    [Header("Knockback Settings")]
    [SerializeField] float knockbackPower = 10f;
    [SerializeField] float knockbackDuration = 0.5f;
    [SerializeField] float actionWaitDuration = 0.2f;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 3.5f;

    // 💡 追加: マージレベル（0: MiniMask, 1: HatMask ...）
    // スイカゲームのように、同じレベル同士を合体させたり、上位レベルを合体対象から外すのに使う
    [Header("Merge Settings")]
    public int mergeLevel = 0;

    // 現在の状態
    private AllyState currentState = AllyState.Chase;
    // 現在実行中のアクション
    private EnemyAction currentAction;

    // プレイヤーの参照（Follow用）
    private Transform playerTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        statusManager = GetComponent<StatusManager>();
        
        // プレイヤーを探しておく
        GameObject player = GameObject.FindWithTag("Player");
        if(player != null) playerTransform = player.transform;

        // ターゲットを初期設定
        SearchDefaultTarget();

        // イベント購読
        if (statusManager != null)
        {
            statusManager.OnDamageTaken += OnDamageTaken;
            statusManager.OnDead += OnDeadHandler;
        }

        // アクション取得（EnemyActionを流用）
        var allActions = GetComponents<EnemyAction>();
        foreach (var action in allActions)
        {
            if (action.actionType == ActionType.Chase)
            {
                chaseAction = action; 
            }
            else
            {
                attackActions.Add(action); 
            }
        }

        // AIループ開始
        StartCoroutine(MainStateMachine());
    }

    void OnDestroy()
    {
        if (statusManager != null)
        {
            statusManager.OnDamageTaken -= OnDamageTaken;
            statusManager.OnDead -= OnDeadHandler;
        }
    }

    // 🧠 メインステートマシン
    private IEnumerator MainStateMachine()
    {
        while (true)
        {
            // Dizzyなら何もしない
            if (currentState == AllyState.Dizzy)
            {
                yield return null;
                continue;
            }

            switch (currentState)
            {
                case AllyState.Chase:
                    yield return StartCoroutine(DoActionRoutine(chaseAction));
                    break;

                case AllyState.Battle:
                    EnemyAction selectedAction = null;
                    if (attackActions.Count > 0)
                    {
                        selectedAction = attackActions[Random.Range(0, attackActions.Count)];
                    }
                    yield return StartCoroutine(DoActionRoutine(selectedAction));
                    break;

                case AllyState.Stun:
                    yield return new WaitForSeconds(knockbackDuration);
                    currentState = AllyState.Battle;
                    break;
                
                case AllyState.Follow:
                    // 追跡アクションを使ってプレイヤーへ向かう
                    Transform originalTarget = target;
                    target = playerTransform;
                    yield return StartCoroutine(DoActionRoutine(chaseAction));
                    target = originalTarget; // 戻す
                    
                    // プレイヤーに近づいたらWander/Searchに戻る
                     if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < stopFollowRange)
                    {
                        currentState = AllyState.Wander; 
                    }
                    else if (playerTransform == null)
                    {
                        currentState = AllyState.Wander;
                    }
                    break;

                 case AllyState.Wander:
                    // 周囲を索敵
                    SearchDefaultTarget();
                    if(target != null)
                    {
                         currentState = AllyState.Chase;
                    }
                    else
                    {
                        // 暇ならプレイヤーについていく判定
                        if(playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > followRange)
                        {
                            currentState = AllyState.Follow;
                        }
                    }
                    yield return new WaitForSeconds(0.5f);
                    break;
            }

            yield return null; 
        }
    }

    private IEnumerator DoActionRoutine(EnemyAction action)
    {
        if (action != null)
        {
            currentAction = action;
            yield return StartCoroutine(action.Execute());
            currentAction = null;
            yield return new WaitForSeconds(actionWaitDuration);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        currentState = CheckNextState();
    }

    private AllyState CheckNextState()
    {
        if (currentState == AllyState.Dizzy) return AllyState.Dizzy;

        // 💡 ターゲット検証: nullチェック + タグ確認
        // 敵が死ぬと "Untagged" になるので、それを検知してターゲットから外す
        if (target != null)
        {
            if (target.CompareTag("Untagged") || target.CompareTag("Ally") || !target.gameObject.activeInHierarchy)
            {
                target = null;
            }
        }

        // ターゲットロスト確認
        if (target == null)
        {
            SearchDefaultTarget();
        }

        if (target == null)
        {
             // 敵がいない
             if(playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > followRange)
             {
                 return AllyState.Follow;
             }
             return AllyState.Wander;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange)
        {
            return AllyState.Chase;
        }
        else
        {
            return AllyState.Battle; 
        }
    }

    private void SearchDefaultTarget()
    {
        // 最も近い敵を探す
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(defaultTargetTag);
        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        Debug.Log($"AllyAI: Searching for tag '{defaultTargetTag}'. Found {enemies.Length} objects.");

        foreach (GameObject t in enemies)
        {
            float dist = Vector3.Distance(t.transform.position, currentPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t;
            }
        }

        if (nearest != null)
        {
            target = nearest.transform;
            Debug.Log($"AllyAI: Target found -> {target.name}");
        }
        else
        {
            Debug.Log("AllyAI: No target found.");
            target = null;
        }
    }

    private void OnDamageTaken(Vector3 hitPos, Transform attacker)
    {
        if (currentState == AllyState.Dizzy) return;
        if (currentState == AllyState.Stun) return;

        currentState = AllyState.Stun;
        StopAllCoroutines();
        if (currentAction != null)
        {
            currentAction.Stop();
            currentAction = null;
        }

        ApplyKnockbackForce(hitPos);

        // 反撃：攻撃者がいて、今のターゲットと違うなら切り替える
        if (attacker != null && attacker.CompareTag(defaultTargetTag))
        {
            if (target != attacker)
            {
                target = attacker;
            }
        }

        StartCoroutine(MainStateMachine());
    }

    private void ApplyKnockbackForce(Vector3 attackerPosition)
    {
        Vector3 dir = (transform.position - attackerPosition).normalized;
        dir.y = 0;
        rb.linearVelocity = Vector3.zero;
        Vector3 force = (dir * knockbackPower) + (Vector3.up * knockbackPower);
        rb.AddForce(force, ForceMode.Impulse);
    }

    // 死亡時呼び出し（StatusManagerから）
    void OnDeadHandler()
    {
        // 気絶状態へ
        Debug.Log("Ally Dizzy!");
        StopAllCoroutines();
        currentState = AllyState.Dizzy;
        
        // 物理停止
        if (rb != null)
        {
            rb.isKinematic = false; 
            rb.linearVelocity = Vector3.zero;
        }
        
        // 自身のアクションを停止
        this.enabled = false; 
    }
    
    // 蘇生時呼び出し (外部ReviveManagerから呼ぶ)
    public void Revive()
    {
        Debug.Log("Ally Revived!");
        currentState = AllyState.Wander;
        this.enabled = true;
        StartCoroutine(MainStateMachine());
    }
    
    // 現在Dizzyかどうか返す
    public bool IsDizzy()
    {
        return currentState == AllyState.Dizzy;
    }
}
