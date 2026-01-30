using UnityEngine;

public class GoalController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Payloadがゴールに入ったら
        if (other.CompareTag("Payload"))
        {
            // 💡 シングルトンなので、GetComponent不要でいきなり呼べる！
            GameManager.Instance.GameClear();
        }
    }
}