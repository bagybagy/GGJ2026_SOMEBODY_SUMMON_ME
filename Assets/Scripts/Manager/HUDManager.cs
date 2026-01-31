using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 💡 追加

// 💡 プレイヤーのHPと味方数（Infection Count）を表示するUIマネージャー
public class HUDManager : MonoBehaviour
{
    [Header("Player HP UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText; // Type変更

    [Header("Ally Count UI")]
    [SerializeField] private TextMeshProUGUI allyCountText; // Type変更
    [SerializeField] private float countInterval = 0.2f; // 更新頻度 (負荷軽減)

    // 内部変数
    private StatusManager playerStatus;
    
    void Start()
    {
        // プレイヤーを探してステータスを取得
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerStatus = player.GetComponent<StatusManager>();
        }
        else
        {
            Debug.LogWarning("HUDManager: Player not found.");
        }

        // 味方数カウントコルーチン開始
        StartCoroutine(UpdateAllyCountRoutine());
    }

    void Update()
    {
        // プレイヤーHP更新 (毎フレーム更新でスムーズに)
        UpdatePlayerHP();
    }

    private void UpdatePlayerHP()
    {
        if (playerStatus == null) return;

        float current = playerStatus.CurrentHp;
        float max = playerStatus.MaxHp;

        // Slider更新
        if (hpSlider != null)
        {
            // 0除算対策
            if (max > 0) hpSlider.value = current / max;
            else hpSlider.value = 0;
        }

        // Text更新
        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private IEnumerator UpdateAllyCountRoutine()
    {
        while (true)
        {
            CountAllies();
            yield return new WaitForSeconds(countInterval);
        }
    }

    private void CountAllies()
    {
        if (allyCountText == null) return;

        // "Ally"タグを持つ全オブジェクトを取得
        GameObject[] allAllies = GameObject.FindGameObjectsWithTag("Ally");
        
        int activeCount = 0;

        foreach (var obj in allAllies)
        {
            // 親オブジェクト(prefab root)についているAllyAIを確認
            AllyAI ai = obj.GetComponentInParent<AllyAI>();
            
            // AIが存在し、かつ Dizzy (気絶) していないものをカウント
            if (ai != null && !ai.IsDizzy())
            {
                activeCount++;
            }
        }

        allyCountText.text = $"Allies: {activeCount}";
    }
}
