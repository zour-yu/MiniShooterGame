using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniShooterGame.Core;
using MiniShooterGame.UI;
using MiniShooterGame.Combat;
using MiniShooterGame.Enemy;
using MiniShooterGame.Player;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Master Editor utility to construct the complete Tropical Island Adventure Arena stage.
    /// Sets up the high-resolution vibrant cartoon grass island, turquoise ocean backdrop,
    /// perimeter water boundary colliders, top-down canopy & palm trees, ancient stone pillars,
    /// wooden fences, destructible crates, explosive red hazard barrels, mossy boulders,
    /// 5 landmark loot drop anchors, Weapon Loot Spawner, Multi-Wave Enemy Spawner (Chasers + Ranged Shooters),
    /// Post-Wave Perk Selection Modal, and Complete Player HUD.
    /// Accessible via menu: Tools > Build Island Arena Stage
    /// </summary>
    public static class ArenaSceneBuilder
    {
        private const string GrassFloorPath = "Assets/_Game/Sprites/Environment/Island/Terrain_Grass.png";
        private const string WaterFloorPath = "Assets/_Game/Sprites/Environment/Island/Terrain_Water.png";
        private const string TreeCanopyPath = "Assets/_Game/Sprites/Environment/Island/Prop_Tree_Canopy.png";
        private const string TreePalmPath = "Assets/_Game/Sprites/Environment/Island/Prop_Tree_Palm.png";
        private const string TreeCanopyLargePath = "Assets/_Game/Sprites/Environment/Island/Tree_Tropical_Canopy_Large.png";
        private const string TreeCanopyMediumPath = "Assets/_Game/Sprites/Environment/Island/Tree_Tropical_Canopy_Medium.png";
        private const string TreePalmTallPath = "Assets/_Game/Sprites/Environment/Island/Tree_Tropical_Palm_Tall.png";
        private const string TreePalmSlenderPath = "Assets/_Game/Sprites/Environment/Island/Tree_Tropical_Palm_Slender.png";
        private const string TreePalmCurvedPath = "Assets/_Game/Sprites/Environment/Island/Tree_Tropical_Palm_Curved.png";

        private const string ShorelineStraightPath = "Assets/_Game/Sprites/Environment/Island/Decal_Shoreline_Straight.png";
        private const string ShorelineCornerPath = "Assets/_Game/Sprites/Environment/Island/Decal_Shoreline_Corner.png";
        private const string CoastalRocksPath = "Assets/_Game/Sprites/Environment/Island/Prop_Coastal_Rocks.png";
        private const string CoastalCliffPath = "Assets/_Game/Sprites/Environment/Island/Prop_Coastal_Cliff.png";
        private const string TreasureChestPath = "Assets/_Game/Sprites/Environment/Island/Prop_Treasure_Chest.png";

        private const string DirtTrailLongPath = "Assets/_Game/Sprites/Environment/Island/Decal_Dirt_Trail_Long.png";
        private const string DirtCrossroadsPath = "Assets/_Game/Sprites/Environment/Island/Decal_Dirt_Crossroads.png";
        private const string CampfireGroundPath = "Assets/_Game/Sprites/Environment/Island/Decal_Campfire_Ground.png";
        private const string RuinsRubblePath = "Assets/_Game/Sprites/Environment/Island/Decal_Ruins_Rubble.png";

        private const string WoodCratePath = "Assets/_Game/Sprites/Environment/Island/Prop_Wood_Crate.png";
        private const string BarrelPath = "Assets/_Game/Sprites/Environment/Island/Prop_Barrel_Red.png";
        private const string PillarPath = "Assets/_Game/Sprites/Environment/Island/Prop_Stone_Pillar.png";
        private const string FencePath = "Assets/_Game/Sprites/Environment/Island/Prop_Wood_Fence.png";
        private const string BoulderPath = "Assets/_Game/Sprites/Environment/Island/Prop_Rock_Boulder.png";

        private const string SurvivorLodgePath = "Assets/_Game/Sprites/Environment/Island/Building_Survivor_Lodge.png";
        private const string BeachRimPath = "Assets/_Game/Sprites/Environment/Island/Decal_Beach_Sand_Rim.png";
        private const string WoodenPierPath = "Assets/_Game/Sprites/Environment/Island/Decal_Wooden_Pier.png";
        private const string RuinsPavementPath = "Assets/_Game/Sprites/Environment/Island/Decal_Ruins_Pavement.png";
        private const string SandbagCurvedPath = "Assets/_Game/Sprites/Environment/Island/Prop_Sandbag_Curved.png";
        private const string SandbagStraightPath = "Assets/_Game/Sprites/Environment/Island/Prop_Sandbag_Straight.png";
        private const string DirtPatchPath = "Assets/_Game/Sprites/Environment/Island/Decal_Dirt_Patch.png";

        private const string SoldierSpritePath = "Assets/_Game/Sprites/KenneyTopDown/Characters/Soldier1/soldier1_gun.png";
        private const string ChaserPrefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Chaser.prefab";
        private const string ShooterPrefabPath = "Assets/_Game/Prefabs/Enemies/Enemy_Shooter.prefab";
        private const string DefaultProjectilePath = "Assets/_Game/Prefabs/Projectiles/Projectile_Default.prefab";

        private const string HealthFramePath = "Assets/_Game/Sprites/UI/Health_Frame.png";
        private const string HealthFillPath = "Assets/_Game/Sprites/UI/Health_Fill.png";
        private const string VignettePath = "Assets/_Game/Sprites/UI/Vignette_Red.png";

        private const string StageRootName = "[Arena_Stage]";
        private const string ObstacleTag = "Obstacle";

        [MenuItem("Tools/Build Island Arena Stage", priority = 10)]
        [MenuItem("Tools/Build Cyber Arena Stage", priority = 11)]
        [MenuItem("Tools/Architecture/Build Island Arena Stage", priority = 102)]
        public static void BuildArenaStageMenu()
        {
            BuildArenaStage(true);
        }

        public static void BuildArenaStage(bool showDialog = false)
        {
            EnsureObstacleTagExists();

            // 1. Find or Create Root [Arena_Stage]
            GameObject stageRoot = GameObject.Find(StageRootName);
            if (stageRoot == null)
            {
                stageRoot = new GameObject(StageRootName);
                Undo.RegisterCreatedObjectUndo(stageRoot, "Create Arena Stage");
            }
            stageRoot.transform.position = Vector3.zero;
            stageRoot.transform.rotation = Quaternion.identity;
            stageRoot.transform.localScale = Vector3.one;

            // 2. Setup Modular Island & Ocean Floor
            SetupModularFloor(stageRoot);

            // 3. Setup Perimeter Water Boundaries
            SetupPerimeterBoundaries(stageRoot);

            // 4. Setup Tactical Cover & Obstacle Props
            SetupTacticalCover(stageRoot);

            // 5. Configure Camera (Action View)
            ConfigureCamera();

            // 6. Ensure Weapons & Enemy Prefabs Exist
            var weapons = EnsureWeaponAssetsExist();
            GameObject shooterPrefab = EnsureEnemyShooterPrefabExists();
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChaserPrefabPath);

            // 7. Setup Wave Manager (Chaser + Ranged Shooters)
            SetupWaveManager(stageRoot, chaserPrefab, shooterPrefab);

            // 8. Setup Weapon Loot Spawner at Landmark Anchors
            SetupWeaponLootSpawner(stageRoot, weapons);

            // 9. Setup Complete Player HUD & Perk Modal
            SetupHUD(stageRoot);

            // 10. Wire Player Default Weapon
            ConfigurePlayerDefaultWeapon(weapons[0]); // Pistol

            // Mark scene dirty, save to disk, and select stage
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = stageRoot;

            Debug.Log("[ArenaSceneBuilder] Tropical Island Arena Stage & Systems constructed successfully!");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Island Arena Builder",
                    "Tropical Island Arena Stage & Gameplay Systems built successfully!\n\n" +
                    "• Playable Grass Island: 38x26 units (Sorting Order -50)\n" +
                    "• Ocean Water Backdrop: 56x40 units (Sorting Order -60)\n" +
                    "• Destructible Crates, Red Exploding Barrels, Trees, Pillars, Boulders\n" +
                    "• 5 Loot Anchors: Center, North Grove, South Ruins, East Camp, West Shore\n" +
                    "• Weapon Loot Crates: Combat Shotgun & Assault Rifle drops with finite ammo\n" +
                    "• Enemy Archetypes: Zombie Melee Chasers + Soldier Ranged Shooters\n" +
                    "• Post-Wave 3-Card Perk Selection Screen\n" +
                    "• Player HUD with Weapon/Ammo Badge and 130px Mobile Dash Button",
                    "OK"
                );
            }
        }

        private static void SetupModularFloor(GameObject stageRoot)
        {
            // Ocean Water Backdrop
            Transform waterTransform = stageRoot.transform.Find("Water_Backdrop");
            GameObject waterObj = waterTransform != null ? waterTransform.gameObject : new GameObject("Water_Backdrop");
            if (waterTransform == null)
            {
                waterObj.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(waterObj, "Create Water Backdrop");
            }

            waterObj.transform.localPosition = Vector3.zero;
            waterObj.transform.localRotation = Quaternion.identity;
            waterObj.transform.localScale = Vector3.one;

            Sprite waterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WaterFloorPath);
            SpriteRenderer waterSr = waterObj.GetComponent<SpriteRenderer>();
            if (waterSr == null) waterSr = waterObj.AddComponent<SpriteRenderer>();

            if (waterSprite != null)
            {
                waterSr.sprite = waterSprite;
                waterSr.drawMode = SpriteDrawMode.Tiled;
                waterSr.size = new Vector2(56f, 40f);
                waterSr.color = Color.white;
            }
            waterSr.sortingLayerName = "Default";
            waterSr.sortingOrder = -60;

            // Clean up legacy Beach_Sand_Rim if present
            Transform legacyBeach = stageRoot.transform.Find("Beach_Sand_Rim");
            if (legacyBeach != null)
            {
                Undo.DestroyObjectImmediate(legacyBeach.gameObject);
            }

            // Island Grass Ground
            Transform groundTransform = stageRoot.transform.Find("Island_Ground");
            GameObject groundObj = groundTransform != null ? groundTransform.gameObject : new GameObject("Island_Ground");
            if (groundTransform == null)
            {
                groundObj.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(groundObj, "Create Island Ground");
            }

            // Remove legacy Arena_Background if it exists
            Transform legacyBg = stageRoot.transform.Find("Arena_Background");
            if (legacyBg != null)
            {
                Undo.DestroyObjectImmediate(legacyBg.gameObject);
            }

            groundObj.transform.localPosition = Vector3.zero;
            groundObj.transform.localRotation = Quaternion.identity;
            groundObj.transform.localScale = Vector3.one;

            Sprite grassSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrassFloorPath);
            SpriteRenderer grassSr = groundObj.GetComponent<SpriteRenderer>();
            if (grassSr == null) grassSr = groundObj.AddComponent<SpriteRenderer>();

            if (grassSprite != null)
            {
                grassSr.sprite = grassSprite;
                grassSr.drawMode = SpriteDrawMode.Tiled;
                grassSr.size = new Vector2(36f, 24f);
                grassSr.color = new Color(0.96f, 1f, 0.96f, 1f);
            }
            grassSr.sortingLayerName = "Default";
            grassSr.sortingOrder = -50;

            // Ground Decals Container (Ruins Pavement, Pier, Dirt Patches)
            Transform decalsRootTransform = stageRoot.transform.Find("[Ground_Decals]");
            if (decalsRootTransform != null)
            {
                Undo.DestroyObjectImmediate(decalsRootTransform.gameObject);
            }
            GameObject decalsRoot = new GameObject("[Ground_Decals]");
            decalsRoot.transform.SetParent(stageRoot.transform);
            Undo.RegisterCreatedObjectUndo(decalsRoot, "Create Ground Decals Root");
            decalsRoot.transform.localPosition = Vector3.zero;

            // Load Decal Sprites
            Sprite ruinsPavementSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RuinsPavementPath);
            Sprite woodenPierSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodenPierPath);
            Sprite beachSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BeachRimPath);

            Sprite shorelineStraightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShorelineStraightPath);
            Sprite shorelineCornerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShorelineCornerPath);

            Sprite crossroadsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DirtCrossroadsPath);
            Sprite trailLongSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DirtTrailLongPath);
            Sprite campfireSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CampfireGroundPath);
            Sprite ruinsRubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RuinsRubblePath);

            // ==========================================
            // 1. Natural Coastline Shoreline Perimeter (Seamless overlapping beach border)
            // ==========================================
            if (shorelineStraightSprite != null)
            {
                // North Shore (Sand faces south down into island, water faces north: rot = 180°)
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_1", new Vector2(-17.5f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_2", new Vector2(-11.5f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_3", new Vector2(-5.8f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_4", new Vector2(0.0f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_5", new Vector2(5.8f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_6", new Vector2(11.5f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_North_7", new Vector2(17.5f, 12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 180f, -48, Color.white);

                // South Shore (Sand faces north up into island, water faces south: rot = 0°)
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_1", new Vector2(-17.5f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_2", new Vector2(-11.5f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_3", new Vector2(-5.8f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_4", new Vector2(0.0f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_5", new Vector2(5.8f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_6", new Vector2(11.5f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_South_7", new Vector2(17.5f, -12.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 0f, -48, Color.white);

                // West Shore (Sand faces east into island, water faces west: rot = -90°)
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_West_North_2", new Vector2(-18.0f, 11.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), -90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_West_North_1", new Vector2(-18.0f, 5.8f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), -90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_West_South_1", new Vector2(-18.0f, -5.8f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), -90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_West_South_2", new Vector2(-18.0f, -11.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), -90f, -48, Color.white);

                // East Shore (Sand faces west into island, water faces east: rot = 90°)
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_East_1", new Vector2(18.0f, 11.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_East_2", new Vector2(18.0f, 5.5f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_East_3", new Vector2(18.0f, 0.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_East_4", new Vector2(18.0f, -5.5f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 90f, -48, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Shore_East_5", new Vector2(18.0f, -11.0f), shorelineStraightSprite, new Vector2(1.4f, 1.25f), 90f, -48, Color.white);
            }

            // ==========================================
            // 2. West Coastal Wooden Pier & Sandy Cove
            // ==========================================
            if (beachSprite != null)
            {
                CreateOrUpdateDecalWithRot(decalsRoot, "Beach_Cove_Pier", new Vector2(-17.5f, 0.0f), beachSprite, new Vector2(-1.0f, 1.15f), 0f, -47, new Color(1f, 1f, 1f, 0.95f));
            }
            CreateOrUpdateDecalWithRot(decalsRoot, "Decal_West_Pier", new Vector2(-17.2f, 0.0f), woodenPierSprite, new Vector2(1.15f, 1.15f), 0f, -42, Color.white);

            // ==========================================
            // 3. Central Crossroads & Continuous Connecting Pathways
            // ==========================================
            if (crossroadsSprite != null)
            {
                CreateOrUpdateDecalWithRot(decalsRoot, "Decal_Center_Crossroads", Vector2.zero, crossroadsSprite, new Vector2(1.6f, 1.6f), 0f, -49, Color.white);
            }

            if (trailLongSprite != null)
            {
                // North Path leading from Crossroads up to Survivor Lodge
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_North_1", new Vector2(0f, 3.2f), trailLongSprite, new Vector2(0.85f, 0.55f), 0f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_North_2", new Vector2(0f, 5.8f), trailLongSprite, new Vector2(0.85f, 0.55f), 0f, -49, Color.white);

                // South Path leading from Crossroads down to Ancient Ruins
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_South_1", new Vector2(0.3f, -3.2f), trailLongSprite, new Vector2(0.85f, 0.55f), 0f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_South_2", new Vector2(0.5f, -5.8f), trailLongSprite, new Vector2(0.85f, 0.55f), 0f, -49, Color.white);

                // East Path leading continuously from Crossroads to Fortified Camp
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_East_1", new Vector2(3.6f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), -90f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_East_2", new Vector2(6.8f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), -90f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_East_3", new Vector2(10.0f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), -90f, -49, Color.white);

                // West Path leading continuously from Crossroads to Wooden Pier
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_West_1", new Vector2(-3.6f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), 90f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_West_2", new Vector2(-6.8f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), 90f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_West_3", new Vector2(-10.0f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), 90f, -49, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Path_West_4", new Vector2(-13.2f, 0f), trailLongSprite, new Vector2(0.85f, 0.55f), 90f, -49, Color.white);
            }

            // ==========================================
            // 4. East Survivor Camp Ground Foundation
            // ==========================================
            if (campfireSprite != null)
            {
                CreateOrUpdateDecalWithRot(decalsRoot, "Decal_Campfire_Ground", new Vector2(12.8f, 0.0f), campfireSprite, new Vector2(2.0f, 2.0f), 0f, -48, Color.white);
            }

            // ==========================================
            // 5. South Ancient Ruins Flagstone Platform & Rubble Scatter
            // ==========================================
            CreateOrUpdateDecalWithRot(decalsRoot, "Decal_Ruins_Floor", new Vector2(0.5f, -8.8f), ruinsPavementSprite, new Vector2(3.2f, 2.0f), 0f, -46, Color.white);
            if (ruinsRubbleSprite != null)
            {
                CreateOrUpdateDecalWithRot(decalsRoot, "Decal_Ruins_Rubble_L", new Vector2(-4.6f, -8.8f), ruinsRubbleSprite, new Vector2(0.85f, 0.85f), 0f, -47, Color.white);
                CreateOrUpdateDecalWithRot(decalsRoot, "Decal_Ruins_Rubble_R", new Vector2(5.6f, -8.8f), ruinsRubbleSprite, new Vector2(-0.85f, 0.85f), 0f, -47, Color.white);
            }
        }

        private static void SetupPerimeterBoundaries(GameObject stageRoot)
        {
            Transform boundRootTransform = stageRoot.transform.Find("[Boundaries]");
            GameObject boundRoot = boundRootTransform != null ? boundRootTransform.gameObject : new GameObject("[Boundaries]");
            if (boundRootTransform == null)
            {
                boundRoot.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(boundRoot, "Create Boundaries Root");
            }

            boundRoot.transform.localPosition = Vector3.zero;

            // Perimeter walls calibrated to 36x24 playable island
            CreateOrUpdateBoxCollider(boundRoot, "Wall_North", new Vector2(0f, 12.2f), new Vector2(36f, 1.4f));
            CreateOrUpdateBoxCollider(boundRoot, "Wall_South", new Vector2(0f, -12.2f), new Vector2(36f, 1.4f));
            CreateOrUpdateBoxCollider(boundRoot, "Wall_East", new Vector2(18.2f, 0f), new Vector2(1.4f, 24f));
            CreateOrUpdateBoxCollider(boundRoot, "Wall_West", new Vector2(-18.2f, 0f), new Vector2(1.4f, 24f));
        }

        private static void CreateOrUpdateBoxCollider(GameObject parent, string name, Vector2 localPos, Vector2 size)
        {
            Transform child = parent.transform.Find(name);
            GameObject obj = child != null ? child.gameObject : new GameObject(name);
            if (child == null)
            {
                obj.transform.SetParent(parent.transform);
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            }

            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            BoxCollider2D box = obj.GetComponent<BoxCollider2D>();
            if (box == null) box = obj.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = false;
        }

        private static void SetupTacticalCover(GameObject stageRoot)
        {
            Transform obsRootTransform = stageRoot.transform.Find("[Obstacles]");
            GameObject obsRoot = obsRootTransform != null ? obsRootTransform.gameObject : new GameObject("[Obstacles]");
            if (obsRootTransform == null)
            {
                obsRoot.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(obsRoot, "Create Obstacles Root");
            }

            obsRoot.transform.localPosition = Vector3.zero;

            // Completely wipe out all legacy barricades, server towers, and old dummy cover
            for (int i = obsRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(obsRoot.transform.GetChild(i).gameObject);
            }

            // Also clean up any stray legacy scene objects from earlier iterations
            string[] legacyNames = new string[] { 
                "Barricade_North", "Barricade_South", "Barricade_East", "Barricade_West", 
                "Tower_TopLeft", "Tower_TopRight", "Tower_BottomLeft", "Tower_BottomRight",
                "DummyEnemy", "Arena_Background"
            };
            foreach (var legName in legacyNames)
            {
                GameObject legObj = GameObject.Find(legName);
                if (legObj != null)
                {
                    Object.DestroyImmediate(legObj);
                }
            }

            // Load Island Assets
            Sprite treeCanopyLargeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TreeCanopyLargePath);
            Sprite treeCanopyMedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TreeCanopyMediumPath);
            Sprite treePalmTallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TreePalmTallPath);
            Sprite treePalmSlenderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TreePalmSlenderPath);
            Sprite treePalmCurvedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TreePalmCurvedPath);
            Sprite coastalRocksSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoastalRocksPath);
            Sprite coastalCliffSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoastalCliffPath);
            Sprite treasureChestSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TreasureChestPath);

            Sprite woodCrateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WoodCratePath);
            Sprite barrelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarrelPath);
            Sprite pillarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PillarPath);
            Sprite fenceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FencePath);
            Sprite boulderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoulderPath);
            Sprite lodgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SurvivorLodgePath);
            Sprite sandbagCurvedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SandbagCurvedPath);
            Sprite sandbagStraightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SandbagStraightPath);

            // ==========================================
            // 1. Central Tactical Flanks (Spacious 16x10m open center for high-mobility kiting)
            // ==========================================
            // North-West cover pocket
            CreateOrUpdateCrate(obsRoot, "Crate_Center_NW", new Vector2(-6.0f, 3.5f), woodCrateSprite, true);
            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Center_NW", new Vector2(-5.0f, 4.0f), barrelSprite);

            // South-East cover pocket
            CreateOrUpdateFenceProp(obsRoot, "Fence_Center_SE", new Vector2(5.5f, -3.5f), fenceSprite, new Vector2(0.35f, 0.35f));
            CreateOrUpdateCrate(obsRoot, "Crate_Center_SE", new Vector2(6.8f, -3.2f), woodCrateSprite, true);

            // ==========================================
            // 2. North Grove & Survivor Outpost Lodge (y = 8.8f with lush canopy backdrop)
            // ==========================================
            // Main Survivor Lodge (Solid Line-of-Sight blocker & level anchor)
            CreateOrUpdateLodge(obsRoot, "Building_Survivor_Lodge", new Vector2(0.0f, 8.8f), lodgeSprite);

            // Dense tropical canopy trees peeking over the lodge rooftop and ridge
            CreateOrUpdateTreeProp(obsRoot, "Tree_Canopy_Back_FarL", new Vector2(-6.8f, 11.0f), treeCanopyMedSprite, new Vector2(0.95f, 0.95f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Canopy_Back_L", new Vector2(-3.4f, 11.2f), treeCanopyLargeSprite, new Vector2(1.1f, 1.1f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Canopy_Back_C", new Vector2(0.0f, 11.6f), treeCanopyMedSprite, new Vector2(1.0f, 1.0f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Canopy_Back_R", new Vector2(3.4f, 11.2f), treeCanopyLargeSprite, new Vector2(-1.1f, 1.1f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Canopy_Back_FarR", new Vector2(6.8f, 11.0f), treeCanopyMedSprite, new Vector2(-0.95f, 0.95f));

            // Flanking North High-Ground Canopy & Palms
            CreateOrUpdateTreeProp(obsRoot, "Tree_Grove_NW_1", new Vector2(-10.5f, 9.2f), treeCanopyLargeSprite, new Vector2(1.05f, 1.05f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Grove_NW_2", new Vector2(-14.2f, 9.5f), treeCanopyMedSprite, new Vector2(0.95f, 0.95f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Grove_NE_1", new Vector2(10.5f, 9.2f), treeCanopyLargeSprite, new Vector2(-1.05f, 1.05f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Grove_NE_2", new Vector2(14.2f, 9.5f), treeCanopyMedSprite, new Vector2(0.95f, 0.95f));

            // Flanking Sandbag Bunkers protecting the Lodge front perimeter
            CreateOrUpdateSandbag(obsRoot, "Sandbag_Lodge_West", new Vector2(-4.2f, 7.2f), sandbagStraightSprite, new Vector2(0.65f, 0.65f));
            CreateOrUpdateSandbag(obsRoot, "Sandbag_Lodge_East", new Vector2(4.2f, 7.2f), sandbagStraightSprite, new Vector2(-0.65f, 0.65f));

            // Boulders, Supply Crates, and Barrels
            CreateOrUpdateBoulderProp(obsRoot, "Boulder_North_1", new Vector2(-7.2f, 7.2f), boulderSprite, new Vector2(0.35f, 0.35f));
            CreateOrUpdateBoulderProp(obsRoot, "Boulder_North_2", new Vector2(7.2f, 7.2f), boulderSprite, new Vector2(0.38f, 0.38f));
            CreateOrUpdateCrate(obsRoot, "Crate_Grove_1", new Vector2(-2.6f, 6.8f), woodCrateSprite, true);
            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Grove_1", new Vector2(2.6f, 6.8f), barrelSprite);

            // ==========================================
            // 3. South Ancient Ruins (Stone Pillars, Altar Chest & Ruins Palms)
            // ==========================================
            CreateOrUpdatePillarProp(obsRoot, "Pillar_Ruins_1", new Vector2(-9.0f, -8.5f), pillarSprite, new Vector2(0.40f, 0.42f));
            CreateOrUpdatePillarProp(obsRoot, "Pillar_Ruins_2", new Vector2(-4.5f, -9.8f), pillarSprite, new Vector2(0.40f, 0.42f));
            CreateOrUpdatePillarProp(obsRoot, "Pillar_Ruins_3", new Vector2(0.5f, -9.8f), pillarSprite, new Vector2(0.40f, 0.42f));
            CreateOrUpdatePillarProp(obsRoot, "Pillar_Ruins_4", new Vector2(5.5f, -9.8f), pillarSprite, new Vector2(0.40f, 0.42f));
            CreateOrUpdatePillarProp(obsRoot, "Pillar_Ruins_5", new Vector2(10.0f, -8.5f), pillarSprite, new Vector2(0.40f, 0.42f));

            // Altar Treasure Chest in the center of the ruins
            CreateOrUpdateTreasureChest(obsRoot, "Chest_Ruins_Altar", new Vector2(0.5f, -8.2f), treasureChestSprite, new Vector2(0.70f, 0.70f));

            // Flanking Curved Tropical Palms
            CreateOrUpdateTreeProp(obsRoot, "Palm_Ruins_L", new Vector2(-12.5f, -9.0f), treePalmCurvedSprite, new Vector2(-0.9f, 0.9f));
            CreateOrUpdateTreeProp(obsRoot, "Palm_Ruins_R", new Vector2(12.5f, -9.0f), treePalmCurvedSprite, new Vector2(0.9f, 0.9f));

            // Ruins Boulders & Barrels
            CreateOrUpdateBoulderProp(obsRoot, "Boulder_Ruins_1", new Vector2(-2.2f, -8.6f), boulderSprite, new Vector2(0.35f, 0.35f));
            CreateOrUpdateBoulderProp(obsRoot, "Boulder_Ruins_2", new Vector2(3.2f, -8.6f), boulderSprite, new Vector2(0.38f, 0.38f));
            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Ruins_1", new Vector2(2.5f, -7.5f), barrelSprite);
            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Ruins_2", new Vector2(-2.5f, -7.5f), barrelSprite);

            // ==========================================
            // 4. East Survivor Camp / Outpost (Campfire, Fortified Sandbags & Palms)
            // ==========================================
            CreateOrUpdateFenceProp(obsRoot, "Fence_Camp_1", new Vector2(13.0f, 3.5f), fenceSprite, new Vector2(0.35f, 0.35f));
            CreateOrUpdateFenceProp(obsRoot, "Fence_Camp_2", new Vector2(13.0f, -3.5f), fenceSprite, new Vector2(0.35f, 0.35f));

            // Fortified curved & straight sandbags
            CreateOrUpdateSandbag(obsRoot, "Sandbag_Camp_Curved_North", new Vector2(11.2f, 2.5f), sandbagCurvedSprite, new Vector2(0.70f, 0.70f));
            CreateOrUpdateSandbag(obsRoot, "Sandbag_Camp_Curved_South", new Vector2(11.2f, -2.5f), sandbagCurvedSprite, new Vector2(-0.70f, 0.70f));
            CreateOrUpdateSandbag(obsRoot, "Sandbag_Camp_Straight_Rear", new Vector2(15.2f, 0.0f), sandbagStraightSprite, new Vector2(0.65f, 0.65f));

            CreateOrUpdateCrate(obsRoot, "Crate_Camp_1", new Vector2(13.5f, 1.4f), woodCrateSprite, true);
            CreateOrUpdateCrate(obsRoot, "Crate_Camp_2", new Vector2(13.5f, 0.4f), woodCrateSprite, true);
            CreateOrUpdateCrate(obsRoot, "Crate_Camp_3", new Vector2(14.5f, -1.2f), woodCrateSprite, false);

            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Camp_1", new Vector2(14.5f, 2.0f), barrelSprite);
            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Camp_2", new Vector2(13.2f, -2.0f), barrelSprite);

            CreateOrUpdateTreeProp(obsRoot, "Tree_Camp_Palm_1", new Vector2(16.5f, 5.0f), treePalmTallSprite, new Vector2(0.85f, 0.85f));
            CreateOrUpdateTreeProp(obsRoot, "Tree_Camp_Palm_2", new Vector2(16.5f, -5.0f), treePalmCurvedSprite, new Vector2(-0.85f, 0.85f));

            // ==========================================
            // 5. West Shoreline / Palm Beach (Pier & Coast)
            // ==========================================
            CreateOrUpdateTreeProp(obsRoot, "Palm_West_1", new Vector2(-15.2f, 8.0f), treePalmTallSprite, new Vector2(0.85f, 0.85f));
            CreateOrUpdateTreeProp(obsRoot, "Palm_West_2", new Vector2(-16.0f, 3.2f), treePalmCurvedSprite, new Vector2(-0.85f, 0.85f));
            CreateOrUpdateTreeProp(obsRoot, "Palm_West_3", new Vector2(-15.0f, -3.2f), treePalmSlenderSprite, new Vector2(0.85f, 0.85f));
            CreateOrUpdateTreeProp(obsRoot, "Palm_West_4", new Vector2(-15.8f, -8.0f), treePalmTallSprite, new Vector2(0.85f, 0.85f));

            CreateOrUpdateBoulderProp(obsRoot, "Boulder_West_1", new Vector2(-13.5f, 4.5f), boulderSprite, new Vector2(0.35f, 0.35f));
            CreateOrUpdateBoulderProp(obsRoot, "Boulder_West_2", new Vector2(-13.5f, -5.0f), boulderSprite, new Vector2(0.32f, 0.32f));

            CreateOrUpdateCrate(obsRoot, "Crate_Beach_1", new Vector2(-13.0f, 2.0f), woodCrateSprite, true);
            CreateOrUpdateExplosiveBarrel(obsRoot, "Barrel_Beach_1", new Vector2(-13.0f, -2.0f), barrelSprite);

            // ==========================================
            // 6. Coastal Sea Rocks, Cliffs & Capes (Natural ocean perimeter anchors at corners and margins)
            // ==========================================
            // 4 Distinct Corner Capes with Sea Rocks & Leaning Palms
            CreateOrUpdateCoastalRock(obsRoot, "Cape_Rock_NW", new Vector2(-18.0f, 12.0f), coastalRocksSprite, new Vector2(1.15f, 1.15f));
            CreateOrUpdateTreeProp(obsRoot, "Cape_Palm_NW", new Vector2(-16.8f, 10.8f), treePalmCurvedSprite, new Vector2(-0.95f, 0.95f));

            CreateOrUpdateCoastalRock(obsRoot, "Cape_Cliff_NE", new Vector2(18.0f, 12.0f), coastalCliffSprite, new Vector2(1.15f, 1.15f));
            CreateOrUpdateTreeProp(obsRoot, "Cape_Palm_NE", new Vector2(16.8f, 10.8f), treePalmCurvedSprite, new Vector2(0.95f, 0.95f));

            CreateOrUpdateCoastalRock(obsRoot, "Cape_Cliff_SW", new Vector2(-18.0f, -12.0f), coastalCliffSprite, new Vector2(-1.15f, 1.15f));
            CreateOrUpdateTreeProp(obsRoot, "Cape_Palm_SW", new Vector2(-16.8f, -10.8f), treePalmCurvedSprite, new Vector2(-0.95f, 0.95f));

            CreateOrUpdateCoastalRock(obsRoot, "Cape_Rock_SE", new Vector2(18.0f, -12.0f), coastalRocksSprite, new Vector2(1.15f, 1.15f));
            CreateOrUpdateTreeProp(obsRoot, "Cape_Palm_SE", new Vector2(16.8f, -10.8f), treePalmCurvedSprite, new Vector2(0.95f, 0.95f));

            // Coastal surf boulders along the perimeter
            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_North_C", new Vector2(0.0f, 13.8f), coastalRocksSprite, new Vector2(0.90f, 0.90f));
            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_North_W", new Vector2(-7.5f, 13.2f), coastalRocksSprite, new Vector2(-0.85f, 0.85f));
            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_North_E", new Vector2(7.5f, 13.2f), coastalRocksSprite, new Vector2(0.85f, 0.85f));

            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_South_C", new Vector2(0.0f, -13.6f), coastalRocksSprite, new Vector2(-0.90f, 0.90f));
            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_South_W", new Vector2(-7.5f, -13.2f), coastalRocksSprite, new Vector2(0.85f, 0.85f));
            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_South_E", new Vector2(7.5f, -13.2f), coastalRocksSprite, new Vector2(-0.85f, 0.85f));

            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_West_1", new Vector2(-19.0f, -2.8f), coastalRocksSprite, new Vector2(0.80f, 0.80f));
            CreateOrUpdateCoastalRock(obsRoot, "Rock_Coast_West_2", new Vector2(-19.0f, 2.8f), coastalCliffSprite, new Vector2(0.75f, 0.75f));
            CreateOrUpdateCoastalRock(obsRoot, "Cliff_Coast_East", new Vector2(19.0f, 0.0f), coastalCliffSprite, new Vector2(0.85f, 0.85f));

            // ==========================================
            // 7. Setup Dedicated Loot Spawner Anchors
            // ==========================================
            SetupLootDropAnchors(stageRoot);
        }

        private static void CreateOrUpdateTreeProp(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            CircleCollider2D circle = GetOrAddComponent<CircleCollider2D>(obj);
            circle.offset = new Vector2(0f, -0.7f);
            circle.radius = 0.35f;
            circle.isTrigger = false;
        }

        private static void CreateOrUpdateBoulderProp(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            CircleCollider2D circle = GetOrAddComponent<CircleCollider2D>(obj);
            circle.offset = Vector2.zero;
            circle.radius = 0.95f;
            circle.isTrigger = false;
        }

        private static void CreateOrUpdatePillarProp(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            BoxCollider2D box = GetOrAddComponent<BoxCollider2D>(obj);
            box.offset = new Vector2(0f, -0.8f);
            box.size = new Vector2(1.2f, 1.4f);
            box.isTrigger = false;
        }

        private static void CreateOrUpdateFenceProp(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            BoxCollider2D box = GetOrAddComponent<BoxCollider2D>(obj);
            box.offset = Vector2.zero;
            box.size = new Vector2(2.6f, 1.0f);
            box.isTrigger = false;
        }

        private static void CreateOrUpdateExplosiveBarrel(GameObject parent, string name, Vector2 localPos, Sprite sprite)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector2(0.30f, 0.30f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            CircleCollider2D circle = GetOrAddComponent<CircleCollider2D>(obj);
            circle.offset = Vector2.zero;
            circle.radius = 0.70f;
            circle.isTrigger = false;

            ExplosiveBarrel barrel = GetOrAddComponent<ExplosiveBarrel>(obj);
        }

        private static void CreateOrUpdateCrate(GameObject parent, string name, Vector2 localPos, Sprite sprite, bool isDestructible)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector2(0.32f, 0.32f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            BoxCollider2D box = GetOrAddComponent<BoxCollider2D>(obj);
            box.offset = Vector2.zero;
            box.size = new Vector2(1.8f, 1.8f);
            box.isTrigger = false;

            if (isDestructible)
            {
                DestructibleCrate crate = GetOrAddComponent<DestructibleCrate>(obj);
            }
        }

        private static void CreateOrUpdateLodge(GameObject parent, string name, Vector2 localPos, Sprite sprite)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            // BoxCollider2D covering the solid foundation of the building
            BoxCollider2D box = GetOrAddComponent<BoxCollider2D>(obj);
            box.offset = new Vector2(0f, -0.6f);
            box.size = new Vector2(7.0f, 3.2f);
            box.isTrigger = false;
        }

        private static void CreateOrUpdateDecal(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 scale, int sortingOrder, Color color)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = color;
            }
            sr.sortingLayerName = "Default";
            sr.sortingOrder = sortingOrder;
        }

        private static void CreateOrUpdateDecalWithRot(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 scale, float rotZ, int sortingOrder, Color color)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = color;
            }
            sr.sortingLayerName = "Default";
            sr.sortingOrder = sortingOrder;
        }

        private static void CreateOrUpdateCoastalRock(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            CircleCollider2D circle = GetOrAddComponent<CircleCollider2D>(obj);
            circle.offset = Vector2.zero;
            circle.radius = 1.0f;
            circle.isTrigger = false;
        }

        private static void CreateOrUpdateTreasureChest(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            BoxCollider2D box = GetOrAddComponent<BoxCollider2D>(obj);
            box.offset = new Vector2(0f, -0.2f);
            box.size = new Vector2(1.8f, 1.2f);
            box.isTrigger = false;
        }

        private static void CreateOrUpdateSandbag(GameObject parent, string name, Vector2 localPos, Sprite sprite, Vector2 visualScale)
        {
            GameObject obj = GetOrCreateChild(parent, name);
            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = visualScale;

            SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(obj);
            if (sprite != null) sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = Mathf.RoundToInt(-localPos.y * 10) + 100;

            BoxCollider2D box = GetOrAddComponent<BoxCollider2D>(obj);
            box.offset = Vector2.zero;
            if (sprite != null)
            {
                box.size = new Vector2(sprite.bounds.size.x * 0.85f, sprite.bounds.size.y * 0.70f);
            }
            else
            {
                box.size = new Vector2(2.5f, 1.2f);
            }
            box.isTrigger = false;
        }

        private static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            return obj;
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            if (obj == null) return null;
            T comp = obj.GetComponent<T>();
            if (comp == null)
            {
                comp = obj.AddComponent<T>();
            }
            return comp;
        }

        private static void SetupLootDropAnchors(GameObject stageRoot)
        {
            Transform anchorRootTransform = stageRoot.transform.Find("[Loot_Anchors]");
            GameObject anchorRoot = anchorRootTransform != null ? anchorRootTransform.gameObject : new GameObject("[Loot_Anchors]");
            if (anchorRootTransform == null)
            {
                anchorRoot.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(anchorRoot, "Create Loot Anchors Root");
            }

            anchorRoot.transform.localPosition = Vector3.zero;

            CreateAnchorPoint(anchorRoot, "LootPoint_Center", new Vector2(0f, 0f));
            CreateAnchorPoint(anchorRoot, "LootPoint_NorthGrove", new Vector2(0f, 6.5f));
            CreateAnchorPoint(anchorRoot, "LootPoint_SouthRuins", new Vector2(0.5f, -8.2f));
            CreateAnchorPoint(anchorRoot, "LootPoint_EastCamp", new Vector2(12.8f, 0f));
            CreateAnchorPoint(anchorRoot, "LootPoint_WestShore", new Vector2(-15.0f, 0f));
        }

        private static void CreateAnchorPoint(GameObject parent, string name, Vector2 localPos)
        {
            Transform child = parent.transform.Find(name);
            if (child == null)
            {
                GameObject obj = new GameObject(name);
                obj.transform.SetParent(parent.transform);
                obj.transform.localPosition = localPos;
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            }
            else
            {
                child.localPosition = localPos;
            }
        }

        private static void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Undo.RecordObject(cam, "Configure Camera Orthographic Size");
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;

            if (cam.TryGetComponent<CameraFollow2D>(out var follow))
            {
                Undo.RecordObject(follow, "Configure CameraFollow2D Bounds");
                SerializedObject serializedFollow = new SerializedObject(follow);
                serializedFollow.FindProperty("clampToBounds").boolValue = true;
                serializedFollow.FindProperty("minBounds").vector2Value = new Vector2(-12.5f, -7.0f);
                serializedFollow.FindProperty("maxBounds").vector2Value = new Vector2(12.5f, 7.0f);
                serializedFollow.FindProperty("enableLookAhead").boolValue = true;
                serializedFollow.FindProperty("lookAheadDistance").floatValue = 1.2f;
                serializedFollow.ApplyModifiedProperties();
            }
        }

        private static WeaponDataSO[] EnsureWeaponAssetsExist()
        {
            string folder = "Assets/_Game/Data/Weapons";
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Weapons");

            // 1. Pistol (Default)
            string pistolPath = $"{folder}/Weapon_Pistol.asset";
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(pistolPath);
            if (pistol == null)
            {
                pistol = ScriptableObject.CreateInstance<WeaponDataSO>();
                pistol.weaponName = "Pistol";
                pistol.damage = 15f;
                pistol.fireRate = 3.5f;
                pistol.projectileSpeed = 16f;
                pistol.pelletsPerShot = 1;
                pistol.spreadAngle = 0f;
                pistol.maxAmmo = -1; // Unlimited
                AssetDatabase.CreateAsset(pistol, pistolPath);
            }

            // 2. Combat Shotgun
            string shotgunPath = $"{folder}/Weapon_Shotgun.asset";
            var shotgun = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(shotgunPath);
            if (shotgun == null)
            {
                shotgun = ScriptableObject.CreateInstance<WeaponDataSO>();
                shotgun.weaponName = "Combat Shotgun";
                shotgun.damage = 12f;
                shotgun.fireRate = 1.4f;
                shotgun.projectileSpeed = 18f;
                shotgun.pelletsPerShot = 5;
                shotgun.spreadAngle = 24f;
                shotgun.maxAmmo = 24;
                AssetDatabase.CreateAsset(shotgun, shotgunPath);
            }

            // 3. Assault Rifle
            string riflePath = $"{folder}/Weapon_AssaultRifle.asset";
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(riflePath);
            if (rifle == null)
            {
                rifle = ScriptableObject.CreateInstance<WeaponDataSO>();
                rifle.weaponName = "Assault Rifle";
                rifle.damage = 18f;
                rifle.fireRate = 7.5f;
                rifle.projectileSpeed = 20f;
                rifle.pelletsPerShot = 1;
                rifle.spreadAngle = 6f;
                rifle.maxAmmo = 60;
                AssetDatabase.CreateAsset(rifle, riflePath);
            }

            AssetDatabase.SaveAssets();
            return new WeaponDataSO[] { pistol, shotgun, rifle };
        }

        private static GameObject EnsureEnemyShooterPrefabExists()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ShooterPrefabPath);
            if (existing != null) return existing;

            // Load chaser prefab as base template
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChaserPrefabPath);
            if (chaserPrefab == null) return null;

            GameObject instance = Object.Instantiate(chaserPrefab);
            instance.name = "Enemy_Shooter";

            // Swap ChaserEnemy component for RangedShooterEnemy
            var chaserComp = instance.GetComponent<ChaserEnemy>();
            if (chaserComp != null) Object.DestroyImmediate(chaserComp);

            var rangedComp = instance.GetComponent<RangedShooterEnemy>();
            if (rangedComp == null) rangedComp = instance.AddComponent<RangedShooterEnemy>();

            // Assign Soldier Gun Sprite
            Sprite soldierSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SoldierSpritePath);
            if (soldierSprite != null)
            {
                SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = soldierSprite;
                    sr.color = Color.white;
                }
            }

            // Assign Projectile
            GameObject defaultProj = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultProjectilePath);
            if (defaultProj != null)
            {
                SerializedObject so = new SerializedObject(rangedComp);
                so.FindProperty("enemyProjectilePrefab").objectReferenceValue = defaultProj;
                so.ApplyModifiedProperties();
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, ShooterPrefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log($"[ArenaSceneBuilder] Created Ranged Shooter Enemy Prefab at: {ShooterPrefabPath}");
            return savedPrefab;
        }

        private static void SetupWaveManager(GameObject stageRoot, GameObject chaserPrefab, GameObject shooterPrefab)
        {
            Transform waveTransform = stageRoot.transform.Find("[Wave_Manager]");
            GameObject waveObj = waveTransform != null ? waveTransform.gameObject : new GameObject("[Wave_Manager]");
            if (waveTransform == null)
            {
                waveObj.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(waveObj, "Create Wave Manager");
            }

            WaveSpawner spawner = waveObj.GetComponent<WaveSpawner>();
            if (spawner == null) spawner = waveObj.AddComponent<WaveSpawner>();

            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("spawnRadius").floatValue = 14f;
            so.FindProperty("arenaMin").vector2Value = new Vector2(-16.5f, -10.5f);
            so.FindProperty("arenaMax").vector2Value = new Vector2(16.5f, 10.5f);

            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.ClearArray();

            // Wave 1: 6 Chaser Zombies
            wavesProp.InsertArrayElementAtIndex(0);
            SerializedProperty w1 = wavesProp.GetArrayElementAtIndex(0);
            w1.FindPropertyRelative("waveName").stringValue = "Wave 1 - Island Outbreak";
            w1.FindPropertyRelative("totalEnemies").intValue = 6;
            w1.FindPropertyRelative("spawnInterval").floatValue = 1.8f;
            SerializedProperty w1Prefabs = w1.FindPropertyRelative("enemyPrefabs");
            w1Prefabs.ClearArray();
            w1Prefabs.InsertArrayElementAtIndex(0);
            w1Prefabs.GetArrayElementAtIndex(0).objectReferenceValue = chaserPrefab;

            // Wave 2: 10 Enemies (Chasers + Ranged Shooters)
            wavesProp.InsertArrayElementAtIndex(1);
            SerializedProperty w2 = wavesProp.GetArrayElementAtIndex(1);
            w2.FindPropertyRelative("waveName").stringValue = "Wave 2 - Rogue Mercenaries";
            w2.FindPropertyRelative("totalEnemies").intValue = 10;
            w2.FindPropertyRelative("spawnInterval").floatValue = 1.4f;
            SerializedProperty w2Prefabs = w2.FindPropertyRelative("enemyPrefabs");
            w2Prefabs.ClearArray();
            w2Prefabs.InsertArrayElementAtIndex(0);
            w2Prefabs.GetArrayElementAtIndex(0).objectReferenceValue = chaserPrefab;
            if (shooterPrefab != null)
            {
                w2Prefabs.InsertArrayElementAtIndex(1);
                w2Prefabs.GetArrayElementAtIndex(1).objectReferenceValue = shooterPrefab;
            }

            // Wave 3: 16 Enemies (Intense Crossfire)
            wavesProp.InsertArrayElementAtIndex(2);
            SerializedProperty w3 = wavesProp.GetArrayElementAtIndex(2);
            w3.FindPropertyRelative("waveName").stringValue = "Wave 3 - Total Invasion";
            w3.FindPropertyRelative("totalEnemies").intValue = 16;
            w3.FindPropertyRelative("spawnInterval").floatValue = 1.0f;
            SerializedProperty w3Prefabs = w3.FindPropertyRelative("enemyPrefabs");
            w3Prefabs.ClearArray();
            w3Prefabs.InsertArrayElementAtIndex(0);
            w3Prefabs.GetArrayElementAtIndex(0).objectReferenceValue = chaserPrefab;
            if (shooterPrefab != null)
            {
                w3Prefabs.InsertArrayElementAtIndex(1);
                w3Prefabs.GetArrayElementAtIndex(1).objectReferenceValue = shooterPrefab;
            }

            so.ApplyModifiedProperties();
        }

        private static void SetupWeaponLootSpawner(GameObject stageRoot, WeaponDataSO[] weapons)
        {
            Transform spawnerTransform = stageRoot.transform.Find("[Weapon_Loot_Spawner]");
            GameObject spawnerObj = spawnerTransform != null ? spawnerTransform.gameObject : new GameObject("[Weapon_Loot_Spawner]");
            if (spawnerTransform == null)
            {
                spawnerObj.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(spawnerObj, "Create Weapon Loot Spawner");
            }

            WeaponLootSpawner lootSpawner = spawnerObj.GetComponent<WeaponLootSpawner>();
            if (lootSpawner == null) lootSpawner = spawnerObj.AddComponent<WeaponLootSpawner>();

            SerializedObject so = new SerializedObject(lootSpawner);
            SerializedProperty poolProp = so.FindProperty("weaponPool");
            poolProp.ClearArray();
            // Add Shotgun and Assault Rifle to loot pool
            if (weapons.Length > 1)
            {
                poolProp.InsertArrayElementAtIndex(0);
                poolProp.GetArrayElementAtIndex(0).objectReferenceValue = weapons[1]; // Shotgun
            }
            if (weapons.Length > 2)
            {
                poolProp.InsertArrayElementAtIndex(1);
                poolProp.GetArrayElementAtIndex(1).objectReferenceValue = weapons[2]; // Assault Rifle
            }

            so.FindProperty("crateSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(WoodCratePath);
            so.FindProperty("dropInterval").floatValue = 24f;
            so.FindProperty("spawnOnWaveComplete").boolValue = true;
            so.ApplyModifiedProperties();
        }

        private static void ConfigurePlayerDefaultWeapon(WeaponDataSO defaultPistol)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                PlayerShooter shooter = player.GetComponent<PlayerShooter>();
                if (shooter != null)
                {
                    SerializedObject so = new SerializedObject(shooter);
                    so.FindProperty("defaultWeapon").objectReferenceValue = defaultPistol;
                    so.ApplyModifiedProperties();
                }

                SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 150;
                }
            }
        }

        private static void SetupHUD(GameObject stageRoot)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            Transform hudTransform = canvas.transform.Find("[Player_HUD]");
            GameObject hudObj = hudTransform != null ? hudTransform.gameObject : new GameObject("[Player_HUD]");
            if (hudTransform == null)
            {
                hudObj.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(hudObj, "Create Player HUD");
            }

            RectTransform hudRect = hudObj.GetComponent<RectTransform>();
            if (hudRect == null) hudRect = hudObj.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            PlayerHUD playerHUD = hudObj.GetComponent<PlayerHUD>();
            if (playerHUD == null) playerHUD = hudObj.AddComponent<PlayerHUD>();

            // Setup Top-Left Health Bar
            SetupHealthBar(hudObj);

            // Setup Weapon & Ammo Info Badge below Health Bar
            SetupWeaponHUD(hudObj);

            // Setup Top-Center Wave Display
            SetupWaveDisplay(hudObj);

            // Setup Low Health Vignette
            SetupVignette(hudObj);

            // Setup Post-Wave Perk Modal
            SetupPerkModal(canvas, stageRoot);

            // Enlarge Dash Button
            EnlargeDashButton(canvas);
        }

        private static void SetupHealthBar(GameObject hudParent)
        {
            Transform containerTrans = hudParent.transform.Find("Health_Container");
            GameObject container = containerTrans != null ? containerTrans.gameObject : new GameObject("Health_Container");
            if (containerTrans == null) container.transform.SetParent(hudParent.transform, false);

            RectTransform rect = GetOrAddComponent<RectTransform>(container);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -40f);
            rect.sizeDelta = new Vector2(340f, 46f);

            Image frameImg = GetOrAddComponent<Image>(container);
            frameImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFramePath);
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;

            Transform fillTrans = container.transform.Find("Health_Fill");
            GameObject fillObj = fillTrans != null ? fillTrans.gameObject : new GameObject("Health_Fill");
            if (fillTrans == null) fillObj.transform.SetParent(container.transform, false);

            RectTransform fillRect = GetOrAddComponent<RectTransform>(fillObj);
            fillRect.anchorMin = new Vector2(0.02f, 0.12f);
            fillRect.anchorMax = new Vector2(0.98f, 0.88f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImg = GetOrAddComponent<Image>(fillObj);
            fillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFillPath);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = new Color(0.1f, 1f, 0.5f);

            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("healthFillImage").objectReferenceValue = fillImg;
            serializedHUD.ApplyModifiedProperties();
        }

        private static void SetupWeaponHUD(GameObject hudParent)
        {
            Transform containerTrans = hudParent.transform.Find("Weapon_Container");
            GameObject container = containerTrans != null ? containerTrans.gameObject : new GameObject("Weapon_Container");
            if (containerTrans == null) container.transform.SetParent(hudParent.transform, false);

            RectTransform rect = GetOrAddComponent<RectTransform>(container);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -94f);
            rect.sizeDelta = new Vector2(280f, 40f);

            Image bg = GetOrAddComponent<Image>(container);
            bg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

            // Weapon Name Text
            Transform nameTrans = container.transform.Find("Weapon_Name");
            GameObject nameObj = nameTrans != null ? nameTrans.gameObject : new GameObject("Weapon_Name");
            if (nameTrans == null) nameObj.transform.SetParent(container.transform, false);

            RectTransform nameRect = GetOrAddComponent<RectTransform>(nameObj);
            nameRect.anchorMin = new Vector2(0.05f, 0f);
            nameRect.anchorMax = new Vector2(0.65f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            TextMeshProUGUI nameTmp = GetOrAddComponent<TextMeshProUGUI>(nameObj);
            nameTmp.text = "PISTOL";
            nameTmp.fontSize = 20f;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = new Color(0.4f, 0.85f, 1f);

            // Ammo Count Text
            Transform ammoTrans = container.transform.Find("Ammo_Count");
            GameObject ammoObj = ammoTrans != null ? ammoTrans.gameObject : new GameObject("Ammo_Count");
            if (ammoTrans == null) ammoObj.transform.SetParent(container.transform, false);

            RectTransform ammoRect = GetOrAddComponent<RectTransform>(ammoObj);
            ammoRect.anchorMin = new Vector2(0.65f, 0f);
            ammoRect.anchorMax = new Vector2(0.95f, 1f);
            ammoRect.offsetMin = Vector2.zero;
            ammoRect.offsetMax = Vector2.zero;

            TextMeshProUGUI ammoTmp = GetOrAddComponent<TextMeshProUGUI>(ammoObj);
            ammoTmp.text = "∞";
            ammoTmp.fontSize = 22f;
            ammoTmp.alignment = TextAlignmentOptions.MidlineRight;
            ammoTmp.color = new Color(1f, 0.85f, 0.2f);

            // Wire to PlayerHUD
            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("weaponNameText").objectReferenceValue = nameTmp;
            serializedHUD.FindProperty("ammoCountText").objectReferenceValue = ammoTmp;
            serializedHUD.ApplyModifiedProperties();
        }

        private static void SetupWaveDisplay(GameObject hudParent)
        {
            Transform waveTrans = hudParent.transform.Find("Wave_Display");
            GameObject waveObj = waveTrans != null ? waveTrans.gameObject : new GameObject("Wave_Display");
            if (waveTrans == null) waveObj.transform.SetParent(hudParent.transform, false);

            RectTransform rect = GetOrAddComponent<RectTransform>(waveObj);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(300f, 60f);

            TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(waveObj);
            tmp.text = "<color=#00E5FF>WAVE 1</color>";
            tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center;

            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("waveTitleText").objectReferenceValue = tmp;
            serializedHUD.ApplyModifiedProperties();
        }

        private static void SetupVignette(GameObject hudParent)
        {
            Transform vigTrans = hudParent.transform.Find("LowHealth_Vignette");
            GameObject vigObj = vigTrans != null ? vigTrans.gameObject : new GameObject("LowHealth_Vignette");
            if (vigTrans == null) vigObj.transform.SetParent(hudParent.transform, false);

            RectTransform rect = GetOrAddComponent<RectTransform>(vigObj);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = GetOrAddComponent<Image>(vigObj);
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
            img.color = new Color(1f, 0f, 0.1f, 0.8f);
            img.raycastTarget = false;

            CanvasGroup group = GetOrAddComponent<CanvasGroup>(vigObj);
            group.alpha = 0f;

            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("lowHealthVignette").objectReferenceValue = group;
            serializedHUD.ApplyModifiedProperties();
        }

        private static void SetupPerkModal(Canvas canvas, GameObject stageRoot)
        {
            Transform modalTrans = canvas.transform.Find("[Perk_Selection_Modal]");
            GameObject modalObj = modalTrans != null ? modalTrans.gameObject : new GameObject("[Perk_Selection_Modal]");
            if (modalTrans == null)
            {
                modalObj.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(modalObj, "Create Perk Selection Modal");
            }

            RectTransform modalRect = GetOrAddComponent<RectTransform>(modalObj);
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            Image backdrop = GetOrAddComponent<Image>(modalObj);
            backdrop.color = new Color(0.02f, 0.04f, 0.06f, 0.88f);

            // Modal Title
            Transform titleTrans = modalObj.transform.Find("Title_Text");
            GameObject titleObj = titleTrans != null ? titleTrans.gameObject : new GameObject("Title_Text");
            if (titleTrans == null) titleObj.transform.SetParent(modalObj.transform, false);

            RectTransform titleRect = GetOrAddComponent<RectTransform>(titleObj);
            titleRect.anchorMin = new Vector2(0.5f, 0.82f);
            titleRect.anchorMax = new Vector2(0.5f, 0.82f);
            titleRect.sizeDelta = new Vector2(600f, 80f);

            TextMeshProUGUI titleTmp = GetOrAddComponent<TextMeshProUGUI>(titleObj);
            titleTmp.text = "<color=#FFD700>★ CHOOSE AN UPGRADE ★</color>";
            titleTmp.fontSize = 38f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 3 Perk Card Buttons
            Button[] cardButtons = new Button[3];
            TextMeshProUGUI[] cardTitles = new TextMeshProUGUI[3];
            TextMeshProUGUI[] cardDescs = new TextMeshProUGUI[3];
            TextMeshProUGUI[] cardIcons = new TextMeshProUGUI[3];
            Image[] cardBgs = new Image[3];

            float[] xOffsets = new float[] { -380f, 0f, 380f };
            for (int i = 0; i < 3; i++)
            {
                string cardName = $"Perk_Card_{i + 1}";
                Transform cardTrans = modalObj.transform.Find(cardName);
                GameObject cardObj = cardTrans != null ? cardTrans.gameObject : new GameObject(cardName);
                if (cardTrans == null) cardObj.transform.SetParent(modalObj.transform, false);

                RectTransform cardRect = GetOrAddComponent<RectTransform>(cardObj);
                cardRect.anchorMin = new Vector2(0.5f, 0.45f);
                cardRect.anchorMax = new Vector2(0.5f, 0.45f);
                cardRect.sizeDelta = new Vector2(320f, 380f);
                cardRect.anchoredPosition = new Vector2(xOffsets[i], 0f);

                Image cardBg = GetOrAddComponent<Image>(cardObj);
                cardBg.color = new Color(0.12f, 0.15f, 0.20f, 0.95f);
                cardBgs[i] = cardBg;

                Button btn = GetOrAddComponent<Button>(cardObj);
                cardButtons[i] = btn;

                // Icon Text
                Transform iconTrans = cardObj.transform.Find("Icon");
                GameObject iconObj = iconTrans != null ? iconTrans.gameObject : new GameObject("Icon");
                if (iconTrans == null) iconObj.transform.SetParent(cardObj.transform, false);
                RectTransform iconRect = GetOrAddComponent<RectTransform>(iconObj);
                iconRect.anchorMin = new Vector2(0.5f, 0.72f);
                iconRect.anchorMax = new Vector2(0.5f, 0.72f);
                iconRect.sizeDelta = new Vector2(100f, 80f);
                TextMeshProUGUI iconTmp = GetOrAddComponent<TextMeshProUGUI>(iconObj);
                iconTmp.fontSize = 54f;
                iconTmp.alignment = TextAlignmentOptions.Center;
                cardIcons[i] = iconTmp;

                // Title Text
                Transform cardTitleTrans = cardObj.transform.Find("Title");
                GameObject cardTitleObj = cardTitleTrans != null ? cardTitleTrans.gameObject : new GameObject("Title");
                if (cardTitleTrans == null) cardTitleObj.transform.SetParent(cardObj.transform, false);
                RectTransform cardTitleRect = GetOrAddComponent<RectTransform>(cardTitleObj);
                cardTitleRect.anchorMin = new Vector2(0.5f, 0.48f);
                cardTitleRect.anchorMax = new Vector2(0.5f, 0.48f);
                cardTitleRect.sizeDelta = new Vector2(280f, 50f);
                TextMeshProUGUI cTitleTmp = GetOrAddComponent<TextMeshProUGUI>(cardTitleObj);
                cTitleTmp.fontSize = 24f;
                cTitleTmp.alignment = TextAlignmentOptions.Center;
                cardTitles[i] = cTitleTmp;

                // Description Text
                Transform descTrans = cardObj.transform.Find("Desc");
                GameObject descObj = descTrans != null ? descTrans.gameObject : new GameObject("Desc");
                if (descTrans == null) descObj.transform.SetParent(cardObj.transform, false);
                RectTransform descRect = GetOrAddComponent<RectTransform>(descObj);
                descRect.anchorMin = new Vector2(0.5f, 0.22f);
                descRect.anchorMax = new Vector2(0.5f, 0.22f);
                descRect.sizeDelta = new Vector2(260f, 80f);
                TextMeshProUGUI descTmp = GetOrAddComponent<TextMeshProUGUI>(descObj);
                descTmp.fontSize = 17f;
                descTmp.alignment = TextAlignmentOptions.Center;
                descTmp.color = new Color(0.85f, 0.88f, 0.95f);
                cardDescs[i] = descTmp;
            }

            // PerkSelectionManager on stage
            PerkSelectionManager manager = GetOrAddComponent<PerkSelectionManager>(stageRoot);
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("modalRoot").objectReferenceValue = modalObj;

            SerializedProperty buttonsProp = so.FindProperty("cardButtons");
            buttonsProp.ClearArray();
            for (int i = 0; i < 3; i++)
            {
                buttonsProp.InsertArrayElementAtIndex(i);
                buttonsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardButtons[i];
            }

            SerializedProperty titlesProp = so.FindProperty("cardTitleTexts");
            titlesProp.ClearArray();
            for (int i = 0; i < 3; i++)
            {
                titlesProp.InsertArrayElementAtIndex(i);
                titlesProp.GetArrayElementAtIndex(i).objectReferenceValue = cardTitles[i];
            }

            SerializedProperty descsProp = so.FindProperty("cardDescriptionTexts");
            descsProp.ClearArray();
            for (int i = 0; i < 3; i++)
            {
                descsProp.InsertArrayElementAtIndex(i);
                descsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardDescs[i];
            }

            SerializedProperty iconsProp = so.FindProperty("cardIconTexts");
            iconsProp.ClearArray();
            for (int i = 0; i < 3; i++)
            {
                iconsProp.InsertArrayElementAtIndex(i);
                iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardIcons[i];
            }

            SerializedProperty bgsProp = so.FindProperty("cardBackgrounds");
            bgsProp.ClearArray();
            for (int i = 0; i < 3; i++)
            {
                bgsProp.InsertArrayElementAtIndex(i);
                bgsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardBgs[i];
            }

            so.ApplyModifiedProperties();

            // Initially hide modal
            modalObj.SetActive(false);
        }

        private static void EnlargeDashButton(Canvas canvas)
        {
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.ToLower().Contains("dash"))
                {
                    RectTransform rt = btn.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Undo.RecordObject(rt, "Enlarge Dash Button");
                        rt.sizeDelta = new Vector2(130f, 130f);
                        Debug.Log("[ArenaSceneBuilder] Enlarged Dash button to 130x130 for touch comfort.");
                    }
                    break;
                }
            }
        }

        private static void EnsureObstacleTagExists()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            bool exists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(ObstacleTag))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTag.stringValue = ObstacleTag;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[ArenaSceneBuilder] Created tag: '{ObstacleTag}'");
            }
        }
    }
}
