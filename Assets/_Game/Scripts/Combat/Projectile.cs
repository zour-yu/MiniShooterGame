using UnityEngine;

namespace MiniShooterGame.Combat
{
    /// <summary>
    /// Handles projectile movement, lifetime cleanup, and 2D collision/damage detection.
    /// Can move via transform translation or Rigidbody2D velocity.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [Tooltip("Travel speed in units per second.")]
        [SerializeField] private float speed = 15f;

        [Tooltip("Damage dealt upon hitting an enemy.")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Time in seconds before the projectile self-destructs if it hits nothing.")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("The tag of objects that this projectile damages and collides with.")]
        [SerializeField] private string targetTag = "Enemy";

        [Header("Impact Feedback (Optional)")]
        [Tooltip("Optional visual effect / particles spawned at hit point.")]
        [SerializeField] private GameObject hitEffectPrefab;

        private Vector2 _moveDirection = Vector2.up;
        private Rigidbody2D _rb;
        private bool _isInitialized;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 0f;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // Ensure collider is set to trigger
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            // Auto-cleanup after lifetime expires
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            // If no Rigidbody2D is attached, translate manually along the forward/up vector
            if (_rb == null)
            {
                transform.Translate(_moveDirection * (speed * Time.deltaTime), Space.World);
            }
        }

        private void FixedUpdate()
        {
            // If Rigidbody2D is present, drive velocity through physics for optimal 2D collision precision
            if (_rb != null && _isInitialized)
            {
                Vector2 targetVelocity = _moveDirection * speed;
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = targetVelocity;
#else
                _rb.velocity = targetVelocity;
#endif
            }
        }

        /// <summary>
        /// Initializes the projectile with direction, optional shooter collider to ignore, and custom stats.
        /// </summary>
        /// <param name="direction">Normalized travel direction.</param>
        /// <param name="shooterCollider">Optional collider of the entity that fired this projectile.</param>
        /// <param name="customDamage">Optional override for damage (set <= 0 to use inspector default).</param>
        /// <param name="customSpeed">Optional override for speed (set <= 0 to use inspector default).</param>
        public void Initialize(Vector2 direction, Collider2D shooterCollider = null, float customDamage = -1f, float customSpeed = -1f)
        {
            _moveDirection = direction.normalized;

            if (customDamage > 0f) damage = customDamage;
            if (customSpeed > 0f) speed = customSpeed;

            // Ignore collision with the shooter's own collider
            if (shooterCollider != null)
            {
                Collider2D myCol = GetComponent<Collider2D>();
                if (myCol != null)
                {
                    Physics2D.IgnoreCollision(myCol, shooterCollider, true);
                }
            }

            // Orient the projectile visual towards the flight direction (assuming art points UP)
            float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _isInitialized = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Explicitly ignore the player and pickups
            if (other.CompareTag("Player") || other.CompareTag("Pickup")) return;

            // Hit an obstacle / solid cover (blocks bullet)
            if (other.CompareTag("Obstacle"))
            {
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
                return;
            }

            // Only interact with designated target tag (e.g. "Enemy")
            if (other.CompareTag(targetTag))
            {
                // Inflict damage if target implements IDamageable
                if (other.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }

                // Spawn hit impact effect if assigned
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                }

                // Destroy projectile on impact
                Destroy(gameObject);
            }
        }
    }
}
