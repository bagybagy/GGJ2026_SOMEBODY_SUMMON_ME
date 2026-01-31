using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For LINQ

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject hatMaskPrefab; // HatMaskプレハブ
    [SerializeField] private int requiredAllyCount = 10;
    [SerializeField] private float mergeRange = 10f; // プレイヤー周囲の有効範囲

    // 💡 追加: 合体対象とするレベル（0ならMiniMaskだけを集める）
    [SerializeField] private int targetMergeLevel = 0;

    [SerializeField] private bool autoMerge = true; // 💡 追加: 自動合体フラグ

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

    void Update()
    {
        // 💡 追加: 自動合体が有効なら常時チェック
        if (autoMerge)
        {
            // プレイヤーを探して距離チェック（シングルトンやタグで検索）
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                TryMerge(player.transform.position);
            }
        }
    }

    // プレイヤーから呼ばれる（または自動）
    public void TryMerge(Vector3 playerPosition)
    {
        // 1. 範囲内の有効なAllyを探す
        // Tag "Ally" を持つオブジェクトを検索
        GameObject[] allAllies = GameObject.FindGameObjectsWithTag("Ally");
        
        // 💡 修正: Hitboxなどを重複カウントしないように、ルートのAllyAIコンポーネントで管理
        HashSet<AllyAI> candidates = new HashSet<AllyAI>();

        foreach (var obj in allAllies)
        {
            // 親を辿ってAllyAIを探す（Hitbox対策）
            AllyAI ai = obj.GetComponentInParent<AllyAI>();
            
            // AIがない、または既にリストにあるならスキップ
            if (ai == null || candidates.Contains(ai)) continue;

            // 💡 追加: 指定したマージレベルでなければ除外（例: HatMaskは合体しない）
            if (ai.mergeLevel != targetMergeLevel) continue;

            // 距離チェック
            if (Vector3.Distance(ai.transform.position, playerPosition) > mergeRange) continue;

            // Dizzy状態なら除外
            if (ai.IsDizzy()) continue;

            // 生存確認（念のため）
            if (!ai.gameObject.activeInHierarchy) continue;

            candidates.Add(ai);
        }

        // デバッグログ多すぎると重いので、数が足りた時だけ出す等の調整推奨
        // Debug.Log($"Merge: Candidates found = {candidates.Count}");

        // 2. 数が足りているかチェック
        if (candidates.Count >= requiredAllyCount)
        {
            Debug.Log($"Merge: Requirements Met! Merging {requiredAllyCount} allies...");

            // 3. 10体選出して削除
            int count = 0;
            foreach (var ai in candidates)
            {
                if (count >= requiredAllyCount) break;

                // 💡 修正: ルートオブジェクトを削除
                Destroy(ai.gameObject);
                count++;
            }

            // 4. HatMask生成
            if (hatMaskPrefab != null)
            {
                Instantiate(hatMaskPrefab, playerPosition, Quaternion.identity);
                Debug.Log("Merge: HatMask Summoned!");
            }
            else
            {
                Debug.LogWarning("Merge: HatMask Prefab is not assigned!");
            }
        }
    }
}
