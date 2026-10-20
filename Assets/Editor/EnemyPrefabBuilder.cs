using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using MiniShooterGame.Combat;
using MiniShooterGame.Core;
using MiniShooterGame.Enemy;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class EnemyPrefabBuilder
    {
        private const string EnemyFolder = "Assets/_Game/Prefabs/Enemies";
        private const string ChaserPrefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Chaser.prefab";
        private const string ShooterPrefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Shooter.prefab";
        private const string BrutePrefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Brute.prefab";
        private const string ExploderPrefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Exploder.prefab";

        private const string ZombieSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Zombie1/zoimbie1_hold.png";
        private const string ExploderSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Zombie1/zoimbie1_reload.png";
        private const string SoldierSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Soldier1/soldier1_gun.png";
        private const string RobotSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Robot1/robot1_machine.png";

        private const string ProjectilePrefabPath = "Assets/_Game/Prefabs/Combat/Projectile_Laser.prefab";
        private const string FloatingTextPrefabPath = "Assets/_Game/Prefabs/UI/FloatingDamageText.prefab";
        private const string HealthPickupPath = "Assets/_Game/Prefabs/Combat/Pickup_Health.prefab";
        private const string RapidPickupPath = "Assets/_Game/Prefabs/Combat/Pickup_RapidFire.prefab";

        static EnemyPrefabBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                BuildAllEnemiesAndWaves();
            };
        }

        [MenuItem("Tools/Enemies/Build & Configure All 4 Enemy Types", priority = 20)]
        public static void BuildMenu()
        {
            BuildAllEnemiesAndWaves();
        }

        public static void BuildAllEnemiesAndWaves()
        {
            if (!AssetDatabase.IsValidFolder(EnemyFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Enemies");
            }

            GameObject chaser = EnsureChaserPrefab();
            GameObject shooter = EnsureShooterPrefab();
            GameObject brute = EnsureBrutePrefab();
            GameObject exploder = EnsureExploderPrefab();

            ConfigureSceneWaves(chaser, shooter, brute, exploder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnemyPrefabBuilder] Successfully built and configured all 4 enemy archetypes and 5 waves!");
        }

        private static GameObject EnsureChaserPrefab()
        {
            GameObject obj = new GameObject("Enemy_Chaser");
            obj.tag = "Enemy";

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ZombieSpritePath);
            sr.sortingOrder = 148;
            sr.color = Color.white;

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            EnemyHealth health = obj.AddComponent<EnemyHealth>();
            SetupHealthDefaults(health, 40f, 0.0f);

            ChaserEnemy chaser = obj.AddComponent<ChaserEnemy>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(obj, ChaserPrefabPath);
            Object.DestroyImmediate(obj);
            return saved;
        }

        private static GameObject EnsureShooterPrefab()
        {
            GameObject obj = new GameObject("Enemy_Shooter");
            obj.tag = "Enemy";

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SoldierSpritePath);
            sr.sortingOrder = 148;
            sr.color = Color.white;

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            EnemyHealth health = obj.AddComponent<EnemyHealth>();
            SetupHealthDefaults(health, 45f, 0.1f);

            RangedShooterEnemy shooter = obj.AddComponent<RangedShooterEnemy>();
            GameObject proj = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (proj != null)
            {
                SerializedObject so = new SerializedObject(shooter);
                so.FindProperty("enemyProjectilePrefab").objectReferenceValue = proj;
                so.ApplyModifiedProperties();
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(obj, ShooterPrefabPath);
            Object.DestroyImmediate(obj);
            return saved;
        }

        private static GameObject EnsureBrutePrefab()
        {
            GameObject obj = new GameObject("Enemy_Brute");
            obj.tag = "Enemy";
            obj.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RobotSpritePath);
            sr.sortingOrder = 149;
            sr.color = new Color(0.85f, 0.9f, 1f, 1f); // Metallic blue-steel tint

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = 3.5f;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.65f;

            EnemyHealth health = obj.AddComponent<EnemyHealth>();
            SetupHealthDefaults(health, 140f, 0.70f); // 140 HP, 70% knockback resistance

            BruteEnemy brute = obj.AddComponent<BruteEnemy>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(obj, BrutePrefabPath);
            Object.DestroyImmediate(obj);
            return saved;
        }

        private static GameObject EnsureExploderPrefab()
        {
            GameObject obj = new GameObject("Enemy_Exploder");
            obj.tag = "Enemy";
            obj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExploderSpritePath);
            sr.sortingOrder = 148;
            sr.color = new Color(1f, 0.85f, 0.2f); // Radioactive toxic yellow tint

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.48f;

            EnemyHealth health = obj.AddComponent<EnemyHealth>();
            SetupHealthDefaults(health, 25f, 0.0f); // Fragile 25 HP

            ExploderEnemy exploder = obj.AddComponent<ExploderEnemy>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(obj, ExploderPrefabPath);
            Object.DestroyImmediate(obj);
            return saved;
        }

        private static void SetupHealthDefaults(EnemyHealth health, float maxHp, float kbResist)
        {
            SerializedObject so = new SerializedObject(health);
            so.FindProperty("maxHealth").floatValue = maxHp;
            so.FindProperty("knockbackResistance").floatValue = kbResist;

            GameObject floatingText = AssetDatabase.LoadAssetAtPath<GameObject>(FloatingTextPrefabPath);
            if (floatingText != null)
            {
                so.FindProperty("floatingTextPrefab").objectReferenceValue = floatingText;
            }

            SerializedProperty lootProp = so.FindProperty("lootPrefabs");
            lootProp.ClearArray();
            GameObject healthPickup = AssetDatabase.LoadAssetAtPath<GameObject>(HealthPickupPath);
            GameObject rapidPickup = AssetDatabase.LoadAssetAtPath<GameObject>(RapidPickupPath);

            int index = 0;
            if (healthPickup != null)
            {
                lootProp.InsertArrayElementAtIndex(index);
                lootProp.GetArrayElementAtIndex(index).objectReferenceValue = healthPickup;
                index++;
            }
            if (rapidPickup != null)
            {
                lootProp.InsertArrayElementAtIndex(index);
                lootProp.GetArrayElementAtIndex(index).objectReferenceValue = rapidPickup;
            }

            so.ApplyModifiedProperties();
        }

        private static void ConfigureSceneWaves(GameObject chaser, GameObject shooter, GameObject brute, GameObject exploder)
        {
            WaveSpawner spawner = Object.FindFirstObjectByType<WaveSpawner>();
            if (spawner == null) return;

            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.ClearArray();

            // Wave 1: 8 Chasers (Learn movement strafe & lunge dodge)
            AddWave(wavesProp, 0, "Wave 1 - Zombie Outbreak", 8, 1.6f, new GameObject[] { chaser });

            // Wave 2: 12 Enemies (Chasers + Tactical Snipers)
            AddWave(wavesProp, 1, "Wave 2 - Sniper Crossfire", 12, 1.4f, new GameObject[] { chaser, shooter });

            // Wave 3: 14 Enemies (Chasers + Kamikaze Exploders)
            AddWave(wavesProp, 2, "Wave 3 - Volatile Swarm", 14, 1.2f, new GameObject[] { chaser, exploder });

            // Wave 4: 18 Enemies (Chasers + Snipers + Exploders)
            AddWave(wavesProp, 3, "Wave 4 - Total Warfare", 18, 1.0f, new GameObject[] { chaser, shooter, exploder });

            // Wave 5: 22 Enemies (2 Heavy Robot Brutes + Swarmers + Snipers + Exploders)
            AddWave(wavesProp, 4, "Wave 5 - Ironclad Titan Invasion", 22, 0.85f, new GameObject[] { chaser, shooter, exploder, brute });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            Debug.Log("[EnemyPrefabBuilder] WaveSpawner configured with 5 escalating tactical waves!");
        }

        private static void AddWave(SerializedProperty wavesProp, int index, string name, int total, float interval, GameObject[] prefabs)
        {
            wavesProp.InsertArrayElementAtIndex(index);
            SerializedProperty wave = wavesProp.GetArrayElementAtIndex(index);
            wave.FindPropertyRelative("waveName").stringValue = name;
            wave.FindPropertyRelative("totalEnemies").intValue = total;
            wave.FindPropertyRelative("spawnInterval").floatValue = interval;

            SerializedProperty prefabsProp = wave.FindPropertyRelative("enemyPrefabs");
            prefabsProp.ClearArray();
            int pIndex = 0;
            foreach (var p in prefabs)
            {
                if (p == null) continue;
                prefabsProp.InsertArrayElementAtIndex(pIndex);
                prefabsProp.GetArrayElementAtIndex(pIndex).objectReferenceValue = p;
                pIndex++;
            }
        }
    }
}
