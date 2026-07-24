using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一鍵建立六個夜晚事件 ScriptableObject。
/// 選單：Tools → Generate Night Event Assets
/// </summary>
public static class NightEventDataGenerator
{
    private const string OutputFolder = "Assets/ScriptableObjects/NightEvents";

    [MenuItem("Tools/Generate Night Event Assets")]
    public static void Generate()
    {
        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_AttackShip",
            type        = NightEventType.AttackShip,
            titleZH     = "攻擊其他船",
            titleEN     = "Attack Another Ship",
            descZH      = "前方出現一艘商船，你的手下躍躍欲試。\n是否派幾名船員去搶奪食糧？\n\n[同意] 食物 +120 / 船員 -2\n[拒絕] 無影響",
            descEN      = "A merchant vessel appears ahead. Your crew is eager.\nSend some hands to raid their supplies? Casualties are expected.\n\n[Accept] Food +120 / Crew -2\n[Decline] No effect",
            acceptLZH   = "出擊",
            acceptLEN   = "Attack",
            rejectLZH   = "放棄",
            rejectLEN   = "Pass",
            acceptDelta = new ResourceDelta { food = 120, crew = -2 },
            canReject   = true,
        });

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_HireRepairman",
            type        = NightEventType.HireRepairman,
            titleZH     = "修船工",
            titleEN     = "Ship Repairman",
            descZH      = "一名流浪修船工求搭便船，聲稱願意以修船換取食物補給。\n\n[接受] 船體 +30 / 食物 -60\n[拒絕] 無影響",
            descEN      = "A wandering repairman seeks passage and offers to patch the hull in exchange for food.\n\n[Accept] Ship HP +30 / Food -60\n[Decline] No effect",
            acceptLZH   = "接受",
            acceptLEN   = "Accept",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { shipHP = 30f, food = -60 },
            canReject   = true,
        });

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_SellPartsForCrew",
            type        = NightEventType.SellPartsForCrew,
            titleZH     = "賣零件換人",
            titleEN     = "Trade Parts for Crew",
            descZH      = "附近港口有失業水手，但他們要求以船上零件作為雇傭費。\n\n[交易] 船員 +3 / 船體 -20\n[拒絕] 無影響",
            descEN      = "Unemployed sailors at a nearby port demand ship parts as payment to join your crew.\n\n[Trade] Crew +3 / Ship HP -20\n[Decline] No effect",
            acceptLZH   = "交易",
            acceptLEN   = "Trade",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { crew = 3, shipHP = -20f },
            canReject   = true,
        });

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_SellPartsForFood",
            type        = NightEventType.SellPartsForFood,
            titleZH     = "賣零件換食物",
            titleEN     = "Trade Parts for Food",
            descZH      = "港口商人願意用大量食物換取你船上的備用零件。\n\n[出售] 食物 +100 / 船體 -15\n[拒絕] 無影響",
            descEN      = "A port merchant offers a bulk of food in exchange for your spare ship parts.\n\n[Sell] Food +100 / Ship HP -15\n[Decline] No effect",
            acceptLZH   = "出售",
            acceptLEN   = "Sell",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { food = 100, shipHP = -15f },
            canReject   = true,
        });

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_TradeCrewForFood",
            type        = NightEventType.TradeCrewForFood,
            titleZH     = "拿人換食物",
            titleEN     = "Trade Crew for Food",
            descZH      = "島上土著願意用大量食物換取一名船員定居，說是「聯姻」。\n\n[同意] 食物 +80 / 船員 -1\n[拒絕] 無影響",
            descEN      = "Island natives offer plentiful food for one crew member to settle with them — they call it \"marriage.\"\n\n[Agree] Food +80 / Crew -1\n[Decline] No effect",
            acceptLZH   = "同意",
            acceptLEN   = "Agree",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { food = 80, crew = -1 },
            canReject   = true,
        });

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_CrewAsBomb",
            type        = NightEventType.CrewAsBomb,
            titleZH     = "炸藥事故",
            titleEN     = "Human Torpedo",
            descZH      = "海盜船在後方緊追不捨。\n有幾個船員拿起了炸藥\n你知道這些笨蛋船員有可能炸到自己\n要阻止他們嗎？\n\n[無視] 船體 +40 / 船員 -2\n[阻止] 船體 -20",
            descEN      = "Pirates are hot on our tail.\nSome of the crew have grabbed explosives.\nYou know these idiots might blow themselves up.\nDo you want to stop them?\n\n[Ignore] Ship HP +40 / Crew -2\n[Stop] Ship HP -20",
            acceptLZH   = "無視",
            acceptLEN   = "Ignore",
            rejectLZH   = "阻止",
            rejectLEN   = "Stop",
            acceptDelta = new ResourceDelta { shipHP = 40f, crew = -2 },
            rejectDelta = new ResourceDelta { shipHP = -20f },
            canReject   = true,
        });

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"已在 {OutputFolder} 建立 6 個 NightEventData asset。", "OK");
        Debug.Log($"[NightEventDataGenerator] Done → {OutputFolder}");
    }

    // ── Internal ──────────────────────────────────────────

    private static void CreateEvent(NightEventConfig c)
    {
        var asset = ScriptableObject.CreateInstance<NightEventData>();
        asset.type          = c.type;
        asset.titleZH       = c.titleZH;
        asset.titleEN       = c.titleEN;
        asset.descriptionZH = c.descZH;
        asset.descriptionEN = c.descEN;
        asset.acceptLabelZH = c.acceptLZH;
        asset.acceptLabelEN = c.acceptLEN;
        asset.rejectLabelZH = c.rejectLZH;
        asset.rejectLabelEN = c.rejectLEN;
        asset.acceptDelta   = c.acceptDelta;
        asset.rejectDelta   = c.rejectDelta; // 預設全 0，炸藥事件有懲罰
        asset.spawnWeight   = 1f;
        asset.canReject     = c.canReject;

        string path = $"{OutputFolder}/{c.fileName}.asset";
        AssetDatabase.CreateAsset(asset, path);
    }

    private struct NightEventConfig
    {
        public string         fileName;
        public NightEventType type;
        public string         titleZH, titleEN;
        public string         descZH, descEN;
        public string         acceptLZH, acceptLEN;
        public string         rejectLZH, rejectLEN;
        public ResourceDelta  acceptDelta;
        public ResourceDelta  rejectDelta; // 預設全 0，有需要才填
        public bool           canReject;
    }
}
