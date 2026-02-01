using UnityEngine;

// 💡 落下死・床抜け対策スクリプト
public class FallKillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーの場合はゲームオーバー（リスタート）
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null) GameManager.Instance.GameOver();
            return;
        }

        // それ以外（エネミー、弾など）は削除
        // 階層構造がない前提でシンプルに root を削除
        if (other.gameObject.transform.root != null)
        {
            Destroy(other.gameObject.transform.root.gameObject);
        }
    }
}
