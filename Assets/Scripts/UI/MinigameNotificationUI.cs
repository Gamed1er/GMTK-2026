using UnityEngine;

/// <summary>
/// 任務完成 / 失敗通知管理員。
/// 掛在右上角的 Layout Group 容器上。
/// 把 notificationPrefab 拖入（Prefab 上掛 MinigameNotificationItem）。
/// </summary>
public class MinigameNotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject notificationPrefab; // MinigameNotificationItem prefab
    [SerializeField] private Transform  container;          // Layout Group 的 Transform（可不填，預設用自身）

    private Transform Container => container != null ? container : transform;

    private void Start()
    {
        MinigameManager.Instance.OnMinigameResolved += OnResolved;
    }

    private void OnDestroy()
    {
        if (MinigameManager.Instance != null)
            MinigameManager.Instance.OnMinigameResolved -= OnResolved;
    }

    private void OnResolved(MinigameInstance minigame, bool success)
    {
        bool zh = LocalizationManager.IsZH;

        string taskName = zh ? minigame.Data.nameCN : minigame.Data.nameEN;
        string text = success
            ? (zh ? $"任務「{taskName}」完成" : $"Task \"{taskName}\" Completed")
            : (zh ? $"任務「{taskName}」失敗" : $"Task \"{taskName}\" Failed");

        var go = Instantiate(notificationPrefab, Container);
        go.GetComponent<MinigameNotificationItem>().Init(text, success);
    }
}
