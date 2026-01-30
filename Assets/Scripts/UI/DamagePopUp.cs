using UnityEngine;
using TMPro; // 💡 TextMeshProを使うために必要

public class DamagePopup : MonoBehaviour
{
    // TextMeshProコンポーネントへの参照
    [SerializeField] private TextMeshPro textMesh;

    // 演出パラメータ
    [SerializeField] float moveSpeed = 2.0f;
    [SerializeField] float lifeTime = 1.0f;

    [Header("Visuals")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color criticalColor = Color.yellow;
    [SerializeField] Color superCriticalColor = new Color(1f, 0.5f, 0f); // オレンジ
    [SerializeField] Color hyperCriticalColor = Color.red;

    // 💡 Step 8.5 変更: Enumを受け取って分岐
    public void Setup(int damage, CriticalType type)
    {
        textMesh.text = damage.ToString();

        switch (type)
        {
            case CriticalType.Normal:
                textMesh.color = normalColor;
                break;
            case CriticalType.Critical:
                textMesh.color = criticalColor;
                transform.localScale *= 1.5f;
                break;
            case CriticalType.SuperCritical:
                textMesh.color = superCriticalColor;
                transform.localScale *= 2.0f;
                break;
            case CriticalType.HyperCritical:
                textMesh.color = hyperCriticalColor;
                transform.localScale *= 2.5f;
                break;
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // 1. 上へ移動（ふわっと浮かぶ）
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. カメラの方向を向く（ビルボード）
        // これがないと文字が裏返ったりして読めない
        if (Camera.main != null)
        {
            // カメラと逆方向（自分 - カメラ）を向くのが定石
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}