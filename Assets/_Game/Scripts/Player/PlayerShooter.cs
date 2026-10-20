#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System;
using System.Collections;
using UnityEngine;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Player
{
    /// <summary>
    /// Automatic targeting and shooting system for 2D top-down mobile arena.
    /// Detects the nearest target tagged "Enemy" within range, aims, and fires projectiles.
    /// Supports modular WeaponDataSO profiles, multi-pellet spread (Shotgun), rapid fire (Rifle),
    /// finite ammo consumption with automatic reversion to default infinite sidearm.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [Tooltip("Default infinite-ammo sidearm.")]
        [SerializeField] private WeaponDataSO defaultWeapon;

        [Tooltip("Currently equipped weapon.")]
        [SerializeField] private WeaponDataSO currentWeapon;

        [Header("Shooting Setup")]
        [Tooltip("The fallback projectile prefab to instantiate when shooting.")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("Transform position from which projectiles originate. If null, uses the player's transform.")]
        [SerializeField] private Transform firePoint;

        [Header("Fire Rate & Range")]
        [Tooltip("Base shots fired per second (used if no weapon assigned).")]
        [SerializeField] private float fireRate = 3f;

        [Tooltip("Radius around the player to search for enemies.")]
        [SerializeField] private float targetRange = 8f;

        [Header("Target Filtering")]
        [Tooltip("LayerMask containing enemy colliders for efficient 2D physics querying.")]
        [SerializeField] private LayerMask enemyLayer = ~0; // Default to Everything

        [Tooltip("Tag used to identify valid targets.")]
        [SerializeField] private string targetTag = "Enemy";

        [Header("Desktop & Manual Controls")]
        [Tooltip("If enabled, allows clicking Left Mouse or pressing Ctrl/J to manual-fire even when no enemy is targeted.")]
        [SerializeField] private bool allowManualFire = true;

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
        private int _currentAmmo = -1;
        private float _buffMultiplier = 1f;
        private float _damageMultiplier = 1f;

        // Events for UI HUD binding
        public event Action<WeaponDataSO, int> OnWeaponEquipped;
        public event Action<int, int> OnAmmoUpdated; // (current, max)

        public WeaponDataSO CurrentWeapon => currentWeapon;
        public int CurrentAmmo => _currentAmmo;
        public bool HasFiniteAmmo => currentWeapon != null && currentWeapon.maxAmmo > 0;
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

            InitializeDefaultWeapon();
        }

        private void InitializeDefaultWeapon()
        {
            if (defaultWeapon == null)
            {
                defaultWeapon = ScriptableObject.CreateInstance<WeaponDataSO>();
                defaultWeapon.weaponName = "Standard Pistol";
                defaultWeapon.fireRate = fireRate;
                defaultWeapon.damage = 15f;
                defaultWeapon.projectileSpeed = 16f;
                defaultWeapon.pelletsPerShot = 1;
                defaultWeapon.spreadAngle = 0f;
                defaultWeapon.maxAmmo = -1; // Unlimited
                defaultWeapon.projectilePrefab = projectilePrefab;
                defaultWeapon.shootSound = shootSound;
            }

            if (currentWeapon == null)
            {
                currentWeapon = defaultWeapon;
                _currentAmmo = defaultWeapon.maxAmmo;
            }
            else
            {
                _currentAmmo = currentWeapon.maxAmmo;
            }
        }

        /// <summary>
        /// Equips a new weapon (e.g. from a loot drop crate) with finite or custom ammo.
        /// </summary>
        public void EquipWeapon(WeaponDataSO newWeapon)
        {
            if (newWeapon == null) return;

            currentWeapon = newWeapon;
            _currentAmmo = newWeapon.maxAmmo;
            _fireCooldownTimer = 0f; // Instant readiness

            OnWeaponEquipped?.Invoke(currentWeapon, _currentAmmo);
            OnAmmoUpdated?.Invoke(_currentAmmo, newWeapon.maxAmmo);

            Debug.Log($"[PlayerShooter] Equipped {newWeapon.weaponName}! Ammo: {(_currentAmmo < 0 ? "Infinite" : _currentAmmo.ToString())}");
        }

        /// <summary>
        /// Reverts back to the default unlimited sidearm.
        /// </summary>
        public void RevertToDefaultWeapon()
        {
            currentWeapon = defaultWeapon;
            _currentAmmo = defaultWeapon.maxAmmo;

            OnWeaponEquipped?.Invoke(currentWeapon, _currentAmmo);
            OnAmmoUpdated?.Invoke(_currentAmmo, -1);

            Debug.Log("[PlayerShooter] Weapon ammo exhausted! Reverted to Standard Pistol.");
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

        private IEnumerator RapidFireRoutine(float multiplier, float duration)
        {
            _buffMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            _buffMultiplier = 1f;
            _rapidFireCoroutine = null;
        }

        /// <summary>
        /// Permanently boosts the player's base fire rate multiplier.
        /// </summary>
        public void ModifyBaseFireRate(float multiplier)
        {
            fireRate *= multiplier;
            if (defaultWeapon != null) defaultWeapon.fireRate *= multiplier;
        }

        /// <summary>
        /// Permanently boosts outgoing projectile damage multiplier.
        /// </summary>
        public void ModifyDamageMultiplier(float multiplier)
        {
            _damageMultiplier *= multiplier;
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

        private bool CheckManualFireInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Keyboard.current != null && (Keyboard.current.ctrlKey.isPressed || Keyboard.current.jKey.isPressed)) return true;
#else
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.J)) return true;
#endif
            return false;
        }

        /// <summary>
        /// Manages weapon cooldown and triggers firing when an enemy is targeted or manual fire is pressed.
        /// </summary>
        private void HandleShooting()
        {
            _fireCooldownTimer -= Time.deltaTime;

            bool manualFire = allowManualFire && CheckManualFireInput();

            if (_currentTarget == null && !manualFire) return;

            float currentRate = currentWeapon != null ? currentWeapon.fireRate : fireRate;
            float effectiveRate = currentRate * _buffMultiplier;

            if (_fireCooldownTimer <= 0f)
            {
                FireProjectile();
                _fireCooldownTimer = 1f / Mathf.Max(0.01f, effectiveRate);
            }
        }

        /// <summary>
        /// Instantiates and directs projectile(s) towards the target or aiming direction,
        /// handling multi-pellet spread, custom damage, and ammo countdown.
        /// </summary>
        private void FireProjectile()
        {
            GameObject prefabToUse = (currentWeapon != null && currentWeapon.projectilePrefab != null)
                ? currentWeapon.projectilePrefab
                : projectilePrefab;

            if (prefabToUse == null) return;

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
            Vector2 aimDirection = Vector2.right;

            if (_currentTarget != null)
            {
                aimDirection = ((Vector2)_currentTarget.position - (Vector2)spawnPosition).normalized;
            }
            else if (firePoint != null)
            {
                // Aim directly out of the gun barrel
                aimDirection = firePoint.right;
                if (aimDirection.sqrMagnitude < 0.001f) aimDirection = transform.right;
            }
            else
            {
                aimDirection = transform.right;
            }

            int pellets = currentWeapon != null ? Mathf.Max(1, currentWeapon.pelletsPerShot) : 1;
            float spreadAngle = currentWeapon != null ? currentWeapon.spreadAngle : 0f;
            float damage = (currentWeapon != null ? currentWeapon.damage : 15f) * _damageMultiplier;
            float speed = currentWeapon != null ? currentWeapon.projectileSpeed : 16f;
            float knockback = currentWeapon != null ? currentWeapon.knockbackForce : 4.5f;

            for (int i = 0; i < pellets; i++)
            {
                float offsetAngle = 0f;
                if (pellets > 1)
                {
                    offsetAngle = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, (float)i / (pellets - 1));
                }
                else if (spreadAngle > 0f)
                {
                    offsetAngle = UnityEngine.Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
                }

                Vector2 firedDirection = (Quaternion.Euler(0f, 0f, offsetAngle) * aimDirection).normalized;

                GameObject projObj = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
                if (projObj.TryGetComponent<Projectile>(out var projectile))
                {
                    projectile.Initialize(firedDirection, _playerCollider, damage, speed, "Enemy", knockback);
                }
            }

            // Visual feedback
            if (TryGetComponent<CharacterVisualController>(out var visual))
            {
                visual.TriggerRecoil();
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            // Audio feedback
            AudioClip clipToPlay = (currentWeapon != null && currentWeapon.shootSound != null)
                ? currentWeapon.shootSound
                : shootSound;

            if (audioSource != null && clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }

            // Finite Ammo Consumption
            if (currentWeapon != null && currentWeapon.maxAmmo > 0)
            {
                _currentAmmo--;
                OnAmmoUpdated?.Invoke(_currentAmmo, currentWeapon.maxAmmo);

                if (_currentAmmo <= 0)
                {
                    RevertToDefaultWeapon();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, targetRange);

            if (Application.isPlaying && _currentTarget != null)
            {
                Gizmos.color = Color.red;
                Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
                Gizmos.DrawLine(startPos, _currentTarget.position);
            }
        }
    }
}
