using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class VFXDamageFeedback : MonoBehaviour
{
    [Header("Settings")]
    // 💡 Step12.2 単体ではなく配列で持つ
    private VisualEffect[] allVFXs;
    [SerializeField] string propertyName = "GlitchIntensity";
    [SerializeField] string trailPropertyName = "TrailRate";

    [Header("Glitch Parameters")]
    [SerializeField] float glitchDuration = 0.2f; // 一瞬だけ揺らす
    [SerializeField] float glitchPower = 25f;    // Turbulenceの強さ（20〜25くらい）

    // 💡 追加: 死亡演出中かどうかのフラグ
    private bool isDying = false;

    private StatusManager status;

    void Start()
    {
        // 💡 Step12.2 自分以下の全てのVFXを自動取得する
        // これならSurfaceだろうがJointsだろうが武器だろうが全部取れる
        allVFXs = GetComponentsInChildren<VisualEffect>();
        status = GetComponent<StatusManager>();

        // イベント購読
        if (status != null)
        {
            status.OnDamageTaken += PlayGlitch;
            status.OnDead += PlayDeathEffect;
        }
    }

    void OnDestroy()
    {
        if (status != null)
        {
            status.OnDamageTaken -= PlayGlitch;
            status.OnDead -= PlayDeathEffect;
        }
    }

    // ダメージイベントから呼ばれる
    void PlayGlitch(Vector3 hitPos, Transform attacker)
    {
        // 💡 修正: 死に始めていたら、グリッチ演出は無視する（StopAllCoroutinesさせない！）
        if (isDying) return;

        // 既に揺れていても上書きして再生
        StopAllCoroutines();
        StartCoroutine(GlitchRoutine());
    }

    IEnumerator GlitchRoutine()
    {
        // 1. ノイズON（数値を渡す）
        // 💡 配列内のすべてのVFXに対して設定
        foreach (var v in allVFXs)
        {
            if (v != null)
            {
                v.SetFloat(trailPropertyName, 0f);     // トレイルOFF（敵用）
                v.SetFloat(propertyName, glitchPower); // ノイズON
            }
        }

        // 2. 指定時間待つ
        yield return new WaitForSeconds(glitchDuration);

        // 3. ノイズOFF（0に戻す）
        foreach (var v in allVFXs)
        {
            if (v != null)
            {
                v.SetFloat(trailPropertyName, 1f);     // トレイルON（敵用）
                v.SetFloat(propertyName, 0f);          // ノイズOFF
            }
        }
    }
    // Step10.2 死亡時のVFX Event
    void PlayDeathEffect()
    {
        // 💡 追加: 死亡フラグを立てる
        isDying = true;

        status.OnDead -= PlayDeathEffect; // 二重呼び出し防止
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 1. VFX切り替え（本体消去＋爆発生成）
        foreach (var v in allVFXs)
        {
            if (v != null)
            {
                // 本体を消す (System 1 の Kill が作動)
                v.SetBool("IsDead", true);
                // 爆発させる (System 3 が作動)
                v.SendEvent("OnDeath");
            }
        }

        // 2. 余韻を待つ（VFXのLifetimeに合わせる）
        yield return new WaitForSeconds(3.0f);
        
        // 3. 後始末
        if (gameObject.CompareTag("Player"))
        {
            // 💡 プレイヤーの場合：消さずに非表示＆ゲームオーバー処理
            Debug.Log("<color=red>GAME OVER</color>");
            gameObject.SetActive(false);
            // ※ここでTime.timeScale = 0; とか SceneManager.LoadScene などを呼ぶのが一般的
        }
        else
        {
            // 敵の場合：消滅
            Destroy(gameObject);
        }
    }
}