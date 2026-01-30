using UnityEngine;

// Visualsオブジェクトにアタッチし、アニメーションイベントを親の制御スクリプトに橋渡しする
public class VisualsEventSender : MonoBehaviour
{
    // 親オブジェクトのPlayerスクリプトへの参照
    private Player playerController;

    void Start()
    {
        // 親のGameObjectからPlayerスクリプトを取得する
        // VisualsオブジェクトがPlayer Rootの直下の子である前提
        if (transform.parent != null)
        {
            playerController = GetComponentInParent<Player>();
            if (playerController == null)
            {
                Debug.LogError("親オブジェクト (Player Root) に Player コンポーネントが見つかりませんでした。");
            }
        }
        else
        {
            Debug.LogError("VisualsEventSender が Player Root の子オブジェクトにアタッチされていません。");
        }
    }

    // アニメーションイベントから呼ばれる関数：コライダー有効化をPlayerスクリプトに依頼
    // (アニメーションイベント名を 'OnAttackStart' などと設定)
    public void OnAttackStart()
    {
        if (playerController != null)
        {
            // Playerスクリプト内のコライダー有効化メソッドを呼び出す
            playerController.AttackColliderOn();
        }
    }

    // アニメーションイベントから呼ばれる関数：コライダー無効化をPlayerスクリプトに依頼
    // (アニメーションイベント名を 'OnAttackEnd' などと設定)
    public void OnAttackEnd()
    {
        if (playerController != null)
        {
            // Playerスクリプト内のコライダー無効化メソッドを呼び出す
            playerController.AttackColliderOff();
        }
    }
    
    // 必要に応じて、他のイベント（足音、エフェクト生成など）もここに追加できます。
}