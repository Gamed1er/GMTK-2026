using UnityEngine;

/// <summary>
/// 放在 TileMap MiniGame 層的世界物件。
/// 邏輯座標（船員導航、佔位判斷）永遠使用 instance.SpawnPoint，不受本腳本影響。
///
/// 視覺位置規則：
/// - 若 instance.SpawnPoint 在 Camera 可視範圍內：物件顯示在原本座標（transform.position = SpawnPoint）。
/// - 若在可視範圍外：物件會被「夾」到畫面邊緣附近顯示（螢幕邊緣提示），
///   但這只是視覺呈現，不影響 instance.SpawnPoint 本身，船員依然照原座標走。
///
/// 互動規則：
/// - 只有當 SpawnPoint 在畫面內時才能點擊開啟；在畫面外（顯示在邊緣）時不可互動，
///   玩家必須讓實際座標進入視野範圍才能點開小遊戲。
///
/// 顯示邏輯（沿用原本設計）：
/// - 沒有船員執行（!HasEnoughCrew）：顯示「倒數中」背景 + fill 圖片，
///   fill 依「目前時間 / 總時間」決定（Timer / countdownDuration）。
/// - 有船員執行（HasEnoughCrew）：倒數暫停，改顯示「工作中」背景 + fill 圖片，
///   fill 依「船員工作進度」決定（CrewWorkProgress / TotalWorkRequired）。
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
///
/// 需要掛一個 Collider2D（例如 CircleCollider2D）才能接收點擊。
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

    [Header("Off-Screen Clamp（視野外時移到邊緣顯示）")]
    [Tooltip("夾到邊緣時，距離螢幕邊界內縮多少視口比例（0~0.5），避免完全貼邊")]
    [SerializeField, Range(0f, 0.45f)] private float viewportEdgePadding = 0.05f;
    [Tooltip("視野外時整體淡出的透明度（0~1，1 = 不淡化）")]
    [SerializeField, Range(0f, 1f)] private float offScreenAlpha = 0.6f;

    private MinigameInstance myInstance;
    private Camera mainCamera;
    private bool isOnScreen = true; // 目前 SpawnPoint 是否在畫面內，決定能否互動

    private SpriteRenderer[] allRenderers;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;
        transform.position = instance.WorldPosition; // 初始顯示在原本座標

        mainCamera = Camera.main;
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        UpdateVisual();
        UpdateScreenClamp();
    }

    private void Update()
    {
        if (myInstance == null || myInstance.IsCompleted)
        {
            Destroy(gameObject);
            return;
        }

        // 玩家親自執行這個小遊戲時（面板開著），直接隱藏世界物件本身
        if (myInstance.IsPlayerAssigned)
        {
            SetVisible(false);
            return;
        }
        SetVisible(true);

        UpdateVisual();
        UpdateScreenClamp();
    }

    private void SetVisible(bool visible)
    {
        if (allRenderers == null) return;
        foreach (var sr in allRenderers)
        {
            if (sr != null) sr.enabled = visible;
        }
    }

    // ── Fill 顯示（倒數中 / 工作中，沿用原本邏輯）────────────

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
            float total = myInstance.TotalWorkRequired;
            float progress = total > 0f ? 1 - myInstance.CrewWorkProgress / total : 0f;
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
            scale.y = amount * 2 * 4.25f;
        else
            scale.x = amount;
        maskTransform.localScale = scale;
    }

    private void SetActiveSafe(SpriteRenderer sr, bool active)
    {
        if (sr != null)
            sr.gameObject.SetActive(active);
    }

    // ── Off-Screen Clamp（視野外時把物件本身移到邊緣）─────

    /// <summary>
    /// 判斷 instance.SpawnPoint（邏輯座標）是否在攝影機可視範圍內；
    /// 在範圍內就把本物件顯示在原本座標，範圍外則夾到畫面邊緣顯示。
    /// 注意：這裡只改 transform.position（純視覺），instance.SpawnPoint 完全不變，
    /// 船員導航、佔位判斷都不受影響。
    /// </summary>
    private void UpdateScreenClamp()
    {
        if (mainCamera == null)
        {
            transform.position = myInstance.WorldPosition;
            isOnScreen = true;
            return;
        }

        Vector3 worldPos = myInstance.WorldPosition;
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(worldPos);
        bool isBehindCamera = viewportPoint.z < 0f;

        bool inView = !isBehindCamera &&
                      viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
                      viewportPoint.y >= 0f && viewportPoint.y <= 1f;

        isOnScreen = inView;

        if (inView)
        {
            transform.position = worldPos;
            SetAlpha(1f);
            return;
        }

        // 視野外：把 viewport 座標夾在邊界內（含 padding），再轉回世界座標顯示
        if (isBehindCamera)
        {
            viewportPoint.x = 1f - viewportPoint.x;
            viewportPoint.y = 1f - viewportPoint.y;
        }

        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 dir = (new Vector2(viewportPoint.x, viewportPoint.y) - center).normalized;

        float halfX = 0.5f - viewportEdgePadding;
        float halfY = 0.5f - viewportEdgePadding;

        float cos = dir.x;
        float sin = dir.y;
        float scaleX = cos != 0f ? Mathf.Abs(halfX / cos) : float.MaxValue;
        float scaleY = sin != 0f ? Mathf.Abs(halfY / sin) : float.MaxValue;
        float scale = Mathf.Min(scaleX, scaleY);

        Vector2 clampedViewport = center + dir * scale;

        float depth = Mathf.Abs(mainCamera.transform.position.z - worldPos.z);
        Vector3 clampedWorld = mainCamera.ViewportToWorldPoint(
            new Vector3(clampedViewport.x, clampedViewport.y, depth));
        clampedWorld.z = worldPos.z;

        transform.position = clampedWorld;
        SetAlpha(offScreenAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (allRenderers == null) return;

        foreach (var sr in allRenderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    // ── Click / Interact（視野外禁止互動）──────────────────

    // 玩家點擊這個物件時觸發（需要 Collider2D）
    private void OnMouseDown()
    {
        if (myInstance == null || myInstance.IsCompleted) return;
 
        if (myInstance.IsPlayerAssigned)
        {
            Debug.Log("[MinigameObject] 玩家正在執行這個小遊戲，暫時無法點擊。");
            return;
        }

        if (!isOnScreen)
        {
            Debug.Log("[MinigameObject] 小遊戲目前在視野外，需要讓實際位置進入畫面才能開啟。");
            return;
        }

        MinigameUIManager.Instance.OpenPanel(myInstance);
    }
}