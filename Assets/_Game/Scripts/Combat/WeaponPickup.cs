using UnityEngine;
using MiniShooterGame.Player;
using MiniShooterGame.UI;

namespace MiniShooterGame.Combat
{
    /// <summary>
    /// Interactive weapon loot pickup in the arena.
    /// Grants the player a specific WeaponDataSO with finite ammo when collected.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Weapon Payload")]
        [Tooltip("The weapon data awarded to the player upon collection.")]
        [SerializeField] private WeaponDataSO weaponData;

        [Header("Animation & Feedback")]
        [Tooltip("Speed of gentle hover bobbing.")]
        [SerializeField] private float bobbingSpeed = 2.5f;

        [Tooltip("Amplitude of gentle hover bobbing.")]
        [SerializeField] private float bobbingHeight = 0.15f;

        [Tooltip("Optional particle effect spawned upon collection.")]
        [SerializeField] private GameObject collectEffectPrefab;

        [Tooltip("Optional audio clip played upon collection.")]
        [SerializeField] private AudioClip collectSound;

        private Vector3 _basePosition;
        private bool _isCollected;

        public void Initialize(WeaponDataSO weapon)
        {
            weaponData = weapon;
        }

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            _basePosition = transform.position;
        }

        private void Update()
        {
            // Gentle hovering animation
            float newY = _basePosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = new Vector3(_basePosition.x, newY, _basePosition.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player"))
            {
                if (other.TryGetComponent<PlayerShooter>(out var playerShooter))
                {
                    if (weaponData != null)
                    {
                        _isCollected = true;
                        playerShooter.EquipWeapon(weaponData);

                        // Spawn floating notification text
                        string ammoLabel = weaponData.maxAmmo > 0 ? $" ({weaponData.maxAmmo} Rounds)" : "";
                        FloatingText.Spawn(
                            transform.position + Vector3.up * 0.5f,
                            $"<color=#FFD700>★ {weaponData.weaponName.ToUpper()}{ammoLabel}!</color>",
                            1.4f
                        );

                        if (collectEffectPrefab != null)
                        {
                            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                        }

                        if (collectSound != null)
                        {
                            AudioSource.PlayClipAtPoint(collectSound, transform.position);
                        }

                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
