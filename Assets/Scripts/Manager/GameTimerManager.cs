using System.Collections;
using System.Collections.Generic; // 💡 追加
using UnityEngine;
using UnityEngine.UI;

// 💡 3分間のゲームカウントダウンと、カットイン演出を管理
public class GameTimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameTime = 180f; // 3分 = 180秒

    [Header("UI References")]
    [SerializeField] private Image cutInImageUI;      // 表示するImageコンポーネント (Canvas内)
    [SerializeField] private RectTransform cutInRect; // そのRectTransform (移動制御用)
    
    [Header("Cut-in Assets")]
    [SerializeField] private Sprite sprite2Min; // 残り2分
    [SerializeField] private Sprite sprite1Min; // 残り1分
    [SerializeField] private Sprite spriteBoss; // ボス登場（0秒）

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip seWarning;
    [SerializeField] private AudioClip seBoss;

    [Header("Boss Spawn Settings")]
    [SerializeField] private GameObject bossPrefab;           // ボスのPrefab
    [SerializeField] private Transform bossSpawnPoint;        // 出現位置
    [SerializeField] private Vector3 bossSpawnOffset = Vector3.zero; // 位置オフセット（SpawnPointがない場合用）
    [SerializeField] private CameraIntroManager cameraIntroManager; // カメラフォーカス用
    [SerializeField] private float bossBannerTimeOffset = 0f; // ボスバナーを表示するタイミングのオフセット（0=3分ちょうど、5=5秒前）

    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.5f; // イン/アウトにかかる時間
    [SerializeField] private float stayDuration = 2.0f;  // ボス時などに画面中央に留まる時間
    [SerializeField] private float stayDurationShort = 1.0f; // 残り2分などの短い表示時間
    // 画面外(右) -> 中央 -> 画面外(左)
    [SerializeField] private Vector2 startPos = new Vector2(1500, 0);
    [SerializeField] private Vector2 centerPos = new Vector2(0, 0);
    [SerializeField] private Vector2 endPos = new Vector2(-1500, 0);

    // 内部フラグ
    private bool announced2Min = false;
    private bool announced1Min = false;
    private bool announcedBoss = false;
    private bool bossSpawned = false; // ボス生成済みフラグ

    private float currentTime;
    private bool isTimerRunning = false;

    [Header("Idle Operation Guide")]
    [SerializeField] private GameObject operationUI; // 操作ガイド全体の親
    [SerializeField] private Image operationImage;   // 切り替え表示する画像
    [SerializeField] private List<Sprite> operationSprites; // ランダム画像のリスト
    [SerializeField] private float idleThreshold = 5.0f; // 放置判定時間
    [SerializeField] private float imageCycleInterval = 2.0f; // 画像切り替え間隔
    
    // Idle UI用
    private bool isIdleStats = false;
    private CanvasGroup opCanvasGroup;
    private Coroutine imageCycleCoroutine;

    void Start()
    {
        currentTime = gameTime;
        isTimerRunning = true;

        if (cutInRect == null && cutInImageUI != null)
        {
            cutInRect = cutInImageUI.GetComponent<RectTransform>();
        }

        // 初期化: 画面外へ
        if (cutInRect != null)
        {
            cutInRect.anchoredPosition = startPos;
        }
        if (cutInImageUI != null)
        {
            cutInImageUI.enabled = false; // 見えないようにしておく
        }
        
        // Idle UI初期化
        if (operationUI != null)
        {
            opCanvasGroup = operationUI.GetComponent<CanvasGroup>();
            if (opCanvasGroup == null) opCanvasGroup = operationUI.AddComponent<CanvasGroup>();
            opCanvasGroup.alpha = 0f; // 最初は隠す
            operationUI.SetActive(false);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;

        // タイマーチェック
        if (currentTime <= 120f && !announced2Min) // 残り2分 (120秒)
        {
            announced2Min = true;
            PlayAnnouncement(sprite2Min, seWarning, stayDurationShort);
        }
        
        if (currentTime <= 60f && !announced1Min) // 残り1分 (60秒)
        {
            announced1Min = true;
            PlayAnnouncement(sprite1Min, seWarning, stayDurationShort);
        }

        // ボスバナー表示チェック
        if (currentTime <= bossBannerTimeOffset && !announcedBoss)
        {
            announcedBoss = true;
            PlayAnnouncement(spriteBoss, seBoss, stayDuration);
        }

        // ボス生成チェック
        if (currentTime <= 0f && !bossSpawned)
        {
            bossSpawned = true;
            SpawnBoss();
            
            // タイマー停止（あるいはボス戦フェーズへ移行）
            // isTimerRunning = false; 
        }
        
        // 放置チェック
        CheckIdleState();
    }

    // 放置判定とUI制御
    private void CheckIdleState()
    {
        // 経過時間計算
        float timeSinceInput = Time.time - Player.LastInputTime;

        if (timeSinceInput >= idleThreshold)
        {
            // 放置状態へ
            if (!isIdleStats)
            {
                isIdleStats = true;
                ShowIdleUI();
            }
        }
        else
        {
            // 操作中
            if (isIdleStats)
            {
                isIdleStats = false;
                HideIdleUI();
            }
        }
    }

    private void ShowIdleUI()
    {
        if (operationUI == null) 
        {
            Debug.LogWarning("GameTimerManager: operationUI is not assigned in Inspector!");
            return;
        }
        operationUI.SetActive(true);
        
        // 特定のコルーチンだけを安全に停止
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeUI(1.0f));

        if (imageCycleCoroutine != null) StopCoroutine(imageCycleCoroutine);
        imageCycleCoroutine = StartCoroutine(CycleImages());
    }

    private void HideIdleUI()
    {
        if (operationUI == null) return;
        
        if (imageCycleCoroutine != null) StopCoroutine(imageCycleCoroutine);
        
        // フェードアウト
        StartCoroutine(FadeUI(0.0f, () => {
            operationUI.SetActive(false);
        }));
    }

    private IEnumerator FadeUI(float targetAlpha, System.Action onComplete = null)
    {
        if (opCanvasGroup == null) yield break;
        float startAlpha = opCanvasGroup.alpha;
        float t = 0f;
        while(t < 0.5f)
        {
            t += Time.deltaTime;
            opCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / 0.5f);
            yield return null;
        }
        opCanvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    private IEnumerator CycleImages()
    {
        if (operationImage == null)
        {
            Debug.LogError("Operation Image is null!");
            yield break;
        }
        if (operationSprites == null || operationSprites.Count == 0)
        {
            Debug.LogError("Operation Sprites list is empty/null!");
            yield break;
        }

        Debug.Log($"Starting CycleImages. Sprite Count: {operationSprites.Count}");

        while (true)
        {
            // ランダム選択
            int index = Random.Range(0, operationSprites.Count);
            Sprite sprite = operationSprites[index];
            if (sprite == null) Debug.LogWarning($"Sprite at index {index} is null!");

            operationImage.sprite = sprite;
            operationImage.SetNativeSize(); // 必要なら
            
            yield return new WaitForSeconds(imageCycleInterval);
        }
    }

    // Coroutine tracking
    private Coroutine cutInCoroutine;
    private Coroutine fadeCoroutine;

    private void PlayAnnouncement(Sprite sprite, AudioClip clip, float displayTime)
    {
        if (cutInImageUI == null) return;
        
        // 1. 画像セット
        if (sprite != null)
        {
            cutInImageUI.sprite = sprite;
            cutInImageUI.SetNativeSize(); 
        }
        
        // 2. SE再生
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
 
        // 3. アニメーション開始
        if (cutInCoroutine != null) StopCoroutine(cutInCoroutine);
        cutInCoroutine = StartCoroutine(CutInSequence(displayTime));
    }

    private IEnumerator CutInSequence(float displayTime)
    {
        cutInImageUI.enabled = true;
        
        // --- Slide In (EaseOut) ---
        float timer = 0f;
        while (timer < slideDuration)
        {
            timer += Time.deltaTime;
            float t = timer / slideDuration;
            // EaseOutCubic: 1 - (1-t)^3
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            
            cutInRect.anchoredPosition = Vector2.Lerp(startPos, centerPos, ease);
            yield return null;
        }
        cutInRect.anchoredPosition = centerPos;

        // --- Stay ---
        yield return new WaitForSeconds(displayTime);

        // --- Slide Out (EaseIn) ---
        timer = 0f;
        while (timer < slideDuration)
        {
            timer += Time.deltaTime;
            float t = timer / slideDuration;
            // EaseInCubic: t^3
            float ease = t * t * t;
            
            cutInRect.anchoredPosition = Vector2.Lerp(centerPos, endPos, ease);
            yield return null;
        }
        cutInRect.anchoredPosition = endPos;

        cutInImageUI.enabled = false;
        cutInCoroutine = null;
    }

    // デバッグ用: 強制的に時間をセットする
    public void DebugSetTime(float seconds)
    {
        currentTime = seconds;
        // フラグのリセットは状況によるが、テスト時は再生成するか手動リセットが必要
    }

    // 💡 ボス生成処理
    private void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("GameTimerManager: Boss Prefab is not assigned!");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;

        if (bossSpawnPoint != null)
        {
            // SpawnPointが指定されている場合
            spawnPosition = bossSpawnPoint.position;
            spawnRotation = bossSpawnPoint.rotation;
        }
        else
        {
            // SpawnPointがない場合はオフセットを使用
            spawnPosition = bossSpawnOffset;
        }

        GameObject boss = Instantiate(bossPrefab, spawnPosition, spawnRotation);
        Debug.Log($"Boss spawned at {spawnPosition}");

        // カメラフォーカス
        if (cameraIntroManager != null && boss != null)
        {
            cameraIntroManager.FocusOnBoss(boss.transform);
        }
    }
}
