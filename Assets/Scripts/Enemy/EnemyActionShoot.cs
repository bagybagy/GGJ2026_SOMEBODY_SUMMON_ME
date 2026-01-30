using System.Collections;
using UnityEngine;

// 💡 遠距離攻撃用のアクション
public class EnemyActionShoot : EnemyAction
{
    [Header("Shooting Settings")]
    [SerializeField] GameObject projectilePrefab; // EnemyLaser
    [SerializeField] Transform shootPoint;        // 発射位置（口や手）
    [SerializeField] float shootDelay = 0.5f;     // 構えてから撃つまでの時間（予備動作）
    [SerializeField] float cooldown = 2.0f;       // 次の行動までの隙

    // 持ち主のステータス（弾に渡す用）
    private StatusManager ownerStatus;

    void Start()
    {
        ownerStatus = GetComponentInParent<StatusManager>();

        // shootPointが未設定なら自分の位置にする
        if (shootPoint == null) shootPoint = transform;
    }

    public override IEnumerator Execute()
    {
        // ターゲットがいなければ何もしない
        if (Target == null) yield break;

        // 1. ターゲットの方を向く
        // (Y軸だけ回転させて、不自然に傾かないようにする)
        Vector3 targetPos = Target.position;
        targetPos.y = transform.position.y;
        transform.LookAt(targetPos);

        // 2. 予備動作（チャージ演出などがあればここで再生）
        yield return new WaitForSeconds(shootDelay);

        // 3. 発射！
        if (projectilePrefab != null)
        {
            GameObject bullet = Instantiate(projectilePrefab, shootPoint.position, transform.rotation);

            // ステータス継承
            var source = bullet.GetComponent<DamageSource>();
            if (source != null && ownerStatus != null)
            {
                source.Initialize(ownerStatus);
            }

            // ターゲット誘導設定
            var laser = bullet.GetComponent<SimpleLaser>();
            if (laser != null)
            {
                // 親AIが狙っているターゲットを弾に教える
                laser.SetTarget(Target);
            }
        }

        // 4. 硬直時間
        yield return new WaitForSeconds(cooldown);
    }

    public override void Stop()
    {
        // 特になし（コルーチン停止は親AIがやってくれる）
    }
}