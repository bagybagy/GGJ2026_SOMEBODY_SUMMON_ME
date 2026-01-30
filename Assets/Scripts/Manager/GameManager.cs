using UnityEngine;
using UnityEngine.SceneManagement; // シーン操作（リロード）に必要
using TMPro; // UI操作に必要
using System.Collections;

// 💡 ゲーム全体の進行（勝ち負け）を管理するクラス
// このスクリプトは、シーン内に「ただ1つ」しか存在してはいけない（シングルトン）
public class GameManager : MonoBehaviour
{
    // ========================================================================
    // 👑 シングルトン（Singleton）パターンの実装
    // ========================================================================

    // 「static」をつけると、この変数は「このクラスの全インスタンスで共有」されます。
    // つまり、どこからでも「GameManager.Instance」と書くだけで、このスクリプトにアクセスできるようになります。
    public static GameManager Instance;

    // AwakeはStartより前、ゲームが始まった瞬間に呼ばれます
    void Awake()
    {
        // 1. もし、まだ誰も「管理者」として登録されていなければ...
        if (Instance == null)
        {
            // 自分自身（this）を管理者として登録する
            Instance = this;
        }
        // 2. もし、既に別の管理者が存在していたら...（重複対策）
        else
        {
            // 自分は不要なので、即座に消滅する
            // これにより、世界にGameManagerが1つしかない状態を保証する
            Destroy(gameObject);
        }
    }
    // ========================================================================


    [Header("UI References")]
    [SerializeField] GameObject gameOverUI;  // 負けた時の文字
    [SerializeField] GameObject gameClearUI; // 勝った時の文字

    [Header("Settings")]
    [SerializeField] float waitSecondsBeforeReload = 5.0f; // 終了からリロードまでの余韻

    // ゲームが終わったかどうかのフラグ（二回死んだりしないように）
    private bool isGameEnded = false;

    void Start()
    {
        // --- 敗北条件の監視設定 ---

        // 1. プレイヤーを探して、死んだら教えてもらうようにする
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var status = player.GetComponent<StatusManager>();
            if (status != null)
            {
                // 「GameOver」メソッドをイベントに登録（購読）
                status.OnDead += GameOver;
            }
        }

        // 2. ペイロードを探して、死んだら教えてもらうようにする
        GameObject payload = GameObject.FindWithTag("Payload");
        if (payload != null)
        {
            var status = payload.GetComponent<StatusManager>();
            if (status != null)
            {
                status.OnDead += GameOver;
            }
        }
    }

    // 💀 ゲームオーバー処理（StatusManagerから呼ばれる）
    public void GameOver()
    {
        // 既に終わっていたら何もしない（多重呼び出し防止）
        if (isGameEnded) return;
        isGameEnded = true;

        Debug.Log("Game Over...");

        // UIを表示
        if (gameOverUI != null) gameOverUI.SetActive(true);

        // 数秒後に最初からやり直す
        StartCoroutine(ReloadScene());
    }

    // 🎉 ゲームクリア処理（ゴール地点から呼ばれる）
    public void GameClear()
    {
        if (isGameEnded) return;
        isGameEnded = true;

        Debug.Log("Mission Complete!");

        // UIを表示
        if (gameClearUI != null) gameClearUI.SetActive(true);

        // 数秒後にリロード（本来は次のステージへ）
        StartCoroutine(ReloadScene());
    }

    // シーン再読み込みコルーチン
    IEnumerator ReloadScene()
    {
        // 余韻を楽しむために待つ
        yield return new WaitForSeconds(waitSecondsBeforeReload);

        // 現在のシーン名を取得して、読み込み直す
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}