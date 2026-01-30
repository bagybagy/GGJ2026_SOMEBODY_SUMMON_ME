using UnityEngine;
using UnityEngine.AI; // 💡 これが必要！

// 💡 NavMeshAgentの基本を学ぶための、一番シンプルな追跡スクリプト
public class SimpleNavMeshChase : MonoBehaviour
{
    // 追いかける相手
    private Transform target;
    
    // NavMeshAgentコンポーネントを入れる変数
    private NavMeshAgent agent;

    void Start()
    {
        // 自分のNavMeshAgentを取得
        agent = GetComponent<NavMeshAgent>();
        
        // ターゲット（Player）を自動で見つける
        target = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        // 💡 たったこれだけ！
        // 毎フレーム「ターゲットの位置」を目的地に設定し続ける
        agent.SetDestination(target.position);
    }
}