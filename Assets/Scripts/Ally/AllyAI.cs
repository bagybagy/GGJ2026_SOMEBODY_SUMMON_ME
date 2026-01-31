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

    // 💡 追加: 追従アクション
    private AllyActionFollow followAction;
    private bool isGathering = false; // 集合命令中かフラグ

    // 💡 アニメーター参照
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        statusManager = GetComponent<StatusManager>();
        animator = GetComponentInChildren<Animator>();
        
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

        // ... (省略: アクション取得) ...
        var allActions = GetComponents<EnemyAction>();
        
        foreach (var action in allActions)
        {
             // ... (省略) ...
            if (!action.enabled) continue;

            if (action is AllyActionFollow)
            {
                followAction = (AllyActionFollow)action;
                continue;
            }

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

    // 💡 追加: 外部からの集合命令
    public void ForceGather()
    {
        if (currentState == AllyState.Dizzy) return;
        
        Debug.Log("Ally Gather Command Received!");
        isGathering = true;
        // 現在のアクションを中断して集合へ
        StopAllCoroutines();
        if (currentAction != null) currentAction.Stop();
        
        currentState = AllyState.Follow;
        target = null; // ターゲット破棄
        
        StartCoroutine(MainStateMachine());
    }

    public void StopGather()
    {
        // 命令解除
        isGathering = false;
    }

    private AllyActionFollow GetFollowAction()
    {
        if(followAction == null) followAction = GetComponent<AllyActionFollow>();
        return followAction;
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
                    // 集合命令が出たら中断してFollowへ
                    if (isGathering) 
                    {
                        currentState = AllyState.Follow;
                        break;
                    }
                    yield return StartCoroutine(DoActionRoutine(chaseAction));
                    break;

                case AllyState.Battle:
                    if (isGathering) 
                    {
                        currentState = AllyState.Follow;
                        break;
                    }
                    EnemyAction selectedAction = null;
                    if (attackActions.Count > 0)
                    {
                        selectedAction = attackActions[Random.Range(0, attackActions.Count)];
                    }
                    yield return StartCoroutine(DoActionRoutine(selectedAction));
                    break;

                case AllyState.Stun:
                    yield return new WaitForSeconds(knockbackDuration);
                    currentState = CheckNextState(); // 復帰判断
                    break;
                
                case AllyState.Follow:
                    // 追従アクション実行
                    EnemyAction act = GetFollowAction();
                    if (act != null)
                    {
                         yield return StartCoroutine(DoActionRoutine(act));
                    }
                    else
                    {
                        // なければ仕方ないので待機
                        yield return new WaitForSeconds(0.5f);
                    }
                    
                    // アクション終了後の判断

                    // 1. 敵がいれば戦う（集合命令中でも自衛はする、あるいは命令優先ならここを変える）
                    // 今回は「敵がいたら戦う」を優先し、戦い終わったらまた集合する挙動にする
                    SearchDefaultTarget();
                    if (target != null)
                    {
                        // 敵発見 -> 集合は一時中断扱い（フラグは維持してもいいが、Stateを変える）
                        currentState = AllyState.Chase;
                        // 戦闘に入ったら集合命令を解除するか？ -> 今回は「解除する」
                        isGathering = false;
                    }
                    else
                    {
                        // 敵がいない
                        if (isGathering)
                        {
                            // まだ命令中ならFollow継続
                            currentState = AllyState.Follow;
                        }
                        else
                        {
                            // 自律モード
                            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < stopFollowRange)
                            {
                                currentState = AllyState.Wander;
                            }
                        }
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
                        // 集合命令が出ている、または離れすぎている
                        if (isGathering)
                        {
                            currentState = AllyState.Follow;
                        }
                        else if(playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > followRange)
                        {
                            currentState = AllyState.Follow;
                        }
                    }
                    yield return new WaitForSeconds(0.1f);
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
        
        // アニメーション: Knockout Trigger
        if (animator != null) animator.SetTrigger("Knockout");

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
        
        // アニメーション: Revive Trigger
        if (animator != null) animator.SetTrigger("Revive");

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
