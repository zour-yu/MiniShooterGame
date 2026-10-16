using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiniShooterGame.Core
{
    [System.Serializable]
    public class WaveData
    {
        [Tooltip("Identifier name for this wave (e.g. 'Wave 1 - Scouts').")]
        public string waveName = "Wave 1";

        [Tooltip("Array of enemy prefabs that can spawn in this wave.")]
        public GameObject[] enemyPrefabs;

        [Tooltip("Total number of enemies to spawn in this wave.")]
        public int totalEnemies = 8;

        [Tooltip("Delay in seconds between successive enemy spawns.")]
        public float spawnInterval = 1.0f;
    }

    /// <summary>
    /// Manages progressive enemy wave spawning outside the visible arena perimeter,
    /// tracking active enemies and triggering wave transitions.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Wave Configuration")]
        [SerializeField] private List<WaveData> waves = new List<WaveData>();

        [Tooltip("Delay in seconds between clearing a wave and starting the next.")]
        [SerializeField] private float timeBetweenWaves = 3.0f;

        [Header("Spawn Perimeter")]
        [Tooltip("Radius around the center/player where enemies spawn.")]
        [SerializeField] private float spawnRadius = 14f;

        [Tooltip("Optional transform center for spawning. If null, uses this transform or player.")]
        [SerializeField] private Transform spawnCenter;

        [Header("Playable Island Bounds")]
        [Tooltip("Min & Max coordinates to ensure enemies spawn on land inside the island boundaries.")]
        [SerializeField] private Vector2 arenaMin = new Vector2(-16.5f, -10.5f);
        [SerializeField] private Vector2 arenaMax = new Vector2(16.5f, 10.5f);

        [Header("Events")]
        public UnityEvent<int, string> OnWaveStarted; // (waveIndex, waveName)
        public UnityEvent<int> OnWaveCompleted; // (waveIndex)
        public UnityEvent<int, int> OnEnemyCountChanged; // (aliveCount, totalRemainingToSpawn)

        private int _currentWaveIndex;
        private int _enemiesSpawnedThisWave;
        private readonly List<GameObject> _activeEnemies = new List<GameObject>();
        private bool _isSpawning;
        private bool _allWavesCleared;

        private static WaveSpawner _instance;
        public static WaveSpawner Instance => _instance;

        /// <summary>
        /// 1-based current wave number.
        /// </summary>
        public int CurrentWaveNumber => _currentWaveIndex + 1;

        /// <summary>
        /// Total number of configured waves.
        /// </summary>
        public int TotalWaves => waves.Count;

        /// <summary>
        /// Count of currently alive active enemies.
        /// </summary>
        public int ActiveEnemyCount => _activeEnemies.Count;

        /// <summary>
        /// Total enemies planned for the current wave.
        /// </summary>
        public int CurrentWaveTargetTotal => GetCurrentWaveTotal();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[WaveSpawner] Duplicate WaveSpawner detected on {gameObject.name}. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Enforce tight playable island bounds so enemies NEVER spawn in river water (river starts at x > 8.0)
            arenaMin = new Vector2(-12.0f, -7.0f);
            arenaMax = new Vector2(7.5f, 7.0f);
            spawnRadius = 8.5f;
        }

        private void Start()
        {
            FindSpawnCenter();

            if (waves.Count > 0)
            {
                StartCoroutine(StartWaveRoutine(_currentWaveIndex));
            }
        }

        private void FindSpawnCenter()
        {
            if (spawnCenter != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                spawnCenter = player.transform;
                return;
            }

            var movement = Object.FindFirstObjectByType<MiniShooterGame.Player.PlayerMovement>();
            if (movement != null)
            {
                spawnCenter = movement.transform;
                return;
            }

            if (Camera.main != null)
            {
                spawnCenter = Camera.main.transform;
            }
        }

        private void Update()
        {
            int prevCount = _activeEnemies.Count;
            // Clean up destroyed enemies from active list
            _activeEnemies.RemoveAll(e => e == null);

            // Safety Watchdog: relocate any enemy that got pushed into the river water (x > 8.0)
            Vector2 playerPos = spawnCenter != null ? (Vector2)spawnCenter.position : Vector2.zero;
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                GameObject enemy = _activeEnemies[i];
                if (enemy == null) continue;

                Vector2 pos = enemy.transform.position;
                if (pos.x > 8.0f || pos.x < -14.0f || pos.y > 8.5f || pos.y < -8.5f)
                {
                    Vector2 dirToPlayer = (playerPos - pos).normalized;
                    if (dirToPlayer == Vector2.zero) dirToPlayer = Vector2.left;
                    enemy.transform.position = playerPos - dirToPlayer * 5.0f;
                    Debug.Log($"[WaveSpawner] Relocated out-of-bounds enemy {enemy.name} back onto the grass island.");
                }
            }

            // Immediately notify HUD when an enemy is killed!
            if (prevCount != _activeEnemies.Count)
            {
                int remainingToSpawn = Mathf.Max(0, GetCurrentWaveTotal() - _enemiesSpawnedThisWave);
                OnEnemyCountChanged?.Invoke(_activeEnemies.Count, remainingToSpawn);
            }

            // Check if current wave is completed
            if (!_isSpawning && !_allWavesCleared && _activeEnemies.Count == 0 && _enemiesSpawnedThisWave >= GetCurrentWaveTotal())
            {
                StartCoroutine(WaveCompletedRoutine());
            }
        }

        private int GetCurrentWaveTotal()
        {
            if (_currentWaveIndex >= 0 && _currentWaveIndex < waves.Count)
            {
                return waves[_currentWaveIndex].totalEnemies;
            }
            return 0;
        }

        private IEnumerator StartWaveRoutine(int waveIndex)
        {
            if (waveIndex >= waves.Count)
            {
                _allWavesCleared = true;
                Debug.Log("[WaveSpawner] All waves completed! Arena cleared!");
                yield break;
            }

            _isSpawning = true;
            _enemiesSpawnedThisWave = 0;
            WaveData currentWave = waves[waveIndex];

            Debug.Log($"[WaveSpawner] Starting {currentWave.waveName} ({currentWave.totalEnemies} enemies)");
            OnWaveStarted?.Invoke(waveIndex + 1, currentWave.waveName);
            OnEnemyCountChanged?.Invoke(0, currentWave.totalEnemies);

            while (_enemiesSpawnedThisWave < currentWave.totalEnemies)
            {
                SpawnRandomEnemy(currentWave);
                _enemiesSpawnedThisWave++;

                OnEnemyCountChanged?.Invoke(_activeEnemies.Count, currentWave.totalEnemies - _enemiesSpawnedThisWave);
                yield return new WaitForSeconds(currentWave.spawnInterval);
            }

            _isSpawning = false;

            // In case all spawned enemies were already defeated during spawning
            int remainingToSpawnAfter = Mathf.Max(0, currentWave.totalEnemies - _enemiesSpawnedThisWave);
            OnEnemyCountChanged?.Invoke(_activeEnemies.Count, remainingToSpawnAfter);
        }

        private void SpawnRandomEnemy(WaveData wave)
        {
            if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0) return;

            int randomIndex = Random.Range(0, wave.enemyPrefabs.Length);
            GameObject prefab = wave.enemyPrefabs[randomIndex];
            if (prefab == null) return;

            Vector2 spawnPos = GetRandomPerimeterPosition();
            GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

            _activeEnemies.Add(enemyObj);
        }

        private Vector2 GetRandomPerimeterPosition()
        {
            if (spawnCenter == null) FindSpawnCenter();

            Vector2 playerPos = spawnCenter != null ? (Vector2)spawnCenter.position : Vector2.zero;

            // Pick an inland spawn position strictly on the grass island
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(5.5f, Mathf.Max(7.0f, spawnRadius));
                Vector2 candidate = playerPos + new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * dist;

                // Clamp to grass island bounds (keeps enemies completely away from river)
                candidate.x = Mathf.Clamp(candidate.x, arenaMin.x, arenaMax.x);
                candidate.y = Mathf.Clamp(candidate.y, arenaMin.y, arenaMax.y);

                if (Vector2.Distance(candidate, playerPos) >= 5f)
                {
                    return candidate;
                }
            }

            // Fallback: spawn on left grass side
            float fallbackX = Mathf.Clamp(playerPos.x - 6.0f, arenaMin.x, arenaMax.x);
            float fallbackY = Mathf.Clamp(playerPos.y + Random.Range(-3f, 3f), arenaMin.y, arenaMax.y);
            return new Vector2(fallbackX, fallbackY);
        }

        private IEnumerator WaveCompletedRoutine()
        {
            _isSpawning = true; // Prevent multiple triggers
            Debug.Log($"[WaveSpawner] Wave {CurrentWaveNumber} Cleared!");

            try
            {
                OnWaveCompleted?.Invoke(_currentWaveIndex + 1);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WaveSpawner] Error during OnWaveCompleted invocation: {ex}");
            }

            // Wait while game is paused in perk selection modal
            while (Time.timeScale == 0f)
            {
                yield return null;
            }

            // Short, punchy 1.2s breather before the next wave kicks off
            float breather = 1.2f;
            while (breather > 0f)
            {
                if (Time.timeScale > 0f) breather -= Time.unscaledDeltaTime;
                yield return null;
            }

            _currentWaveIndex++;
            if (_currentWaveIndex < waves.Count)
            {
                StartCoroutine(StartWaveRoutine(_currentWaveIndex));
            }
            else
            {
                _allWavesCleared = true;
                Debug.Log("[WaveSpawner] Victory! All arena waves eliminated.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
            Gizmos.DrawWireSphere(center, spawnRadius);
        }
    }
}
