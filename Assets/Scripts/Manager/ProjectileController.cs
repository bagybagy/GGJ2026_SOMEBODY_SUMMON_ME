using UnityEngine;

// 💡 弾の制御クラス
// ヒット時にStatusManager経由でダメージを与える
// DamageSourceと連携して「誰が撃ったか」を管理する
public class ProjectileController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 5.0f;
    
    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Rigidbody rb;
    private bool isInitialized = false;

    // 当たり判定の除外用（発射主のタグなど）
    private string ownerTag;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 万が一 Rigidbody がなければ追加する（簡易保険）
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.useGravity = false; // 重力無しでまっすぐ飛ぶ
        rb.isKinematic = false; // 物理演算有効
        
        // 衝突モード：Continuous推奨（すり抜け防止）
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 時間経過で消滅
        Destroy(gameObject, lifeTime);
    }

    // 💡 生成時に外部から呼び出す初期化メソッド
    public void Initialize(StatusManager owner, Vector3 direction)
    {
        // 1. ダメージ計算用のソースに持ち主を登録
        DamageSource ds = GetComponent<DamageSource>();
        if (ds != null && owner != null)
        {
            ds.Initialize(owner);
            ownerTag = owner.tag; // 持ち主のタグを保存
        }

        // 2. 向きと速度を設定
        transform.forward = direction;
        if (rb != null)
        {
            // Unity 6以降なら linearVelocity だが、バージョン安全にするなら velocity でも可。
            // ユーザー環境に合わせて linearVelocity を使用
            rb.linearVelocity = direction.normalized * speed;
        }

        isInitialized = true;
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        if (!isInitialized) return;

        // 1. 自分自身や発射主には当たらない
        if (other.CompareTag(ownerTag)) return;
        
        // 2. 既に死んでいる、またはVFXなどは無視 (Triggerの場合)
        // 必要に応じてフィルタリング

        // 3. ダメージ処理
        // DamageSourceがアタッチされていれば、接触相手のStatusManagerを探して計算などは
        // DamageSource側の仕組み（あるいはStatusManager側で受け取る仕組み）に依存するが、
        // 既存設計では「攻撃モーション時にColliderをOnにする」方式だった。
        // 弾の場合は「当たった瞬間」に処理したい。
        
        StatusManager targetStatus = other.GetComponent<StatusManager>();
        DamageSource myDamageSource = GetComponent<DamageSource>();

        if (targetStatus != null && myDamageSource != null)
        {
            // 計算
            CriticalType type;
            int dmg = myDamageSource.CalculateDamage(out type);
            
            // ダメージ適用 (DamageSourceのOwnerTransformを渡す)
            targetStatus.Damage(dmg, transform.position, type, myDamageSource.OwnerTransform);
        }

        // 4. ヒットエフェクト
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // 5. 消滅
        Destroy(gameObject);
    }
}
