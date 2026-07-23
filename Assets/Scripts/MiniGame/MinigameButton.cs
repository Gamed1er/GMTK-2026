using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 地圖上代表一個小遊戲的按鈕。
/// 由 MinigameUIManager 生成，持有對應的 MinigameInstance。
/// </summary>
public class MinigameButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timerText;

    private MinigameInstance myInstance;
    private MinigameUIManager uiManager;

    /// <summary>生成後立刻呼叫</summary>
    public void Init(MinigameInstance instance, MinigameUIManager manager)
    {
        myInstance = instance;
        uiManager = manager;

        // 顯示小遊戲名稱
        nameText.text = instance.Data.type.ToString(); // 之後可換成 LocalizationManager
    }

    private void Update()
    {
        // 小遊戲已結束（完成或超時），按鈕自動消失
        if (myInstance == null || myInstance.IsCompleted)
        {
            Destroy(gameObject);
            return;
        }

        timerText.text = $"{myInstance.Timer:F0}s";
    }

    /// <summary>玩家點按鈕時（綁在 Button 的 OnClick）</summary>
    public void OnClick()
    {
        uiManager.OpenPanel(myInstance);
    }
}
