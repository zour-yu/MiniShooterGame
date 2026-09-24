using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Player
{
    /// <summary>
    /// Automatic targeting and shooting system for 2D top-down mobile arena.
    /// Detects the nearest target tagged "Enemy" within range, aims, and fires projectiles at a configurable rate.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Shooting Setup")]
        [Tooltip("The projectile prefab to instantiate when shooting.")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("Transform position from which projectiles originate. If null, uses the player's transform.")]
        [SerializeField] private Transform firePoint;

        [Header("Fire Rate & Range")]
        [Tooltip("Shots fired per second (e.g. 3 = 3 shots per second).")]
        [SerializeField] private float fireRate = 3f;

        [Tooltip("Radius around the player to search for enemies.")]
        [SerializeField] private float targetRange = 8f;

        [Header("Target Filtering")]
        [Tooltip("LayerMask containing enemy colliders for efficient 2D physics querying.")]
        [SerializeField] private LayerMask enemyLayer = ~0; // Default to Everything

        [Tooltip("Tag used to identify valid targets.")]
        [SerializeField] private string targetTag = "Enemy";

        [Header("Audio & Visuals (Optional)")]
        [Tooltip("Optional muzzle flash particle effect played on fire.")]
        [SerializeField] private ParticleSystem muzzleFlash;

        [Tooltip("Optional audio clip played on fire.")]
        [SerializeField] private AudioClip shootSound;

        [Tooltip("AudioSource component for playback. If null, attempts to find one.")]
        [SerializeField] private AudioSource audioSource;

        private float _fireCooldownTimer;
        private Transform _currentTarget;
        private float _baseFireRate;
        private Coroutine _rapidFireCoroutine;
        private Collider2D _playerCollider;

        /// <summary>
        /// Currently targeted enemy Transform (or null if no enemies in range).
        /// </summary>
        public Transform CurrentTarget => _currentTarget;

        private void Awake()
        {
            _baseFireRate = fireRate;
            _playerCollider = GetComponent<Collider2D>();

            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Temporarily multiplies the player's fire rate for a specified duration.
        /// </summary>
        public void ApplyRapidFireBuff(float multiplier = 2f, float duration = 5f)
        {
            if (_rapidFireCoroutine != null)
            {
                StopCoroutine(_rapidFireCoroutine);
            }
            _rapidFireCoroutine = StartCoroutine(RapidFireRoutine(multiplier, duration));
        }

        private System.Collections.IEnumerator RapidFireRoutine(float multiplier, float duration)
        {
            fireRate = _baseFireRate * multiplier;
            yield return new WaitForSeconds(duration);
            fireRate = _baseFireRate;
            _rapidFireCoroutine = null;
        }

        private void Update()
        {
            UpdateTarget();
            HandleShooting();
        }

        /// <summary>
        /// Scans the immediate area for the closest valid enemy.
        /// </summary>
        private void UpdateTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, targetRange, enemyLayer);
            Transform nearestEnemy = null;
            float closestDistanceSqr = Mathf.Infinity;
            Vector2 currentPosition = transform.position;

            foreach (Collider2D hit in hits)
            {
                // Verify hit object has the correct tag
                if (!hit.CompareTag(targetTag)) continue;

                Vector2 directionToEnemy = (Vector2)hit.transform.position - currentPosition;
                float distSqr = directionToEnemy.sqrMagnitude;

                if (distSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distSqr;
                    nearestEnemy = hit.transform;
                }
            }

            _currentTarget = nearestEnemy;
        }

        /// <summary>
        /// Manages weapon cooldown and triggers firing when an enemy is targeted.
        /// </summary>
        private void HandleShooting()
        {
            _fireCooldownTimer -= Time.deltaTime;

            if (_currentTarget == null || projectilePrefab == null) return;

            if (_fireCooldownTimer <= 0f)
            {
                FireProjectile();
                _fireCooldownTimer = 1f / Mathf.Max(0.01f, fireRate);
            }
        }

        /// <summary>
        /// Instantiates and directs the projectile towards the current target.
        /// </summary>
        private void FireProjectile()
        {
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            Vector2 aimDirection = ((Vector2)_currentTarget.position - (Vector2)spawnPosition).normalized;

            // Spawn projectile
            GameObject projObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

            // Initialize projectile component
            if (projObj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Initialize(aimDirection, _playerCollider);
            }

            // Visual feedback
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // Audio feedback
            if (audioSource != null && shootSound != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw detection range in Scene view
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, targetRange);

            // Draw line to current target in Play mode
            if (Application.isPlaying && _currentTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
                Gizmos.DrawLine(startPos, _currentTarget.position);
            }
        }
    }
}
