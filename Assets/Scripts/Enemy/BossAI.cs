using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 💡 ボス専用AI
// HPに応じたフェーズ遷移、行動パターンの追加、ノックバック無効化を持つ
public class BossAI : MonoBehaviour
{
    private enum BossState { Chase, Battle } // Stun無し

    [Header("Settings")]
    [SerializeField] private string defaultTargetTag = "Payload";
    [SerializeField] private float searchRange = 50f; // 💡 探索範囲を拡大 (デフォルト20->50)
    [SerializeField] private float attackRange = 10f; // 💡 マジックナンバー排除
    [SerializeField] private float speedPhase1 = 3.5f;
    [SerializeField] private float speedPhase3 = 6.0f; // フェーズ3で高速化

    [Header("Actions - Phase 1 (HP 100%~)")]
    [SerializeField] private List<EnemyAction> phase1Actions = new List<EnemyAction>();

    [Header("Actions - Phase 2 Additions (HP 70%~)")]
    [SerializeField] private List<EnemyAction> phase2Actions = new List<EnemyAction>();

    [Header("Actions - Phase 3 Additions (HP 30%~)")]
    [SerializeField] private List<EnemyAction> phase3Actions = new List<EnemyAction>();

    // 内部変数
    private NavMeshAgent agent;
    private StatusManager statusManager;
    private Transform target;
    private Rigidbody rb;
    private BossState currentState = BossState.Chase;
    private EnemyAction currentAction; // 実行中のアクション

    private int currentPhase = 1;

    // 💡 外部公開用のターゲットプロパティ (EnemyAction系が参照する)
    public Transform CurrentTarget => target;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        statusManager = GetComponent<StatusManager>();
        rb = GetComponent<Rigidbody>();

        // StatusManagerの初期設定
        if (statusManager != null)
        {
            // statusManager.ApplyStats(); // 削除: Startで自動で行われるため不要
            // イベント登録
            statusManager.OnDamageTaken += OnDamageTaken;
            statusManager.OnDead += OnDead;
        }

        agent.speed = speedPhase1;
        
        // 頻繁にターゲットロストしないよう、Start時にしっかり探す
        FindTarget();
        StartCoroutine(MainStateMachine());
    }

    void OnDestroy()
    {
        if (statusManager != null)
        {
            statusManager.OnDamageTaken -= OnDamageTaken;
            statusManager.OnDead -= OnDead;
        }
    }

    // 💡 メインループ
    IEnumerator MainStateMachine()
    {
        while (true)
        {
            // フェーズチェック
            UpdatePhase();

            // ターゲット更新
            if (target == null) FindTarget();

            switch (currentState)
            {
                case BossState.Chase:
                    yield return CheckAndChase();
                    break;
                case BossState.Battle:
                    yield return PerformAction();
                    break;
            }
            yield return null;
        }
    }

    private void UpdatePhase()
    {
        if (statusManager == null) return;
        
        float hpRate = statusManager.CurrentHp / statusManager.MaxHp;

        if (hpRate <= 0.3f && currentPhase < 3)
        {
            currentPhase = 3;
            agent.speed = speedPhase3; // 移動速度上昇
            Debug.Log($"Boss Entered Phase 3! Speed: {agent.speed}");
            // 必要ならエフェクト再生など
        }
        else if (hpRate <= 0.7f && currentPhase < 2)
        {
            currentPhase = 2;
            Debug.Log("Boss Entered Phase 2!");
        }
    }

    // 💡 追跡行動
    private IEnumerator CheckAndChase()
    {
        // ターゲットがいなければ探す
        if (target == null) 
        {
            FindTarget();
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // Action実行可能か距離チェック
        float dist = Vector3.Distance(transform.position, target.position);
        
        // 攻撃範囲内ならBattleへ
        if (dist < attackRange) 
        {
            currentState = BossState.Battle;
            yield break;
        }

        // 💡 修正: NavMeshAgentがActionによって無効化されている可能性があるため、ここで強制的に有効化
        if (agent != null)
        {
            if (!agent.enabled) 
            {
                agent.enabled = true;
                // 有効化した瞬間にワープするのを防ぐ（必要なら）
                if(agent.isOnNavMesh) agent.ResetPath();
            }
            if (rb != null && !rb.isKinematic) rb.isKinematic = true; // ナビ移動中は物理無効

            // 移動
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    // 💡 攻撃行動
    private IEnumerator PerformAction()
    {
        // 行動候補リストを作成
        List<EnemyAction> availableActions = new List<EnemyAction>(phase1Actions);
        if (currentPhase >= 2) availableActions.AddRange(phase2Actions);
        if (currentPhase >= 3) availableActions.AddRange(phase3Actions);

        if (availableActions.Count == 0 || target == null)
        {
            // 行動がない、または対象がいない -> Chaseに戻る
            currentState = BossState.Chase;
            yield break;
        }

        // ランダムに選択
        currentAction = availableActions[Random.Range(0, availableActions.Count)];
        
        // 実行
        yield return currentAction.Execute();
        currentAction = null;

        // 行動終了後は少し様子見してChaseへ戻る（連続攻撃させたい場合は調整）
        yield return new WaitForSeconds(0.5f);
        currentState = BossState.Chase;
    }

    private void FindTarget()
    {
        // 1. デフォルトターゲット（Payloadなど）が指定されているなら最優先
        if (!string.IsNullOrEmpty(defaultTargetTag))
        {
            GameObject obj = GameObject.FindGameObjectWithTag(defaultTargetTag);
            if (obj != null) 
            {
                target = obj.transform;
                return;
            }
        }

        // 2. 指定（defaultTargetTag）がいなかった場合、近くの「Player」か「Ally」を探す
        // OverlapSphereで範囲内のコライダーを探す (LayerMaskが必要なら追加するが今回はLayer不問)
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRange);
        
        Transform bestTarget = null;
        float closeDst = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("Ally"))
            {
                // 生きているか確認 (StatusManagerがあれば)
                StatusManager st = hit.GetComponent<StatusManager>();
                if (st != null && st.CurrentHp > 0)
                {
                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < closeDst)
                    {
                        closeDst = d;
                        bestTarget = hit.transform;
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            target = bestTarget;
        }
    }

    // 💡 ダメージ受信時
    // 修正: シグネチャを StatusManager.OnDamageTaken (Action<Vector3, Transform>) に合わせる
    private void OnDamageTaken(Vector3 impactPos, Transform attacker)
    {
        // ヘイト管理: 攻撃してきた相手をターゲットにする
        if (attacker != null)
        {
            target = attacker;
            
            // アクション実行中でなければ即座に向き直るなどの処理をいれてもいい
            // ただし、ボスの威厳のため、アクション中は中断しない
        }

        // ノックバック処理: ボスは無効 (Stunステートに遷移しない)
    }

    private void OnDead()
    {
        // 死亡処理
        StopAllCoroutines();
        if (agent != null) agent.enabled = false;
        if (rb != null) rb.isKinematic = true;

        // VFXなどはStatusManager側やVFXDamageFeedbackがやってくれるはず
        // ボス特有の演出があればここに追加
        Destroy(gameObject, 5f);
    }
}
