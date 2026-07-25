using UnityEngine;

/// <summary>
/// 同步控制 Body / Clothes / Hair 三個子 Animator。
/// CrewMember 呼叫這裡的方法，不直接操作個別 Animator。
/// </summary>
public class CrewAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private Animator clothesAnimator;
    [SerializeField] private Animator hairAnimator;    // 可能被隱藏，需要 null check

    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int IsWorking = Animator.StringToHash("isWorking");

    public void SetWalking(bool value)
    {
        SetBool(IsWalking, value);
    }

    public void SetWorking(bool value)
    {
        SetBool(IsWorking, value);
    }

    private void SetBool(int hash, bool value)
    {
        bodyAnimator?.SetBool(hash, value);
        clothesAnimator?.SetBool(hash, value);

        // Hair 可能被 CrewAppearance 隱藏，只在啟用時更新
        if (hairAnimator != null && hairAnimator.gameObject.activeSelf)
            hairAnimator.SetBool(hash, value);
    }
}
