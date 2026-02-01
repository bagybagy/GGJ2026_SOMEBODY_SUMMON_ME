using UnityEngine;

// 💡 放物線を描く投射物
// 目標地点に正確に着弾する初速を計算して飛んでいく
public class ParabolicProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float arcHeight = 5.0f; // 放物線の高さ
    [SerializeField] private GameObject explosionEffect; // 着弾時エフェクト
    [SerializeField] private GameObject predictionPrefab; // 着弾予測円のプレハブ

    [Header("Damage")]
    [SerializeField] private float explosionRadius = 2.0f;

    // 内部変数
    private Rigidbody rb;
    private GameObject predictionMarker; // インスタンス化された予測円
    private string ownerTag;
    private StatusManager ownerStatus;

    // 初期化（発射時に呼ぶ）
    public void Initialize(Vector3 startPos, Vector3 targetPos, StatusManager owner)
    {
        rb = GetComponent<Rigidbody>();
        ownerStatus = owner;
        if (ownerStatus != null) ownerTag = ownerStatus.tag;

        // 💡 DamageSourceの初期化
        // これが無いとクリティカル判定やダメージ計算が正しく行われない
        DamageSource ds = GetComponent<DamageSource>();
        if (ds != null && owner != null)
        {
            ds.Initialize(owner);
        }
        else if (ds == null)
        {
            // なければ追加してあげる優しさ（既存プレハブ修正漏れ対策）
            ds = gameObject.AddComponent<DamageSource>();
            if (owner != null) ds.Initialize(owner);
        }

        transform.position = startPos;

        // 1. 初速計算
        Vector3 velocity = CalculateVelocity(startPos, targetPos, arcHeight);
        if (float.IsNaN(velocity.x)) velocity = Vector3.zero; // 安全策

        rb.linearVelocity = velocity;

        // 2. 予測円の生成
        if (predictionPrefab != null)
        {
            // 地面スレスレに表示したいので、少しYを調整（Raycastしてもいいが簡易的にtargetPos採用）
            // targetPosが空中判定だと浮くので、とりあえずtargetPosそのものに出す
            predictionMarker = Instantiate(predictionPrefab, targetPos + Vector3.up * 0.1f, Quaternion.identity);
            
            // 予測円を少しずつ赤くするなどの演出も可能だが、今回は生成のみ
        }
    }
    
    // 物理法則に基づいた初速計算
    private Vector3 CalculateVelocity(Vector3 start, Vector3 target, float height)
    {
        // Y軸と水平面の距離成分を分離
        float displacementY = target.y - start.y;
        Vector3 displacementXZ = new Vector3(target.x - start.x, 0, target.z - start.z);
        float distanceXZ = displacementXZ.magnitude; // 水平距離

        // 簡易的な計算として、「頂点高さ h まで到達してから落ちる」と仮定
        // ただし、ターゲットの方が高い場合などは h + displacementY 分上がる必要がある
        // ここでは「最高点 = start.y + height」となるように計算する（startよりtargetが高くても、そこから更にheight分上がる）
        
        // 重力
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // 頂点までの高さ (targetの方が高ければ、target基準でheight足すなど調整)
        float apexHeight = Mathf.Max(start.y, target.y) + height - start.y;

        // 上昇速度 (Vy) : v^2 = 2gh
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(2 * gravity * apexHeight);

        // 落下時間までの総時間 t
        // 上昇時間 t_up + 下降時間 t_down
        float timeUp = Mathf.Sqrt(2 * apexHeight / gravity);
        float timeDown = Mathf.Sqrt(2 * (apexHeight - displacementY) / gravity);
        float totalTime = timeUp + timeDown;

        // 水平速度 (Vx, Vz)
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }

    void OnCollisionEnter(Collision collision)
    {
        // 自分自身や発射主とは衝突しない（レイヤー分けが理想だがコードでもガード）
        if (ownerTag != null && collision.gameObject.CompareTag(ownerTag)) return;

        // ヒット処理
        Explode();
    }

    private void Explode()
    {
        // エフェクト
        if (explosionEffect != null)
        {
            var fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(fx, 3.0f); // エフェクト寿命
        }

        // ダメージ判定（爆発範囲）
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            // 自分や発射主は除外
            if (hit.gameObject == gameObject) continue;
            if (ownerTag != null && hit.CompareTag(ownerTag)) continue;

            StatusManager targetStatus = hit.GetComponent<StatusManager>();
            DamageSource myDamageSource = GetComponent<DamageSource>();

            if (targetStatus != null && myDamageSource != null)
            {
                // DamageSource経由で計算 (Crit判定含む)
                CriticalType type;
                int dmg = myDamageSource.CalculateDamage(out type);

                // Initialize時にOwnerを登録していれば、OwnerTransformは自動的にOwnerのものになる
                targetStatus.Damage(dmg, transform.position, type, myDamageSource.OwnerTransform);
            }
            
            // ノックバック用Rigidbody
            Rigidbody targetRb = hit.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.AddExplosionForce(10f, transform.position, explosionRadius, 1.0f, ForceMode.Impulse);
            }
        }

        // 予測円の削除
        if (predictionMarker != null)
        {
            Destroy(predictionMarker);
        }

        // 自身を削除
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // 念のため、途中で消された場合も予測円を消す
        if (predictionMarker != null)
        {
            Destroy(predictionMarker);
        }
    }
}
