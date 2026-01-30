using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡Step6.2　継承の仕組みを使って、EnemyActionで定義した事を使えるように変更
// 敵へ「突進」と「クールダウン」を繰り返す行動を管理します。
public class EnemyActionDash : EnemyAction
{
    // --- インスペクターから設定できる項目 ---
    [Header("Dash Settings")]
    [SerializeField] float dashSpeed = 15f;         // 突進時の速度
    [SerializeField] float dashDuration = 0.5f;     // 突進が持続する時間
    [SerializeField] float dashCooldown = 1.0f;     // 突進後の待機時間
    [SerializeField] float dashPreparationTime = 0.5f; // 突進前の予備動作にかかる時間

    private Rigidbody rb;
    private Vector3 dashDirection;
    
    // 突進動作のループを停止するために、コルーチンを保持します。
    private Coroutine dashRoutine;

    // 攻撃判定用のコライダー変数
    [Header("Attack Settings")]
    [SerializeField] Collider attackCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the EnemyActionDash object.");
            enabled = false;
            return;
        }
    }

    // 💡Step6.2 継承元のEnemyActionから呼び出された時、この行動を停止するための命令にオーバーライド（上書き）して実行。
    // 敵がスタンした時や破壊された時などに、確実に行動を止められます。
    public override void Stop()
    {
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        if (rb != null) rb.linearVelocity = Vector3.zero;
        AttackColliderOff();
    }

    // 💡Step6.2 継承元のEnemyActionから呼び出された時、敵が繰り返し行動するために必要なメインのルーチンです。
    public override IEnumerator Execute()
    {
        dashRoutine = StartCoroutine(DashSequence());

        // 💡 コルーチンが終わるのを待つ
        yield return dashRoutine;
    }


    // 突進の一連の行動（予備動作 -> 突進 -> クールダウン）を管理します。
    private IEnumerator DashSequence()
    {
        // ------------------ 1. 予備動作 (タメ) ------------------
        // 💡 プレイヤーに回避の猶予を与えるため、突進前にタメの時間を設けます。
        float startTime = Time.time;
        // 予備動作に小ジャンプを追加
        rb.AddForce(Vector3.up * 3.0f, ForceMode.Impulse);
        // タメ動作中、ターゲットの方を向き続けます
        while (Time.time < startTime + dashPreparationTime)
        {
            // 💡 Step13.1修正: "Target" は親クラスのプロパティを指すようになる
            dashDirection = (Target.position - transform.position);
            dashDirection.y = 0; // 水平方向のみ回転させるためY軸は無視
            rb.rotation = Quaternion.LookRotation(dashDirection); // Rigidbodyの回転を直接操作

            // 物理演算のタイミングに合わせて次のフレームを待ちます
            yield return new WaitForFixedUpdate(); 
        }
        // ------------------ 2. 突進実行 ------------------

        dashDirection.Normalize();
        startTime = Time.time;
        
        // 突進開始時に攻撃判定を有効にする
        AttackColliderOn();

        while (Time.time < startTime + dashDuration)
        {
            // Y軸（落下速度）を維持しつつ、水平方向の速度を上書き
            Vector3 finalVelocity = dashDirection * dashSpeed;
            
            // 💡 Rigidbody.linearVelocityを使用し、重力による落下速度を維持します。
            finalVelocity.y = rb.linearVelocity.y; 

            rb.linearVelocity = finalVelocity;
            yield return new WaitForFixedUpdate();
        }

        // 突進終了時に攻撃判定を無効にする
        AttackColliderOff();

        // 突進終了後、水平方向の速度を0にして停止
        Vector3 stopVelocity = rb.linearVelocity;
        stopVelocity.x = 0;
        stopVelocity.z = 0;
        rb.linearVelocity = stopVelocity;

        // ------------------ 3. クールダウン ------------------
        // 次の突進までの待機時間
        yield return new WaitForSeconds(dashCooldown);
    }

    // 近接攻撃用のコライダーを有効にする関数
    void AttackColliderOn()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
            Debug.Log("Attack Collider ON");
            // 💡 攻撃エフェクトや音の再生もここで行う
        }
    }

    // 近接攻撃用のコライダーを無効にする関数
    void AttackColliderOff()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
            Debug.Log("Attack Collider OFF");
        }
    }
}