using UnityEngine;

namespace MiniShooterGame.Combat
{
    /// <summary>
    /// Tactical cover crate that blocks enemy bullets and movement,
    /// can be destroyed by sustained fire, and occasionally drops pickups.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DestructibleCrate : MonoBehaviour, IDamageable
    {
        [Header("Durability")]
        [SerializeField] private float maxHealth = 30f;

        [Header("Drop on Break (Optional)")]
        [SerializeField] private GameObject[] dropPrefabs;
        [Range(0f, 1f)]
        [SerializeField] private float dropChance = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.6f, 0.6f);

        private float _currentHealth;
        private Color _originalColor;
        private bool _isDestroyed;

        private void Awake()
        {
            _currentHealth = maxHealth;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) _originalColor = spriteRenderer.color;
        }

        public void TakeDamage(float damage)
        {
            if (_isDestroyed) return;

            _currentHealth -= damage;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitFlashColor;
                Invoke(nameof(ResetColor), 0.08f);
            }

            if (_currentHealth <= 0f)
            {
                BreakCrate();
            }
        }

        private void ResetColor()
        {
            if (spriteRenderer != null) spriteRenderer.color = _originalColor;
        }

        private void BreakCrate()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            // Spawn optional loot drop
            if (dropPrefabs != null && dropPrefabs.Length > 0 && Random.value <= dropChance)
            {
                int idx = Random.Range(0, dropPrefabs.Length);
                if (dropPrefabs[idx] != null)
                {
                    Instantiate(dropPrefabs[idx], transform.position, Quaternion.identity);
                }
            }

            Destroy(gameObject);
        }
    }
}
