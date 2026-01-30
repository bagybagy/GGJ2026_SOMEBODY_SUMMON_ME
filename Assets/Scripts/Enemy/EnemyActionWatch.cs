using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 敵の「監視（警戒しながらターゲットを向いて待機）」という行動だけを管理するスクリプトです。
public class EnemyActionWatch : EnemyAction
{
    // 💡 実行中のコルーチン（行動ルーチン）を停止させるために覚えておく変数です。
    private Coroutine watchRoutine;

    private Rigidbody rb;   // 💡 Rigidbodyへの参照

    [SerializeField] float swayAmplitude = 1.5f;   // 揺れる幅 (速度の振幅)
    [SerializeField] float swayFrequency = 1f;     // 揺れる速さ (周期)
    [SerializeField] float dampingFactor = 0.9f;   // 減衰係数（動きを滑らかに、暴走を防ぐ）
    [SerializeField] float swayTimeOffset;         // ゆらゆら動作の開始時間をずらすためのオフセット
    [SerializeField] float watchDuration = 3.0f;   // 凝視する時間

    // ゲーム開始時に一度だけ呼ばれます
    void Start()
    {
        // 💡 Rigidbodyを取得
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("EnemyActionWatch requires a Rigidbody component on the same GameObject.");
            enabled = false; // Rigidbodyがないと動作しないため、スクリプトを無効化
            return;
        }        

        swayTimeOffset = Random.Range(0f, 2f * Mathf.PI);   // ゆらゆらのオフセット時間の設定
    }

    // 「監視しながら待機する」行動を開始する命令です。
    public override IEnumerator Execute()
    {
        // 「凝視と待機」の一連の動作をコルーチンとして開始します。
        watchRoutine = StartCoroutine(WatchRoutine());

        // そのコルーチンが終わるまで、親AIを待たせる
        yield return watchRoutine;
    }
    
    // 「監視しながら待機する」行動を停止する命令です。
    public override void Stop()
    {
        // 実行中のコルーチンを停止します。
        if (watchRoutine != null) StopCoroutine(watchRoutine);
    }

    // =========================================================
    
    // 凝視の動作を管理します。
    private IEnumerator WatchRoutine()
    {
        // 物理演算の処理が終わるタイミングで実行を開始します。（ガクつきを防ぐため）
        yield return new WaitForFixedUpdate();

        float timer = 0f;

        while (timer <　watchDuration) 
        {
            // ----------------------------------------------------
            // 💡 1. Y軸を無視した LookAt の実装（回転）
            // ----------------------------------------------------
            // 💡 Step13.1修正: "Target" は親クラスのプロパティを指すようになる
            Vector3 direction = Target.position - transform.position;
            direction.y = 0; 
            
            if (direction != Vector3.zero)
            {
                rb.rotation = Quaternion.LookRotation(direction); // Rigidbodyの回転を直接操作
            }
            
            // ----------------------------------------------------
            // 💡 2. Rigidbodyの速度（Velocity）によるゆらゆら動作
            // ----------------------------------------------------
            
            // 1. Rigidbodyの現在の速度を取得（重力による落下速度 rb.velocity.y を保持するため）
            Vector3 currentVelocity = rb.linearVelocity; 
            
            // 現在の進行方向（オブジェクトの右方向）を基準に揺れを計算
            Vector3 right = transform.right; 
            
            // サイン波を使って、左右に揺れる速度ベクトルを計算
            float swaySpeed = Mathf.Sin((Time.time * swayFrequency) + swayTimeOffset) * swayAmplitude;
            
            // 揺れ成分（水平方向の速度）を計算
            Vector3 swayVelocityXZ = right * swaySpeed * dampingFactor;
            
            // 2. 最終的な新しい速度を構築
            // 垂直方向の速度は、currentVelocity.y をそのまま維持する
            Vector3 finalVelocity = new Vector3(
                swayVelocityXZ.x,           // 揺れのX成分
                currentVelocity.y,          // 💡 ここが重要：重力による落下速度を維持！
                swayVelocityXZ.z            // 揺れのZ成分
            );

            // 3. Rigidbodyに適用
            rb.linearVelocity = finalVelocity;

            yield return new WaitForFixedUpdate();
            
            timer += Time.fixedDeltaTime;
        }
    }
}