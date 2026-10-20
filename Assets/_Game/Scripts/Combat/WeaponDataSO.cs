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

        [Header("Hand Grip & Sockets")]
        [Tooltip("Local offset for main trigger hand relative to weapon pivot.")]
        public Vector2 mainHandOffset = new Vector2(0f, -0.04f);

        [Tooltip("Whether this weapon requires a second support hand (e.g. rifles, shotguns).")]
        public bool isTwoHanded = false;

        [Tooltip("Local offset for second support hand relative to weapon pivot.")]
        public Vector2 supportHandOffset = new Vector2(0.38f, -0.02f);

        [Tooltip("Forward offset along the aim vector to prevent long stocks from clipping into character body.")]
        public float stockForwardOffset = 0f;

        [Tooltip("Local position of the muzzle tip relative to weapon pivot for projectile spawns and muzzle flashes.")]
        public Vector2 muzzleOffset = new Vector2(0.5f, 0.05f);
    }
}


