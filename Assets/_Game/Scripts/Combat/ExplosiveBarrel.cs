using UnityEngine;
using MiniShooterGame.UI;

namespace MiniShooterGame.Combat
{
    /// <summary>
    /// Interactive explosive hazard in the arena. Detonates when shot,
    /// dealing heavy radial AOE damage and knockback to enemies.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        [Header("Health & Resistance")]
        [SerializeField] private float maxHealth = 15f;
        
        [Header("Explosion Properties")]
        [SerializeField] private float explosionRadius = 3.2f;
        [SerializeField] private float explosionDamage = 50f;
        [SerializeField] private LayerMask damageLayers = ~0;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color hitFlashColor = Color.white;
        
        private float _currentHealth;
        private Color _originalColor;
        private bool _isExploded;

        private void Awake()
        {
            _currentHealth = maxHealth;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) _originalColor = spriteRenderer.color;
        }

        public void TakeDamage(float damage)
        {
            if (_isExploded) return;

            _currentHealth -= damage;

            // Flash effect
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitFlashColor;
                Invoke(nameof(ResetColor), 0.08f);
            }

            if (_currentHealth <= 0f)
            {
                Explode();
            }
        }

        private void ResetColor()
        {
            if (spriteRenderer != null) spriteRenderer.color = _originalColor;
        }

        private void Explode()
        {
            if (_isExploded) return;
            _isExploded = true;

            // Deal radial AOE damage
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageLayers);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(explosionDamage);
                }
            }

            // Spawn visual effect if assigned
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
