using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 オーブウォーク（旋回）しながら射撃を行うアクション
// ターゲットとの距離を保ちつつ横移動し、同時に攻撃を行います。
public class EnemyActionOrbWalkFire : EnemyAction
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.0f;       // 移動速度
    [SerializeField] private float idealDistance = 10.0f;  // 維持したい距離
    [SerializeField] private float distanceThreshold = 1.0f; // 許容誤差プラスマイナス
    [SerializeField] private float duration = 5.0f;        // 行動持続時間
    [SerializeField] private bool clockwise = true;        // 時計回りかどうか（ランダム化も可）

    [Header("Shooting Settings")]
    [SerializeField] private ProjectileController projectilePrefab; // 弾のPrefab
    [SerializeField] private Transform muzzlePoint;                 // 発射地点
    [SerializeField] private float fireInterval = 0.5f;             // 発射間隔
    [SerializeField] private Vector3 muzzleOffset = new Vector3(0, 1.5f, 0.5f); // 自動生成時の位置補正

    private Rigidbody rb;
    private StatusManager myStatus;
    private float nextFireTime;

    void Start()
    {
        actionType = ActionType.Attack; // 攻撃行動扱い
        rb = GetComponent<Rigidbody>();
        myStatus = GetComponent<StatusManager>();

        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("AutoMuzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = muzzleOffset;
            muzzlePoint = muzzleObj.transform;
        }
    }

    public override IEnumerator Execute()
    {
        if (Target == null) yield break;

        float timer = 0f;
        nextFireTime = Time.time + fireInterval;

        // 毎回ランダムな方向に旋回するのもありだが、今回はプロパティに従う
        // clockwise = (Random.value > 0.5f); 

        AnimSetRun(true); // 移動モーション（Run=true）

        while (timer < duration)
        {
            if (Target == null) break;

            // 1. 向きの制御 (常にターゲットを見る)
            Vector3 dirToTarget = Target.position - transform.position;
            dirToTarget.y = 0; // 高さは無視
            if (dirToTarget != Vector3.zero)
            {
                rb.rotation = Quaternion.LookRotation(dirToTarget);
            }

            // 2. 移動の制御 (オーブウォーク)
            Vector3 currentVel = rb.linearVelocity;
            float currentDist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(Target.position.x, 0, Target.position.z));

            // 基本は横移動
            Vector3 sideDir = transform.right * (clockwise ? 1f : -1f);
            Vector3 moveVec = sideDir * moveSpeed;

            // 距離調整
            if (currentDist > idealDistance + distanceThreshold)
            {
                // 遠すぎるので近づく成分を足す
                moveVec += transform.forward * (moveSpeed * 0.5f);
            }
            else if (currentDist < idealDistance - distanceThreshold)
            {
                // 近すぎるので離れる成分を足す
                moveVec -= transform.forward * (moveSpeed * 0.5f);
            }

            // Y軸（重力）は維持して適用
            rb.linearVelocity = new Vector3(moveVec.x, currentVel.y, moveVec.z);

            // 3. 射撃の制御
            if (Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireInterval;
            }

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 終了時停止
        if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }

    private void Fire()
    {
        if (projectilePrefab == null || muzzlePoint == null) return;

        // 攻撃アニメーショントリガー（走りながら撃つモーションがあればBestだが、なければ上半身レイヤーなどで対応想定）
        // AnimTriggerAttack(); 

        // 弾生成
        var projectileObj = Instantiate(projectilePrefab.gameObject, muzzlePoint.position, muzzlePoint.rotation);
        
        // Targetへの方向を計算（偏差射撃はせず、現在の位置へ）
        Vector3 targetDir = (Target.position - muzzlePoint.position).normalized;

        // 初期化
        projectileObj.GetComponent<ProjectileController>()?.Initialize(myStatus, targetDir);
    }

    public override void Stop()
    {
        AnimSetRun(false);
        StopAllCoroutines();
        // 停止
        if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
    }
}
