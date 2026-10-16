using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiniShooterGame.Player;
using MiniShooterGame.Combat;
using MiniShooterGame.Core;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class PlayerPrefabBuilder
    {
        private const string SurvivorSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Survivor1/survivor1_gun.png";
        private const string PistolWeaponPath = "Assets/_Game/Data/Weapons/Weapon_Pistol.asset";
        private const string ProjectilePrefabPath = "Assets/_Game/Prefabs/Combat/Projectile_Laser.prefab";
        private const string PlayerPrefabFolder = "Assets/_Game/Prefabs/Characters";
        public const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/Player.prefab";

        static PlayerPrefabBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                EnsurePlayerPrefabExists();
            };
        }

        [MenuItem("Tools/Open Main Game Scene (SampleScene)", priority = 1)]
        public static void OpenSampleScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }
        }

        [MenuItem("Tools/Spawn Player in Current Scene", priority = 15)]
        public static GameObject SpawnPlayerInScene()
        {
            GameObject existing = GameObject.FindWithTag("Player");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[PlayerPrefabBuilder] Player already exists in the scene: " + existing.name);
                return existing;
            }

            GameObject prefab = EnsurePlayerPrefabExists();
            GameObject instance = null;

            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                instance = CreatePlayerGameObject();
            }

            instance.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Player");

            // Attach CameraFollow2D if main camera exists
            Camera cam = Camera.main;
            if (cam != null)
            {
                CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
                if (follow == null)
                {
                    follow = cam.gameObject.AddComponent<CameraFollow2D>();
                }
                SerializedObject soCam = new SerializedObject(follow);
                soCam.FindProperty("target").objectReferenceValue = instance.transform;
                soCam.FindProperty("cameraZoom").floatValue = 6.5f;
                soCam.ApplyModifiedProperties();
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PlayerPrefabBuilder] Spawned Player successfully at (0,0,0)!");
            return instance;
        }

        public static GameObject EnsurePlayerPrefabExists()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existingPrefab != null) return existingPrefab;

            if (!AssetDatabase.IsValidFolder(PlayerPrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "Characters");
            }

            GameObject tempPlayer = CreatePlayerGameObject();
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempPlayer, PlayerPrefabPath);
            Object.DestroyImmediate(tempPlayer);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerPrefabBuilder] Created Player prefab at " + PlayerPrefabPath);
            return savedPrefab;
        }

        public static GameObject CreatePlayerGameObject()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            // SpriteRenderer
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SurvivorSpritePath);
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 150;

            // Rigidbody2D
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 1f;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // CapsuleCollider2D
            CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
            col.direction = CapsuleDirection2D.Vertical;

            // PlayerMovement
            PlayerMovement movement = player.AddComponent<PlayerMovement>();
            SerializedObject soMove = new SerializedObject(movement);
            soMove.FindProperty("allowKeyboardFallback").boolValue = true;
            soMove.FindProperty("moveSpeed").floatValue = 6f;
            soMove.FindProperty("rotateTowardsMovement").boolValue = true;
            soMove.FindProperty("smoothTurnSpeed").floatValue = 720f;
            soMove.FindProperty("spriteAngleOffset").floatValue = 0f;
            soMove.ApplyModifiedProperties();

            // FirePoint child
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(player.transform, false);
            firePoint.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            // PlayerShooter
            PlayerShooter shooter = player.AddComponent<PlayerShooter>();
            SerializedObject soShoot = new SerializedObject(shooter);
            WeaponDataSO pistol = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(PistolWeaponPath);
            GameObject projectile = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            soShoot.FindProperty("defaultWeapon").objectReferenceValue = pistol;
            soShoot.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            soShoot.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            soShoot.FindProperty("fireRate").floatValue = 3f;
            soShoot.FindProperty("targetRange").floatValue = 8f;
            soShoot.FindProperty("targetTag").stringValue = "Enemy";
            soShoot.ApplyModifiedProperties();

            // PlayerDash
            PlayerDash dash = player.AddComponent<PlayerDash>();
            SerializedObject soDash = new SerializedObject(dash);
            soDash.FindProperty("dashSpeedMultiplier").floatValue = 3.2f;
            soDash.FindProperty("dashDuration").floatValue = 0.2f;
            soDash.FindProperty("dashCooldown").floatValue = 1.5f;
            soDash.FindProperty("invulnerableAlpha").floatValue = 0.5f;
            soDash.FindProperty("allowKeyboardShortcut").boolValue = true;
            soDash.ApplyModifiedProperties();

            // PlayerHealth
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            SerializedObject soHealth = new SerializedObject(health);
            soHealth.FindProperty("maxHealth").intValue = 100;
            soHealth.FindProperty("spawnGracePeriod").floatValue = 1f;
            soHealth.FindProperty("damageFlashDuration").floatValue = 0.1f;
            soHealth.FindProperty("damageFlashColor").colorValue = new Color(1f, 0.3f, 0.3f, 1f);
            soHealth.ApplyModifiedProperties();

            return player;
        }
    }
}
