#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiniShooterGame.Player
{
    /// <summary>
    /// Player dash ability controller.
    /// Provides burst speed displacement in the moving or facing direction,
    /// temporary invulnerability frames (I-frames), and cooldown management.
    /// Supports both UI touch button triggers and desktop keyboard shortcuts (Space / Shift).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerDash : MonoBehaviour
    {
        [Header("Dash Configuration")]
        [Tooltip("Speed multiplier applied to base move speed during dash.")]
        [SerializeField] private float dashSpeedMultiplier = 3.2f;

        [Tooltip("Duration of the dash in seconds.")]
        [SerializeField] private float dashDuration = 0.2f;

        [Tooltip("Cooldown period in seconds between dashes.")]
        [SerializeField] private float dashCooldown = 1.5f;

        [Header("Visual & I-Frames Feedback")]
        [Tooltip("Alpha transparency during invulnerability frames.")]
        [SerializeField] private float invulnerableAlpha = 0.5f;

        [Tooltip("Optional dash particle trail effect.")]
        [SerializeField] private ParticleSystem dashTrailParticles;

        [Header("Events")]
        public UnityEvent OnDashStarted;
        public UnityEvent OnDashEnded;

        private Rigidbody2D _rb;
        private PlayerMovement _playerMovement;
        private SpriteRenderer _spriteRenderer;
        private Color _originalSpriteColor;
        private float _lastDashTime = -999f;
        private bool _isDashing;
        private bool _isInvulnerable;

        /// <summary>
        /// True if the player is currently executing a dash.
        /// </summary>
        public bool IsDashing => _isDashing;

        /// <summary>
        /// True if the player is currently invulnerable to incoming damage.
        /// </summary>
        public bool IsInvulnerable => _isInvulnerable;

        /// <summary>
        /// True if the dash is off cooldown and ready to use.
        /// </summary>
        public bool CanDash => Time.time >= _lastDashTime + dashCooldown && !_isDashing;

        /// <summary>
        /// Cooldown progress from 0 (ready) to 1 (just used).
        /// </summary>
        public float CooldownProgress => Mathf.Clamp01((_lastDashTime + dashCooldown - Time.time) / dashCooldown);

        /// <summary>
        /// Permanently reduces dash cooldown duration.
        /// </summary>
        public void ReduceCooldown(float seconds)
        {
            dashCooldown = Mathf.Max(0.5f, dashCooldown - seconds);
        }

        [Header("Editor & Desktop Controls")]
        [Tooltip("If enabled, allows keyboard keys (Space, Left Shift, E) and Right Mouse Click to trigger dash.")]
        [SerializeField] private bool allowKeyboardShortcut = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _playerMovement = GetComponent<PlayerMovement>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_spriteRenderer != null)
            {
                _originalSpriteColor = _spriteRenderer.color;
            }
        }

        private void Update()
        {
            if (allowKeyboardShortcut)
            {
                CheckKeyboardInput();
            }
        }

        private void CheckKeyboardInput()
        {
            bool trigger = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                trigger = Keyboard.current.spaceKey.wasPressedThisFrame ||
                          Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                          Keyboard.current.eKey.wasPressedThisFrame;
            }
            if (!trigger && Mouse.current != null)
            {
                trigger = Mouse.current.rightButton.wasPressedThisFrame;
            }
#else
            trigger = Input.GetKeyDown(KeyCode.Space) ||
                      Input.GetKeyDown(KeyCode.LeftShift) ||
                      Input.GetKeyDown(KeyCode.E) ||
                      Input.GetMouseButtonDown(1);
#endif

            if (trigger)
            {
                TriggerDash();
            }
        }

        /// <summary>
        /// Public entry point for UI Dash button and gameplay scripts.
        /// </summary>
        public void TriggerDash()
        {
            if (!CanDash || !enabled) return;

            StartCoroutine(DashRoutine());
        }

        private IEnumerator DashRoutine()
        {
            _isDashing = true;
            _isInvulnerable = true;
            _lastDashTime = Time.time;

            OnDashStarted?.Invoke();

            if (dashTrailParticles != null)
            {
                dashTrailParticles.Play();
            }

            // Lock regular player movement input
            if (_playerMovement != null)
            {
                _playerMovement.IsMovementLocked = true;
            }

            // Determine dash direction: use current input vector or character facing direction
            Vector2 dashDirection = Vector2.zero;
            if (_playerMovement != null && _playerMovement.CurrentInput.sqrMagnitude > 0.001f)
            {
                dashDirection = _playerMovement.CurrentInput.normalized;
            }
            else if (TryGetComponent<CharacterVisualController>(out var visual))
            {
                // Dash in the direction the character is currently facing
                switch (visual.CurrentFacing)
                {
                    case CharacterFacing.Side:
                        dashDirection = visual.IsFacingLeft ? Vector2.left : Vector2.right;
                        break;
                    case CharacterFacing.Up:
                        dashDirection = Vector2.up;
                        break;
                    case CharacterFacing.Down:
                    default:
                        dashDirection = Vector2.down;
                        break;
                }
            }
            else if (TryGetComponent<PlayerShooter>(out var shooter) && shooter.CurrentTarget != null)
            {
                dashDirection = ((Vector2)shooter.CurrentTarget.position - (Vector2)transform.position).normalized;
            }
            else
            {
                // Dash in the direction the sprite is currently facing
                float angleRad = (_rb.rotation + 90f) * Mathf.Deg2Rad;
                dashDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
            }

            float baseSpeed = _playerMovement != null ? _playerMovement.MoveSpeed : 6f;
            Vector2 dashVelocity = dashDirection * (baseSpeed * dashSpeedMultiplier);

            // Apply invulnerability visual (translucent sprite on player and gun mount)
            var allRenderers = GetComponentsInChildren<SpriteRenderer>();
            var originalColors = new Dictionary<SpriteRenderer, Color>();
            foreach (var r in allRenderers)
            {
                if (r != null)
                {
                    originalColors[r] = r.color;
                    Color c = r.color;
                    c.a = invulnerableAlpha;
                    r.color = c;
                }
            }

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                // Drive physics velocity during dash
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = dashVelocity;
#else
                _rb.velocity = dashVelocity;
#endif
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            // Restore state
            _isDashing = false;
            _isInvulnerable = false;

            if (_playerMovement != null)
            {
                _playerMovement.IsMovementLocked = false;
            }

            // Restore original sprite alphas
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.color = kvp.Value;
                }
            }

            OnDashEnded?.Invoke();
        }
    }
}
