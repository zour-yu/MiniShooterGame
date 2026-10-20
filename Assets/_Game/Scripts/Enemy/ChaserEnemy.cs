using System.Collections;
using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Enemy
{
    public enum ChaserState
    {
        Chasing,
        LungeWindup,
        Lunging,
        LungeRecovery
    }

    /// <summary>
    /// Upgraded Chaser / Swarm Zombie AI (Code Monkey style):
    /// 1. Flocking & soft separation so enemies encircle the player instead of stacking.
    /// 2. Physics knockback & hit-stun when shot.
    /// 3. Lunge Attack: telegraphs with an orange windup when in close range, then launches forward at high speed!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class ChaserEnemy : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Chasing movement speed in units per second.")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float rotationSpeed = 360f;

        [Tooltip("Facing angle offset. -90 if sprite art points UP, 0 if sprite points RIGHT.")]
        [SerializeField] private float spriteAngleOffset = -90f;

        [Header("Horde Flocking & Spacing")]
        [Tooltip("Radius around this enemy to check for neighbors to separate from.")]
        [SerializeField] private float separationRadius = 1.1f;

        [Tooltip("Weight of separation repulsion force.")]
        [SerializeField] private float separationWeight = 0.65f;

        [Header("Lunge Attack Mechanic")]
        [Tooltip("Whether this enemy can lunge at the player when in close proximity.")]
        [SerializeField] private bool canLunge = true;

        [Tooltip("Distance to player that triggers the lunge windup.")]
        [SerializeField] private float lungeTriggerDistance = 2.2f;

        [Tooltip("Movement speed multiplier during the lunge dash.")]
        [SerializeField] private float lungeSpeedMultiplier = 2.6f;

        [Tooltip("Duration of the windup tell before lunging (in seconds).")]
        [SerializeField] private float lungeWindupDuration = 0.22f;

        [Tooltip("Duration of the forward lunge dash (in seconds).")]
        [SerializeField] private float lungeDuration = 0.20f;

        [Tooltip("Cooldown between successive lunges.")]
        [SerializeField] private float lungeCooldown = 2.5f;

        [Header("Combat Settings")]
        [Tooltip("Damage dealt to the player on contact.")]
        [SerializeField] private float contactDamage = 15f;

        [Tooltip("Time in seconds between successive contact damage hits.")]
        [SerializeField] private float attackCooldown = 0.8f;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Transform _playerTransform;
        private float _lastAttackTime = -999f;
        private float _nextLungeTime = 0f;

        // Knockback & Hit Stun
        private Vector2 _knockbackVelocity;
        private float _hitStunTimer;

        // Lunge State
        private ChaserState _state = ChaserState.Chasing;
        private Vector2 _lungeDirection;
        private Color _baseColor = Color.white;

        public ChaserState CurrentState => _state;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
            }
            ValidateRigidbodySettings();
        }

        private void Start()
        {
            FindPlayer();
            _nextLungeTime = Time.time + Random.Range(0.5f, 1.5f); // Stagger initial lunges
        }

        public void ApplyKnockback(object[] args)
        {
            if (args != null && args.Length >= 2)
            {
                Vector2 dir = (Vector2)args[0];
                float force = (float)args[1];
                float stun = args.Length >= 3 ? (float)args[2] : 0.12f;
                ApplyKnockback(dir, force, stun);
            }
        }

        /// <summary>
        /// Delivers physics impulse knockback and halts chasing locomotion briefly.
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float force, float stunDuration = 0.12f)
        {
            _knockbackVelocity = direction.normalized * force;
            _hitStunTimer = stunDuration;

            // If knocked back during windup, interrupt the lunge
            if (_state == ChaserState.LungeWindup)
            {
                StopAllCoroutines();
                ResetVisuals();
                _state = ChaserState.Chasing;
            }
        }

        private void FixedUpdate()
        {
            // 1. Process active knockback impulse
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

            // 2. Process hit stun
            if (_hitStunTimer > 0f)
            {
                _hitStunTimer -= Time.fixedDeltaTime;
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = Vector2.zero;
#else
                _rb.velocity = Vector2.zero;
#endif
                return;
            }

            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            // 3. State Machine Execution
            switch (_state)
            {
                case ChaserState.Chasing:
                    ExecuteChasing();
                    break;

                case ChaserState.LungeWindup:
#if UNITY_6000_0_OR_NEWER
                    _rb.linearVelocity = Vector2.zero;
#else
                    _rb.velocity = Vector2.zero;
#endif
                    RotateTowardsPlayer();
                    break;

                case ChaserState.Lunging:
                    ExecuteLunging();
                    break;

                case ChaserState.LungeRecovery:
#if UNITY_6000_0_OR_NEWER
                    _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, Vector2.zero, 12f * Time.fixedDeltaTime);
#else
                    _rb.velocity = Vector2.MoveTowards(_rb.velocity, Vector2.zero, 12f * Time.fixedDeltaTime);
#endif
                    break;
            }
        }

        private void ExecuteChasing()
        {
            Vector2 currentPos = _rb.position;
            Vector2 targetPos = (Vector2)_playerTransform.position;
            Vector2 toPlayer = targetPos - currentPos;
            float distToPlayer = toPlayer.magnitude;

            // Check if ready to trigger lunge
            if (canLunge && distToPlayer <= lungeTriggerDistance && Time.time >= _nextLungeTime)
            {
                StartCoroutine(LungeRoutine(toPlayer.normalized));
                return;
            }

            // Flocking separation: steer away from dense clusters of fellow enemies
            Vector2 separation = CalculateSeparation();
            Vector2 desiredDirection = (toPlayer.normalized + separation * separationWeight).normalized;

            Vector2 velocity = desiredDirection * moveSpeed;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
#else
            _rb.velocity = velocity;
#endif
            RotateTowardsDirection(desiredDirection);
        }

        private void ExecuteLunging()
        {
            Vector2 velocity = _lungeDirection * (moveSpeed * lungeSpeedMultiplier);
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
#else
            _rb.velocity = velocity;
#endif
            RotateTowardsDirection(_lungeDirection);
        }

        private IEnumerator LungeRoutine(Vector2 initialDirection)
        {
            _state = ChaserState.LungeWindup;
            _lungeDirection = initialDirection;

            // Telegraph: flash amber/orange and squish back slightly
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 0.55f, 0.15f);
            }

            yield return new WaitForSeconds(lungeWindupDuration);

            // Re-target player at exact moment of lunge launch for accuracy
            if (_playerTransform != null)
            {
                _lungeDirection = ((Vector2)_playerTransform.position - _rb.position).normalized;
            }

            _state = ChaserState.Lunging;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
            }

            yield return new WaitForSeconds(lungeDuration);

            // Recovery phase
            _state = ChaserState.LungeRecovery;
            ResetVisuals();

            yield return new WaitForSeconds(0.35f);

            _nextLungeTime = Time.time + lungeCooldown;
            _state = ChaserState.Chasing;
        }

        private void ResetVisuals()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }
        }

        /// <summary>
        /// Calculates a soft repulsive vector away from nearby enemy colliders to create flocking separation.
        /// </summary>
        private Vector2 CalculateSeparation()
        {
            Vector2 separation = Vector2.zero;
            Collider2D[] nearby = Physics2D.OverlapCircleAll(_rb.position, separationRadius);
            int count = 0;

            for (int i = 0; i < nearby.Length; i++)
            {
                Collider2D col = nearby[i];
                if (col.gameObject == gameObject) continue;
                if (!col.CompareTag("Enemy")) continue;

                Vector2 diff = _rb.position - (Vector2)col.transform.position;
                float dist = diff.magnitude;
                if (dist > 0.001f && dist < separationRadius)
                {
                    separation += diff.normalized * ((separationRadius - dist) / separationRadius);
                    count++;
                }
            }

            return count > 0 ? separation.normalized : Vector2.zero;
        }

        private void RotateTowardsPlayer()
        {
            if (_playerTransform == null) return;
            Vector2 direction = (Vector2)_playerTransform.position - _rb.position;
            RotateTowardsDirection(direction);
        }

        private void RotateTowardsDirection(Vector2 direction)
        {
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
            TryDealDamage(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealDamage(other.gameObject);
        }

        private void TryDealDamage(GameObject targetObj)
        {
            if (Time.time < _lastAttackTime + attackCooldown) return;

            if ((targetObj.CompareTag("Player") || targetObj.GetComponent<MiniShooterGame.Player.PlayerHealth>() != null) &&
                targetObj.TryGetComponent<IDamageable>(out var damageable))
            {
                float damage = _state == ChaserState.Lunging ? contactDamage * 1.3f : contactDamage;
                damageable.TakeDamage(damage);
                _lastAttackTime = Time.time;
            }
        }

        private void ValidateRigidbodySettings()
        {
            if (_rb == null) return;

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }
}
