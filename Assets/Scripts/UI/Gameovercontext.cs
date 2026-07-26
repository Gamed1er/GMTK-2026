using UnityEngine;

/// <summary>
/// 用來在切換到「結局 Scene」前，暫存這局的 GameOverReason。
/// GameManager 觸發 OnGameOver 時寫入，結局 Scene 的 GameOverUI 在 Start 時讀取。
/// 不需要掛在任何物件上，純靜態資料容器。
/// </summary>
public static class GameOverContext
{
    public static GameOverReason Reason { get; private set; }
    public static bool HasReason { get; private set; }

    public static void SetReason(GameOverReason reason)
    {
        Reason = reason;
        HasReason = true;
    }

    public static void Clear()
    {
        HasReason = false;
    }
}