using UnityEngine;

/// <summary>
/// 放在 TileMap MiniGame 層的世界物件。
/// 玩家點擊後觸發對應小遊戲面板。
/// 需要掛一個 Collider2D（例如 CircleCollider2D）才能接收點擊。
///
/// 顯示邏輯：
/// - 沒有船員執行（!HasEnoughCrew）：顯示「倒數中」背景 + fill 圖片，
///   fill 依「目前時間 / 總時間」決定（Timer / countdownDuration）。
/// - 有船員執行（HasEnoughCrew）：倒數暫停（Timer 不遞減，見 MinigameManager），
///   改顯示「工作中」背景 + fill 圖片，fill 依「船員工作進度」決定
///   （CrewWorkProgress / TotalWorkRequired）。
///
/// Fill 實作方式（世界物件不能直接用 UI.Image，因為 UI.Image 需要 Canvas 才會渲染）：
/// 用 SpriteRenderer 顯示背景圖 + fill 圖，fill 圖的「填滿比例」用一個子物件
/// （fillMaskTransform）的 localScale.x（或 y，看你要橫向還是縱向 fill）來模擬，
/// 搭配 SpriteMask 元件裁切超出範圍的部分。
///
/// 設置步驟（Inspector）：
/// 1. 背景：一個掛 SpriteRenderer 的子物件，指定對應背景 Sprite。
/// 2. Fill：一個掛 SpriteRenderer 的子物件（例如叫 FillSprite），指定 fill Sprite，
///    其 pivot 建議設在左側（Sprite Editor 裡把 pivot 設為 Left 或自訂），
///    這樣 scale.x 從 0→1 時會由左往右填滿，符合一般讀條視覺。
/// 3. 在 FillSprite 底下（或同一個物件上）加一個 SpriteMask 元件，
///    Sprite 設一個跟 fill 區域等大的遮罩圖（純色矩形即可），
///    並把 FillSprite 的 SpriteRenderer 的 Mask Interaction 設為
///    "Visible Inside Mask"。
/// 4. 把該 SpriteMask 所在物件的 Transform 拖進 fillMaskTransform 欄位；
///    程式會透過縮放這個物件的 localScale.x 來模擬 fillAmount（0~1）。
///    （若你的美術希望用縱向 fill，把程式裡 scale.x 改成 scale.y 即可）
/// </summary>
public class MinigameObject : MonoBehaviour
{
    [Header("倒數中（無船員執行）")]
    [Tooltip("倒數狀態的背景圖")]
    [SerializeField] private SpriteRenderer countdownBackgroundRenderer;
    [Tooltip("倒數狀態的 fill 圖（本身是完整圖片，靠遮罩裁切顯示比例）")]
    [SerializeField] private SpriteRenderer countdownFillRenderer;
    [Tooltip("控制倒數 fill 比例的遮罩物件（SpriteMask 掛在這個物件或其子物件上），\n用這個物件的 localScale.x 模擬 0~1 的 fillAmount")]
    [SerializeField] private Transform countdownFillMaskTransform;

    [Header("工作中（船員執行中）")]
    [Tooltip("船員工作中的背景圖")]
    [SerializeField] private SpriteRenderer workingBackgroundRenderer;
    [Tooltip("船員工作中的 fill 圖")]
    [SerializeField] private SpriteRenderer workingFillRenderer;
    [Tooltip("控制工作 fill 比例的遮罩物件")]
    [SerializeField] private Transform workingFillMaskTransform;

    [Header("Fill 方向")]
    [Tooltip("勾選則用縱向（scale.y）模擬 fill，否則用橫向（scale.x）")]
    [SerializeField] private bool fillVertically = false;

    private MinigameInstance myInstance;

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;
        transform.position = instance.WorldPosition;

        UpdateVisual();
    }

    private void Update()
    {
        if (myInstance == null || myInstance.IsCompleted)
        {
            Destroy(gameObject);
            return;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        bool isWorking = myInstance.HasEnoughCrew;

        SetActiveSafe(countdownBackgroundRenderer, !isWorking);
        SetActiveSafe(countdownFillRenderer, !isWorking);
        if (countdownFillMaskTransform != null)
            countdownFillMaskTransform.gameObject.SetActive(!isWorking);

        SetActiveSafe(workingBackgroundRenderer, isWorking);
        SetActiveSafe(workingFillRenderer, isWorking);
        if (workingFillMaskTransform != null)
            workingFillMaskTransform.gameObject.SetActive(isWorking);

        if (isWorking)
        {
            float total = myInstance.Data.crewRequiredToComplete * myInstance.Data.crewCompletionTime;
            float progress = total > 0f ? myInstance.CrewWorkProgress / total : 0f;
            SetFill(workingFillMaskTransform, Mathf.Clamp01(progress));
        }
        else
        {
            float total = myInstance.Data.countdownDuration;
            float progress = total > 0f ? myInstance.Timer / total : 0f;
            SetFill(countdownFillMaskTransform, Mathf.Clamp01(progress));
        }
    }

    /// <summary>
    /// 用遮罩物件的 localScale 模擬 fillAmount（0~1）。
    /// 遮罩的 pivot/anchor 需自行在美術端對齊（例如物件的 local origin 設在左邊），
    /// 否則縮放會從中心往兩邊縮，而不是從一側往另一側填滿。
    /// </summary>
    private void SetFill(Transform maskTransform, float amount)
    {
        if (maskTransform == null) return;

        Vector3 scale = maskTransform.localScale;
        if (fillVertically)
            scale.y = amount * 2;
        else
            scale.x = amount;
        maskTransform.localScale = scale;
    }

    private void SetActiveSafe(SpriteRenderer sr, bool active)
    {
        if (sr != null)
            sr.gameObject.SetActive(active);
    }

    // 玩家點擊這個物件時觸發（需要 Collider2D）
    private void OnMouseDown()
    {
        if (myInstance == null || myInstance.IsCompleted) return;
        MinigameUIManager.Instance.OpenPanel(myInstance);
    }
}