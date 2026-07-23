using UnityEngine;
using System;
using System.Collections.Generic;

public enum Language { EN, ZH }

/// <summary>
/// 輕量級雙語系統（中文/英文）
/// Lightweight bilingual system (Chinese / English)
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public Language CurrentLanguage { get; private set; } = Language.ZH;

    public event Action OnLanguageChanged;

    // ── String Table ──────────────────────────────────────
    // key → (EN, ZH)
    private static readonly Dictionary<string, (string en, string zh)> table = new()
    {
        // 小遊戲名稱
        ["minigame.steering"]    = ("Steer Ship",        "開船"),
        ["minigame.fishing"]     = ("Fishing",           "釣魚"),
        ["minigame.repairnet"]   = ("Repair Net",        "補網子"),
        ["minigame.pirate"]      = ("Fight Pirates",     "打海盜"),
        ["minigame.patchhole"]   = ("Patch Hole",        "補木板洞"),
        ["minigame.fire"]        = ("Extinguish Fire",   "滅火"),

        // 小遊戲狀態
        ["status.inprogress"]    = ("Crew Working",      "船員進行中"),
        ["status.needcrew"]      = ("Need More Crew",    "沒有足夠船員"),

        // UI 資源標籤
        ["ui.food"]              = ("Food",              "食物"),
        ["ui.crew"]              = ("Crew",              "船員"),
        ["ui.shiphp"]            = ("Ship HP",           "船隻耐久"),
        ["ui.navprogress"]       = ("Progress",          "航行進度"),
        ["ui.fooddays"]          = ("Days of food left", "糧食還可撐"),
        ["ui.day"]               = ("Day",               "第"),
        ["ui.days"]              = ("days",              "天"),

        // 結局
        ["ending.victory"]       = ("You reached the New World!",   "抵達新大陸！"),
        ["ending.nofood"]        = ("The crew starved at sea.",     "糧食耗盡，全員餓死"),
        ["ending.sunk"]          = ("The ship sank.",               "船沉了"),
        ["ending.captaindead"]   = ("The captain has fallen.",      "船長陣亡"),

        // 晚上事件按鈕
        ["night.continue"]       = ("Continue",          "繼續航行"),
    };

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────

    public string Get(string key)
    {
        if (!table.TryGetValue(key, out var pair))
        {
            Debug.LogWarning($"[Localization] Missing key: {key}");
            return $"[{key}]";
        }
        return CurrentLanguage == Language.EN ? pair.en : pair.zh;
    }

    public void SetLanguage(Language lang)
    {
        CurrentLanguage = lang;
        OnLanguageChanged?.Invoke();
    }

    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == Language.EN ? Language.ZH : Language.EN);
    }

    /// <summary>依 MinigameType 取得名稱</summary>
    public string GetMinigameName(MinigameType type)
    {
        string key = type switch
        {
            MinigameType.Steering    => "minigame.steering",
            MinigameType.Fishing     => "minigame.fishing",
            MinigameType.RepairNet   => "minigame.repairnet",
            MinigameType.FightPirate => "minigame.pirate",
            MinigameType.PatchHole   => "minigame.patchhole",
            MinigameType.Firefighting => "minigame.fire",
            _ => "?"
        };
        return Get(key);
    }
}
