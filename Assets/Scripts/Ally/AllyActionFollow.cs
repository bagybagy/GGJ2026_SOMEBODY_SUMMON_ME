using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AllyActionFollow : EnemyAction
{
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform playerTransform;

    [Header("Follow Settings")]
    [SerializeField] float followUpdateInterval = 0.5f; // 負荷軽減のため更新頻度を下げる
    [SerializeField] float stopDistance = 3.0f; // プレイヤーの周りで止まる距離

    private Coroutine followRoutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        actionType = ActionType.Chase; // 基本は移動タイプだが、AllyAI側で型判定してリストから除外する
        
        // プレイヤーを探す（AllyAIが持っているものを使うのが理想だが、簡易的にタグ検索）
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (agent == null)
        {
            // なければ追加（EnemyAIのPrefab流用の場合ついてないかも）
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        // 初期化時は無効
        if(agent != null) agent.enabled = false;
    }

    public override IEnumerator Execute()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
            if (playerTransform == null) yield break; // プレイヤーいないなら何もしない
        }

        // 1. 物理演算を一時停止
        if (rb != null) rb.isKinematic = true;
        
        // 2. NavMeshAgent有効化
        if (agent != null)
        {
            agent.enabled = true;
            agent.stoppingDistance = stopDistance;
        }

        // 3. 追従ループ開始
        followRoutine = StartCoroutine(FollowSequence());
        yield return followRoutine;
    }

    public override void Stop()
    {
        if (followRoutine != null) StopCoroutine(followRoutine);
        StopAgent();
    }

    private void StopAgent()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        if (rb != null) 
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
        }
    }

    private IEnumerator FollowSequence()
    {
        // 終了条件は外部（AllyAIのステート切り替え）に任せるので、無限ループ
        // または一定時間で区切るか。今回はAllyAI側で制御する想定なので無限ループさせるが、
        // Executeが終了しないと次の行動に移れないEnemyAIの設計上、
        // 「一定時間歩く」か「近づいたら終わる」必要がある。
        
        // 仕様変更: AllyAIのStateがFollowである限り、Executeを呼び続ける設計にするか、
        // 一度のExecuteで目標地点まで行くか。
        // ここでは「近づくまで実行」にする。

        while (true)
        {
             if (playerTransform == null || agent == null || !agent.enabled) break;

             float dist = Vector3.Distance(transform.position, playerTransform.position);
             
             // 目的地セット
             agent.SetDestination(playerTransform.position);

             // 十分近づいたら終了
             if (dist <= stopDistance)
             {
                 break; 
             }

             yield return new WaitForSeconds(followUpdateInterval);
        }

        StopAgent();
    }
}
