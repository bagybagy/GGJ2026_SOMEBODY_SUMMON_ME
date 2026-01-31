using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveManager : MonoBehaviour
{
    public static ReviveManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject reviveEffectPrefab;

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

    // 全域蘇生
    public void ReviveAll()
    {
        Debug.Log("Global Revive Activated!");
        
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
