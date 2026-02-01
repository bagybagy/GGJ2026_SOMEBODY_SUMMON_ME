using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.x Namespace

public class CameraIntroManager : MonoBehaviour
{
    [Header("Cinemachine Settings")]
    [SerializeField] private CinemachineCamera introCam; // 3.xではCinemachineVirtualCameraではなくCinemachineCamera
    [SerializeField] private CinemachineCamera playerCam; // 戻る先のカメラ（なければPriority制御のみで任せる）

    [Header("Target Settings")]
    [SerializeField] private Transform playerTransform; // 最終的に寄る場所
    [SerializeField] private Vector3 stageCenter = new Vector3(100f, 50f, 0f); // ステージ中央（LookAt用）

    [Header("Motion Settings")]
    [SerializeField] private float duration = 6.0f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // 空撮の軌道定義
    [SerializeField] private Vector3 startOffset = new Vector3(-200f, 150f, -200f); // Playerからの相対、あるいはWorld座標
    [SerializeField] private float flyHeight = 120f; // 飛行高度

    void Awake()
    {
        // プレイヤーがいなければ探す
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else Debug.LogWarning("CameraIntroManager: Player not found!");
        }

        // カメラ初期化（Awakeで確実に行う）
        if (introCam != null)
        {
            // Cinemachineの自動制御を切る（スクリプトで動かすため）
            introCam.Follow = null;
            introCam.LookAt = null;
            introCam.Priority = 1000;
            
            // 初期位置計算
            // 終了位置は「プレイヤーの足元」ではなく「プレイヤーを映すメインカメラの位置」にしたい
            Vector3 pEndPos = GetEndCameraPosition();
            
            // StartOffsetは "EndPosition" からのオフセットとして計算
            Vector3 pStart = pEndPos + startOffset; 
            
            introCam.transform.position = pStart;
            // 向きは一旦ステージ中央へ
            if(stageCenter != Vector3.zero) introCam.transform.LookAt(stageCenter);
        }
    }

    void Start()
    {
        // イントロ再生
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        if (introCam == null) yield break;

        float timer = 0f;

        // 座標定義
        // Camera End Position
        Vector3 pEndPos = GetEndCameraPosition();
        Vector3 pStart = pEndPos + startOffset; 
        
        // Look At Targets
        Vector3 lookStart = stageCenter;
        Vector3 lookEnd = playerTransform != null ? playerTransform.position + Vector3.up * 1.5f : Vector3.zero; // プレイヤーの胸元あたりを見る

        // 制御点: ステージ中央、かつ少し手前にずらして「旋回感」を出す
        Vector3 pMid = stageCenter;
        pMid.y = flyHeight; // 高さは維持

        Debug.Log($"Intro Start: {pStart} -> Mid: {pMid} -> End: {pEndPos}");

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rate = timer / duration;
            float t = speedCurve.Evaluate(rate);

            // 1. 位置の計算 (2次ベジェ曲線)
            Vector3 pos = CalculateBezier(t, pStart, pMid, pEndPos);

            // 2. カメラ位置を直接更新 (CinemachineのFollowを使わない)
            introCam.transform.position = pos;

            // 3. 注視点（回転）の計算
            // 最初はステージ全体(stageCenter)を見て、徐々にPlayer(pEnd)にフォーカスする
            Vector3 currentLookTarget = Vector3.Lerp(lookStart, lookEnd, t);
            introCam.transform.LookAt(currentLookTarget);
            
            yield return null;
        }

        Debug.Log("Intro Finished. Switching to Player Camera.");
        
        // ... (以下既存コードと同じ)
        
        // 終了処理
        introCam.Priority = -1; // 優先度を下げてPlayerカメラへブレンド開始

        // ブレンド時間待ってから無効化
        yield return new WaitForSeconds(2.0f);
        introCam.gameObject.SetActive(false);
        this.enabled = false;
    }

    // 終了時のカメラ位置を取得
    private Vector3 GetEndCameraPosition()
    {
        // 1. PlayerCamがあればその位置を使う（これが一番確実）
        if (playerCam != null) return playerCam.transform.position;
        
        // 2. なければMainCamera
        if (Camera.main != null) return Camera.main.transform.position;

        // 3. それもなければPlayer周辺の適当な位置を計算
        if (playerTransform != null)
        {
            // 背後上方
            return playerTransform.position + Vector3.up * 3.0f - playerTransform.forward * 5.0f; 
        }

        return Vector3.zero;
    }

    // 2次ベジェ曲線
    Vector3 CalculateBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2(1-t)t * P1
        p += tt * p2;        // t^2 * P2

        return p;
    }

    // 💡 ボス出現時のカメラフォーカス機能
    [Header("Boss Focus Settings")]
    [SerializeField] private float bossFocusDuration = 2.0f; // ボスにフォーカスする時間
    [SerializeField] private Vector3 bossCameraOffset = new Vector3(10f, 10f, 10f); // ボスからのカメラオフセット

    private Coroutine bossFocusCoroutine;

    /// <summary>
    /// ボスにカメラをフォーカスして、その後プレイヤーに戻る
    /// </summary>
    /// <param name="bossTransform">ボスのTransform</param>
    public void FocusOnBoss(Transform bossTransform)
    {
        Debug.Log("CameraIntroManager: FocusOnBoss called!");
        
        if (bossTransform == null)
        {
            Debug.LogWarning("CameraIntroManager: Boss Transform is null!");
            return;
        }

        if (introCam == null)
        {
            Debug.LogError("CameraIntroManager: Intro Camera is null! Cannot focus on boss.");
            return;
        }

        Debug.Log($"CameraIntroManager: Starting boss focus sequence. Boss at {bossTransform.position}");

        // コンポーネントが無効化されている場合は再有効化
        if (!enabled)
        {
            enabled = true;
            Debug.Log("CameraIntroManager: Re-enabled component for boss focus");
        }

        // 既存のフォーカスコルーチンがあれば停止
        if (bossFocusCoroutine != null)
        {
            StopCoroutine(bossFocusCoroutine);
        }

        bossFocusCoroutine = StartCoroutine(BossFocusSequence(bossTransform));
    }

    private IEnumerator BossFocusSequence(Transform bossTransform)
    {
        Debug.Log("CameraIntroManager: BossFocusSequence started");
        
        if (introCam == null)
        {
            Debug.LogError("CameraIntroManager: Intro Camera is null in sequence!");
            yield break;
        }

        // 1. イントロカメラを有効化して優先度を上げる
        introCam.gameObject.SetActive(true);
        introCam.Priority = 1000;
        Debug.Log($"CameraIntroManager: Intro camera activated with priority 1000");

        // 2. 現在のプレイヤーカメラ位置を保存
        Vector3 playerCamPos = GetEndCameraPosition();
        Vector3 playerLookTarget = playerTransform != null ? playerTransform.position + Vector3.up * 1.5f : Vector3.zero;

        // 3. ボスのカメラ位置と注視点を計算
        Vector3 bossCamPos = bossTransform.position + bossCameraOffset;
        Vector3 bossLookTarget = bossTransform.position + Vector3.up * 2.0f; // ボスの少し上を見る

        // 4. ボスへ移動（瞬間移動または短時間で移動）
        float moveToTime = 0.3f;
        float timer = 0f;

        while (timer < moveToTime)
        {
            timer += Time.deltaTime;
            float t = timer / moveToTime;

            introCam.transform.position = Vector3.Lerp(playerCamPos, bossCamPos, t);
            Vector3 currentLook = Vector3.Lerp(playerLookTarget, bossLookTarget, t);
            introCam.transform.LookAt(currentLook);

            yield return null;
        }

        introCam.transform.position = bossCamPos;
        introCam.transform.LookAt(bossLookTarget);

        // 5. ボスを映す（停止）
        yield return new WaitForSeconds(bossFocusDuration);

        // 6. プレイヤーカメラに戻る
        timer = 0f;
        float returnTime = 0.5f;

        while (timer < returnTime)
        {
            timer += Time.deltaTime;
            float t = timer / returnTime;

            introCam.transform.position = Vector3.Lerp(bossCamPos, playerCamPos, t);
            Vector3 currentLook = Vector3.Lerp(bossLookTarget, playerLookTarget, t);
            introCam.transform.LookAt(currentLook);

            yield return null;
        }

        // 7. 優先度を下げてプレイヤーカメラに切り替え
        introCam.Priority = -1;

        // 8. ブレンド完了後に無効化
        yield return new WaitForSeconds(1.0f);
        introCam.gameObject.SetActive(false);
    }
}
