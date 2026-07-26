using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 主選單 UI 控制器。
///
/// 場景結構（建議）：
/// Canvas
/// ├─ PlayButton          (Button + TMP)
/// ├─ SettingsButton      (Button + TMP)
/// ├─ LanguageButton      (Button + TMP)
/// └─ SettingsPanel       (GameObject，預設隱藏)
///    ├─ BGMLabel         (TMP)
///    ├─ BGMSlider        (Slider，0~1)
///    ├─ SFXLabel         (TMP)
///    ├─ SFXSlider        (Slider，0~1)
///    ├─ CreditsText      (TMP)
///    └─ CloseButton      (Button + TMP)
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("主選單按鈕")]
    [SerializeField] private Button          playButton;
    [SerializeField] private TextMeshProUGUI playLabel;
    [SerializeField] private Button          settingsButton;
    [SerializeField] private TextMeshProUGUI settingsLabel;
    [SerializeField] private Button          languageButton;
    [SerializeField] private TextMeshProUGUI languageLabel;

    [Header("設定面板")]
    [SerializeField] private GameObject      settingsPanel;
    [SerializeField] private TextMeshProUGUI settingsTitleLabel;
    [SerializeField] private TextMeshProUGUI bgmLabel;
    [SerializeField] private Slider          bgmSlider;
    [SerializeField] private TextMeshProUGUI sfxLabel;
    [SerializeField] private Slider          sfxSlider;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private Button          closeButton;
    [SerializeField] private TextMeshProUGUI closeLabel;

    [Header("場景")]
    [SerializeField] private string gameSceneName = "Main";

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()
    {
        settingsPanel.SetActive(false);

        // 按鈕綁定
        playButton    .onClick.AddListener(OnPlay);
        settingsButton.onClick.AddListener(OnOpenSettings);
        languageButton.onClick.AddListener(OnToggleLanguage);
        closeButton   .onClick.AddListener(OnCloseSettings);

        // 滑桿初始值
        if (AudioVolumeManager.Instance != null)
        {
            bgmSlider.value = AudioVolumeManager.Instance.BGMVolume;
            sfxSlider.value = AudioVolumeManager.Instance.SFXVolume;
        }
        bgmSlider.onValueChanged.AddListener(v => AudioVolumeManager.Instance?.SetBGM(v));
        sfxSlider.onValueChanged.AddListener(v => AudioVolumeManager.Instance?.SetSFX(v));

        // 語言變更時刷新所有文字
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshAll;

        RefreshAll();
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshAll;
    }

    // ── Handlers ──────────────────────────────────────────

    private void OnPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnOpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    private void OnCloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    private void OnToggleLanguage()
    {
        LocalizationManager.Instance?.ToggleLanguage();
        // GameManager.Instance?.lang 在遊戲場景才存在，主選單同步 LocalizationManager 即可
    }

    // ── Refresh ───────────────────────────────────────────

    private void RefreshAll()
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return;

        // 主選單按鈕
        if (playLabel     != null) playLabel    .text = loc.Get("menu.play");
        if (settingsLabel != null) settingsLabel.text = loc.Get("menu.settings");

        // 語言按鈕：顯示目前選中的語言
        string langKey = LocalizationManager.IsZH ? "menu.language.zh" : "menu.language.en";
        if (languageLabel != null) languageLabel.text = loc.Get(langKey);

        // 設定面板標籤
        if (settingsTitleLabel != null) settingsTitleLabel.text = loc.Get("menu.settingstitle");
        if (bgmLabel   != null) bgmLabel  .text = loc.Get("menu.bgm");
        if (sfxLabel   != null) sfxLabel  .text = loc.Get("menu.sfx");
        if (closeLabel != null) closeLabel.text = loc.Get("menu.close");

        // 製作者名單
        if (creditsText != null)
            creditsText.text = BuildCredits(loc);
    }

    private static string BuildCredits(LocalizationManager loc)
    {
        return
            $"{loc.Get("credits.studio")}\n\n" +
            $"{loc.Get("credits.hayu")}\n" +
            $"{loc.Get("credits.huan")}\n" +
            $"{loc.Get("credits.gdnb")}\n" +
            $"{loc.Get("credits.ibu")}\n" +
            $"{loc.Get("credits.dirty")}";
    }
}
