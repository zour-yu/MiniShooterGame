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
        [SerializeField] private float spawnRadius = 16f;

        [Tooltip("Optional transform center for spawning. If null, uses this transform or player.")]
        [SerializeField] private Transform spawnCenter;

        [Header("Events")]
        public UnityEvent<int, string> OnWaveStarted; // (waveIndex, waveName)
        public UnityEvent<int> OnWaveCompleted; // (waveIndex)
        public UnityEvent<int, int> OnEnemyCountChanged; // (aliveCount, totalRemainingToSpawn)

        private int _currentWaveIndex;
        private int _enemiesSpawnedThisWave;
        private readonly List<GameObject> _activeEnemies = new List<GameObject>();
        private bool _isSpawning;
        private bool _allWavesCleared;

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
            // Clean up destroyed enemies from active list
            _activeEnemies.RemoveAll(e => e == null);

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

            while (_enemiesSpawnedThisWave < currentWave.totalEnemies)
            {
                SpawnRandomEnemy(currentWave);
                _enemiesSpawnedThisWave++;

                OnEnemyCountChanged?.Invoke(_activeEnemies.Count, currentWave.totalEnemies - _enemiesSpawnedThisWave);
                yield return new WaitForSeconds(currentWave.spawnInterval);
            }

            _isSpawning = false;
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

            Vector2 origin = spawnCenter != null ? (Vector2)spawnCenter.position : (Vector2)transform.position;
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Ensure enemies NEVER spawn directly on top of the player (minimum 12 units away)
            float safeRadius = Mathf.Max(12f, spawnRadius);
            Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * safeRadius;
            return origin + offset;
        }

        private IEnumerator WaveCompletedRoutine()
        {
            _isSpawning = true; // Prevent multiple triggers
            Debug.Log($"[WaveSpawner] Wave {CurrentWaveNumber} Cleared!");
            OnWaveCompleted?.Invoke(_currentWaveIndex + 1);

            yield return new WaitForSeconds(timeBetweenWaves);

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
