using UnityEngine;

// 💡 ボスエネミーが倒されたときにゲームクリアを呼び出すスクリプト
// ボスのGameObjectにアタッチしてください
public class BossDefeatTrigger : MonoBehaviour
{
    private StatusManager statusManager;

    void Start()
    {
        // StatusManagerを取得
        statusManager = GetComponent<StatusManager>();
        
        if (statusManager == null)
        {
            Debug.LogError("BossDefeatTrigger: StatusManager not found on this GameObject!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // ボスが死亡したかチェック（HP が 0 以下）
        if (statusManager != null && statusManager.CurrentHp <= 0)
        {
            OnBossDefeated();
            enabled = false; // 一度だけ実行
        }
    }

    private void OnBossDefeated()
    {
        Debug.Log("Boss defeated! Triggering Game Clear...");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameClear();
        }
        else
        {
            Debug.LogError("BossDefeatTrigger: GameManager.Instance is null!");
        }
    }
}
