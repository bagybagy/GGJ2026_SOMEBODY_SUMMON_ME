using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 ミサイル流星群アクション
// ターゲット周辺に5発のミサイルを放物線軌道で撃ち込む
public class EnemyActionMissileBarrage : EnemyAction
{
    [Header("Missile Settings")]
    [SerializeField] private ParabolicProjectile missilePrefab;
    [SerializeField] private Transform launchPoint; // 発射位置（背中とか）
    [SerializeField] private int shotCount = 5;
    [SerializeField] private float shotInterval = 0.2f;
    [SerializeField] private float dispersionRadius = 3.0f; // 散らばり具合
    [SerializeField] private float cooldown = 5.0f; // クールダウン

    [Header("Visual")]
    [SerializeField] private float preDelay = 1.0f; // 溜め時間
    [SerializeField] private GameObject chargeEffect; // 溜めエフェクト

    private float nextFireTime = 0f;
    private StatusManager statusManager;

    void Start()
    {
        actionType = ActionType.Attack;
        statusManager = GetComponent<StatusManager>();
        if (launchPoint == null) launchPoint = transform; // なければ足元から出る
        
        // 最初はすぐに撃てるようにする
        nextFireTime = Time.time;
    }

    public override IEnumerator Execute()
    {
        // クールダウンチェック（もし親AIが管理していない場合）
        // EnemyAIの仕組み的に、Actionを選んだ時点で実行されるので、ここでチェックしても遅いかもしれないが
        // 連続で選ばれた場合の保険として
        /*
        if (Time.time < nextFireTime)
        {
            yield break;
        }
        */

        if (Target == null) yield break;

        // 1. 溜め動作
        AnimTriggerAttack(); // 攻撃モーション
        if (chargeEffect != null)
        {
            var fx = Instantiate(chargeEffect, launchPoint.position, Quaternion.identity, launchPoint);
            Destroy(fx, preDelay + 0.5f);
        }

        // 足を止める
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        yield return new WaitForSeconds(preDelay);

        // 2. 連射
        Vector3 targetBasePos = Target.position;

        for (int i = 0; i < shotCount; i++)
        {
            if (missilePrefab != null)
            {
                // ランダムなオフセット
                Vector2 randomCircle = Random.insideUnitCircle * dispersionRadius;
                Vector3 targetPos = targetBasePos + new Vector3(randomCircle.x, 0, randomCircle.y);

                // 発射
                var missile = Instantiate(missilePrefab, launchPoint.position, Quaternion.identity);
                missile.Initialize(launchPoint.position, targetPos, statusManager);
            }

            yield return new WaitForSeconds(shotInterval);
        }

        // 3. 硬直
        yield return new WaitForSeconds(1.0f);

        // 次回発射時刻更新
        nextFireTime = Time.time + cooldown;
    }

    public override void Stop()
    {
        StopAllCoroutines();
    }
}
