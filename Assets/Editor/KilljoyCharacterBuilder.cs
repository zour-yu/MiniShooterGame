using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using MiniShooterGame.Player;
using MiniShooterGame.Combat;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class KilljoyCharacterBuilder
    {
        private const string SheetPath = "Assets/_Game/Sprites/Characters/Killjoy/SourceSheet.jpg";
        private const string OutputFolder = "Assets/Resources/Characters/Killjoy";

        static KilljoyCharacterBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                ApplyKilljoyToSceneAndPrefab();
            };
        }

        private struct SpriteDef
        {
            public string Name;
            public int MinX;
            public int MaxX;
            public int MinY; // Top-down coordinate (0 = top of image)
            public int MaxY;
            public Vector2 Pivot;

            public SpriteDef(string name, int minX, int maxX, int minY, int maxY, Vector2 pivot)
            {
                Name = name;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                Pivot = pivot;
            }
        }

        [MenuItem("Tools/Valorant Characters/1. Extract Killjoy Sprites", priority = 50)]
        public static void ExtractSpritesMenu()
        {
            ExtractKilljoySprites();
        }

        [MenuItem("Tools/Valorant Characters/2. Apply Killjoy to Player", priority = 51)]
        public static void ApplyKilljoyToPlayerMenu()
        {
            ExtractKilljoySprites();
            ApplyKilljoyToSceneAndPrefab();
        }

        public static void ExtractKilljoySprites()
        {
            if (File.Exists($"{OutputFolder}/Killjoy_HoldGun_Side.png"))
            {
                Debug.Log("[KilljoyBuilder] Sprites already extracted cleanly! Skipping re-extraction.");
                return;
            }

            byte[] fileData = File.ReadAllBytes(SheetPath);
            Texture2D sourceTex = new Texture2D(2, 2);
            sourceTex.LoadImage(fileData);

            int imgW = sourceTex.width;
            int imgH = sourceTex.height;

            // Image coordinates in Texture2D are (0,0) at BOTTOM-LEFT.
            // Our previous measurements were from TOP: Y_top_down.
            // So Unity Y = (imgH - 1) - Y_top_down.
            int ToUnityY(int topDownY) => (imgH - 1) - topDownY;

            // Character pivot: feet level ~ (0.5, 0.08)
            Vector2 charPivot = new Vector2(0.5f, 0.08f);

            var spriteDefs = new List<SpriteDef>
            {
                new SpriteDef("Killjoy_Idle_Front", 33, 102, 104, 232, charPivot),
                new SpriteDef("Killjoy_Walk_Front_1", 137, 206, 104, 235, charPivot),
                new SpriteDef("Killjoy_Walk_Front_2", 226, 295, 104, 235, charPivot),
                new SpriteDef("Killjoy_HoldGun_Front", 320, 415, 104, 232, charPivot),
                new SpriteDef("Killjoy_Side_Idle", 435, 503, 104, 232, charPivot),
                new SpriteDef("Killjoy_Side_Walk", 538, 611, 104, 235, charPivot),
                new SpriteDef("Killjoy_HoldGun_Side", 632, 727, 104, 232, charPivot),
                new SpriteDef("Killjoy_Back_Idle", 744, 819, 104, 232, charPivot),
                new SpriteDef("Killjoy_Back_Walk", 832, 905, 104, 235, charPivot),
                new SpriteDef("Killjoy_HoldGun_Back", 918, 1016, 104, 232, charPivot),
                // Gun Placeholder extracted cleanly from HoldGun_Side (excluding face and arm)
                new SpriteDef("Gun_Pistol_Placeholder", 678, 727, 172, 192, new Vector2(0.55f, 0.4f))
            };

            foreach (var def in spriteDefs)
            {
                int minUnityY = ToUnityY(def.MaxY);
                int maxUnityY = ToUnityY(def.MinY);

                int width = def.MaxX - def.MinX + 1;
                int height = maxUnityY - minUnityY + 1;

                // Add 2px border margin for clean flood fill
                int margin = 2;
                int cropW = width + margin * 2;
                int cropH = height + margin * 2;

                Texture2D cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[cropW * cropH];

                // Initialize with crop pixels
                for (int y = 0; y < cropH; y++)
                {
                    for (int x = 0; x < cropW; x++)
                    {
                        int srcX = Mathf.Clamp(def.MinX - margin + x, 0, imgW - 1);
                        int srcY = Mathf.Clamp(minUnityY - margin + y, 0, imgH - 1);
                        pixels[y * cropW + x] = sourceTex.GetPixel(srcX, srcY);
                    }
                }

                // Flood fill from all 4 boundaries to clear background
                bool[,] visited = new bool[cropW, cropH];
                Queue<Vector2Int> queue = new Queue<Vector2Int>();

                bool IsBg(Color32 c)
                {
                    // Background is pale gray-blue (#DCE1E7, R~221, G~226, B~230)
                    // Grid lines are (#CCD1D7, R~204, G~209, B~215)
                    if (c.r > 180 && c.g > 185 && c.b > 190)
                    {
                        int rg = Mathf.Abs(c.r - c.g);
                        int gb = Mathf.Abs(c.g - c.b);
                        int rb = Mathf.Abs(c.r - c.b);
                        if (rg < 18 && gb < 18 && rb < 18) return true;
                    }
                    if (def.Name == "Gun_Pistol_Placeholder")
                    {
                        if (c.r > 160 && c.g > 110 && c.b > 80 && (c.r - c.b) > 20) return true;
                        if (c.r > 150 && c.g > 140 && c.b < 100) return true;
                    }
                    return false;
                }

                // Seed queue with edge pixels matching background
                for (int x = 0; x < cropW; x++)
                {
                    if (IsBg(pixels[0 * cropW + x])) { queue.Enqueue(new Vector2Int(x, 0)); visited[x, 0] = true; }
                    if (IsBg(pixels[(cropH - 1) * cropW + x])) { queue.Enqueue(new Vector2Int(x, cropH - 1)); visited[x, cropH - 1] = true; }
                }
                for (int y = 0; y < cropH; y++)
                {
                    if (!visited[0, y] && IsBg(pixels[y * cropW + 0])) { queue.Enqueue(new Vector2Int(0, y)); visited[0, y] = true; }
                    if (!visited[cropW - 1, y] && IsBg(pixels[y * cropW + (cropW - 1)])) { queue.Enqueue(new Vector2Int(cropW - 1, y)); visited[cropW - 1, y] = true; }
                }

                Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

                while (queue.Count > 0)
                {
                    Vector2Int pt = queue.Dequeue();
                    pixels[pt.y * cropW + pt.x] = new Color32(0, 0, 0, 0);

                    foreach (var d in dirs)
                    {
                        int nx = pt.x + d.x;
                        int ny = pt.y + d.y;
                        if (nx >= 0 && nx < cropW && ny >= 0 && ny < cropH)
                        {
                            if (!visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                if (IsBg(pixels[ny * cropW + nx]))
                                {
                                    queue.Enqueue(new Vector2Int(nx, ny));
                                }
                            }
                        }
                    }
                }

                cropped.SetPixels32(pixels);
                cropped.Apply();

                byte[] pngBytes = cropped.EncodeToPNG();
                string outPath = $"{OutputFolder}/{def.Name}.png";
                File.WriteAllBytes(outPath, pngBytes);
                Object.DestroyImmediate(cropped);

                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 64f; // ~2 world units tall
                    importer.filterMode = FilterMode.Point; // Crisp pixel art
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;

                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = def.Pivot;
                    importer.SetTextureSettings(settings);

                    importer.SaveAndReimport();
                }
            }

            Object.DestroyImmediate(sourceTex);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KilljoyBuilder] All 10 Killjoy sprites and Gun placeholder extracted cleanly with transparent backgrounds!");
        }

        public static void ApplyKilljoyToSceneAndPrefab()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[KilljoyBuilder] No Player found in the scene to update.");
                return;
            }

            Undo.RecordObject(player, "Apply Killjoy Visuals");

            // Update main SpriteRenderer
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr == null) sr = player.AddComponent<SpriteRenderer>();

            Sprite idleFront = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Idle_Front.png");
            if (idleFront != null)
            {
                sr.sprite = idleFront;
            }
            sr.sortingOrder = 150;
            sr.color = Color.white;

            // Configure CapsuleCollider2D for proper chibi proportions
            var col = player.GetComponent<CapsuleCollider2D>();
            if (col == null)
            {
                var circle = player.GetComponent<CircleCollider2D>();
                if (circle != null) Object.DestroyImmediate(circle);
                col = player.AddComponent<CapsuleCollider2D>();
            }
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.9f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);

            // Add or configure CharacterVisualController
            CharacterVisualController visual = player.GetComponent<CharacterVisualController>();
            if (visual == null) visual = player.AddComponent<CharacterVisualController>();

            // Setup Gun Mount child
            Transform gunMount = player.transform.Find("Weapon_Mount");
            if (gunMount == null)
            {
                GameObject gmObj = new GameObject("Weapon_Mount");
                gmObj.transform.SetParent(player.transform, false);
                gunMount = gmObj.transform;
            }
            gunMount.localPosition = new Vector3(0.35f, 0.8f, 0f);

            // Remove legacy SpriteRenderer directly on gunMount pivot if present
            var oldMountSr = gunMount.GetComponent<SpriteRenderer>();
            if (oldMountSr != null) Object.DestroyImmediate(oldMountSr);

            // Setup dedicated Weapon_Visual child for gun sprite
            Transform gunVisual = gunMount.Find("Weapon_Visual");
            if (gunVisual == null)
            {
                GameObject gvObj = new GameObject("Weapon_Visual");
                gvObj.transform.SetParent(gunMount, false);
                gunVisual = gvObj.transform;
            }
            SpriteRenderer gunSr = gunVisual.GetComponent<SpriteRenderer>();
            if (gunSr == null) gunSr = gunVisual.gameObject.AddComponent<SpriteRenderer>();

            Sprite placeholderGun = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Gun_Pistol_Placeholder.png");
            if (placeholderGun != null)
            {
                gunSr.sprite = placeholderGun;
            }
            gunSr.sortingOrder = 151; // in front of body by default
            gunSr.color = Color.white;

            // Setup Main Hand child on gun mount
            Transform mainHand = gunMount.Find("Main_Hand");
            if (mainHand == null)
            {
                GameObject mhObj = new GameObject("Main_Hand");
                mhObj.transform.SetParent(gunMount, false);
                mainHand = mhObj.transform;
            }
            SpriteRenderer mhSr = mainHand.GetComponent<SpriteRenderer>();
            if (mhSr == null) mhSr = mainHand.gameObject.AddComponent<SpriteRenderer>();
            Sprite mainHandSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Hand_Main_WithCuff.png");
            if (mainHandSprite == null) mainHandSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Hand_Main.png");
            if (mainHandSprite != null) mhSr.sprite = mainHandSprite;
            mhSr.sortingOrder = 152;

            // Setup Support Hand child on gun mount (for two-handed weapons)
            Transform supportHand = gunMount.Find("Support_Hand");
            if (supportHand == null)
            {
                GameObject shObj = new GameObject("Support_Hand");
                shObj.transform.SetParent(gunMount, false);
                supportHand = shObj.transform;
            }
            SpriteRenderer shSr = supportHand.GetComponent<SpriteRenderer>();
            if (shSr == null) shSr = supportHand.gameObject.AddComponent<SpriteRenderer>();
            Sprite supportHandSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Hand_Support.png");
            if (supportHandSprite != null) shSr.sprite = supportHandSprite;
            shSr.sortingOrder = 150;

            // Setup FirePoint at gun muzzle
            Transform firePoint = gunMount.Find("FirePoint");
            if (firePoint == null)
            {
                GameObject fpObj = new GameObject("FirePoint");
                fpObj.transform.SetParent(gunMount, false);
                firePoint = fpObj.transform;
            }
            firePoint.localPosition = new Vector3(0.6f, 0.05f, 0f);

            // Link firePoint to PlayerShooter
            PlayerShooter shooter = player.GetComponent<PlayerShooter>();
            if (shooter != null)
            {
                SerializedObject soShooter = new SerializedObject(shooter);
                var fpProp = soShooter.FindProperty("firePoint");
                if (fpProp != null) fpProp.objectReferenceValue = firePoint;
                soShooter.ApplyModifiedProperties();
            }

            // Wire CharacterVisualController references
            SerializedObject soVisual = new SerializedObject(visual);
            soVisual.FindProperty("idleFront").objectReferenceValue = idleFront;
            soVisual.FindProperty("walkFront1").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Walk_Front_1.png");
            soVisual.FindProperty("walkFront2").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Walk_Front_2.png");
            soVisual.FindProperty("idleSide").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Side_Idle.png");
            soVisual.FindProperty("walkSide").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Side_Walk.png");
            soVisual.FindProperty("idleBack").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Back_Idle.png");
            soVisual.FindProperty("walkBack").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_Back_Walk.png");
            soVisual.FindProperty("holdGunFront").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_HoldGun_Front.png");
            soVisual.FindProperty("holdGunSide").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_HoldGun_Side.png");
            soVisual.FindProperty("holdGunBack").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{OutputFolder}/Killjoy_HoldGun_Back.png");
            soVisual.FindProperty("gunMount").objectReferenceValue = gunMount;
            soVisual.FindProperty("gunRenderer").objectReferenceValue = gunSr;
            soVisual.FindProperty("defaultPlaceholderGun").objectReferenceValue = placeholderGun;
            soVisual.FindProperty("mainHandMount").objectReferenceValue = mainHand;
            soVisual.FindProperty("mainHandRenderer").objectReferenceValue = mhSr;
            soVisual.FindProperty("mainHandSprite").objectReferenceValue = mainHandSprite;
            soVisual.FindProperty("supportHandMount").objectReferenceValue = supportHand;
            soVisual.FindProperty("supportHandRenderer").objectReferenceValue = shSr;
            soVisual.FindProperty("supportHandSprite").objectReferenceValue = supportHandSprite;
            soVisual.ApplyModifiedProperties();


            // Also update PlayerMovement: spriteAngleOffset = 0 since Killjoy faces DOWN / SIDE normally, not Kenny top-down angle
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                SerializedObject soMove = new SerializedObject(movement);
                var rotProp = soMove.FindProperty("rotateTowardsMovement");
                if (rotProp != null) rotProp.boolValue = false; // We do not rotate the entire Rigidbody transform anymore; CharacterVisualController handles 4-way sprites!
                soMove.ApplyModifiedProperties();
            }

            player.transform.rotation = Quaternion.identity;

            EditorUtility.SetDirty(player);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);

            string prefabPath = "Assets/_Game/Prefabs/Characters/Player.prefab";
            if (File.Exists(prefabPath))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(player, prefabPath, InteractionMode.AutomatedAction);
                Debug.Log("[KilljoyBuilder] Synchronized scene Player changes to Player.prefab!");
            }

            Debug.Log("[KilljoyBuilder] Applied Killjoy character, animations, and modular equippable gun mount to Player!");
        }
    }
}
