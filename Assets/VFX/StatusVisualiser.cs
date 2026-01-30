using UnityEngine;
using UnityEngine.VFX;

public class StatusVisualizer : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private StatusManager status;

    // 色を変える対象（Surfaceのみ指定する想定）
    [SerializeField] private VisualEffect targetVFX;
    // Step12.4 経験値等表示用VFX
    [SerializeField] private VisualEffect statusVFX;

    [Header("HP Settings")]
    [ColorUsage(true, true)]
    [SerializeField] private Color healthyColor = new Color(0, 1, 1, 1) * 4; // 通常時（シアン）
    [ColorUsage(true, true)]
    [SerializeField] private Color criticalColor = new Color(1, 0, 0, 1) * 4; // 瀕死時（赤）

    void Start()
    {
        if (status == null) status = GetComponent<StatusManager>();
    }

    void Update()
    {
        if (status == null || targetVFX == null) return;

        // 💡 追加: ステータス更新
        if (statusVFX != null) UpdateStatusVisuals();

        UpdateHPVisuals();
    }

    private void UpdateHPVisuals()
    {
        // 1. HP率計算 (0.0 〜 1.0)
        float hpRatio = (float)status.CurrentHp / Mathf.Max(status.MaxHp, 1);

        // 2. 基本色の計算 (青 → 赤)
        Color currentColor = Color.Lerp(criticalColor, healthyColor, hpRatio);

        // 3. 瀕死時の点滅演出 (HP30%以下)
        if (hpRatio < 0.3f)
        {
            // 時間経過で 0.0 〜 1.0 を往復させる
            float blink = Mathf.PingPong(Time.time * 10.0f, 1.0f);

            // 色の強さ（輝度）を 0.1倍(暗) 〜 1.0倍(明) の間で揺らす
            currentColor *= (0.1f + blink * 0.9f);
        }

        // 4. VFXに適用
        targetVFX.SetVector4("BodyColor", currentColor);
    }

    private void UpdateStatusVisuals()
    {
        // 1. EXP率の計算 (0.0 〜 1.0)
        float expRatio = 0f;
        if (status.ExpToNextLevel > 0)
        {
            expRatio = (float)status.CurrentExp / status.ExpToNextLevel;
        }

        // 2. VFXに送信
        statusVFX.SetFloat("ExpRatio", expRatio);
        statusVFX.SetFloat("Level", (float)status.Level);
    }
}