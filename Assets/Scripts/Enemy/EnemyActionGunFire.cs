using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 💡 遠距離攻撃（銃撃）アクション
// ActionType: Attack
// その場で立ち止まり、ターゲットの方を向いて弾を発射します。
public class EnemyActionGunFire : EnemyAction
{
    [Header("Shooting Settings")]
    [SerializeField] private ProjectileController projectilePrefab; // 発射する弾のPrefab
    [SerializeField] private Transform muzzlePoint;                 // 発射地点（銃口）
    [SerializeField] private float faceTargetSpeed = 5.0f;          // ターゲットを向く速度
    
    [Header("Pattern Settings")]
    [SerializeField] private int burstCount = 1;      // 1回の行動で撃つ弾数
    [SerializeField] private float burstInterval = 0.2f; // 連射時の間隔 (秒)
    [SerializeField] private float cooldown = 2.0f;      // 次の行動までの待機時間

    // 内部変数
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Coroutine shootRoutine;

    // ステータス参照（弾に渡すため）
    private StatusManager myStatus;

    void Awake()
    {
        actionType = ActionType.Attack; // 攻撃タイプ
        
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        myStatus = GetComponent<StatusManager>();

        // 銃口が未設定なら自分の位置＋少し前/上
        if (muzzlePoint == null)
        {
            // 簡易的に空のオブジェクトを作ってセットしておく
            GameObject muzzleObj = new GameObject("AutoMuzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f); // 頭の少し前
            muzzlePoint = muzzleObj.transform;
        }
    }

    public override IEnumerator Execute()
    {
        // ターゲットがいなければ何もしない
        if (Target == null) yield break;

        // 移動停止
        if (agent != null) 
        {
            agent.enabled = true;
            agent.ResetPath();
            agent.isStopped = true;
        }
        if (rb != null) rb.linearVelocity = Vector3.zero;

        shootRoutine = StartCoroutine(ShootSequence());
        yield return shootRoutine;
    }

    public override void Stop()
    {
        if (shootRoutine != null) StopCoroutine(shootRoutine);
        if (agent != null) agent.isStopped = false;
    }

    private IEnumerator ShootSequence()
    {
        // 1. ターゲットの方を向く (予備動作中に合わせる)
        float aimDuration = 0.5f; // 照準合わせの時間
        float timer = 0f;

        while (timer < aimDuration)
        {
            if (Target != null)
            {
                // Y軸のみ回転
                Vector3 dir = (Target.position - transform.position);
                dir.y = 0;
                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceTargetSpeed);
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. 発射 (バースト)
        for (int i = 0; i < burstCount; i++)
        {
            Fire();
            // 連射間隔待機 (最後の一発の後は待たない)
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        // 3. クールダウン (射撃後の硬直)
        yield return new WaitForSeconds(cooldown);
        
        // 移動再開許可はAI側で行われるが、念のため
    }

    private void Fire()
    {
        if (projectilePrefab == null || muzzlePoint == null) return;

        // アニメーション (Trigger: "Attack")
        AnimTriggerAttack(); 

        // 弾生成
        var projectileObj = Instantiate(projectilePrefab.gameObject, muzzlePoint.position, muzzlePoint.rotation);
        
        // 初期化 (自分自身のStatusをOwnerとして渡す)
        // 方向はMuzzleの正面
        projectileObj.GetComponent<ProjectileController>()?.Initialize(myStatus, muzzlePoint.forward);
    }
}
