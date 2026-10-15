using UnityEngine;

namespace MiniShooterGame.Combat
{
    /// <summary>
    /// ScriptableObject defining weapon attributes, projectile properties, spread,
    /// and finite ammo capacity before reverting to default weapon.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "MiniShooter/Weapon Data", order = 1)]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("Display name of the weapon.")]
        public string weaponName = "Pistol";

        [Tooltip("Optional weapon icon for HUD display.")]
        public Sprite weaponIcon;

        [Header("Firing Characteristics")]
        [Tooltip("Shots fired per second.")]
        public float fireRate = 3f;

        [Tooltip("Damage dealt per projectile hit.")]
        public float damage = 15f;

        [Tooltip("Travel speed of the projectile.")]
        public float projectileSpeed = 16f;

        [Tooltip("Number of projectiles fired simultaneously per trigger pull (e.g. 1 for pistol, 5 for shotgun).")]
        public int pelletsPerShot = 1;

        [Tooltip("Maximum angular spread in degrees.")]
        public float spreadAngle = 0f;

        [Header("Ammo & Durability")]
        [Tooltip("Total ammo capacity. Set to -1 for unlimited ammo (default sidearm).")]
        public int maxAmmo = -1;

        [Header("Prefabs & Audio")]
        [Tooltip("Projectile prefab fired by this weapon. If null, falls back to default projectile.")]
        public GameObject projectilePrefab;

        [Tooltip("Audio clip played upon firing.")]
        public AudioClip shootSound;

        [Tooltip("Optional muzzle flash color tint.")]
        public Color muzzleColor = Color.yellow;
    }
}
