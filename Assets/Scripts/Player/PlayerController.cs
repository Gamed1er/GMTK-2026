using UnityEngine;

/// <summary>
/// 玩家（船長）控制器：WASD 移動，TileMap Collider 處理碰撞
/// 需要掛：Rigidbody2D（Collision Detection: Continuous, Gravity Scale: 0）
///         Collider2D（例如 CapsuleCollider2D）
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"), // A/D 或 ←/→
            Input.GetAxisRaw("Vertical")    // W/S 或 ↑/↓
        ).normalized;

        // 左右翻轉 Sprite
        if (spriteRenderer != null)
        {
            if (moveInput.x > 0) spriteRenderer.flipX = false;
            else if (moveInput.x < 0) spriteRenderer.flipX = true;
        }

        // 切換動畫
        if (animator != null)
            animator.SetBool("isWalking", moveInput != Vector2.zero);
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }
}
