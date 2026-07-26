using UnityEngine;

/// <summary>
/// 某一天的小遊戲生成設定。
/// 在 Project 視窗右鍵 → Create → Game → Day Spawn Config 建立。
/// GameManager.dayConfigs[dayCount - 1] 取得當天設定；超出 List 範圍用最後一筆。
/// </summary>
[CreateAssetMenu(fileName = "DaySpawnConfig", menuName = "Game/Day Spawn Config")]
public class DaySpawnConfig : ScriptableObject
{
    [Header("生成間隔（秒）")]
    public float minSpawnInterval = 3f;
    public float maxSpawnInterval = 5f;

    [Header("每波生成數量")]
    public int minSpawnCount = 1;
    public int maxSpawnCount = 3;

    [Header("各小遊戲生成權重（0 = 本日不生成）")]
    public float steeringWeight     = 0f; // 開船通常 onlyOneAtATime，設 0 讓它走固定邏輯
    public float fishingWeight      = 1f;
    public float patchHoleWeight    = 1f;
    public float fightPirateWeight  = 1f;
    public float firefightingWeight = 1f;

    /// <summary>依 MinigameType 取得本日權重</summary>
    public float GetWeight(MinigameType type) => type switch
    {
        MinigameType.Steering     => steeringWeight,
        MinigameType.Fishing      => fishingWeight,
        MinigameType.PatchHole    => patchHoleWeight,
        MinigameType.FightPirate  => fightPirateWeight,
        MinigameType.Firefighting => firefightingWeight,
        _                         => 0f,
    };
}
