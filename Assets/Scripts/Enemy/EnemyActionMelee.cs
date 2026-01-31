using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyActionMelee : EnemyAction
{
    [Header("Melee Settings")]
    [SerializeField] private float attackDuration = 0.5f; // 攻撃持続時間
    [SerializeField] private float cooldown = 2.0f; // クールダウン
    [SerializeField] private Collider attackCollider; // 攻撃判定用コライダー

    private float lastAttackTime = -10f;
    private bool isActive = false;
    private UnityEngine.AI.NavMeshAgent agent;

    void Awake()
    {
        // アクションタイプ設定
        actionType = ActionType.Attack;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
            attackCollider.isTrigger = true;
        }
    }

    public override IEnumerator Execute()
    {
        if (Time.time < lastAttackTime + cooldown)
        {
            yield break;
        }

        isActive = true;

        // 停止 & ターゲット方向を向く
        StopAgent();
        if (Target != null)
        {
            Vector3 dir = (Target.position - transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // アニメーション: 攻撃トリガー
        AnimTriggerAttack();

        // 攻撃判定ON
        AttackColliderOn();

        // 攻撃持続
        yield return new WaitForSeconds(attackDuration);

        // 攻撃判定OFF
        AttackColliderOff();

        isActive = false;
        lastAttackTime = Time.time;
    }

    private void StopAgent()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            // NavMeshAgentはvelocity
            agent.velocity = Vector3.zero;
        }
        else
        {
             // NavMeshAgentがない、または無効ならRigidbodyを止める
            Rigidbody rb = GetComponent<Rigidbody>();
            if(rb != null) rb.linearVelocity = Vector3.zero;
        }
    }

    public override void Stop()
    {
        isActive = false;
        AttackColliderOff();
    }

    private void AttackColliderOn()
    {
        if (attackCollider != null) attackCollider.enabled = true;
    }

    private void AttackColliderOff()
    {
        if (attackCollider != null) attackCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || attackCollider == null || !attackCollider.enabled) return;

        // 攻撃判定が自分の子供にある場合、親のOnTriggerEnterには来ないこともあるが
        // コライダーを直接アタッチしている場合を想定
        
        // 敵・味方判定などはStatusManagerへのダメージ適用で処理される想定
        // ここでは単純にStatusManagerを探してダメージを与える
        StatusManager status = other.GetComponent<StatusManager>();
        if (status != null && status.gameObject != this.gameObject)
        {
             // 自分自身でなければダメージ
             // 味方かどうかはStatusManager側あるいはAI側で判断するが
             // 簡易的にタグチェックなどは入れても良い
             
             // ターゲットと同じタグなら攻撃
             bool isEnemy = false;
             if (Target != null && other.CompareTag(Target.tag)) isEnemy = true;
             
             // あるいは無差別に攻撃して、StatusManager側でFriendlyFireを防ぐ設計ならそのまま
             if (isEnemy || other.CompareTag("Player")) // プレイヤーにも当たるなら
             {
                 // ダメージ適用: (ダメージ量, 位置, クリティカルタイプ, 攻撃者)
                 status.Damage(10, transform.position, CriticalType.Normal, transform); 
             }
        }
    }
}
