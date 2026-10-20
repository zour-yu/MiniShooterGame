using System.Collections;
using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Enemy
{
    /// <summary>
    /// Volatile Kamikaze / Exploder AI (Code Monkey style):
    /// 1. Sprints aggressively at the player.
    /// 2. Detonation Countdown: when close or on death, stops, swells up, flashes bright red rapidly.
    /// 3. Explodes in a large AoE, damaging the player AND triggering glorious chain-reaction damage to nearby enemies!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(EnemyHealth))]
    public class ExploderEnemy : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Sprinting speed toward player.")]
        [SerializeField] private float sprintSpeed = 4.6f;

        [Tooltip("Rotation speed towards the player in degrees per second.")]
        [SerializeField] private float rotationSpeed = 360f;

        [Tooltip("Sprite facing angle offset (-90 if art faces UP, 0 if art faces RIGHT).")]
        [SerializeField] private float spriteAngleOffset = -90f;

        [Header("Detonation Mechanics")]
        [Tooltip("Proximity distance that triggers the detonation countdown.")]
        [SerializeField] private float detonationTriggerDistance = 1.8f;

        [Tooltip("Fuse countdown duration in seconds before exploding.")]
        [SerializeField] private float fuseDuration = 0.65f;

        [Tooltip("Blast radius of the explosion.")]
        [SerializeField] private float explosionRadius = 2.8f;

        [Tooltip("Damage dealt to the player caught in the blast.")]
        [SerializeField] private float damageToPlayer = 28f;

        [Tooltip("Damage dealt to other enemies caught in the blast (allows chain reactions!).")]
        [SerializeField] private float damageToEnemies = 55f;

        [Header("Visual Warning")]
        [Tooltip("Fuse flash warning color.")]
        [SerializeField] private Color fuseFlashColor = new Color(1f, 0.1f, 0.1f);

        [Tooltip("Optional explosion particle effect prefab.")]
        [SerializeField] private GameObject explosionEffectPrefab;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private EnemyHealth _health;
        private Transform _playerTransform;
        private Color _originalColor = Color.white;
        private Vector3 _originalScale = Vector3.one;

        private bool _isDetonating = false;
        private bool _hasExploded = false;

        // Knockback
        private Vector2 _knockbackVelocity;
        private float _hitStunTimer;

        public bool IsDetonating => _isDetonating;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<EnemyHealth>();
            _originalScale = transform.localScale;

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            if (_health != null)
            {
                _health.OnDied += HandleDeath;
            }

            ValidateRigidbody();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDied -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            if (!_hasExploded)
            {
                // Instant detonation on fatal damage to wipe surrounding mobs!
                DetonateImmediate();
            }
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

        public void ApplyKnockback(Vector2 direction, float force, float stunDuration = 0.12f)
        {
            _knockbackVelocity = direction.normalized * force;
            _hitStunTimer = stunDuration;
        }

        private void FixedUpdate()
        {
            if (_hasExploded) return;

            // 1. Process active knockback
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

            if (_isDetonating)
            {
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

            // 3. Sprint toward player
            Vector2 currentPos = _rb.position;
            Vector2 playerPos = (Vector2)_playerTransform.position;
            Vector2 toPlayer = playerPos - currentPos;
            float dist = toPlayer.magnitude;

            if (dist <= detonationTriggerDistance)
            {
                StartCoroutine(DetonationCountdownRoutine());
                return;
            }

            Vector2 velocity = toPlayer.normalized * sprintSpeed;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
#else
            _rb.velocity = velocity;
#endif
            RotateTowardsPlayer();
        }

        private IEnumerator DetonationCountdownRoutine()
        {
            _isDetonating = true;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif

            // Rapid flashing and pulsating swelling
            float elapsed = 0f;
            int flashState = 0;

            while (elapsed < fuseDuration)
            {
                elapsed += 0.09f;
                flashState++;

                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = flashState % 2 == 0 ? fuseFlashColor : Color.white;
                }

                // Swell up as fuse burns
                float scaleT = elapsed / fuseDuration;
                transform.localScale = Vector3.Lerp(_originalScale, Vector3.Scale(_originalScale, new Vector3(1.35f, 1.35f, 1f)), scaleT);

                yield return new WaitForSeconds(0.09f);
            }

            DetonateImmediate();
        }

        private void DetonateImmediate()
        {
            if (_hasExploded) return;
            _hasExploded = true;

            Vector2 blastCenter = transform.position;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(blastCenter, explosionRadius);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col.gameObject == gameObject) continue;

                // 1. Damage Player
                if (col.CompareTag("Player") || col.GetComponent<MiniShooterGame.Player.PlayerHealth>() != null)
                {
                    if (col.TryGetComponent<IDamageable>(out var playerDmg))
                    {
                        playerDmg.TakeDamage(damageToPlayer);
                    }

                    // Push player away with explosive blast impulse
                    if (col.TryGetComponent<Rigidbody2D>(out var playerRb))
                    {
                        Vector2 blastDir = ((Vector2)col.transform.position - blastCenter).normalized;
                        if (blastDir.sqrMagnitude < 0.001f) blastDir = Vector2.up;
                        playerRb.AddForce(blastDir * 14f, ForceMode2D.Impulse);
                    }
                }
                // 2. Damage Other Enemies (Chain Reactions!)
                else if (col.CompareTag("Enemy") || col.GetComponent<EnemyHealth>() != null)
                {
                    if (col.TryGetComponent<EnemyHealth>(out var enemyHealth))
                    {
                        Vector2 blastDir = ((Vector2)col.transform.position - blastCenter).normalized;
                        enemyHealth.TakeDamage(damageToEnemies, blastDir, 12f);
                    }
                }
            }

            // Visual explosion feedback
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, blastCenter, Quaternion.identity);
            }

            Destroy(gameObject);
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

        private void ValidateRigidbody()
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
