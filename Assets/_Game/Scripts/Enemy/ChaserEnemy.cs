using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Enemy
{
    /// <summary>
    /// Basic Chaser AI: tracks the player's position, moves towards them using continuous 2D physics,
    /// rotates to face the player, and deals periodic contact damage upon collision.
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

        [Header("Combat Settings")]
        [Tooltip("Damage dealt to the player on contact.")]
        [SerializeField] private float contactDamage = 15f;

        [Tooltip("Time in seconds between successive contact damage hits.")]
        [SerializeField] private float attackCooldown = 1.0f;

        private Rigidbody2D _rb;
        private Transform _playerTransform;
        private float _lastAttackTime = -999f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            ValidateRigidbodySettings();
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

            MoveTowardsPlayer();
            RotateTowardsPlayer();
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

        private void MoveTowardsPlayer()
        {
            Vector2 currentPos = _rb.position;
            Vector2 targetPos = (Vector2)_playerTransform.position;
            Vector2 direction = (targetPos - currentPos).normalized;

            Vector2 velocity = direction * moveSpeed;
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
                damageable.TakeDamage(contactDamage);
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
