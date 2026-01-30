using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    [Header("Loot Settings")]
    [SerializeField] private GameObject itemPrefab; // ExpItemのRootプレハブ
    [SerializeField] private int dropCount = 3;     // 落とす数
    [SerializeField] private float spreadForce = 5f; // 飛び散る強さ

    private StatusManager status;

    void Start()
    {
        status = GetComponentInParent<StatusManager>();

        // いつものイベント購読処理、もう慣れたかな？

        if (status != null)
        {
            status.OnDead += SpawnLoot;
        }
    }

    void OnDestroy()
    {
        if (status != null) status.OnDead -= SpawnLoot;
    }

    void SpawnLoot()
    {
        if (itemPrefab == null) return;

        for (int i = 0; i < dropCount; i++)
        {
            // 敵の中心位置から少し上
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f;

            // 生成
            GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

            // 物理演算でポーンと飛ばす (アイテムのRootにRigidbodyがある前提)
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // ランダムな方向へ弾き飛ばす
                // insideUnitSphereを使うことで、全方向へ均等に散らばる
                Vector3 force = Random.insideUnitSphere * spreadForce;

                // 地面に埋まらないよう、必ず少し上方向の力を加える
                force.y = Mathf.Abs(force.y) + 2f;

                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }
}