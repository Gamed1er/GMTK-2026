/// <summary>
/// 所有小遊戲面板腳本（TmpGame、SteeringMinigame...）都要實作這個介面，
/// 讓 MinigameUIManager 開面板時可以統一呼叫 Init，不用一一判斷型別。
/// </summary>
public interface IMinigamePanel
{
    void Init(MinigameInstance instance);
}