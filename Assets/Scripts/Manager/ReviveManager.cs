using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveManager : MonoBehaviour
{
    public static ReviveManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject reviveEffectPrefab;
    [SerializeField] private float cooldownDuration = 30f; // クールダウン時間（秒）

    // 現在のクールダウン残り時間
    private float currentCooldown = 0f;

    // 外部公開用: クールダウンの進捗率 (0.0f = 使用可能, 1.0f = 直後)
    public float CooldownRatio 
    {
        get 
        {
            if (cooldownDuration <= 0f) return 0f;
            return Mathf.Clamp01(currentCooldown / cooldownDuration);
        }
    }

    // 外部公開用: 使用可能かどうか
    public bool CanRevive => currentCooldown <= 0f;

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
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
        }
    }

    // 全域蘇生
    public void ReviveAll()
    {
        // クールダウンチェック
        if (currentCooldown > 0f)
        {
            Debug.Log($"Revive is on cooldown! Remaining: {currentCooldown:F1}s");
            return;
        }

        Debug.Log("Global Revive Activated!");
        
        // クールダウン開始
        currentCooldown = cooldownDuration;
        
        // シーン内の全Allyを検索
        // FindGameObjectsWithTagは非アクティブなオブジェクトを見つけられない場合があるが、
        // Dizzy状態でもGameObject自体はActiveで、ComponentだけDisableなら見つかる。
        // もしGameObjectをDisableしているなら見つからない。
        // 仕様: "AllyAI コンポーネントを enabled = false" -> GameObjectはActive。
        GameObject[] allies = GameObject.FindGameObjectsWithTag("Ally");

        foreach (var allyObj in allies)
        {
            AllyAI allyAI = allyObj.GetComponent<AllyAI>();
            StatusManager status = allyObj.GetComponent<StatusManager>();

            if (allyAI != null && status != null)
            {
                // Dizzy状態かチェック
                if (allyAI.IsDizzy())
                {
                    // 蘇生処理
                    status.Resurrect(); // HP全快 & isDead解除
                    allyAI.Revive();    // AI再開
                    
                    // VFXリセット
                    VFXDamageFeedback vfx = allyObj.GetComponent<VFXDamageFeedback>();
                    if (vfx != null)
                    {
                        vfx.Resurrect();
                    }
                    
                    // エフェクト
                    if (reviveEffectPrefab != null)
                    {
                        Instantiate(reviveEffectPrefab, allyObj.transform.position, Quaternion.identity);
                    }
                }
            }
        }
    }
}
