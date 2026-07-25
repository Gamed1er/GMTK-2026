using UnityEngine;

/// <summary>
/// 在 Awake 時隨機決定船員外觀。
///
/// Crew GameObject 結構：
///   CrewMember
///     ├── Body    (Animator)  ← 膚色
///     ├── Clothes (Animator)  ← 衣服
///     └── Hair    (Animator)  ← 頭髮（可隱藏）
///
/// 新增變體只需把新的 AnimatorController 拖入對應陣列即可。
/// </summary>
public class CrewAppearance : MonoBehaviour
{
    [Header("子物件 Animator")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Animator clothesAnimator;
    [SerializeField] private Animator hairAnimator;

    [Header("膚色（必選一，拖入幾個都行）")]
    [SerializeField] private RuntimeAnimatorController[] skinControllers;

    [Header("衣服（必選一，拖入幾個都行）")]
    [SerializeField] private RuntimeAnimatorController[] clothesControllers;

    [Header("頭髮（可不選；陣列只放有髮型的，無頭髮機率自動加入）")]
    [SerializeField] private RuntimeAnimatorController[] hairControllers;

    private void Awake()
    {
        ApplyRandomAppearance();
    }

    // ── Public ────────────────────────────────────────────

    /// <summary>若需要在 Awake 之後重新隨機化，可直接呼叫</summary>
    public void ApplyRandomAppearance()
    {
        SetSkin();
        SetClothes();
        SetHair();
    }

    // ── Private ───────────────────────────────────────────

    private void SetSkin()
    {
        if (skinControllers == null || skinControllers.Length == 0)
        {
            Debug.LogWarning("[CrewAppearance] skinControllers 是空的！", gameObject);
            return;
        }
        bodyAnimator.runtimeAnimatorController =
            skinControllers[Random.Range(0, skinControllers.Length)];
    }

    private void SetClothes()
    {
        if (clothesControllers == null || clothesControllers.Length == 0)
        {
            Debug.LogWarning("[CrewAppearance] clothesControllers 是空的！", gameObject);
            return;
        }
        clothesAnimator.runtimeAnimatorController =
            clothesControllers[Random.Range(0, clothesControllers.Length)];
    }

    private void SetHair()
    {
        if (hairControllers == null || hairControllers.Length == 0)
        {
            hairAnimator.gameObject.SetActive(false);
            return;
        }

        // 選項數 = 有頭髮的種類 + 1（無頭髮）
        // 0 → 無頭髮；1~N → hairControllers[index - 1]
        int choice = Random.Range(0, hairControllers.Length + 1);

        if (choice == 0)
        {
            hairAnimator.gameObject.SetActive(false);
        }
        else
        {
            hairAnimator.gameObject.SetActive(true);
            hairAnimator.runtimeAnimatorController = hairControllers[choice - 1];
        }
    }
}
