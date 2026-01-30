using System.Collections;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgentを使うために必要

// 💡 敵の「追跡」行動を管理するモジュール。
// EnemyActionを継承しているため、アタッチするだけでEnemyAIが自動的に認識し、
// ランダム行動の一つとして実行してくれます。
public class EnemyActionChase : EnemyAction
{
    private NavMeshAgent agent;
    private Rigidbody rb;
    
    [Header("Chase Settings")]
    [SerializeField] float chaseDuration = 3.0f; // 1回の追跡行動の長さ

    // 実行中のコルーチン保持用
    private Coroutine chaseRoutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found.");
            enabled = false;
            return;
        }
        
        // 普段は物理移動（Dashやノックバック）の邪魔をしないよう、Agentは無効化しておく
        agent.enabled = false;
    }

    // 💡 親AIからの実行命令
    public override IEnumerator Execute()
    {
        // 💡 1. 物理演算を一時停止（これで落下や床抜けを防止）
        if (rb != null) rb.isKinematic = true;
        
        // 2. 追跡開始：NavMeshAgentを有効化
        agent.enabled = true;

        // 2. 追跡シーケンスを開始し、完了まで待機
        chaseRoutine = StartCoroutine(ChaseSequence());
        yield return chaseRoutine;
    }

    // 💡 親AIからの停止命令（ノックバック時など）
    public override void Stop()
    {
        if (chaseRoutine != null) StopCoroutine(chaseRoutine);
        StopAgent();
    }

    // Agentを安全に停止・無効化する処理
    private void StopAgent()
    {
        if (agent != null && agent.enabled)
        {
            // パス（経路）をクリア
            agent.ResetPath();
            // Agentを無効化することで、Rigidbodyによる物理移動（ノックバック等）を阻害しないようにする
            agent.enabled = false;
        }
        // 物理演算を再開（ノックバックなどで飛ばされるように）
        if (rb != null) 
        {
            rb.isKinematic = false;
            // 復活直後の不安定さを消すため、速度を一度ゼロにする
            rb.linearVelocity = Vector3.zero;
        }
    }

    // 実際の追跡ロジック
    private IEnumerator ChaseSequence()
    {
        float timer = 0f;
        
        // 指定時間だけ追いかけ続ける
        while (timer < chaseDuration)
        {
            // 💡Step13.1 修正: "Target" は親クラスで取得したプロパティを指すようになる
            if (Target != null && agent.enabled)
            {
                agent.SetDestination(Target.position);
            }
            
            // 毎フレーム更新
            // RigidbodyではないのでFixdUpdateを待つのではなく、単にUpdateを待つnullでOK
            yield return null; 
            timer += Time.deltaTime;
        }
        
        // 時間が来たら終了処理
        StopAgent();
    }
}