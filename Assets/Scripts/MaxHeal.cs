using UnityEngine;

public class MaxHeal : MonoBehaviour
{
    [SerializeField] private int expAmount = 50; // 獲得経験値
    [SerializeField] GameObject mainObject; // 本体の親オブジェクトをアタッチ
    [SerializeField] GameObject pickupSE;   //SE
    [Header("DefaultTag")]
    [SerializeField] private string defaultTag = "Payload"; // デフォルトの拾う対象

    private void OnTriggerEnter(Collider other)
    {
        // 特定タグだけが拾える
        if (other.CompareTag(defaultTag))
        {
            var status = other.GetComponent<StatusManager>();
            if (status != null)
            {
                status.MaxHeal();

                // SE生成
                if (pickupSE != null) Instantiate(pickupSE, transform.position, Quaternion.identity);

                Destroy(mainObject); // アイテム消滅
            }
        }
    }
}