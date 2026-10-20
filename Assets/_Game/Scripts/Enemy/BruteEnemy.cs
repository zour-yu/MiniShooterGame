using System.Collections;
using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Enemy
{
    public enum BruteState
    {
        Walking,
        StompWindup,
        Stomping,
        Recovery
    }

    /// <summary>
    /// Heavy Armored Brute AI (Mini-Boss / Tank):
    /// 1. High HP and 70% knockback resistance.
    /// 2. Shockwave Ground Stomp: winds up when player is in close range, then slams the ground,
    ///    dealing heavy damage and knocking the player back.
    /// 3. Enrages below 50% HP: increases speed and glows glowing red!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(EnemyHealth))]
    public class BruteEnemy : MonoBehaviour
    {
        [Header("Movement & Armor")]
        [Tooltip("Standard walking speed in units per second.")]
        [SerializeField] private float moveSpeed = 2.0f;

        [Tooltip("Speed multiplier when enraged (below 50% HP).")]
        [SerializeField] private float enrageSpeedMultiplier = 1.5f;

        [Tooltip("Rotation speed towards the player in degrees per second.")]
        [SerializeField] private float rotationSpeed = 240f;

        [Tooltip("Sprite facing angle offset (-90 if art faces UP, 0 if art faces RIGHT).")]
        [SerializeField] private float spriteAngleOffset = -90f;

        [Header("Ground Stomp Attack")]
        [Tooltip("Distance threshold to initiate the ground stomp.")]
        [SerializeField] private float stompTriggerDistance = 3.2f;

        [Tooltip("Radius of the ground stomp shockwave.")]
        [SerializeField] private float stompRadius = 3.5f;

        [Tooltip("Damage inflicted by the stomp shockwave.")]
        [SerializeField] private float stompDamage = 22f;

        [Tooltip("Knockback force inflicted on the player by the shockwave.")]
        [SerializeField] private float stompKnockbackForce = 12f;

        [Tooltip("Windup tell duration in seconds before stomping.")]
        [SerializeField] private float stompWindupDuration = 0.5f;

        [Tooltip("Cooldown between successive stomps.")]
        [SerializeField] private float stompCooldown = 4.0f;

        [Header("Contact Damage")]
        [Tooltip("Contact collision damage.")]
        [SerializeField] private float contactDamage = 18f;

        [Tooltip("Contact damage interval.")]
        [SerializeField] private float contactCooldown = 1.0f;

        [Header("Visual Feedback")]
        [Tooltip("Color tint when enraged.")]
        [SerializeField] private Color enrageColor = new Color(1f, 0.25f, 0.25f);

        [Tooltip("Color tint during stomp windup.")]
        [SerializeField] private Color stompWindupColor = new Color(1f, 0.85f, 0.2f);

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private EnemyHealth _health;
        private Transform _playerTransform;
        private Color _originalColor = Color.white;
        private BruteState _state = BruteState.Walking;

        private float _nextStompTime = 0f;
        private float _lastContactTime = -999f;
        private bool _isEnraged = false;

        // Knockback
        private Vector2 _knockbackVelocity;
        private float _hitStunTimer;

        public BruteState CurrentState => _state;
        public bool IsEnraged => _isEnraged;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<EnemyHealth>();

            if (_spriteRenderer != null) _originalColor = _spriteRenderer.color;

            // Ensure 70% knockback resistance
            if (_health != null)
            {
                _health.KnockbackResistance = 0.70f;
                _health.OnDamaged += HandleDamaged;
            }

            ValidateRigidbody();
        }

        private void Start()
        {
            FindPlayer();
            _nextStompTime = Time.time + 1.5f;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDamaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(float dmg)
        {
            if (!_isEnraged && _health != null && _health.HealthPercent <= 0.5f)
            {
                TriggerEnrage();
            }
        }

        private void TriggerEnrage()
        {
            _isEnraged = true;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = enrageColor;
                _originalColor = enrageColor;
            }
            Debug.Log("[BruteEnemy] Heavy Brute entered ENRAGE MODE!");
        }

        public void ApplyKnockback(object[] args)
        {
            if (args != null && args.Length >= 2)
            {
                Vector2 dir = (Vector2)args[0];
                float force = (float)args[1];
                float stun = args.Length >= 3 ? (float)args[2] : 0.08f;
                ApplyKnockback(dir, force, stun);
            }
        }

        public void ApplyKnockback(Vector2 direction, float force, float stunDuration = 0.08f)
        {
            // Reduced knockback for heavy tank
            _knockbackVelocity = direction.normalized * (force * 0.35f);
            _hitStunTimer = stunDuration * 0.5f;
        }

        private void FixedUpdate()
        {
            if (_knockbackVelocity.sqrMagnitude > 0.01f)
            {
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = _knockbackVelocity;
#else
                _rb.velocity = _knockbackVelocity;
#endif
                _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, 18f * Time.fixedDeltaTime);
                return;
            }

            if (_hitStunTimer > 0f)
            {
                _hitStunTimer -= Time.fixedDeltaTime;
                return;
            }

            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            switch (_state)
            {
                case BruteState.Walking:
                    ExecuteWalking();
                    RotateTowardsPlayer();
                    break;

                case BruteState.StompWindup:
#if UNITY_6000_0_OR_NEWER
                    _rb.linearVelocity = Vector2.zero;
#else
                    _rb.velocity = Vector2.zero;
#endif
                    RotateTowardsPlayer();
                    break;

                case BruteState.Stomping:
                case BruteState.Recovery:
#if UNITY_6000_0_OR_NEWER
                    _rb.linearVelocity = Vector2.zero;
#else
                    _rb.velocity = Vector2.zero;
#endif
                    break;
            }
        }

        private void ExecuteWalking()
        {
            Vector2 currentPos = _rb.position;
            Vector2 playerPos = (Vector2)_playerTransform.position;
            Vector2 toPlayer = playerPos - currentPos;
            float dist = toPlayer.magnitude;

            if (dist <= stompTriggerDistance && Time.time >= _nextStompTime)
            {
                StartCoroutine(StompRoutine());
                return;
            }

            float currentSpeed = _isEnraged ? moveSpeed * enrageSpeedMultiplier : moveSpeed;
            Vector2 velocity = toPlayer.normalized * currentSpeed;

#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
#else
            _rb.velocity = velocity;
#endif
        }

        private IEnumerator StompRoutine()
        {
            _state = BruteState.StompWindup;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = stompWindupColor;
            }

            // Squash down preparing to jump/stomp
            Vector3 startScale = transform.localScale;
            transform.localScale = Vector3.Scale(startScale, new Vector3(1.3f, 0.75f, 1f));

            yield return new WaitForSeconds(stompWindupDuration);

            _state = BruteState.Stomping;
            transform.localScale = startScale;

            // Execute Ground Shockwave
            ExecuteGroundShockwave();

            _state = BruteState.Recovery;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            yield return new WaitForSeconds(0.4f);

            _nextStompTime = Time.time + (_isEnraged ? stompCooldown * 0.7f : stompCooldown);
            _state = BruteState.Walking;
        }

        private void ExecuteGroundShockwave()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(_rb.position, stompRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit.gameObject == gameObject) continue;

                if (hit.CompareTag("Player") || hit.GetComponent<MiniShooterGame.Player.PlayerHealth>() != null)
                {
                    if (hit.TryGetComponent<IDamageable>(out var dmg))
                    {
                        dmg.TakeDamage(stompDamage);
                    }

                    // Push player away with physics impulse
                    if (hit.TryGetComponent<Rigidbody2D>(out var playerRb))
                    {
                        Vector2 pushDir = ((Vector2)hit.transform.position - _rb.position).normalized;
                        if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector2.up;
                        playerRb.AddForce(pushDir * stompKnockbackForce, ForceMode2D.Impulse);
                    }
                }
            }

            // Visual ring expand
            StartCoroutine(ShockwaveVisualRoutine());
        }

        private IEnumerator ShockwaveVisualRoutine()
        {
            // Brief visual squash pop
            Vector3 original = transform.localScale;
            transform.localScale = Vector3.Scale(original, new Vector3(1.4f, 1.4f, 1f));
            yield return new WaitForSeconds(0.12f);
            transform.localScale = original;
        }

        private void RotateTowardsPlayer()
        {
            if (_playerTransform == null) return;
            Vector2 direction = (Vector2)_playerTransform.position - _rb.position;
            if (direction.sqrMagnitude < 0.001f) return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            float newAngle = Mathf.MoveTowardsAngle(_rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newAngle);
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                return;
            }

            var pm = Object.FindFirstObjectByType<MiniShooterGame.Player.PlayerMovement>();
            if (pm != null)
            {
                _playerTransform = pm.transform;
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDealContactDamage(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealContactDamage(other.gameObject);
        }

        private void TryDealContactDamage(GameObject targetObj)
        {
            if (Time.time < _lastContactTime + contactCooldown) return;

            if ((targetObj.CompareTag("Player") || targetObj.GetComponent<MiniShooterGame.Player.PlayerHealth>() != null) &&
                targetObj.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(contactDamage);
                _lastContactTime = Time.time;
            }
        }

        private void ValidateRigidbody()
        {
            if (_rb == null) return;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.mass = 3.5f; // Heavy tank mass
        }
    }
}
