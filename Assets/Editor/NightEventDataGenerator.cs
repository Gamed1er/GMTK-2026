using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 一鍵建立所有夜晚事件 ScriptableObject。
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

        // ── 原始 6 個事件 ─────────────────────────────────────────────

        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_AttackShip",
            type        = NightEventType.AttackShip,
            titleZH     = "攻擊其他船",
            titleEN     = "Attack Another Ship",
            descZH      = "前方出現一艘商船，你的手下躍躍欲試。\n是否派幾名船員去搶奪食糧？\n\n[出擊] 食物 +120 / 船員 -2\n[放棄] 無影響",
            descEN      = "A merchant vessel appears ahead. Your crew is eager.\nSend some hands to raid their supplies?\n\n[Attack] Food +120 / Crew -2\n[Pass] No effect",
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

        // ── 新增 26 個事件 ────────────────────────────────────────────

        // 1. 迷霧中的孤船
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_FogShip",
            type        = NightEventType.FogShip,
            titleZH     = "迷霧中的孤船",
            titleEN     = "Ghost Ship in the Fog",
            descZH      = "濃霧中，一艘靜默的船隻出現在視野內。\n上去搶奪，可以得到一些食物，但可能損失幾名船員。\n\n[搶奪] 食物 +20 / 船員 -3\n[放棄] 無影響",
            descEN      = "A silent vessel emerges from the fog.\nBoard it for supplies — but expect some casualties.\n\n[Raid] Food +20 / Crew -3\n[Ignore] No effect",
            acceptLZH   = "搶奪",
            acceptLEN   = "Raid",
            rejectLZH   = "放棄",
            rejectLEN   = "Ignore",
            acceptDelta = new ResourceDelta { food = 20, crew = -3 },
            canReject   = true,
        });

        // 2. 疲憊的工匠
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_TiredCraftsman",
            type        = NightEventType.TiredCraftsman,
            titleZH     = "疲憊的工匠",
            titleEN     = "The Exhausted Repairman",
            descZH      = "一名疲憊不堪的工匠漂上甲板，表示願意修繕船體，但條件是要先餵飽他。\n\n[接受] 船體 +30 / 食物 -40\n[拒絕] 食物 -5",
            descEN      = "An exhausted craftsman drifts onto deck and offers to repair the hull — but demands to be fed first.\n\n[Accept] Ship HP +30 / Food -40\n[Drive Away] Food -5",
            acceptLZH   = "接受",
            acceptLEN   = "Accept",
            rejectLZH   = "趕走",
            rejectLEN   = "Drive Away",
            acceptDelta = new ResourceDelta { shipHP = 30f, food = -40 },
            rejectDelta = new ResourceDelta { food = -5 },
            canReject   = true,
        });

        // 3. 奴隸市場的交易
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_SlaveMarket",
            type        = NightEventType.SlaveMarket,
            titleZH     = "奴隸市場的交易",
            titleEN     = "The Slave Market Deal",
            descZH      = "附近的港口有個地下奴隸市場，他們用廉價勞工換取你的船體零件。\n\n[交易] 船員 +10 / 船體 -30\n[拒絕] 船體 -10 / 船員 +2",
            descEN      = "A black market near port offers labor for ship parts. Dirty business, but effective.\n\n[Trade] Crew +10 / Ship HP -30\n[Refuse] Ship HP -10 / Crew +2",
            acceptLZH   = "交易",
            acceptLEN   = "Trade",
            rejectLZH   = "拒絕",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { crew = 10, shipHP = -30f },
            rejectDelta = new ResourceDelta { shipHP = -10f, crew = 2 },
            canReject   = true,
        });

        // 4. 飢餓的抉擇
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_StarvedChoice",
            type        = NightEventType.StarvedChoice,
            titleZH     = "飢餓的抉擇",
            titleEN     = "A Desperate Trade",
            descZH      = "船隻需要修繕，但你也需要食物。\n有人說：把修船材料扔進海裡換取食物，你同意嗎？\n\n[同意] 食物 +30 / 船體 -40\n[拒絕] 船體 -5 / 食物 +5",
            descEN      = "The ship needs repairs, but the crew needs food.\nSomeone suggests dumping repair materials to trade for food. Do you agree?\n\n[Agree] Food +30 / Ship HP -40\n[Refuse] Ship HP -5 / Food +5",
            acceptLZH   = "同意",
            acceptLEN   = "Agree",
            rejectLZH   = "拒絕",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { food = 30, shipHP = -40f },
            rejectDelta = new ResourceDelta { shipHP = -5f, food = 5 },
            canReject   = true,
        });

        // 5. 異族聯姻
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_TribeWedding",
            type        = NightEventType.TribeWedding,
            titleZH     = "異族聯姻",
            titleEN     = "The Tribal Marriage",
            descZH      = "一個海島族群表示，若送上幾名船員定居，他們便提供豐盛食物作為答謝。\n\n[答應] 食物 +30 / 船員 -4\n[婉拒] 船員 -1 / 食物 +5",
            descEN      = "A coastal tribe offers plentiful food if you give them a few crew members to settle.\n\n[Agree] Food +30 / Crew -4\n[Decline] Crew -1 / Food +5",
            acceptLZH   = "答應",
            acceptLEN   = "Agree",
            rejectLZH   = "婉拒",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { food = 30, crew = -4 },
            rejectDelta = new ResourceDelta { crew = -1, food = 5 },
            canReject   = true,
        });

        // 6. 絕望的肉彈
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_DesperationBomb",
            type        = NightEventType.DesperationBomb,
            titleZH     = "絕望的肉彈",
            titleEN     = "Bombs of Despair",
            descZH      = "你的船被困住了，幾個船員自願跳上敵船引爆炸藥。\n你是否阻止他們？\n\n[讓他們去] 船體 +30 / 船員 -4\n[阻止] 船員 -1 / 船體 -10",
            descEN      = "Your ship is pinned. Some crew volunteer to leap onto the enemy ship and detonate explosives.\nDo you stop them?\n\n[Let Them] Ship HP +30 / Crew -4\n[Stop Them] Crew -1 / Ship HP -10",
            acceptLZH   = "讓他們去",
            acceptLEN   = "Let Them",
            rejectLZH   = "阻止",
            rejectLEN   = "Stop Them",
            acceptDelta = new ResourceDelta { shipHP = 30f, crew = -4 },
            rejectDelta = new ResourceDelta { crew = -1, shipHP = -10f },
            canReject   = true,
        });

        // 7. 陰暗艙底的秘密
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_HoldSecret",
            type        = NightEventType.HoldSecret,
            titleZH     = "陰暗艙底的秘密",
            titleEN     = "The Secret in the Hold",
            descZH      = "夜間巡邏時，你在艙底發現了幾名偷渡者與一批走私食物。\n\n[接收] 食物 +20 / 船員 -3\n[驅逐] 船員 -1 / 食物 +5",
            descEN      = "Night patrol reveals stowaways hiding in the hold — along with smuggled provisions.\n\n[Keep Them] Food +20 / Crew -3\n[Expel Them] Crew -1 / Food +5",
            acceptLZH   = "接收",
            acceptLEN   = "Keep Them",
            rejectLZH   = "驅逐",
            rejectLEN   = "Expel Them",
            acceptDelta = new ResourceDelta { food = 20, crew = -3 },
            rejectDelta = new ResourceDelta { crew = -1, food = 5 },
            canReject   = true,
        });

        // 8. 營火旁的騎士
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_CampfireKnight",
            type        = NightEventType.CampfireKnight,
            titleZH     = "營火旁的騎士",
            titleEN     = "Knight by the Campfire",
            descZH      = "一名騎士在岸邊的營火旁休息，願意以武力換取食物保護你的船隊。\n\n[雇用] 船員 +1 / 食物 -20\n[拒絕] 食物 -5",
            descEN      = "A lone knight resting by a beachside fire offers to protect the fleet — for a price in food.\n\n[Hire] Crew +1 / Food -20\n[Decline] Food -5",
            acceptLZH   = "雇用",
            acceptLEN   = "Hire",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { crew = 1, food = -20 },
            rejectDelta = new ResourceDelta { food = -5 },
            canReject   = true,
        });

        // 9. 獵魔人的委託
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_WitcherContract",
            type        = NightEventType.WitcherContract,
            titleZH     = "獵魔人的委託",
            titleEN     = "The Monster Hunter's Request",
            descZH      = "一名自稱獵魔人的男子拿著一包金幣上船，表示附近有海怪，願意出任務換得食物。\n\n[出擊] 食物 +30 / 船員 -4\n[拒絕] 船體 -10",
            descEN      = "A self-proclaimed Witcher boards with gold in hand — there's a sea monster nearby, and he needs your crew.\n\n[Hunt] Food +30 / Crew -4\n[Refuse] Ship HP -10",
            acceptLZH   = "出擊",
            acceptLEN   = "Hunt",
            rejectLZH   = "拒絕",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { food = 30, crew = -4 },
            rejectDelta = new ResourceDelta { shipHP = -10f },
            canReject   = true,
        });

        // 10. 受到詛咒的金幣
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_CursedGold",
            type        = NightEventType.CursedGold,
            titleZH     = "受到詛咒的金幣",
            titleEN     = "The Cursed Aztec Gold",
            descZH      = "一只箱子在夜裡漂來，裡面裝滿了食物，卻隱隱散發出某種氣息。\n幾個船員自願試吃確認安全……\n\n[接受] 食物 +40 / 船員 -5\n[放棄] 船員 -1",
            descEN      = "A crate drifts to the ship at night, filled with food — but it gives off a strange aura.\nSome crew volunteer to taste-test for safety...\n\n[Take It] Food +40 / Crew -5\n[Leave It] Crew -1",
            acceptLZH   = "接受",
            acceptLEN   = "Take It",
            rejectLZH   = "放棄",
            rejectLEN   = "Leave It",
            acceptDelta = new ResourceDelta { food = 40, crew = -5 },
            rejectDelta = new ResourceDelta { crew = -1 },
            canReject   = true,
        });

        // 11. 膝蓋中箭的衛兵
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_ArrowKneeGuard",
            type        = NightEventType.ArrowKneeGuard,
            titleZH     = "膝蓋中箭的衛兵",
            titleEN     = "The Arrow-Knee Guard",
            descZH      = "一名曾是衛兵的傷患漂到船邊，表示只要補給食物就能修理船隻。\n\n[接受] 船體 +20 / 食物 -30\n[拒絕] 食物 -5",
            descEN      = "A wounded former guard drifts to the ship and offers to repair the hull in exchange for food.\n\n[Accept] Ship HP +20 / Food -30\n[Decline] Food -5",
            acceptLZH   = "接受",
            acceptLEN   = "Accept",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { shipHP = 20f, food = -30 },
            rejectDelta = new ResourceDelta { food = -5 },
            canReject   = true,
        });

        // 12. 咕嚕的指環
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_GollumRing",
            type        = NightEventType.GollumRing,
            titleZH     = "咕嚕的指環",
            titleEN     = "Gollum's Precious",
            descZH      = "一個像是咕嚕的生物攔住你的船，說自己有大量食物，但要你「聽他的話」並送走幾個「討厭的人類」。\n\n[聽他的] 食物 +50 / 船員 -6\n[趕走他] 船員 -2",
            descEN      = "A creature not unlike Gollum blocks your path — it has food, but wants you to send away a few \"nassty sailors.\"\n\n[Obey] Food +50 / Crew -6\n[Drive Off] Crew -2",
            acceptLZH   = "聽他的",
            acceptLEN   = "Obey",
            rejectLZH   = "趕走他",
            rejectLEN   = "Drive Off",
            acceptDelta = new ResourceDelta { food = 50, crew = -6 },
            rejectDelta = new ResourceDelta { crew = -2 },
            canReject   = true,
        });

        // 13. 黃金樹的恩賜
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_GoldenTreeGift",
            type        = NightEventType.GoldenTreeGift,
            titleZH     = "黃金樹的恩賜",
            titleEN     = "The Gift of the Golden Tree",
            descZH      = "海上出現一棵金色的樹，有人說摘取樹果可以修繕船體。\n但摘取樹果需要幾名船員攀爬，部分可能因此墜海。\n\n[摘取] 船體 +40 / 船員 -5\n[放棄] 船員 -1 / 船體 +10",
            descEN      = "A golden tree rises from the sea — its fruit supposedly repairs the hull, but climbing it is dangerous.\n\n[Harvest] Ship HP +40 / Crew -5\n[Leave] Crew -1 / Ship HP +10",
            acceptLZH   = "摘取",
            acceptLEN   = "Harvest",
            rejectLZH   = "放棄",
            rejectLEN   = "Leave",
            acceptDelta = new ResourceDelta { shipHP = 40f, crew = -5 },
            rejectDelta = new ResourceDelta { crew = -1, shipHP = 10f },
            canReject   = true,
        });

        // 14. 凜冬將至
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_WinterComing",
            type        = NightEventType.WinterComing,
            titleZH     = "凜冬將至",
            titleEN     = "Winter Is Coming",
            descZH      = "突然的風暴來臨，海浪打壞了船的一部分，食物也被海水浸濕。\n\n[硬撐] 船體 -20 / 食物 -20\n[轉向避開] 船員 -2",
            descEN      = "A sudden storm strikes — waves damage the hull and seawater ruins part of the food stores.\n\n[Push Through] Ship HP -20 / Food -20\n[Change Course] Crew -2",
            acceptLZH   = "硬撐",
            acceptLEN   = "Push Through",
            rejectLZH   = "轉向",
            rejectLEN   = "Change Course",
            acceptDelta = new ResourceDelta { shipHP = -20f, food = -20 },
            rejectDelta = new ResourceDelta { crew = -2 },
            canReject   = true,
        });

        // 15. 蒼白之血
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_PaleBlood",
            type        = NightEventType.PaleBlood,
            titleZH     = "蒼白之血",
            titleEN     = "The Medical Ship",
            descZH      = "一艘白色船隻靠近，上面有人表示能提供大量食物——但需要幾名船員留在他們的船上作為「交換」。\n\n[同意] 食物 +40 / 船員 -5\n[拒絕] 船員 -1",
            descEN      = "A pale vessel draws near — they offer food aplenty, but want a few crew as \"collateral.\"\n\n[Agree] Food +40 / Crew -5\n[Refuse] Crew -1",
            acceptLZH   = "同意",
            acceptLEN   = "Agree",
            rejectLZH   = "拒絕",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { food = 40, crew = -5 },
            rejectDelta = new ResourceDelta { crew = -1 },
            canReject   = true,
        });

        // 16. 信仰之躍
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_LeapOfFaith",
            type        = NightEventType.LeapOfFaith,
            titleZH     = "信仰之躍",
            titleEN     = "The Leap of Faith",
            descZH      = "一個神秘教派聲稱只要幾名船員跳入海中便能淨化船體，你選擇相信嗎？\n\n[相信] 船體 -30 / 船員 -3\n[不信] 食物 -20 / 船體 -5",
            descEN      = "A mysterious cult claims that sending a few crew members overboard will \"purify\" the ship. Do you believe them?\n\n[Believe] Ship HP -30 / Crew -3\n[Refuse] Food -20 / Ship HP -5",
            acceptLZH   = "相信",
            acceptLEN   = "Believe",
            rejectLZH   = "不信",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { shipHP = -30f, crew = -3 },
            rejectDelta = new ResourceDelta { food = -20, shipHP = -5f },
            canReject   = true,
        });

        // 17. 呀哈哈的考驗
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_YahahaTrial",
            type        = NightEventType.YahahaTrial,
            titleZH     = "呀哈哈的考驗",
            titleEN     = "The Wooden Spirit's Trial",
            descZH      = "一個帶著木偶的老人攔住你的船，說只要讓幾名船員通過「木靈的考驗」，就能得到豐富食物。\n\n[接受考驗] 食物 +30 / 船員 -4\n[拒絕考驗] 船員 -1",
            descEN      = "An old man with a puppet blocks your path, offering food if a few crew pass the \"Wooden Spirit's Trial.\"\n\n[Accept Trial] Food +30 / Crew -4\n[Refuse Trial] Crew -1",
            acceptLZH   = "接受考驗",
            acceptLEN   = "Accept Trial",
            rejectLZH   = "拒絕考驗",
            rejectLEN   = "Refuse Trial",
            acceptDelta = new ResourceDelta { food = 30, crew = -4 },
            rejectDelta = new ResourceDelta { crew = -1 },
            canReject   = true,
        });

        // 18. 烤肉的訣竅
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_DragonRoast",
            type        = NightEventType.DragonRoast,
            titleZH     = "烤肉的訣竅",
            titleEN     = "The Art of Dragon Roasting",
            descZH      = "船上的大廚聲稱找到了一種烹飪方法，只要燃燒船板就能把食物量翻倍。\n你允許嗎？\n\n[允許] 食物 +60 / 船體 -70\n[拒絕] 食物 +10",
            descEN      = "The ship's cook claims burning the deck planks as fuel will double the food supply.\nDo you allow it?\n\n[Allow] Food +60 / Ship HP -70\n[Refuse] Food +10",
            acceptLZH   = "允許",
            acceptLEN   = "Allow",
            rejectLZH   = "拒絕",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { food = 60, shipHP = -70f },
            rejectDelta = new ResourceDelta { food = 10 },
            canReject   = true,
        });

        // 19. 德古拉的盛宴
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_DraculaFeast",
            type        = NightEventType.DraculaFeast,
            titleZH     = "德古拉的盛宴",
            titleEN     = "Dracula's Feast",
            descZH      = "一名蒼白貴族來訪，以大量食物換取船員的「鮮血」，並答應你不會傷及性命。\n\n[同意] 食物 +50 / 船員 -6\n[拒絕] 船員 -1 / 食物 +5",
            descEN      = "A pale nobleman visits, offering food in exchange for the crew's \"blood\" — just a little, he insists.\n\n[Agree] Food +50 / Crew -6\n[Refuse] Crew -1 / Food +5",
            acceptLZH   = "同意",
            acceptLEN   = "Agree",
            rejectLZH   = "拒絕",
            rejectLEN   = "Refuse",
            acceptDelta = new ResourceDelta { food = 50, crew = -6 },
            rejectDelta = new ResourceDelta { crew = -1, food = 5 },
            canReject   = true,
        });

        // 20. 漂流的倖存者
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_DriftingSurvivors",
            type        = NightEventType.DriftingSurvivors,
            titleZH     = "漂流的倖存者",
            titleEN     = "Drifting Survivors",
            descZH      = "一群漂流的倖存者在海上被你發現，他們身上沒有食物，若加入你的船員會消耗大量糧食。\n\n[救起] 船員 +5 / 食物 -60\n[無視] 食物 -5",
            descEN      = "A group of drifting survivors is spotted at sea. They carry no food, and rescuing them will drain supplies.\n\n[Rescue] Crew +5 / Food -60\n[Ignore] Food -5",
            acceptLZH   = "救起",
            acceptLEN   = "Rescue",
            rejectLZH   = "無視",
            rejectLEN   = "Ignore",
            acceptDelta = new ResourceDelta { crew = 5, food = -60 },
            rejectDelta = new ResourceDelta { food = -5 },
            canReject   = true,
        });

        // 21. 傭兵的投誠
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_MercenaryDefection",
            type        = NightEventType.MercenaryDefection,
            titleZH     = "傭兵的投誠",
            titleEN     = "Mercenary Defection",
            descZH      = "一批傭兵主動投靠你的船隊，他們要求食物與修船酬勞。\n\n[雇用] 船員 +4 / 食物 -30 / 船體 -20\n[拒絕] 船體 -5",
            descEN      = "A band of mercenaries offers to join your crew — for food and ship repairs as payment.\n\n[Hire] Crew +4 / Food -30 / Ship HP -20\n[Decline] Ship HP -5",
            acceptLZH   = "雇用",
            acceptLEN   = "Hire",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { crew = 4, food = -30, shipHP = -20f },
            rejectDelta = new ResourceDelta { shipHP = -5f },
            canReject   = true,
        });

        // 22. 海妖的俘虜
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_SeaWitchCaptives",
            type        = NightEventType.SeaWitchCaptives,
            titleZH     = "海妖的俘虜",
            titleEN     = "The Sea Witch's Prisoners",
            descZH      = "你發現海妖正在囚禁一批船員，若奪回他們，海妖會攻擊船身。\n\n[奪回] 船員 +6 / 船體 -40 / 食物 -10\n[放棄] 船體 -5",
            descEN      = "You spot a sea witch holding captives. Rescuing them means taking hull damage in the fight.\n\n[Rescue] Crew +6 / Ship HP -40 / Food -10\n[Flee] Ship HP -5",
            acceptLZH   = "奪回",
            acceptLEN   = "Rescue",
            rejectLZH   = "放棄",
            rejectLEN   = "Flee",
            acceptDelta = new ResourceDelta { crew = 6, shipHP = -40f, food = -10 },
            rejectDelta = new ResourceDelta { shipHP = -5f },
            canReject   = true,
        });

        // 23. 荒野的戰爭男孩
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_WarBoys",
            type        = NightEventType.WarBoys,
            titleZH     = "荒野的戰爭男孩",
            titleEN     = "The War Boys",
            descZH      = "一群荒野中的狂戰士願意加入你的船隊，但他們食量巨大。\n\n[接受] 船員 +5 / 食物 -40\n[拒絕] 船體 -5",
            descEN      = "A band of wild warriors from the wastelands wishes to join — but they eat like a small army.\n\n[Accept] Crew +5 / Food -40\n[Decline] Ship HP -5",
            acceptLZH   = "接受",
            acceptLEN   = "Accept",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { crew = 5, food = -40 },
            rejectDelta = new ResourceDelta { shipHP = -5f },
            canReject   = true,
        });

        // 24. 尾節車廂的叛亂
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_WagonRebellion",
            type        = NightEventType.WagonRebellion,
            titleZH     = "尾節車廂的叛亂",
            titleEN     = "The Cargo Deck Rebellion",
            descZH      = "被囚禁在下層甲板的一批奴工發動叛亂，他們表示願意效力換取自由。\n\n[釋放] 船員 +6 / 食物 -50\n[鎮壓] 船體 -20 / 船員 -1",
            descEN      = "Enslaved workers in the lower deck rise up — they'll serve willingly if freed.\n\n[Free Them] Crew +6 / Food -50\n[Suppress] Ship HP -20 / Crew -1",
            acceptLZH   = "釋放",
            acceptLEN   = "Free Them",
            rejectLZH   = "鎮壓",
            rejectLEN   = "Suppress",
            acceptDelta = new ResourceDelta { crew = 6, food = -50 },
            rejectDelta = new ResourceDelta { shipHP = -20f, crew = -1 },
            canReject   = true,
        });

        // 25. 監獄的倖存者
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_PrisonSurvivors",
            type        = NightEventType.PrisonSurvivors,
            titleZH     = "監獄的倖存者",
            titleEN     = "The Prison Survivors",
            descZH      = "一座沉船底部傳來呼救聲，幾名囚犯被困其中。若救出，他們願意加入船員。\n\n[救出] 船員 +4 / 船體 -40\n[無視] 無影響",
            descEN      = "Cries for help echo from a sunken ship's hull — prisoners are trapped inside. Rescue them and they'll join your crew.\n\n[Rescue] Crew +4 / Ship HP -40\n[Ignore] No effect",
            acceptLZH   = "救出",
            acceptLEN   = "Rescue",
            rejectLZH   = "無視",
            rejectLEN   = "Ignore",
            acceptDelta = new ResourceDelta { crew = 4, shipHP = -40f },
            canReject   = true,
        });

        // 26. 避難所的居民
        CreateEvent(new NightEventConfig
        {
            fileName    = "NightEvent_ShelterDwellers",
            type        = NightEventType.ShelterDwellers,
            titleZH     = "避難所的居民",
            titleEN     = "The Vault Dwellers",
            descZH      = "一個海上避難所的居民表示食物充裕，願意用食物換取在你船上居住的機會。\n\n[接受] 船員 +5 / 食物 -40\n[拒絕] 食物 +10 / 船員 -1",
            descEN      = "Survivors from a sea refuge offer to trade food for passage aboard your ship.\n\n[Accept] Crew +5 / Food -40\n[Decline] Food +10 / Crew -1",
            acceptLZH   = "接受",
            acceptLEN   = "Accept",
            rejectLZH   = "拒絕",
            rejectLEN   = "Decline",
            acceptDelta = new ResourceDelta { crew = 5, food = -40 },
            rejectDelta = new ResourceDelta { food = 10, crew = -1 },
            canReject   = true,
        });

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"已在 {OutputFolder} 建立 32 個 NightEventData asset。", "OK");
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
        asset.rejectDelta   = c.rejectDelta;
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
        public ResourceDelta  rejectDelta;
        public bool           canReject;
    }
}
