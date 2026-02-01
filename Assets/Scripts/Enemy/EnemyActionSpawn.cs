using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 💡 2か所から一定間隔で増援（Prefabs）をInstantiateするアクション
// 扉から敵増援が登場するような演出に使えます。
public class EnemyActionSpawn : EnemyAction
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject spawnPrefab; // 生成するオブジェクト（敵など）
    [SerializeField] private Transform spawnPoint1;
    [SerializeField] private Transform spawnPoint2;
    [SerializeField] private int spawnWaves = 3;     // 生成回数（1回につき2体）
    [SerializeField] private float spawnInterval = 1.0f; // 生成間隔

    [Header("Visual")]
    [SerializeField] private GameObject spawnVFX;    // 出現時のエフェクト
    [SerializeField] private float startDelay = 0.5f; // 開始前のタメ

    private float nextSpawnTime;

    [Header("Auto Point Settings")]
    [SerializeField] private Vector3 point1Offset = new Vector3(-3, 0, 3);
    [SerializeField] private Vector3 point2Offset = new Vector3(3, 0, 3);

    void Start()
    {
        actionType = ActionType.Attack; // 便宜上Attack扱い

        // スポーン地点がなければ自動生成
        if (spawnPoint1 == null)
        {
            GameObject p1 = new GameObject("AutoSpawnPoint1");
            p1.transform.SetParent(transform);
            p1.transform.localPosition = point1Offset;
            spawnPoint1 = p1.transform;
        }

        if (spawnPoint2 == null)
        {
            GameObject p2 = new GameObject("AutoSpawnPoint2");
            p2.transform.SetParent(transform);
            p2.transform.localPosition = point2Offset;
            spawnPoint2 = p2.transform;
        }
    }

    public override IEnumerator Execute()
    {
        AnimTriggerAttack(); // 召喚モーション的なものがあれば

        yield return new WaitForSeconds(startDelay);

        for (int i = 0; i < spawnWaves; i++)
        {
            Spawn(spawnPoint1);
            Spawn(spawnPoint2);

            yield return new WaitForSeconds(spawnInterval);
        }

        // 硬直
        yield return new WaitForSeconds(1.0f);
    }

    private void Spawn(Transform point)
    {
        if (spawnPrefab == null || point == null) return;

        // VFX
        if (spawnVFX != null)
        {
            Instantiate(spawnVFX, point.position, Quaternion.identity);
        }

        // 生成
        GameObject spawnedObj = Instantiate(spawnPrefab, point.position, point.rotation);
    }

    public override void Stop()
    {
        StopAllCoroutines();
    }
}
