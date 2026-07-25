using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 根據 Y 軸位置動態設定 SortingGroup 的 sortingOrder。
/// Y 越小（越靠畫面下方）→ 排序越高 → 顯示在前面。
/// 掛在 Crew 根物件上，子物件（Body/Cloth/Hair）保留各自的相對 Order。
/// </summary>
[RequireComponent(typeof(SortingGroup))]
public class CrewSortingOrder : MonoBehaviour
{
    [Tooltip("乘以 Y 座標轉成整數 Order，建議 10~100")]
    [SerializeField] private float yScale = 100f;

    [Tooltip("填入 Sorting Layer 名稱，必須在 Project Settings 中存在")]
    [SerializeField] private string sortingLayerName = "Characters";

    private SortingGroup sortingGroup;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
        sortingGroup.sortingLayerName = sortingLayerName;
    }

    private void LateUpdate()
    {
        // Y 越小（畫面下方）→ order 越大 → 顯示在其他船員前面
        // 加 10000 偏移，確保即使 Y > 0 也不會產生負數
        sortingGroup.sortingOrder = 10000 + Mathf.RoundToInt(-transform.position.y * yScale);
    }
}
