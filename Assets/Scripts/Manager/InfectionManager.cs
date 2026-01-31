using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfectionManager : MonoBehaviour
{
    public static InfectionManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject allyPrefab; // Ally_MiniMaskプレハブ
    [SerializeField] private float spawnOffsetY = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 敵死亡時に呼ばれる
    // 💡 Step 1: 敵の位置に味方を生成する
    public void SpawnAlly(Vector3 position)
    {
        if (allyPrefab == null)
        {
            Debug.LogWarning("Ally Prefab is not assigned in InfectionManager!");
            return;
        }

        Vector3 spawnPos = position;
        spawnPos.y += spawnOffsetY; // 地面に埋まらないように少し浮かせる

        Instantiate(allyPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"Ally spawned at {spawnPos}");
    }
}
