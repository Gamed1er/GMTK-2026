using UnityEngine;

/// <summary>
/// 讓玩家用滑鼠拖曳船員。
/// - 放在 Ground 上 → 正常落地，恢復 AI
/// - 放在 Wall 上   → 自動移到最近的 Ground
/// - 放在海上（無 tile）→ 落海消失，船員數 -1
///
/// 需要：CrewMember、Collider2D（非 trigger）、Camera 有 Physics 2D Raycaster
/// </summary>
[RequireComponent(typeof(CrewMember))]
public class CrewDragHandler : MonoBehaviour
{
    private CrewMember crewMember;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 grabOffset;
    private int originalSortingOrder;
    private SpriteRenderer sr;

    private void Awake()
    {
        crewMember  = GetComponent<CrewMember>();
        mainCamera  = Camera.main;
        sr          = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        isDragging = true;
        crewMember.SetDragging(true);
        grabOffset = transform.position - GetMouseWorld();

        // 拖曳時置頂顯示
        if (sr != null)
        {
            originalSortingOrder = sr.sortingOrder;
            sr.sortingOrder = 999;
        }
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;
        transform.position = GetMouseWorld() + grabOffset;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        if (sr != null) sr.sortingOrder = originalSortingOrder;

        var tm = TilemapManager.Instance;
        if (tm == null) { crewMember.SetDragging(false); return; }

        Vector2 pos = transform.position;

        if (tm.IsGround(pos))
        {
            // 落在地面，直接恢復
            crewMember.SetDragging(false);
        }
        else if (tm.IsWall(pos))
        {
            // 落在牆上，順移到最近 Ground
            var nearest = tm.FindNearestGround(pos);
            if (nearest.HasValue)
            {
                transform.position = (Vector3)nearest.Value;
                crewMember.SetDragging(false);
            }
            else
            {
                DropOverboard();
            }
        }
        else
        {
            // 落海
            DropOverboard();
        }
    }

    private void DropOverboard()
    {
        Debug.Log($"[CrewDragHandler] {name} 落海！");
        CrewManager.Instance.DropCrew(crewMember);
        // DropCrew 內部會 Destroy(gameObject)，不需要再呼叫
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(mp);
    }
}
