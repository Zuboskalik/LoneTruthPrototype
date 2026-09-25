using UnityEngine;

namespace LoneTruth.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Dodge Roll")]
        [SerializeField] private float dodgeSpeed = 12f;
        [SerializeField] private float dodgeDuration = 0.25f;
        [SerializeField] private float dodgeCooldown = 0.8f;

        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        private Rigidbody2D rb;
        private float moveInput;
        private bool facingRight = true;
        private bool isGrounded;

        private bool isDodging;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private float dodgeDirection;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            moveInput = Input.GetAxisRaw("Horizontal");

            if (dodgeCooldownTimer > 0f)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                TryDodgeRoll();
            }

            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0f)
                {
                    isDodging = false;
                }
            }

            UpdateFacingDirection();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            CheckGrounded();

            if (isDodging)
            {
                rb.linearVelocity = new Vector2(dodgeDirection * dodgeSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
            }
        }

        private void CheckGrounded()
        {
            if (groundCheck == null)
            {
                isGrounded = false;
                return;
            }

            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        private void UpdateFacingDirection()
        {
            if (isDodging)
            {
                return;
            }

            if (moveInput > 0f && !facingRight)
            {
                Flip();
            }
            else if (moveInput < 0f && facingRight)
            {
                Flip();
            }
        }

        private void Flip()
        {
            facingRight = !facingRight;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            float speed = isDodging ? Mathf.Abs(dodgeDirection) : Mathf.Abs(moveInput);
            animator.SetFloat(SpeedParam, speed);
        }

        private void TryDodgeRoll()
        {
            if (isDodging || dodgeCooldownTimer > 0f)
            {
                return;
            }

            dodgeDirection = facingRight ? 1f : -1f;
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                dodgeDirection = Mathf.Sign(moveInput);
            }

            isDodging = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown + dodgeDuration;
        }

        public bool IsFacingRight => facingRight;
        
public bool IsGrounded => isGrounded;
        public bool IsDodging => isDodging;

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
