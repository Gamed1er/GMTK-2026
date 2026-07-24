using UnityEngine;
using TMPro;

/// <summary>
/// 放在 TileMap MiniGame 層的世界物件。
/// 玩家點擊後觸發對應小遊戲面板。
/// 需要掛一個 Collider2D（例如 CircleCollider2D）才能接收點擊。
/// </summary>
public class MinigameObject : MonoBehaviour
{
    [SerializeField] private TextMeshPro timerText; // 世界空間的計時器文字（TMP 世界空間版，非 UGUI）

    private MinigameInstance myInstance;

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;
        transform.position = instance.WorldPosition;
    }

    private void Update()
    {
        if (myInstance == null || myInstance.IsCompleted)
        {
            Destroy(gameObject);
            return;
        }

        if (timerText != null)
            timerText.text = $"{myInstance.Timer:F0}s";
    }

    // 玩家點擊這個物件時觸發（需要 Collider2D）
    private void OnMouseDown()
    {
        if (myInstance == null || myInstance.IsCompleted) return;
        MinigameUIManager.Instance.OpenPanel(myInstance);
    }
}
