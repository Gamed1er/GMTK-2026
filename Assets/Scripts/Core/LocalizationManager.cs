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

    public Language CurrentLanguage { get; private set; } = Language.EN;

    /// <summary>快捷判斷：目前語言是否為中文</summary>
    public static bool IsZH => Instance != null && Instance.CurrentLanguage == Language.ZH;

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

        // 主選單
        ["menu.settings"]        = ("Settings",          "設定"),
        ["menu.language"]        = ("中文 / EN",          "中文 / EN"),
        ["menu.close"]           = ("Close",             "關閉"),
        ["menu.bgm"]             = ("BGM",               "背景音樂"),
        ["menu.sfx"]             = ("SFX",               "音效"),
        ["menu.play"]            = ("Game Start",        "開始遊戲"),
        ["menu.settingstitle"]   = ("Settings",          "設定"),
        ["menu.language.en"]     = ("Language(English)", "Language(English)"),
        ["menu.language.zh"]     = ("Language(中文)",    "Language(中文)"),

        // 製作者名單
        ["credits.studio"]       = ("Made by NCU Game Creator",              "本作品由中央大學創遊社製作"),
        ["credits.hayu"]         = ("HAYU — Lead Designer",                 "HAYU — 主企劃"),
        ["credits.huan"]         = ("Huan — Lead Artist, VFX",             "Huan — 主美術、特效"),
        ["credits.gdnb"]         = ("GDNB — Lead Programmer, Art Support",  "遊戲亡 — 主程式、副美術"),
        ["credits.ibu"]          = ("Ibuprofen — Associate Programmer",     "Ibuprofen — 副程式"),
        ["credits.dirty"]        = ("DirtyShow — Music Composer",           "髒雪 — 配樂"),
        ["credits.egg"]          = ("EggOWO — Tester",                      "雞蛋(EggOWO) — 測試"),
        // credits.thanks / credits.anthropic 已移除
    };

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
