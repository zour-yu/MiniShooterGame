using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiniShooterGame.Player;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Upgrades primitive capsule and circle shapes into high-quality Kenney top-down sprites.
    /// Accessible via menu: Tools > Upgrade Character Visuals
    /// </summary>
    public static class CharacterVisualUpgrader
    {
        private const string SurvivorSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Survivor1/survivor1_gun.png";
        private const string ZombieSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Zombie1/zoimbie1_hold.png";

        [MenuItem("Tools/Upgrade Character Visuals", priority = 20)]
        public static void UpgradeVisualsMenu()
        {
            UpgradeVisuals(true);
        }

        public static void UpgradeVisuals(bool showDialog = false)
        {
            // 1. Upgrade Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SurvivorSpritePath);
                if (playerSprite != null)
                {
                    SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Undo.RecordObject(sr, "Upgrade Player Sprite");
                        sr.sprite = playerSprite;
                        sr.color = Color.white;
                        sr.sortingOrder = 5;
                    }
                }

                // Adjust PlayerMovement offset for right-facing sprite (0 degrees)
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    Undo.RecordObject(movement, "Adjust Player Facing Angle");
                    SerializedObject so = new SerializedObject(movement);
                    so.FindProperty("spriteAngleOffset").floatValue = 0f;
                    so.ApplyModifiedProperties();
                }

                // Adjust Collider to circular player hitbox
                Collider2D col = player.GetComponent<Collider2D>();
                if (col != null)
                {
                    Undo.RecordObject(col, "Adjust Player Collider");
                    if (col is CapsuleCollider2D capsule)
                    {
                        capsule.size = new Vector2(0.8f, 0.8f);
                    }
                }
            }

            // 2. Upgrade DummyEnemy
            GameObject dummy = GameObject.Find("DummyEnemy");
            if (dummy != null)
            {
                Sprite zombieSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ZombieSpritePath);
                if (zombieSprite != null)
                {
                    SpriteRenderer sr = dummy.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Undo.RecordObject(sr, "Upgrade Dummy Sprite");
                        sr.sprite = zombieSprite;
                        sr.color = Color.white;
                        sr.sortingOrder = 4;
                    }
                }
            }

            // 3. Upgrade Enemy_Chaser Prefab
            string prefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Chaser.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                Sprite zombieSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ZombieSpritePath);
                if (zombieSprite != null)
                {
                    SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = zombieSprite;
                        sr.color = Color.white;
                        sr.sortingOrder = 4;
                        EditorUtility.SetDirty(prefab);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[CharacterVisualUpgrader] Characters upgraded to Kenney top-down high-res sprites!");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Visuals Upgraded",
                    "Character Visuals Upgraded!\n\n" +
                    "• Player upgraded from white capsule to Survivor with Rifle\n" +
                    "• DummyEnemy and Enemy_Chaser upgraded to Zombie sprites\n" +
                    "• Facing angle calibrated for smooth directional aiming",
                    "OK"
                );
            }
        }
    }
}
