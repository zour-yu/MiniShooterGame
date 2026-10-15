using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Enemy
{
    /// <summary>
    /// Ranged Enemy AI: Maintains tactical standoff distance from the player,
    /// kites backwards if rushed, aims, and fires telegraphed projectiles that interact
    /// with arena cover and damage the player.
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
        [SerializeField] private float retreatDistance = 4.0f;

        [Tooltip("Rotation speed towards the player in degrees per second.")]
        [SerializeField] private float rotationSpeed = 300f;

        [Tooltip("Sprite facing angle offset (-90 if art faces UP, 0 if art faces RIGHT).")]
        [SerializeField] private float spriteAngleOffset = -90f;

        [Header("Ranged Combat")]
        [Tooltip("Projectile prefab fired by this enemy.")]
        [SerializeField] private GameObject enemyProjectilePrefab;

        [Tooltip("Transform from which projectiles spawn.")]
        [SerializeField] private Transform firePoint;

        [Tooltip("Time between shots in seconds.")]
        [SerializeField] private float fireInterval = 2.2f;

        [Tooltip("Damage dealt per projectile hit.")]
        [SerializeField] private float projectileDamage = 12f;

        [Tooltip("Projectile travel speed.")]
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Visual Telegraph")]
        [Tooltip("Sprite renderer for flash/aim feedback.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("Telegraph warning color prior to firing.")]
        [SerializeField] private Color aimTelegraphColor = new Color(1f, 0.4f, 0.2f);

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private Collider2D _enemyCollider;
        private float _fireTimer;
        private Color _originalColor = Color.white;
        private bool _isTelegraphing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _enemyCollider = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) _originalColor = spriteRenderer.color;

            ValidateRigidbody();
            if (firePoint == null) firePoint = transform;
            _fireTimer = fireInterval * Random.Range(0.6f, 1.0f); // Stagger initial shots
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FixedUpdate()
        {
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
            if (_playerTransform == null) return;

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
            else if (dist > preferredDistance + 1.5f)
            {
                // Advance closer to enter combat range
                moveDir = dir;
            }
            else
            {
                // Strafe perpendicular to player for tactical unpredictability
                Vector2 strafe = new Vector2(-dir.y, dir.x);
                moveDir = strafe * 0.4f;
            }

            Vector2 velocity = moveDir * moveSpeed;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
#else
            _rb.velocity = velocity;
#endif
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

            // Telegraph 0.4s before firing
            if (_fireTimer <= 0.4f && !_isTelegraphing)
            {
                _isTelegraphing = true;
                if (spriteRenderer != null) spriteRenderer.color = aimTelegraphColor;
            }

            if (_fireTimer <= 0f)
            {
                FireProjectileAtPlayer();
                _fireTimer = fireInterval;
                _isTelegraphing = false;
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
