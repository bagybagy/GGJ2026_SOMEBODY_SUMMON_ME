using UnityEngine;
using System.Collections.Generic; // Listを使うために必要

// 💡 近接攻撃がヒットした際に、追撃（拡散レーザー）を発生させるスクリプト
// プレイヤーの剣などの「攻撃判定オブジェクト」にアタッチして使用します。
public class MeleeImpact : MonoBehaviour
{
    [Header("Burst Settings")]
    [SerializeField] GameObject projectilePrefab; // 発生させる弾（プレイヤーのレーザー）
    [SerializeField] int baseBurstCount = 4;      // 最低限発射される数
    [SerializeField] float spreadSpeed = 10f;     // 弾の初速
    [SerializeField] float searchRadius = 20f;    // 敵を探す範囲

    // 持ち主（プレイヤー）のステータス
    // 弾に攻撃力やクリティカル率を引き継ぐために必要
    private StatusManager ownerStatus;

    void Start()
    {
        // 親階層（Player本体）にあるStatusManagerを取得して保持しておく
        ownerStatus = GetComponentInParent<StatusManager>();
    }

    // 攻撃判定が何かに触れた瞬間に呼ばれる
    void OnTriggerEnter(Collider other)
    {
        // 敵に当たった場合のみ発動（壁や床では発動しない）
        if (other.CompareTag("Enemy"))
        {
            // 当たった敵の位置を中心にしてバーストを生成
            SpawnBurst(other.transform.position);
        }
    }

    // 拡散弾生成ロジック
    void SpawnBurst(Vector3 centerPos)
    {
        if (projectilePrefab == null) return;

        // ----------------------------------------------------
        // 1. 発射数の決定
        // ----------------------------------------------------
        // 基本数 + 現在のレベル = 発射数
        // レベルが上がるほど派手になり、攻撃範囲が広がる（成長実感）
        int currentCount = baseBurstCount;
        if (ownerStatus != null)
        {
            currentCount += ownerStatus.Level;
        }

        // ----------------------------------------------------
        // 2. ターゲットの索敵
        // ----------------------------------------------------
        // シーン上の全ての敵を取得し、爆発地点から近い敵だけをリストアップする
        // (物理演算のレイヤー設定に依存しない、確実な検索方法)
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<Transform> validTargets = new List<Transform>();

        foreach (var enemy in allEnemies)
        {
            // 距離判定：指定範囲内にいる敵だけを対象にする
            if (Vector3.Distance(centerPos, enemy.transform.position) <= searchRadius)
            {
                validTargets.Add(enemy.transform);
            }
        }

        // ----------------------------------------------------
        // 3. 生成と発射
        // ----------------------------------------------------
        for (int i = 0; i < currentCount; i++)
        {
            // 円形に配置するための角度計算
            float angle = i * (360f / currentCount);
            Quaternion rotation = Quaternion.Euler(0, angle, 0);

            // 敵の中心から少し上（見やすい位置）に生成
            Vector3 spawnPos = centerPos + Vector3.up * 1.0f;

            // 弾を生成
            GameObject bullet = Instantiate(projectilePrefab, spawnPos, rotation);

            // A. ステータスの継承
            // 生成された弾に「誰が撃ったか」「強さはどれくらいか」を教える
            // これを行わないと、弾の攻撃力が反映されずダメージが0になってしまう
            var source = bullet.GetComponent<DamageSource>();
            if (source != null && ownerStatus != null)
            {
                source.Initialize(ownerStatus);
            }

            // B. ターゲットの割り当て
            // 見つけた敵リストに対して、順番にターゲットを割り振る（均等分散）
            // 例: 敵が3体で弾が5発なら、A, B, C, A, B という順に狙う
            if (validTargets.Count > 0)
            {
                Transform assignedTarget = validTargets[i % validTargets.Count];

                var laserScript = bullet.GetComponent<SimpleLaser>();
                if (laserScript != null)
                {
                    laserScript.SetTarget(assignedTarget);
                }
            }

            // C. 弾を飛ばす
            // 弾が持っているRigidbodyを使って初速を与える
            var rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 弾の正面方向（外側）に向かって加速
                rb.linearVelocity = bullet.transform.forward * spreadSpeed;
            }
        }
    }
}