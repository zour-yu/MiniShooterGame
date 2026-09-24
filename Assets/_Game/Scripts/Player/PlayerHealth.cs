using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using MiniShooterGame.Combat;
using MiniShooterGame.Core;

namespace MiniShooterGame.Player
{
    /// <summary>
    /// Manages the Player's hit points, damage mitigation (invulnerability check),
    /// healing, hit feedback, and death lifecycle.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Pool")]
        [Tooltip("Maximum health capacity.")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Brief invulnerability duration in seconds when the player first spawns.")]
        [SerializeField] private float spawnGracePeriod = 1.0f;

        [Header("Visual & Feedback")]
        [Tooltip("Flash color tint when taking damage.")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Tooltip("Duration of the hit flash in seconds.")]
        [SerializeField] private float damageFlashDuration = 0.1f;

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        public UnityEvent OnPlayerDied;

        private float _currentHealth;
        private float _spawnTime;
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private PlayerDash _playerDash;
        private CameraFollow2D _cameraFollow;
        private bool _isDead;

        /// <summary>
        /// Current health points.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Maximum health capacity.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Normalized health percentage (0.0 to 1.0).
        /// </summary>
        public float HealthPercent => Mathf.Clamp01(_currentHealth / Mathf.Max(1f, maxHealth));

        /// <summary>
        /// True if the player is currently alive.
        /// </summary>
        public bool IsAlive => !_isDead;

        private void Awake()
        {
            _currentHealth = maxHealth;
            _spawnTime = Time.time;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _playerDash = GetComponent<PlayerDash>();
            _cameraFollow = Object.FindFirstObjectByType<CameraFollow2D>();

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void Start()
        {
            _spawnTime = Time.time;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// IDamageable implementation: applies damage unless currently invulnerable.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_isDead) return;

            // Check spawn grace period
            if (Time.time < _spawnTime + spawnGracePeriod) return;

            // Check if invulnerable from dash or status
            if (_playerDash != null && _playerDash.IsInvulnerable)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Screen shake on hit
            if (_cameraFollow != null)
            {
                _cameraFollow.TriggerShake(0.15f, 0.25f);
            }

            // Flash red on damage
            StartCoroutine(DamageFlashRoutine());

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Restores health up to maxHealth.
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f) return;

            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private IEnumerator DamageFlashRoutine()
        {
            if (_spriteRenderer == null) yield break;

            _spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            _spriteRenderer.color = _originalColor;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("[PlayerHealth] Player has died!");
            OnPlayerDied?.Invoke();

            // Disable player control components
            if (TryGetComponent<PlayerMovement>(out var movement)) movement.enabled = false;
            if (TryGetComponent<PlayerShooter>(out var shooter)) shooter.enabled = false;
        }
    }
}
