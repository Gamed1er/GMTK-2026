using UnityEngine;

public enum NightEventType
{
    AttackShip,          // 攻擊其他船
    HireRepairman,       // 修船工
    SellPartsForCrew,    // 賣零件換人
    SellPartsForFood,    // 賣零件換食物
    TradeCrewForFood,    // 拿人換食物
    CrewAsBomb,          // 拿人當武器

    // ── 新增 26 個事件 ──────────────────────────────
    FogShip,             // 迷霧中的孤船
    TiredCraftsman,      // 疲憊的工匠
    SlaveMarket,         // 奴隸市場的交易
    StarvedChoice,       // 飢餓的抉擇
    TribeWedding,        // 異族聯姻
    DesperationBomb,     // 絕望的肉彈
    HoldSecret,          // 陰暗艙底的秘密
    CampfireKnight,      // 營火旁的騎士
    WitcherContract,     // 獵魔人的委託
    CursedGold,          // 受到詛咒的金幣
    ArrowKneeGuard,      // 膝蓋中箭的衛兵
    GollumRing,          // 咕嚕的指環
    GoldenTreeGift,      // 黃金樹的恩賜
    WinterComing,        // 凜冬將至
    PaleBlood,           // 蒼白之血
    LeapOfFaith,         // 信仰之躍
    YahahaTrial,         // 呀哈哈的考驗
    DragonRoast,         // 烤肉的訣竅
    DraculaFeast,        // 德古拉的盛宴
    DriftingSurvivors,   // 漂流的倖存者
    MercenaryDefection,  // 傭兵的投誠
    SeaWitchCaptives,    // 海妖的俘虜
    WarBoys,             // 荒野的戰爭男孩
    WagonRebellion,      // 尾節車廂的叛亂
    PrisonSurvivors,     // 監獄的倖存者
    ShelterDwellers,     // 避難所的居民
}

/// <summary>
/// 夜晚事件資料。
/// 右鍵 → Create → Game → Night Event Data 建立。
/// </summary>
[CreateAssetMenu(fileName = "NightEvent", menuName = "Game/Night Event Data")]
public class NightEventData : ScriptableObject
{
    [Header("識別")]
    public NightEventType type;

    [Header("標題（中 / EN）")]
    public string titleZH;
    public string titleEN;

    [Header("內文（中 / EN）")]
    [TextArea(3, 6)] public string descriptionZH;
    [TextArea(3, 6)] public string descriptionEN;

    [Header("按鈕文字")]
    public string acceptLabelZH = "同意";
    public string acceptLabelEN = "Accept";
    public string rejectLabelZH = "拒絕";
    public string rejectLabelEN = "Decline";

    [Header("接受的結果")]
    public ResourceDelta acceptDelta;

    [Header("拒絕的結果（通常全 0，也可設懲罰）")]
    public ResourceDelta rejectDelta;

    [Header("生成設定")]
    public float spawnWeight = 1f;
    public bool canReject = true; // false = 強制接受
}

/*
 ─────────────────────────────────────────────────────
  建議的六個事件預設值（在 Inspector 填入）
 ─────────────────────────────────────────────────────

 1. AttackShip — 攻擊其他船 / Attack Another Ship
    ZH: 前方出現一艘商船，你的手下躍躍欲試。
        是否派遣幾名船員去搶奪食糧？
    EN: A merchant vessel appears ahead. Your crew is eager.
        Send some hands to raid their supplies?
    acceptDelta:  food +120, crew -2
    rejectDelta:  (全 0)

 2. HireRepairman — 修船工 / Ship Repairman
    ZH: 一名流浪修船工求搭便船，他說願意以修船換取食物。
    EN: A wandering repairman offers to fix the ship in exchange for food.
    acceptDelta:  shipHP +30, food -60
    rejectDelta:  (全 0)

 3. SellPartsForCrew — 賣零件換人 / Trade Parts for Crew
    ZH: 附近港口有失業的水手，但他們要求用船上零件換取雇傭費。
    EN: Unemployed sailors at a nearby port want ship parts as payment.
    acceptDelta:  crew +3, shipHP -20
    rejectDelta:  (全 0)

 4. SellPartsForFood — 賣零件換食物 / Trade Parts for Food
    ZH: 港口商人願意用大量食物換取船上的備用零件。
    EN: A port merchant offers plenty of food for your spare ship parts.
    acceptDelta:  food +100, shipHP -15
    rejectDelta:  (全 0)

 5. TradeCrewForFood — 拿人換食物 / Trade Crew for Food
    ZH: 島上土著願意用食物換取一名船員定居，說是「聯姻」。
    EN: Island natives offer food for a crew member to settle and "marry in."
    acceptDelta:  food +80, crew -1
    rejectDelta:  (全 0)

 6. CrewAsBomb — 拿人當武器 / Crew as Ammunition
    ZH: 海盜船在後方追來。老大提議把炸藥綁在幾個「自願者」身上……
    EN: A pirate ship is gaining on you. Your first mate suggests... volunteers.
    acceptDelta:  shipHP +40, crew -2
    rejectDelta:  (全 0)
    canReject:    true

 ─────────────────────────────────────────────────────
*/
