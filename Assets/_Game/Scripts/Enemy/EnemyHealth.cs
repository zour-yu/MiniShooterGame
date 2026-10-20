using System.Collections;
using UnityEngine;
using MiniShooterGame.Combat;
using MiniShooterGame.UI;

namespace MiniShooterGame.Enemy
{
    /// <summary>
    /// Manages enemy hit points, IDamageable implementation, hit-flash feedback,
    /// floating damage text instantiation, and death sequence.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health pool.")]
        [SerializeField] private float maxHealth = 50f;

        [Header("Damage Feedback")]
        [Tooltip("Prefab containing FloatingText component to spawn on taking damage.")]
        [SerializeField] private GameObject floatingTextPrefab;

        [Tooltip("Positional offset above the enemy where damage numbers appear.")]
        [SerializeField] private Vector3 textSpawnOffset = new Vector3(0f, 0.8f, 0f);

        [Header("Hit Flash Visuals")]
        [Tooltip("Duration of the white flash when taking damage.")]
        [SerializeField] private float hitFlashDuration = 0.08f;

        [Tooltip("Flash color tint when taking damage.")]
        [SerializeField] private Color hitFlashColor = Color.white;

        [Header("Loot Drop Table")]
        [Tooltip("Chance (0.0 to 1.0) of spawning a loot item on death.")]
        [Range(0f, 1f)]
        [SerializeField] private float dropChance = 0.35f;

        [Tooltip("List of pickup prefabs that can be dropped by this enemy.")]
        [SerializeField] private GameObject[] lootPrefabs;

        [Header("Death Effects (Optional)")]
        [Tooltip("Optional explosion particle effect prefab instantiated upon death.")]
        [SerializeField] private GameObject deathEffectPrefab;

        [Header("Physics & Resistance")]
        [Tooltip("Knockback resistance factor (0.0 = full knockback, 1.0 = immovable tank).")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0f;

        private float _currentHealth;
        private SpriteRenderer _spriteRenderer;
        private Color _originalSpriteColor;
        private Coroutine _flashCoroutine;
        private Coroutine _squashCoroutine;
        private Vector3 _originalScale = Vector3.one;
        private bool _isDead;

        public event System.Action<float> OnDamaged;
        public event System.Action OnDied;

        /// <summary>
        /// Current health value.
        /// </summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>
        /// Maximum health value.
        /// </summary>
        public float MaxHealth => maxHealth;

        /// <summary>
        /// Knockback resistance (0.0 to 1.0).
        /// </summary>
        public float KnockbackResistance
        {
            get => knockbackResistance;
            set => knockbackResistance = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Normalized health percentage (0.0 to 1.0).
        /// </summary>
        public float HealthPercent => Mathf.Clamp01(_currentHealth / Mathf.Max(1f, maxHealth));

        private void Awake()
        {
            _currentHealth = maxHealth;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _originalScale = transform.localScale;

            if (_spriteRenderer != null)
            {
                _originalSpriteColor = _spriteRenderer.color;
            }
        }

        /// <summary>
        /// IDamageable implementation: applies damage without direction.
        /// </summary>
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, Vector2.zero, 0f);
        }

        /// <summary>
        /// Overload applying incoming damage, physical knockback impulse, hit-stun, and juice feedback.
        /// </summary>
        public void TakeDamage(float damage, Vector2 hitDirection, float knockbackForce)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);

            // 1. Deliver physics knockback to active enemy movement script
            if (knockbackForce > 0f && hitDirection.sqrMagnitude > 0.001f)
            {
                float effectiveForce = knockbackForce * (1f - knockbackResistance);
                if (effectiveForce > 0.1f)
                {
                    SendMessage("ApplyKnockback", new object[] { hitDirection, effectiveForce, 0.12f }, SendMessageOptions.DontRequireReceiver);
                }
            }

            // 2. Spawn Floating Damage Text
            SpawnDamageText(damage);

            // 3. Trigger Flash Effect & Squash Juice
            TriggerHitFlash();
            TriggerHitSquash();

            OnDamaged?.Invoke(damage);

            // 4. Evaluate Death
            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        private void TriggerHitSquash()
        {
            if (!gameObject.activeInHierarchy) return;

            if (_squashCoroutine != null)
            {
                StopCoroutine(_squashCoroutine);
            }
            _squashCoroutine = StartCoroutine(HitSquashRoutine());
        }

        private IEnumerator HitSquashRoutine()
        {
            Vector3 squashed = Vector3.Scale(_originalScale, new Vector3(1.22f, 0.82f, 1f));
            transform.localScale = squashed;

            float elapsed = 0f;
            float duration = 0.12f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(squashed, _originalScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = _originalScale;
            _squashCoroutine = null;
        }

        private void SpawnDamageText(float damage)
        {
            if (floatingTextPrefab == null) return;

            Vector3 spawnPos = transform.position + textSpawnOffset;
            GameObject textObj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);

            if (textObj.TryGetComponent<FloatingText>(out var floatingText))
            {
                // Simple crit check (e.g. hits above 15 damage or random)
                bool isCrit = damage >= 15f;
                floatingText.Setup(damage, isCrit);
            }
        }

        private void TriggerHitFlash()
        {
            if (_spriteRenderer == null || !gameObject.activeInHierarchy) return;

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            _spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashDuration);
            _spriteRenderer.color = _originalSpriteColor;
            _flashCoroutine = null;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            OnDied?.Invoke();

            // 1. Check and spawn loot drop
            TrySpawnLoot();

            // 2. Spawn optional death particles
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        private void TrySpawnLoot()
        {
            if (lootPrefabs != null && lootPrefabs.Length > 0 && Random.value <= dropChance)
            {
                int randomIndex = Random.Range(0, lootPrefabs.Length);
                GameObject selectedLoot = lootPrefabs[randomIndex];

                if (selectedLoot != null)
                {
                    Instantiate(selectedLoot, transform.position, Quaternion.identity);
                }
            }
        }
    }
}
