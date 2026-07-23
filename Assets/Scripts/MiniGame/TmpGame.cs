using UnityEngine;

public class TmpGame : MonoBehaviour
{
    private MinigameInstance myInstance;


    // 初始化：由 MinigameManager 生成時呼叫，傳入對應的 instance
    public void Init(MinigameInstance instance)
    {
        myInstance = instance;
        Debug.Log($"[TmpGame] Init — type: {instance.Data.type}, difficulty: {instance.Difficulty}");
    }

    // 當遊戲結束時呼叫以下函式
    public void Complete()
    {
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }

    public void Exit()
    {
        MinigameUIManager.Instance.CloseCurrentPanel();
    }
    // MinigameManager.Instance.CompleteMinigame(myInstance);
}