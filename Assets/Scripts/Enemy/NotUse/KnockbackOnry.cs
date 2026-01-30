using System.Collections;
using UnityEngine;
using System; // Action/eventを使うために必要

// 💡 役割：このスクリプトは、敵がダメージを受けた際にノックバック処理だけを実行します。
//    ノックバックが完了したら自動的に終了する、単機能の割り込み処理モジュールです。
public class KnockbackOnly : MonoBehaviour
{
    private Rigidbody rb;
    private StatusManager statusManager;
    
    // --- インスペクターから設定できる項目 ---
    [Header("Knockback Settings")]
    [SerializeField] float knockbackPower = 10f;     // 物理的な力の強さ
    [SerializeField] float knockbackDuration = 0.3f; // 力を加えた後の硬直時間
    
    // 💡 状態管理：現在、ノックバック処理中かどうかのフラグ
    private bool isKnockedBack = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        statusManager = GetComponent<StatusManager>();

        if (rb == null || statusManager == null)
        {
            Debug.LogError("必要なコンポーネント(Rigidbody/StatusManager)が見つかりません。");
            enabled = false;
            return;
        }

        // 💡 連携：StatusManagerのイベントを購読します。
        //    ダメージ通知が来たら、StartKnockbackメソッドを実行するよう予約します。
        statusManager.OnDamageTaken += StartKnockback;
    }
    
    // オブジェクトが破棄される際に、イベントの購読を解除します。
    void OnDestroy()
    {
        if (statusManager != null)
        {
            statusManager.OnDamageTaken -= StartKnockback;
        }
    }
    
    // 💡 外部からのトリガー：StatusManagerから呼び出されるノックバック開始メソッド
    public void StartKnockback(Vector3 hitPos, Transform attacker)
    {
        if (isKnockedBack) return; // 二重ノックバックを防止
        
        // 割り込み処理であるKnockbackRoutineコルーチンを実行
        StartCoroutine(KnockbackRoutine(hitPos));
    }

    // 💡 処理の本体：ノックバックの物理的な力と硬直時間を時間軸で制御します。
    private IEnumerator KnockbackRoutine(Vector3 attackerPosition)
    {
        isKnockedBack = true;
        
        // 1. ノックバックの方向を計算
        Vector3 knockbackDirection = (transform.position - attackerPosition).normalized;
        knockbackDirection.y = 0.5f; // 水平方向のノックアップは固定値を採用（演出）

        // 2. 物理的な力を加える（瞬間的な押し出し）
        rb.linearVelocity = Vector3.zero; // 一度速度をリセットしてから
        rb.AddForce(knockbackDirection * knockbackPower, ForceMode.Impulse);

        // 3. ノックバックの硬直時間待機
        yield return new WaitForSeconds(knockbackDuration);

        // 4. 処理終了
        // rb.linearVelocity = Vector3.zero; // 動きを止める
        isKnockedBack = false; // 状態を解除
        
        // Note: 後のステップで、EnemyAIがこのフラグを監視し、元の行動を再開します。
    }
}