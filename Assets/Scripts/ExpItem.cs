using UnityEngine;

public class ExpItem : MonoBehaviour
{
    [SerializeField] private int expAmount = 50; // 獲得経験値
    [SerializeField] GameObject mainObject; // 本体の親オブジェクトをアタッチ
    [SerializeField] GameObject pickupSE;   //SE

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーだけが拾える
        if (other.CompareTag("Player"))
        {
            var status = other.GetComponent<StatusManager>();
            if (status != null)
            {
                status.AddExp(expAmount);

                // SE生成
                if (pickupSE != null) Instantiate(pickupSE, transform.position, Quaternion.identity);

                Destroy(mainObject); // アイテム消滅
            }
        }
    }
}