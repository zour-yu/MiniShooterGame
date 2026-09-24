using UnityEngine;
using MiniShooterGame.Player;

namespace MiniShooterGame.Combat
{
    public enum PickupType
    {
        Health,
        RapidFire,
        UltimateCharge
    }

    /// <summary>
    /// Collectible pickup item in the arena.
    /// Provides bobbing/floating visual animation and applies instant or timed buffs upon contact with Player.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PickupItem : MonoBehaviour
    {
        [Header("Pickup Configuration")]
        [Tooltip("The category of buff or resource provided by this pickup.")]
        [SerializeField] private PickupType pickupType = PickupType.Health;

        [Tooltip("Amount restored or duration of effect (e.g. 25 HP or 6s duration).")]
        [SerializeField] private float valueAmount = 25f;

        [Tooltip("Multiplier applied for multiplier-based buffs (e.g. 2x fire rate).")]
        [SerializeField] private float buffMultiplier = 2f;

        [Header("Visual Bobbing & Feedback")]
        [Tooltip("Speed of vertical oscillation.")]
        [SerializeField] private float bobbingSpeed = 3f;

        [Tooltip("Height of vertical oscillation.")]
        [SerializeField] private float bobbingHeight = 0.12f;

        [Tooltip("Speed of slow 2D rotational spin.")]
        [SerializeField] private float rotationSpeed = 30f;

        [Tooltip("Optional particle effect instantiated on collection.")]
        [SerializeField] private GameObject collectEffectPrefab;

        [Tooltip("Optional sound clip played on collection.")]
        [SerializeField] private AudioClip collectSound;

        private Vector3 _startPosition;
        private bool _isCollected;

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
            _startPosition = transform.position;
        }

        private void Update()
        {
            // Vertical bobbing motion
            float newY = _startPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Subtle rotation
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player"))
            {
                ApplyPickup(other.gameObject);
            }
        }

        private void ApplyPickup(GameObject playerObj)
        {
            _isCollected = true;

            switch (pickupType)
            {
                case PickupType.Health:
                    if (playerObj.TryGetComponent<PlayerHealth>(out var playerHealth))
                    {
                        playerHealth.Heal(valueAmount);
                    }
                    break;

                case PickupType.RapidFire:
                    if (playerObj.TryGetComponent<PlayerShooter>(out var playerShooter))
                    {
                        playerShooter.ApplyRapidFireBuff(buffMultiplier, valueAmount);
                    }
                    break;

                case PickupType.UltimateCharge:
                    Debug.Log($"[PickupItem] Ultimate Charge +{valueAmount} collected!");
                    break;
            }

            // Spawn collect particles
            if (collectEffectPrefab != null)
            {
                Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            }

            // Play audio if available
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
