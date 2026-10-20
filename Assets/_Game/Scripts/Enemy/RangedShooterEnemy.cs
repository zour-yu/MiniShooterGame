using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Enemy
{
    /// <summary>
    /// Upgraded Ranged Tactical Shooter / Sniper AI:
    /// Maintains tactical standoff distance, kites away from aggressive players,
    /// separates from other enemies, and renders a telegraphed aiming laser line before firing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class RangedShooterEnemy : MonoBehaviour
    {
        [Header("Movement & Spacing")]
        [Tooltip("Movement speed while repositioning or kiting.")]
        [SerializeField] private float moveSpeed = 2.8f;

        [Tooltip("Ideal combat distance from player.")]
        [SerializeField] private float preferredDistance = 6.0f;

        [Tooltip("Distance threshold where enemy begins retreating.")]
        [SerializeField] private float retreatDistance = 3.8f;

        [Tooltip("Rotation speed towards the player in degrees per second.")]
        [SerializeField] private float rotationSpeed = 300f;

        [Tooltip("Sprite facing angle offset (-90 if art faces UP, 0 if art faces RIGHT).")]
        [SerializeField] private float spriteAngleOffset = -90f;

        [Header("Horde Flocking & Spacing")]
        [Tooltip("Radius around this enemy to check for neighbors to separate from.")]
        [SerializeField] private float separationRadius = 1.3f;

        [Tooltip("Weight of separation repulsion force.")]
        [SerializeField] private float separationWeight = 0.6f;

        [Header("Ranged Combat")]
        [Tooltip("Projectile prefab fired by this enemy.")]
        [SerializeField] private GameObject enemyProjectilePrefab;

        [Tooltip("Transform from which projectiles spawn.")]
        [SerializeField] private Transform firePoint;

        [Tooltip("Time between shots in seconds.")]
        [SerializeField] private float fireInterval = 2.4f;

        [Tooltip("Damage dealt per projectile hit.")]
        [SerializeField] private float projectileDamage = 12f;

        [Tooltip("Projectile travel speed.")]
        [SerializeField] private float projectileSpeed = 11f;

        [Header("Visual Telegraph & Laser")]
        [Tooltip("Sprite renderer for flash/aim feedback.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Telegraph warning color prior to firing.")]
        [SerializeField] private Color aimTelegraphColor = new Color(1f, 0.4f, 0.2f);

        [Tooltip("Duration of the telegraph tell prior to firing.")]
        [SerializeField] private float telegraphDuration = 0.55f;

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private Collider2D _enemyCollider;
        private LineRenderer _aimLaser;
        private float _fireTimer;
        private Color _originalColor = Color.white;
        private bool _isTelegraphing;

        // Knockback & Hit Stun
        private Vector2 _knockbackVelocity;
        private float _hitStunTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _enemyCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) _originalColor = spriteRenderer.color;

            ValidateRigidbody();
            if (firePoint == null) firePoint = transform;
            _fireTimer = fireInterval * Random.Range(0.6f, 1.0f); // Stagger initial shots

            SetupAimLaser();
        }

        private void SetupAimLaser()
        {
            _aimLaser = GetComponent<LineRenderer>();
            if (_aimLaser == null)
            {
                _aimLaser = gameObject.AddComponent<LineRenderer>();
            }

            _aimLaser.positionCount = 2;
            _aimLaser.startWidth = 0.04f;
            _aimLaser.endWidth = 0.04f;
            _aimLaser.material = new Material(Shader.Find("Sprites/Default"));
            _aimLaser.startColor = new Color(1f, 0.2f, 0.1f, 0.65f);
            _aimLaser.endColor = new Color(1f, 0.3f, 0.1f, 0.2f);
            _aimLaser.sortingOrder = 145;
            _aimLaser.enabled = false;
        }

        private void Start()
        {
            FindPlayer();
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
            // 1. Process active knockback
            if (_knockbackVelocity.sqrMagnitude > 0.01f)
            {
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = _knockbackVelocity;
#else
                _rb.velocity = _knockbackVelocity;
#endif
                _knockbackVelocity = Vector2.MoveTowards(_knockbackVelocity, Vector2.zero, 16f * Time.fixedDeltaTime);
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

            HandleMovement();
            RotateTowardsPlayer();
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                if (_aimLaser != null && _aimLaser.enabled) _aimLaser.enabled = false;
                return;
            }

            HandleShooting();
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

        private void HandleMovement()
        {
            Vector2 currentPos = _rb.position;
            Vector2 playerPos = (Vector2)_playerTransform.position;
            Vector2 toPlayer = playerPos - currentPos;
            float dist = toPlayer.magnitude;
            Vector2 dir = toPlayer.normalized;

            Vector2 moveDir = Vector2.zero;

            if (dist < retreatDistance)
            {
                // Kite backwards away from aggressive player
                moveDir = -dir;
            }
            else if (dist > preferredDistance + 1.2f)
            {
                // Advance closer to enter combat range
                moveDir = dir;
            }
            else
            {
                // Strafe perpendicular to player for tactical unpredictability
                Vector2 strafe = new Vector2(-dir.y, dir.x);
                moveDir = strafe * 0.45f;
            }

            // Flocking separation: avoid stacking with other snipers/mobs
            Vector2 separation = CalculateSeparation();
            Vector2 desiredDirection = (moveDir + separation * separationWeight).normalized;

            Vector2 velocity = desiredDirection * moveSpeed;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
#else
            _rb.velocity = velocity;
#endif
        }

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
            Vector2 direction = (Vector2)_playerTransform.position - _rb.position;
            if (direction.sqrMagnitude < 0.001f) return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            float newAngle = Mathf.MoveTowardsAngle(_rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newAngle);
        }

        private void HandleShooting()
        {
            _fireTimer -= Time.deltaTime;

            // Telegraph tell prior to firing
            if (_fireTimer <= telegraphDuration && !_isTelegraphing)
            {
                _isTelegraphing = true;
                if (spriteRenderer != null) spriteRenderer.color = aimTelegraphColor;
            }

            // Draw laser aim line during telegraph
            if (_isTelegraphing)
            {
                if (_aimLaser != null)
                {
                    _aimLaser.enabled = true;
                    Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
                    _aimLaser.SetPosition(0, startPos);
                    _aimLaser.SetPosition(1, _playerTransform.position);

                    // Pulse laser width as shot nears
                    float pulse = Mathf.Lerp(0.03f, 0.08f, 1f - (_fireTimer / telegraphDuration));
                    _aimLaser.startWidth = pulse;
                    _aimLaser.endWidth = pulse;
                }
            }
            else
            {
                if (_aimLaser != null && _aimLaser.enabled)
                {
                    _aimLaser.enabled = false;
                }
            }

            if (_fireTimer <= 0f)
            {
                FireProjectileAtPlayer();
                _fireTimer = fireInterval;
                _isTelegraphing = false;
                if (_aimLaser != null) _aimLaser.enabled = false;
                if (spriteRenderer != null) spriteRenderer.color = _originalColor;
            }
        }

        private void FireProjectileAtPlayer()
        {
            if (enemyProjectilePrefab == null || _playerTransform == null) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 aimDir = ((Vector2)_playerTransform.position - (Vector2)spawnPos).normalized;

            GameObject projObj = Instantiate(enemyProjectilePrefab, spawnPos, Quaternion.identity);

            // Configure projectile to target "Player" and collide with obstacles
            if (projObj.TryGetComponent<Projectile>(out var proj))
            {
                proj.Initialize(aimDir, _enemyCollider, projectileDamage, projectileSpeed, "Player");
            }
        }

        private void OnDisable()
        {
            if (_aimLaser != null) _aimLaser.enabled = false;
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
