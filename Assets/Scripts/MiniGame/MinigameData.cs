using UnityEngine;

public enum MinigameType
{
    Steering,    // 開船
    Fishing,     // 釣魚
    RepairNet,   // 補網子
    FightPirate, // 打海盜
    PatchHole,   // 補木板洞
    Firefighting // 滅火
}

/// <summary>
/// 每種小遊戲的靜態設定，用 ScriptableObject 儲存
/// Static config for each minigame type — stored as ScriptableObject
///
/// 建立方式：在 Project 視窗右鍵 → Create → Game → MinigameData
/// </summary>
[CreateAssetMenu(fileName = "MinigameData", menuName = "Game/MinigameData")]
public class MinigameData : ScriptableObject
{
    [Header("Identity")]
    public MinigameType type;
    public string nameEN;
    public string nameCN;

    [Header("Timing")]
    public float countdownDuration = 20f;  // 倒數秒數

    [Header("Crew Requirements")]
    public int crewRequiredToComplete = 5; // 需要幾個船員才能完成最低難度
    public float crewCompletionTime = 5f;  // 用 crewRequired 個船員完成所需秒數

    [Header("Outcomes")]
    public ResourceDelta successDelta;     // 完成後的資源變化
    public ResourceDelta failureDelta;     // 失敗後的資源變化

    [Header("Panel")]
    public GameObject panelPrefab;         // 這個小遊戲對應的 UI 面板 Prefab

    [Header("Spawn Settings")]
    public bool canSpawnRandomly = true;
    public float spawnWeight = 1f;         // 相對出現概率（數字越大越常出現）
    public bool onlyOneAtATime = false;    // 同時只能有一個（例如開船）
}
