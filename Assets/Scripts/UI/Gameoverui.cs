using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 獨立的「結局 Scene」UI。
/// GameManager 在 TriggerGameOver 時會把 reason 存進 GameOverContext，
/// 並切換到本 Scene；本腳本在 Start 時讀取 GameOverContext，
/// 依 reason 顯示對應圖片與文字，用 CanvasGroup 做淡入，按鈕回主選單。
///
/// 結構建議（掛在本 Scene 的根 Panel 上）：
///   GameOverPanel (掛此腳本 + CanvasGroup)
///     ├─ ResultImage (Image)
///     ├─ ResultText  (TextMeshProUGUI)
///     └─ BackButton  (Button) → BackLabel (TextMeshProUGUI)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GameOverUI : MonoBehaviour
{
    [Serializable]
    public class ReasonVisual
    {
        public GameOverReason reason;
        public Sprite image;
    }

    [Header("Result Display")]
    [SerializeField] private Image resultImage;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Reason → Sprite 對照")]
    [SerializeField] private List<ReasonVisual> reasonVisuals = new();

    [Header("Back To Main Menu Button")]
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI backLabel;
    [Tooltip("主選單場景名稱（需已加入 Build Settings）")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Fade In")]
    [SerializeField] private float fadeDuration = 0.6f;

    [Header("Fallback（找不到 GameOverContext 時使用）")]
    [SerializeField] private GameOverReason fallbackReason = GameOverReason.Victory;
    [SerializeField] private Language fallbackLanguage = Language.EN;

    private CanvasGroup canvasGroup;
    private Dictionary<GameOverReason, Sprite> visualLookup;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        visualLookup = new Dictionary<GameOverReason, Sprite>();
        foreach (var rv in reasonVisuals)
            visualLookup[rv.reason] = rv.image;

        if (backButton != null)
            backButton.onClick.AddListener(OnBackToMainMenu);
    }

    private void Start()
    {
        GameOverReason reason = GameOverContext.HasReason ? GameOverContext.Reason : fallbackReason;

        if (!GameOverContext.HasReason)
            Debug.LogWarning("[GameOverUI] 找不到 GameOverContext（可能是直接從編輯器開這個 Scene 測試），改用 fallbackReason。");

        ApplyVisual(reason);
        ApplyText(reason);
        UpdateBackLabel();

        StartCoroutine(FadeIn());

        GameOverContext.Clear();
    }

    // ── Visual / Text ─────────────────────────────────────

    private void ApplyVisual(GameOverReason reason)
    {
        if (resultImage == null) return;

        if (visualLookup.TryGetValue(reason, out var sprite) && sprite != null)
        {
            resultImage.sprite = sprite;
            resultImage.enabled = true;
        }
        else
        {
            resultImage.enabled = false;
            Debug.LogWarning($"[GameOverUI] 找不到 {reason} 對應的圖片，請在 Inspector 設定 Reason Visuals。");
        }
    }

    private bool IsZh()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.lang == Language.ZH;
        return fallbackLanguage == Language.ZH;
    }

    private void ApplyText(GameOverReason reason)
    {
        if (resultText == null) return;

        bool isZh = IsZh();

        string key = reason switch
        {
            GameOverReason.Victory     => "ending.victory",
            GameOverReason.NoFood      => "ending.nofood",
            GameOverReason.ShipSunk    => "ending.sunk",
            GameOverReason.CaptainDead => "ending.captaindead",
            _ => null
        };

        if (key != null && LocalizationManager.Instance != null)
        {
            resultText.text = LocalizationManager.Instance.Get(key);
        }
        else
        {
            // 備援文字：LocalizationManager 在本 Scene 不存在時使用
            resultText.text = reason switch
            {
                GameOverReason.Victory     => isZh ? "抵達新大陸！"        : "You reached the New World!",
                GameOverReason.NoFood      => isZh ? "糧食耗盡，全員餓死"  : "The crew starved at sea.",
                GameOverReason.ShipSunk    => isZh ? "船沉了"              : "The ship sank.",
                GameOverReason.CaptainDead => isZh ? "船長陣亡"            : "The captain has fallen.",
                _ => ""
            };
        }
    }

    private void UpdateBackLabel()
    {
        if (backLabel == null) return;
        backLabel.text = IsZh() ? "回主選單" : "Main Menu";
    }

    // ── Fade In ───────────────────────────────────────────

    private IEnumerator FadeIn()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    // ── Button ────────────────────────────────────────────

    private void OnBackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}