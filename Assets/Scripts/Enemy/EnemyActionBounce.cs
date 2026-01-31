using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 💡 The Bouncy Pounce (バウンド・ジャンプ)
// ActionType: Chase
// 歩かずに、ターゲットに向かって放物線を描いて飛び跳ねながら近づきます。
// 効果: 障害物や他のNPCを飛び越えて後衛を強襲できる。着地時に小さな衝撃波（ノックバック）を発生。
public class EnemyActionBounce : EnemyAction
{
    [Header("Bounce Settings")]
    [SerializeField] float maxJumpDistance = 6.0f; // 1回のジャンプの最大距離
    [SerializeField] float minJumpDistance = 1.0f; // 近すぎるときでも少し跳ねる
    [SerializeField] float jumpHeight = 3.0f;      // ジャンプの高さ (頂点)
    [SerializeField] float jumpInterval = 0.5f;    // ジャンプ間の待機時間

    [Header("Animation Settings")]
    [SerializeField] Transform visualTransform;    // スケール変更する対象（未指定なら自分自身）
    [SerializeField] Vector3 squashScale = new Vector3(1.3f, 0.6f, 1.3f); // 予備動作：潰れる
    [SerializeField] Vector3 stretchScale = new Vector3(0.7f, 1.4f, 0.7f); // ジャンプ中：伸びる
    [SerializeField] Vector3 landScale = new Vector3(1.4f, 0.5f, 1.4f);   // 着地：潰れる（または拡大）
    [SerializeField] float animDuration = 0.15f;   // 変形にかかる時間

    [Header("Impact Settings")]
    [SerializeField] float impactRadius = 2.5f;    // 着地時の衝撃波範囲
    [SerializeField] float impactForce = 8.0f;     // ノックバック力
    [SerializeField] LayerMask targetLayers;       // 衝撃を与えるレイヤー（指定しなくてもコードで判定可能だが、あると便利）

    // 内部変数
    private Rigidbody rb;
    private NavMeshAgent agent;
    private Coroutine bounceRoutine;

    void Awake()
    {
        // 💡 ActionTypeは移動系（Chase）として設定
        actionType = ActionType.Chase;
        
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        
        // デフォルトのターゲットレイヤー（Player or Enemy）
        if (targetLayers == 0)
        {
            targetLayers = LayerMask.GetMask("Player", "Enemy", "Default"); 
        }

        // ビジュアル指定がなければ自分自身（ただしルートを直接弄ると物理に影響が出る可能性あり）
        if (visualTransform == null) visualTransform = transform;
    }

    public override IEnumerator Execute()
    {
        // 物理移動を行うため、NavMeshAgentは無効化
        if (agent != null) agent.enabled = false;
        
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        bounceRoutine = StartCoroutine(BounceSequence());
        yield return bounceRoutine;
    }

    public override void Stop()
    {
        if (bounceRoutine != null) StopCoroutine(bounceRoutine);
        
        // 動きを止める
        if (rb != null) rb.linearVelocity = Vector3.zero;

        // スケールを戻す
        if (visualTransform != null) visualTransform.localScale = Vector3.one;

        // NavMeshAgentを戻しておく（他のアクションが使うかもしれないため）
        if (agent != null)
        {
            agent.enabled = true;
            // 一旦パスをリセットしないと、有効化した瞬間にワープすることがある
            agent.ResetPath();
        }
    }

    private IEnumerator BounceSequence()
    {
        // ターゲットがいなければ終了
        if (Target == null)
        {
            yield break;
        }

        // 1. ジャンプ計算
        Vector3 startPos = transform.position;
        Vector3 targetPos = Target.position;
        
        Vector3 dir = targetPos - startPos;
        dir.y = 0; // 水平方向の距離
        
        float distance = dir.magnitude;
        
        // 近すぎる、またはターゲットまでジャンプ
        // ただし最大距離でクランプ
        float jumpDist = Mathf.Min(distance, maxJumpDistance);
        jumpDist = Mathf.Max(jumpDist, minJumpDistance); // 最低距離保証
        
        // 方向ベクトル（正規化）
        Vector3 jumpDir = dir.normalized;
        
        // 💡 物理の公式： h = v0_y^2 / 2g  => v0_y = sqrt(2gh)
        // 滞空時間 t = 2 * v0_y / g
        // 水平速度 v0_x = dist / t
        
        float g = Mathf.Abs(Physics.gravity.y);
        float v0_y = Mathf.Sqrt(2 * g * jumpHeight);
        float t_flight = 2 * v0_y / g;
        float v0_x = jumpDist / t_flight;
        
        // 速度ベクトル作成
        Vector3 jumpVelocity = jumpDir * v0_x;
        jumpVelocity.y = v0_y;

        // --- 予備動作 (Squash) ---
        // 少し潰れて力をためる
        yield return StartCoroutine(TweenScale(squashScale, animDuration));

        // 2. ジャンプ実行
        // アニメーション
        AnimTriggerJump();

        // --- ジャンプ開始 (Stretch) ---
        // 伸びる
        StartCoroutine(TweenScale(stretchScale, 0.1f));

        // 一瞬だけ敵の方を向く
        if (jumpDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(jumpDir);
        }
        
        rb.linearVelocity = jumpVelocity;
        
        // 3. 着地待ち（滞空時間分待つ）
        // 滞空時間の半分くらいで元のスケールに戻し始める
        yield return new WaitForSeconds(t_flight * 0.5f);
        StartCoroutine(TweenScale(Vector3.one, 0.2f)); // 空中で元に戻る
        yield return new WaitForSeconds(t_flight * 0.5f);
        
        // 4. 着地エフェクト＆攻撃
        rb.linearVelocity = Vector3.zero; // 着地したらピタッと止まる（スライディング防止）
        
        DoLandingImpact();

        // --- 着地 (Land / Expand) ---
        // 衝撃で少し潰れる、または大きくなる
        yield return StartCoroutine(TweenScale(landScale, 0.1f));
        // 元に戻る
        yield return StartCoroutine(TweenScale(Vector3.one, 0.2f));
        
        // 5. 少し待機（着地硬直のようなもの）してから終了
        // これがないと即座に次のジャンプ判定に行き、見た目が忙しなくなる可能性がある
        yield return new WaitForSeconds(jumpInterval);
        
        // ループせずに終了 -> AIが次の判断を行う
    }

    // スケールアニメーション用コルーチン
    private IEnumerator TweenScale(Vector3 targetScale, float duration)
    {
        if (visualTransform == null) yield break;

        Vector3 initialScale = visualTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            // EaseOutBackっぽい動きを入れるとよりCartoonらしくなるが、とりあえずLerpで実装
            visualTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            yield return null;
        }

        visualTransform.localScale = targetScale;
    }

    private void DoLandingImpact()
    {
        // エフェクトがあればここで生成
        // Example: Instantiate(landingEffect, transform.position, ...);
        
        // 範囲内の対象を検索
        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius, targetLayers);
        foreach (var hit in hits)
        {
            // 自分自身は除外
            if (hit.gameObject == gameObject) continue;
            // AllyならAllyには当てない、EnemyならEnemyには当てない等のフィルタ要
            // Tagで簡易判定
            if (hit.CompareTag(gameObject.tag)) continue; 

            Rigidbody hitRb = hit.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                // ノックバック (爆発的な力)
                // AddExplosionForce(force, center, radius, upwardsModifier)
                hitRb.AddExplosionForce(impactForce, transform.position, impactRadius, 1.0f, ForceMode.Impulse);
            }
            
            // 追加：StatusManagerがあればダメージも与えられる
            /*
            StatusManager status = hit.GetComponent<StatusManager>();
            if (status != null) {
                status.TakeDamage(10); // ダメージ値の設定が必要
            }
            */
        }
    }
    
    // ギズモ表示（デバッグ用）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
