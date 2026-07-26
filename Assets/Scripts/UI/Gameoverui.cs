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
        //if (GameManager.Instance != null)
            return LocalizationManager.IsZH;
        //return fallbackLanguage == Language.ZH;
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
            resultText.text = reason switch
            {
                GameOverReason.Victory     => isZh ? "穿過驚濤駭浪，地平線終現曙光。踏上新大陸的土地，展開全新的篇章。\n提示： 表現出色！下次試著挑戰更遠的航程吧。"        
                                                    : "Reached the New World and started a new life.\nTIP: Well done! Try sailing even further next time.",
                GameOverReason.NoFood      => isZh ? "糧盡絕望，糧倉空空如也，全員終究化為海上的孤魂。\n提示： 船員每日皆需消耗口糧。適度裁減人員能降低消耗，避免全員覆滅。"  
                                                    : "Starved to death amidst the boundless sea. The pantry is empty; everyone has turned into drifting souls.\nTIP: Crew members consume rations daily. Reducing headcount lowers consumption and prevents total annihilation.",
                GameOverReason.CaptainDead    => isZh ? "戰力全無，海盜登船血洗，甲板上無人逃過一劫。\n提示： 海盜可不講道義。白兵戰時，船員就是船長最強的盾牌。"              
                                                    : "Captured and slaughtered by pirates due to a lack of defenders.\nTIP: Pirates have no code. During deck combat, crew members are your best meat shields.",
                GameOverReason.ShipSunk => isZh ? "船體解體，殘骸漂至荒島，餘生只能與孤島相伴。\n提示： 沒人能靠一片木板游過汪洋。請隨時留意船體耐久度。"            
                                                    : "The shattered ship wrecked upon a deserted island.\nTIP: No captain can swim across the ocean on a single plank. Always keep an eye on ship durability.",
                _ => ""
            };
        }
        else
        {
            // 備援文字：LocalizationManager 在本 Scene 不存在時使用
            resultText.text = reason switch
            {
                GameOverReason.Victory     => isZh ? "穿過驚濤駭浪，地平線終現曙光。踏上新大陸的土地，展開全新的篇章。\n提示： 表現出色！下次試著挑戰更遠的航程吧。"        
                                                    : "Reached the New World and started a new life.\nTIP: Well done! Try sailing even further next time.",
                GameOverReason.NoFood      => isZh ? "糧盡絕望，糧倉空空如也，全員終究化為海上的孤魂。\n提示： 船員每日皆需消耗口糧。適度裁減人員能降低消耗，避免全員覆滅。"  
                                                    : "Starved to death amidst the boundless sea. The pantry is empty; everyone has turned into drifting souls.\nTIP: Crew members consume rations daily. Reducing headcount lowers consumption and prevents total annihilation.",
                GameOverReason.CaptainDead    => isZh ? "戰力全無，海盜登船血洗，甲板上無人逃過一劫。\n提示： 海盜可不講道義。白兵戰時，船員就是船長最強的盾牌。"              
                                                    : "Captured and slaughtered by pirates due to a lack of defenders.\nTIP: Pirates have no code. During deck combat, crew members are your best meat shields.",
                GameOverReason.ShipSunk => isZh ? "船體解體，殘骸漂至荒島，餘生只能與孤島相伴。\n提示： 沒人能靠一片木板游過汪洋。請隨時留意船體耐久度。"            
                                                    : "The shattered ship wrecked upon a deserted island.\nTIP: No captain can swim across the ocean on a single plank. Always keep an eye on ship durability.",
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