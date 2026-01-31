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
        else if (gameObject.CompareTag("Ally") || gameObject.CompareTag("Untagged")) // 💡 修正: Untaggedもチェック（Dizzy直前の状態によっては必要かもだが、基本はAlly）
        {
            // 💡 Ally（味方）の場合: Dizzy状態になるのでDestroyしない
            // VFX側で「本体を消す(IsDead=true)」処理が走ると見えなくなる可能性がある。
            // Dizzyなら「点滅」や「ダウン」表現にしたいが、今はとりあえずDestroyだけ回避する。
            
            // もしVFXグラフ側でIsDead=trueでパーティクルが完全に消える仕組みなら、
            // ここでIsDead=falseに戻したりする必要があるかもしれない。
            // 一旦Destroy回避のみ実装。
            
            // 💡 Dizzy状態なら何もしない（AllyAI側で制御される）
            // ただし、もしIsDeadがTrueのままだとVFXが消えるなら、ここで蘇生待ちのエフェクト（煙など）を出すべきかも。
        }
        else
        {
            // 敵の場合：消滅
            Destroy(gameObject);
        }
    }


    // 💡 追加: 蘇生時のリセット処理
    public void Resurrect()
    {
        isDying = false;
        
        // イベント再購読（OnDeadで外しているため）
        if (status != null)
        {
            status.OnDead -= PlayDeathEffect; // 念のため
            status.OnDead += PlayDeathEffect;
        }

        // VFX状態リセット
        foreach (var v in allVFXs)
        {
            if (v != null)
            {
                v.SetBool("IsDead", false); // 生存状態に戻す
                v.Reinit(); // パーティクルシステムの再初期化（必要であれば）
            }
        }
        
        StopAllCoroutines();
    }
}