using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 その場で回転しながら2方向から射撃を行うアクション
public class EnemyActionSpinFire : EnemyAction
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotateSpeed = 180.0f; // 度/秒
    [SerializeField] private float duration = 5.0f;      // 持続時間

    [Header("Shooting Settings")]
    [SerializeField] private ProjectileController projectilePrefab;
    [SerializeField] private Transform muzzlePoint1;
    [SerializeField] private Transform muzzlePoint2;
    [SerializeField] private float fireInterval = 0.2f;

    [Header("Auto Muzzle Settings (Legacy Support)")]
    // 自動生成する場合のオフセット（前後などに配置）
    [SerializeField] private Vector3 muzzle1Offset = new Vector3(0, 1.5f, 1.0f);
    [SerializeField] private Vector3 muzzle2Offset = new Vector3(0, 1.5f, -1.0f);

    private Rigidbody rb;
    private StatusManager myStatus;
    private float nextFireTime;

    void Start()
    {
        actionType = ActionType.Attack;
        rb = GetComponent<Rigidbody>();
        myStatus = GetComponent<StatusManager>();

        // Muzzleが無い場合の自動生成
        if (muzzlePoint1 == null)
        {
            GameObject m1 = new GameObject("AutoMuzzle1");
            m1.transform.SetParent(transform);
            m1.transform.localPosition = muzzle1Offset;
            m1.transform.localRotation = Quaternion.identity; // 前向き
            muzzlePoint1 = m1.transform;
        }

        if (muzzlePoint2 == null)
        {
            GameObject m2 = new GameObject("AutoMuzzle2");
            m2.transform.SetParent(transform);
            m2.transform.localPosition = muzzle2Offset;
            m2.transform.localRotation = Quaternion.Euler(0, 180, 0); // 後ろ向き
            muzzlePoint2 = m2.transform;
        }
    }

    public override IEnumerator Execute()
    {
        // ターゲットは不要だが、処理開始のトリガーとしてnullチェックはしないでおく（AI側でチェック済み想定）
        // if (Target == null) yield break;

        float timer = 0f;
        nextFireTime = Time.time + fireInterval;

        AnimTriggerAttack(); // 攻撃モーション

        while (timer < duration)
        {
            // 1. 回転
            // Rigidbodyを使って回転させる（物理挙動と干渉しないように）
            // 角速度を設定してもいいが、位置固定ならMoveRotationもあり
            if (rb != null)
            {
                // Y軸回転
                float angle = rotateSpeed * Time.fixedDeltaTime;
                Quaternion deltaRot = Quaternion.Euler(0, angle, 0);
                rb.MoveRotation(rb.rotation * deltaRot);
                
                // 位置はその場にとどまる（必要なら）
                rb.linearVelocity = Vector3.zero; 
            }
            else
            {
                // Rigidbodyがない場合（非推奨だが）
                transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
            }

            // 2. 射撃
            if (Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireInterval;
            }

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 終了時
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }

    private void Fire()
    {
        if (projectilePrefab == null) return;
        
        // Muzzle1
        if (muzzlePoint1 != null)
        {
            SpawnProjectile(muzzlePoint1);
        }

        // Muzzle2
        if (muzzlePoint2 != null)
        {
            SpawnProjectile(muzzlePoint2);
        }
    }

    private void SpawnProjectile(Transform muzzle)
    {
        var projectileObj = Instantiate(projectilePrefab.gameObject, muzzle.position, muzzle.rotation);
        
        // 弾はMuzzleの正面に飛ぶ
        Vector3 fireDir = muzzle.forward;

        // 初期化
        projectileObj.GetComponent<ProjectileController>()?.Initialize(myStatus, fireDir);
    }

    public override void Stop()
    {
        StopAllCoroutines();
        if (rb != null) rb.linearVelocity = Vector3.zero;
    }
}
