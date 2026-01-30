using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.AI; // NavMeshAgent用

// 💡 オブジェクトの「移動速度」を監視し、VFX Graphのプロパティに流し込むスクリプト
public class VFXSpeedCoupler : MonoBehaviour
{
    [Header("Target VFX")]
    [SerializeField] VisualEffect visualEffect;
    [SerializeField] string propertyName = "Speed"; // VFX側の名前

    // 速度を取得するコンポーネント（自動検知）
    private Rigidbody rb;
    private NavMeshAgent agent;

    void Start()
    {
        // どちらがついているか自動で親を調べる
        rb = GetComponentInParent<Rigidbody>();
        agent = GetComponentInParent<NavMeshAgent>();

        if (visualEffect == null)
        {
            // vfxを取得
            visualEffect = GetComponent<VisualEffect>();
        }
    }

    void Update()
    {
        if (visualEffect == null) return;

        float currentSpeed = 0f;

        // 優先度高：NavMeshAgent（敵）
        if (agent != null && agent.enabled)
        {
            currentSpeed = agent.velocity.magnitude;
        }
        // 優先度低：Rigidbody（物理移動キャラ）
        else if (rb != null)
        {
            // Unity 6以降は linearVelocity 推奨。古い場合は velocity
            currentSpeed = rb.linearVelocity.magnitude;
        }

        // VFX Graphに数値を送信
        visualEffect.SetFloat(propertyName, Mathf.Sqrt(currentSpeed));
    }
}