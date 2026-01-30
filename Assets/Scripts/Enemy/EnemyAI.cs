using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 状態の定義
public enum EnemyState
{
    Chase,  // 追跡中
    Battle, // 通常行動（今はこれだけ）
    Stun    // ノックバック中
}

public class EnemyAI : MonoBehaviour
{
    private Rigidbody rb;
    private StatusManager statusManager;
    private Transform target;

    [Header("DefaultTarget")]
    [SerializeField] private string defaultTargetTag = "Payload"; // デフォルトの狙い

    // 💡 Step 13.1: 外部（ActionChaseなど）に現在のターゲットを公開するプロパティ
    public Transform CurrentTarget => target;

    // 💡 行動リスト
    private List<EnemyAction> attackActions = new List<EnemyAction>(); // 攻撃用
    private EnemyAction chaseAction; // 追跡用

    // Step7.2 追加: 設定項目
    [Header("AI Settings")]
    [SerializeField] float attackRange = 7.0f; 

    // ノックバック設定
    [Header("Knockback Settings")]
    [SerializeField] float knockbackPower = 10f;
    [SerializeField] float knockbackDuration = 0.5f;
    [SerializeField] float actionWaitDuration = 0.2f;

    // 現在の状態
    private EnemyState currentState = EnemyState.Chase;
    // 現在実行中のアクション
    private EnemyAction currentAction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        statusManager = GetComponent<StatusManager>();
        // ターゲットを初期設定
        if (target == null)
        {
            SearchDefaultTarget();
        }

        // イベント購読
        if (statusManager != null)
        {
            statusManager.OnDamageTaken += OnDamageTaken;
            statusManager.OnDead += OnDeadHandler;
        }

        // 💡 Step7.2 変更点: 全アクションを取得して、タイプごとに振り分ける
        var allActions = GetComponents<EnemyAction>();
        
        foreach (var action in allActions)
        {
            if (action.actionType == ActionType.Chase)
            {
                chaseAction = action; // 追跡用として登録
            }
            else
            {
                attackActions.Add(action); // 攻撃用リストに追加
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

    // 🧠 メインステートマシン（思考のループ）
    private IEnumerator MainStateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case EnemyState.Chase:
                    // 追跡アクションを実行
                    yield return StartCoroutine(DoActionRoutine(chaseAction));
                    break;

                case EnemyState.Battle:
                    // 攻撃リストからランダムに選んで実行
                    EnemyAction selectedAction = null;
                    if (attackActions.Count > 0)
                    {
                        selectedAction = attackActions[Random.Range(0, attackActions.Count)];
                    }
                    yield return StartCoroutine(DoActionRoutine(selectedAction));
                    break;

                case EnemyState.Stun:
                    // Stun中は操作不能なので、復帰するまでただ待つ
                    // （実際の物理挙動は OnDamageTaken で瞬間的に加えている）
                    yield return new WaitForSeconds(knockbackDuration);

                    // 復帰
                    currentState = EnemyState.Battle;
                    break;
            }

            yield return null; // 1フレーム待機
        }
    }

    private IEnumerator DoActionRoutine(EnemyAction action)
    {
        if (action != null)
        {
            // 1. 実行中のアクションとして記録（中断できるようにする）
            currentAction = action;
            
            // 2. アクションを実行し、完了するまで待機
            // （Chaseなら距離が詰まるまで、Dashなら突進が終わるまで）
            yield return StartCoroutine(action.Execute());
            
            currentAction = null;
            
            // 3. 行動後のインターバル（隙）
            yield return new WaitForSeconds(actionWaitDuration);
        }
        else
        {
            // アクションがない場合（設定ミスなど）の安全策
            yield return new WaitForSeconds(1.0f);
        }

        // 4. 行動終了後、距離を再確認して次のステートを決定
        currentState = CheckDistance();
    }

    // 距離を判定する
    private EnemyState CheckDistance()
    {
        // ターゲットが消滅していたら（Player死亡時など）
        if (target == null)
        {
            SearchDefaultTarget();
        }

        // それでも見つからないなら待機
        if (target == null) return EnemyState.Battle;

        float distance = Vector3.Distance(transform.position, target.position);
        // 攻撃範囲より遠い場合
        if (distance > attackRange)
        {
            return EnemyState.Chase; // 「まだ遠いから追いかけよう（Chase）」
        }
        // 攻撃範囲に入っている場合
        else
        {
            return EnemyState.Battle; // 「近いから攻撃しよう（Battle）」
        }
    }
    // デフォルト（Payload）を探す処理
    private void SearchDefaultTarget()
    {
        GameObject foundObject = GameObject.FindWithTag(defaultTargetTag);
        if (foundObject != null)
        {
            target = foundObject.transform;
        }
    }
    // ⚡ ダメージ検知（イベント）
    // 💡 Step 13 変更: 引数 attacker を受け取る
    private void OnDamageTaken(Vector3 hitPos, Transform attacker)
    {
        if (currentState == EnemyState.Stun) return;

        // 1. 強制的にStun状態へ移行
        // これにより、BattleRoutineの途中でも次のフレームからStun処理に移る
        currentState = EnemyState.Stun;

        // 2. 実行中のアクションを強制停止
        StopAllCoroutines(); // MainStateMachine も止まるので再起動が必要
        // 💡 実行中のアクションを停止
        if (currentAction != null)
        {
            currentAction.Stop();
            currentAction = null;
        }

        // 3. 物理ノックバック適用
        ApplyKnockbackForce(hitPos);

        // Step13 ヘイト移行: 攻撃者がいて、今のターゲットと違うなら切り替える
        if (attacker != null)
        {
            if (target != attacker)
            {
                target = attacker;
            }
        }

        // 4. ステートマシンを再起動（Stun状態から始まる）
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
    // Step10.2 死亡時の動作停止処理
    void OnDeadHandler()
    {
        // 1. 思考停止
        StopAllCoroutines();

        // 2. 物理停止
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // 3. 当たり判定消去
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 4. ターゲットから外す（★修正箇所）
        // 自分自身(this)を含む、すべての子階層のTransformを取得して回す
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            t.tag = "Untagged";
        }

        // 5. 自分をOFF
        this.enabled = false;
    }
}