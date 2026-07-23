using UnityEngine;

public class TmpGame : MonoBehaviour, IMinigamePanel
{
    private MinigameInstance myInstance;


    // 初始化：由 MinigameManager 生成時呼叫，傳入對應的 instance
    public void Init(MinigameInstance instance)
    {
        myInstance = instance;
    }

    // 當遊戲結束時呼叫以下函式
    public void Complete()
    {
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }

    // 玩家進入面板後不能中途取消，只能透過 Complete() 結束
    // 倒數結束時面板會由 MinigameUIManager.OnResolved 自動關閉
}