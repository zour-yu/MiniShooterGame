#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;
using MiniShooterGame.UI;

namespace MiniShooterGame.Player
{
    /// <summary>
    /// Top-down 2D player movement controller.
    /// Drives Rigidbody2D velocity using VirtualJoystick input (with keyboard fallback for Editor testing)
    /// and smoothly aligns rotation towards the moving direction.
    /// Supports both Unity's New Input System and Legacy Input Manager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Input Source")]
        [Tooltip("Reference to the Virtual Joystick. If not set, will attempt to find one in the scene.")]
        [SerializeField] private VirtualJoystick joystick;

        [Tooltip("If enabled, allows WASD / Arrow keys for testing in the Unity Editor or desktop builds.")]
        [SerializeField] private bool allowKeyboardFallback = true;

        [Header("Movement Settings")]
        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Rotation Settings")]
        [Tooltip("Whether the player rotates to face movement direction.")]
        [SerializeField] private bool rotateTowardsMovement = true;

        [Tooltip("Speed of rotation in degrees per second.")]
        [SerializeField] private float smoothTurnSpeed = 720f;

        [Tooltip("Sprite facing angle offset. Set to 0 if sprite faces Right (+X), or -90 if sprite faces Up (+Y).")]
        [SerializeField] private float spriteAngleOffset = -90f;

        private Rigidbody2D _rb;
        private Vector2 _currentInput;

        /// <summary>
        /// Gets the current movement velocity.
        /// </summary>
        public Vector2 Velocity => _rb != null ? GetLinearVelocity(_rb) : Vector2.zero;

        /// <summary>
        /// True if the player is currently moving.
        /// </summary>
        public bool IsMoving => _currentInput.sqrMagnitude > 0.001f;

        /// <summary>
        /// Base movement speed in units per second.
        /// </summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>
        /// Permanently modifies player base move speed by a multiplier.
        /// </summary>
        public void ModifyMoveSpeed(float multiplier)
        {
            moveSpeed *= multiplier;
        }

        /// <summary>
        /// Normalized direction of the current input vector.
        /// </summary>
        public Vector2 CurrentInput => _currentInput;

        /// <summary>
        /// When true (e.g. during a dash), PlayerMovement will not overwrite the Rigidbody's velocity.
        /// </summary>
        public bool IsMovementLocked { get; set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            ValidateRigidbodySettings();

            if (joystick == null)
            {
                joystick = Object.FindFirstObjectByType<VirtualJoystick>();
            }
        }

        private void Update()
        {
            GatherInput();
        }

        private void FixedUpdate()
        {
            if (!IsMovementLocked)
            {
                ApplyMovement();
            }
            ApplyRotation();
        }

        /// <summary>
        /// Reads input from virtual joystick or keyboard fallback.
        /// Automatically handles both the New Input System and Legacy Input Manager.
        /// </summary>
        private void GatherInput()
        {
            Vector2 input = Vector2.zero;

            if (joystick != null && joystick.IsActive)
            {
                input = joystick.Input;
            }
            else if (allowKeyboardFallback)
            {
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    float h = 0f;
                    float v = 0f;

                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                    if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                    if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;

                    input = new Vector2(h, v);
                }
#else
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                input = new Vector2(h, v);
#endif
                if (input.sqrMagnitude > 1f)
                {
                    input.Normalize();
                }
            }

            _currentInput = input;
        }

        /// <summary>
        /// Applies physics velocity to Rigidbody2D.
        /// </summary>
        private void ApplyMovement()
        {
            Vector2 targetVelocity = _currentInput * moveSpeed;
            SetLinearVelocity(_rb, targetVelocity);
        }

        /// <summary>
        /// Smoothly rotates the Rigidbody2D towards the movement angle.
        /// </summary>
        private void ApplyRotation()
        {
            // If shooting an enemy in range, prioritize aiming directly at the enemy
            if (TryGetComponent<PlayerShooter>(out var shooter) && shooter.CurrentTarget != null)
            {
                Vector2 toTarget = (Vector2)shooter.CurrentTarget.position - _rb.position;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float aimAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg + spriteAngleOffset;
                    float newAimAngle = Mathf.MoveTowardsAngle(_rb.rotation, aimAngle, smoothTurnSpeed * 1.5f * Time.fixedDeltaTime);
                    _rb.MoveRotation(newAimAngle);
                    return;
                }
            }

            if (!rotateTowardsMovement || _currentInput.sqrMagnitude < 0.001f) return;

            // Calculate target angle based on direction vector
            float targetAngle = Mathf.Atan2(_currentInput.y, _currentInput.x) * Mathf.Rad2Deg + spriteAngleOffset;

            // Smoothly interpolate angle
            float currentAngle = _rb.rotation;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, smoothTurnSpeed * Time.fixedDeltaTime);

            _rb.MoveRotation(newAngle);
        }

        /// <summary>
        /// Ensures optimal 2D top-down physics settings on Rigidbody2D.
        /// </summary>
        private void ValidateRigidbodySettings()
        {
            if (_rb == null) return;

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Physics collisions won't impart unintended spin
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth visuals between FixedUpdate steps
        }

        #region Cross-Unity Version Compatibility Helper
        private static void SetLinearVelocity(Rigidbody2D rb, Vector2 velocity)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = velocity;
#else
            rb.velocity = velocity;
#endif
        }

        private static Vector2 GetLinearVelocity(Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
        #endregion
    }
}
