using System.Collections;
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

    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.5f; // イン/アウトにかかる時間
    [SerializeField] private float stayDuration = 2.0f;  // 画面中央に留まる時間
    // 画面外(右) -> 中央 -> 画面外(左)
    [SerializeField] private Vector2 startPos = new Vector2(1500, 0);
    [SerializeField] private Vector2 centerPos = new Vector2(0, 0);
    [SerializeField] private Vector2 endPos = new Vector2(-1500, 0);

    // 内部フラグ
    private bool announced2Min = false;
    private bool announced1Min = false;
    private bool announcedBoss = false;

    private float currentTime;
    private bool isTimerRunning = false;

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
            PlayAnnouncement(sprite2Min, seWarning);
        }
        else if (currentTime <= 60f && !announced1Min) // 残り1分 (60秒)
        {
            announced1Min = true;
            PlayAnnouncement(sprite1Min, seWarning);
        }
        else if (currentTime <= 0f && !announcedBoss) // 終了 (Boss)
        {
            announcedBoss = true;
            PlayAnnouncement(spriteBoss, seBoss);
            // タイマー停止（あるいはボス戦フェーズへ移行）
            // isTimerRunning = false; 
        }
    }

    private void PlayAnnouncement(Sprite sprite, AudioClip clip)
    {
        if (cutInImageUI == null) return;
        
        // 1. 画像セット
        if (sprite != null)
        {
            cutInImageUI.sprite = sprite;
            cutInImageUI.SetNativeSize(); // 画像サイズに合わせる
        }
        
        // 2. SE再生
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }

        // 3. アニメーション開始
        StopAllCoroutines();
        StartCoroutine(CutInSequence());
    }

    private IEnumerator CutInSequence()
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
        yield return new WaitForSeconds(stayDuration);

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
    }

    // デバッグ用: 強制的に時間をセットする
    public void DebugSetTime(float seconds)
    {
        currentTime = seconds;
        // フラグのリセットは状況によるが、テスト時は再生成するか手動リセットが必要
    }
}
