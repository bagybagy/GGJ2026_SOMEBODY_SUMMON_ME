using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For LINQ

// 💡 マージレシピ: どのレベルのAllyを何体集めて、何を生成するか
[System.Serializable]
public class MergeRecipe
{
    [Tooltip("合体対象のAllyレベル (AllyAI.mergeLevel)")]
    public int targetLevel = 0;
    
    [Tooltip("必要な数")]
    public int requiredCount = 10;
    
    [Tooltip("生成するPrefab")]
    public GameObject resultPrefab;
    
    [Tooltip("合体範囲 (プレイヤーからの距離)")]
    public float mergeRange = 10f;
}

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance { get; private set; }

    [Header("Merge Recipes")]
    [SerializeField] private List<MergeRecipe> mergeRecipes = new List<MergeRecipe>();

    [Header("Visual Effects")]
    [SerializeField] private GameObject mergeEffectPrefab; // 合体時の煙エフェクト
    [SerializeField] private float spawnYOffset = 0.5f; // 生成時の高さ調整

    [Header("Auto Merge")]
    [SerializeField] private bool autoMerge = true; // 自動合体フラグ

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
        // 自動合体が有効なら常時チェック
        if (autoMerge)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // 全てのレシピをチェック
                foreach (var recipe in mergeRecipes)
                {
                    TryMerge(player.transform.position, recipe);
                }
            }
        }
    }

    // プレイヤーから呼ばれる（または自動）
    public void TryMerge(Vector3 playerPosition, MergeRecipe recipe)
    {
        if (recipe == null || recipe.resultPrefab == null) return;

        // 1. 範囲内の有効なAllyを探す
        GameObject[] allAllies = GameObject.FindGameObjectsWithTag("Ally");
        
        HashSet<AllyAI> candidates = new HashSet<AllyAI>();

        foreach (var obj in allAllies)
        {
            // 親を辿ってAllyAIを探す（Hitbox対策）
            AllyAI ai = obj.GetComponentInParent<AllyAI>();
            
            // AIがない、または既にリストにあるならスキップ
            if (ai == null || candidates.Contains(ai)) continue;

            // 指定したマージレベルでなければ除外
            if (ai.mergeLevel != recipe.targetLevel) continue;

            // 距離チェック
            if (Vector3.Distance(ai.transform.position, playerPosition) > recipe.mergeRange) continue;

            // Dizzy状態なら除外
            if (ai.IsDizzy()) continue;

            // 生存確認（念のため）
            if (!ai.gameObject.activeInHierarchy) continue;

            candidates.Add(ai);
        }

        // 2. 数が足りているかチェック
        if (candidates.Count >= recipe.requiredCount)
        {
            Debug.Log($"Merge: Requirements Met! Merging {recipe.requiredCount} Lv{recipe.targetLevel} allies...");

            // 3. 必要数だけ選出して削除
            int count = 0;
            Vector3 averagePosition = Vector3.zero;
            
            foreach (var ai in candidates)
            {
                if (count >= recipe.requiredCount) break;

                averagePosition += ai.transform.position;

                // エフェクト生成 (煙など)
                if (mergeEffectPrefab != null)
                {
                    Instantiate(mergeEffectPrefab, ai.transform.position, Quaternion.identity);
                }
                
                Destroy(ai.gameObject);
                count++;
            }

            // 平均位置を計算（合体した場所の中心）
            if (count > 0)
            {
                averagePosition /= count;
            }
            else
            {
                averagePosition = playerPosition;
            }

            // 4. 結果Prefabを生成
            Vector3 spawnPos = averagePosition + Vector3.up * spawnYOffset;
            GameObject result = Instantiate(recipe.resultPrefab, spawnPos, Quaternion.identity);
            
            Debug.Log($"Merge: Created {recipe.resultPrefab.name} at level {recipe.targetLevel + 1}!");
        }
    }

    // 💡 外部から特定レベルのマージを手動で呼び出す用
    public void TryMergeLevel(Vector3 playerPosition, int targetLevel)
    {
        MergeRecipe recipe = mergeRecipes.Find(r => r.targetLevel == targetLevel);
        if (recipe != null)
        {
            TryMerge(playerPosition, recipe);
        }
    }
}
