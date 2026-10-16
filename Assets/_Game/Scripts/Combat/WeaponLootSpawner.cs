using System.Collections.Generic;
using UnityEngine;
using MiniShooterGame.Core;
using MiniShooterGame.UI;

namespace MiniShooterGame.Combat
{
    /// <summary>
    /// Spawns high-value weapon loot crates periodically and after waves
    /// across the designated island landmark anchors (North Grove, South Ruins, etc.).
    /// </summary>
    public class WeaponLootSpawner : MonoBehaviour
    {
        [Header("Weapon Pool")]
        [Tooltip("Available weapons that can spawn in loot crates.")]
        [SerializeField] private WeaponDataSO[] weaponPool;

        [Header("Loot Crate Visuals")]
        [Tooltip("Prefab instantiated for weapon drops. If null, creates standard crate on the fly.")]
        [SerializeField] private GameObject weaponCratePrefab;

        [Tooltip("Sprite used if creating pickup dynamically.")]
        [SerializeField] private Sprite crateSprite;

        [Header("Spawn Settings")]
        [Tooltip("Time in seconds between automatic loot drops.")]
        [SerializeField] private float dropInterval = 28f;

        [Tooltip("Whether to spawn a weapon crate upon each completed wave.")]
        [SerializeField] private bool spawnOnWaveComplete = true;

        [Tooltip("Maximum simultaneous active weapon crates allowed on the island.")]
        [SerializeField] private int maxActiveCrates = 2;

        [Header("Loot Anchors")]
        [Tooltip("Target landmark transforms where loot can drop.")]
        [SerializeField] private List<Transform> lootAnchors = new List<Transform>();

        private float _dropTimer;
        private readonly List<GameObject> _activeCrates = new List<GameObject>();
        private WaveSpawner _waveSpawner;

        private void Awake()
        {
            FindAnchors();
            _dropTimer = dropInterval * 0.5f; // First drop arrives early for early-game fun
        }

        private void Start()
        {
            _waveSpawner = WaveSpawner.Instance != null ? WaveSpawner.Instance : Object.FindAnyObjectByType<WaveSpawner>();
            if (_waveSpawner != null && spawnOnWaveComplete)
            {
                _waveSpawner.OnWaveCompleted.AddListener(OnWaveCompleted);
            }
        }

        private void OnDestroy()
        {
            if (_waveSpawner != null)
            {
                _waveSpawner.OnWaveCompleted.RemoveListener(OnWaveCompleted);
            }
        }

        private void Update()
        {
            // Clean up destroyed crates from list
            _activeCrates.RemoveAll(c => c == null);

            // Ensure any active crate has the glowing padlock indicator
            for (int i = 0; i < _activeCrates.Count; i++)
            {
                var p = _activeCrates[i];
                if (p != null && p.transform.Find("Padlock_Indicator") == null)
                {
                    GameObject lockObj = new GameObject("Padlock_Indicator");
                    lockObj.transform.SetParent(p.transform, false);
                    lockObj.transform.localPosition = new Vector3(0f, -0.12f, 0f);
                    lockObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                    SpriteRenderer lockSr = lockObj.AddComponent<SpriteRenderer>();
                    Sprite padlockSprite = Resources.Load<Sprite>("UI/Padlock_Cyan");
                    if (padlockSprite != null) lockSr.sprite = padlockSprite;
                    lockSr.sortingLayerName = "Default";
                    lockSr.sortingOrder = 95;
                    lockSr.color = Color.white;
                }
            }

            _dropTimer -= Time.deltaTime;
            if (_dropTimer <= 0f)
            {
                _dropTimer = dropInterval;
                if (_activeCrates.Count < maxActiveCrates)
                {
                    SpawnLootCrate();
                }
            }
        }

        private void OnWaveCompleted(int waveNumber)
        {
            if (_activeCrates.Count < maxActiveCrates)
            {
                SpawnLootCrate();
            }
        }

        /// <summary>
        /// Spawns a weapon crate at a vacant or random landmark anchor.
        /// </summary>
        public void SpawnLootCrate()
        {
            if (lootAnchors.Count == 0) FindAnchors();
            if (lootAnchors.Count == 0 || weaponPool == null || weaponPool.Length == 0) return;

            // Pick random anchor
            Transform chosenAnchor = lootAnchors[Random.Range(0, lootAnchors.Count)];
            Vector3 spawnPos = chosenAnchor != null ? chosenAnchor.position : Vector3.zero;

            // Pick random weapon from pool
            WeaponDataSO chosenWeapon = weaponPool[Random.Range(0, weaponPool.Length)];
            if (chosenWeapon == null) return;

            GameObject crateObj;
            if (weaponCratePrefab != null)
            {
                crateObj = Instantiate(weaponCratePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                crateObj = CreateDynamicWeaponCrate(spawnPos, chosenWeapon);
            }

            if (crateObj.TryGetComponent<WeaponPickup>(out var pickup))
            {
                pickup.Initialize(chosenWeapon);
            }

            _activeCrates.Add(crateObj);

            string anchorName = chosenAnchor != null ? chosenAnchor.name.Replace("LootPoint_", "") : "Island";
            FloatingText.Spawn(spawnPos + Vector3.up * 0.8f, $"<color=#00E5FF>WEAPON DROP: {chosenWeapon.weaponName.ToUpper()}</color>", 2.0f);
            Debug.Log($"[WeaponLootSpawner] Dropped {chosenWeapon.weaponName} at {anchorName}!");
        }

        private GameObject CreateDynamicWeaponCrate(Vector3 position, WeaponDataSO weapon)
        {
            GameObject obj = new GameObject($"WeaponDrop_{weapon.weaponName}");
            obj.transform.position = position;
            obj.tag = "Pickup";

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            if (crateSprite != null)
            {
                sr.sprite = crateSprite;
            }
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 90;
            obj.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            WeaponPickup pickup = obj.AddComponent<WeaponPickup>();
            pickup.Initialize(weapon);

            // Holographic Cyan Padlock from Concept Art
            GameObject lockObj = new GameObject("Padlock_Indicator");
            lockObj.transform.SetParent(obj.transform, false);
            lockObj.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            lockObj.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            SpriteRenderer lockSr = lockObj.AddComponent<SpriteRenderer>();
            Sprite padlockSprite = Resources.Load<Sprite>("UI/Padlock_Cyan");
            if (padlockSprite != null) lockSr.sprite = padlockSprite;
            lockSr.sortingLayerName = "Default";
            lockSr.sortingOrder = 95;
            lockSr.color = Color.white;

            return obj;
        }

        private void FindAnchors()
        {
            if (lootAnchors.Count > 0) return;

            GameObject anchorRoot = GameObject.Find("[Loot_Anchors]");
            if (anchorRoot != null)
            {
                foreach (Transform child in anchorRoot.transform)
                {
                    lootAnchors.Add(child);
                }
            }
        }
    }
}
