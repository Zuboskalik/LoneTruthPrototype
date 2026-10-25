using UnityEngine;

namespace LoneTruth.Player
{
    [RequireComponent(typeof(Rigidbody))]
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

        private Rigidbody rb;
        private float moveInput;
        private bool facingRight = true;
        private bool isGrounded;

        private bool isDodging;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private float dodgeDirection;

        private bool isAiming;
        private bool hasAimParam;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int AimParam = Animator.StringToHash("Aim");

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                foreach (AnimatorControllerParameter p in animator.parameters)
                {
                    hasAimParam |= p.nameHash == AimParam && p.type == AnimatorControllerParameterType.Bool;
                }
            }
        }

        /// <summary>Aiming (set by WeaponController): Holt stands still and A / D only turn him.</summary>
        public void SetAiming(bool value)
        {
            isAiming = value;
            if (hasAimParam)
            {
                animator.SetBool(AimParam, value);
            }
        }

        public bool IsAiming => isAiming;

        /// <summary>Horizontal input used while a cutscene owns the player (-1..1, 0 = stand).</summary>
        public float ScriptedInput { get; set; }

        private static bool InputAllowed =>
            LoneTruth.Core.GameMaster.Instance == null ||
            LoneTruth.Core.GameMaster.Instance.IsInState(LoneTruth.Core.GameState.Exploration);

        public void PlayAnimationTrigger(string trigger)
        {
            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == trigger && p.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(trigger);
                    return;
                }
            }
        }

        private void Update()
        {
            bool allowed = InputAllowed;
            moveInput = allowed ? Input.GetAxisRaw("Horizontal") : ScriptedInput;

            if (dodgeCooldownTimer > 0f)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }

            if (isAiming)
            {
                // Turn on the spot, like Resident Evil's aim.
                if (moveInput > 0f && !facingRight || moveInput < 0f && facingRight)
                {
                    Flip();
                }

                moveInput = 0f;
            }

            if (allowed && !isAiming && Input.GetKeyDown(KeyCode.LeftShift))
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
                rb.linearVelocity = new Vector3(dodgeDirection * dodgeSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
            }
            else
            {
                rb.linearVelocity = new Vector3(moveInput * moveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);
            }
        }

        private void CheckGrounded()
        {
            if (groundCheck == null)
            {
                isGrounded = false;
                return;
            }

            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
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
