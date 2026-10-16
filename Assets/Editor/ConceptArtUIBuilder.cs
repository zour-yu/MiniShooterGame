using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MiniShooterGame.UI;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class ConceptArtUIBuilder
    {
        private const string SpritesDir = "Assets/_Game/Sprites/UI";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const string CardCyanPath = SpritesDir + "/Card_Frame_Cyan.png";
        private const string CardPurplePath = SpritesDir + "/Card_Frame_Purple.png";
        private const string CardYellowPath = SpritesDir + "/Card_Frame_Yellow.png";

        private const string IconShoePath = SpritesDir + "/Icon_Swift_Stride.png";
        private const string IconRunnerPath = SpritesDir + "/Icon_Flash_Step.png";
        private const string IconBulletPath = SpritesDir + "/Icon_Rapid_Trigger.png";

        private const string HealthFrameCyanPath = SpritesDir + "/Health_Frame_Cyan.png";
        private const string HealthFillLimePath = SpritesDir + "/Health_Fill_Lime.png";
        private const string WeaponFrameCyanPath = SpritesDir + "/Weapon_Frame_Cyan.png";
        private const string PadlockCyanPath = SpritesDir + "/Padlock_Cyan.png";

        private const string AutoRunFlag = "MiniShooterGame_ConceptArt_Applied_v2";

        static ConceptArtUIBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(AutoRunFlag, false))
                {
                    SessionState.SetBool(AutoRunFlag, true);
                    ApplyConceptArtUI(false);
                }
            };
        }

        [MenuItem("Tools/Match Concept Art Exactly", priority = 1)]
        public static void BuildConceptArtUIMenu()
        {
            ApplyConceptArtUI(true);
        }

        public static void ApplyConceptArtUI(bool showDialog = false)
        {
            EnsureSpritesDirectory();

            // 1. Generate All High-Res Procedural Concept Art Textures
            GenerateAllTextures();
            AssetDatabase.Refresh();

            // 2. Set Sprite Import Settings
            ConfigureSpriteImport(CardCyanPath);
            ConfigureSpriteImport(CardPurplePath);
            ConfigureSpriteImport(CardYellowPath);
            ConfigureSpriteImport(IconShoePath);
            ConfigureSpriteImport(IconRunnerPath);
            ConfigureSpriteImport(IconBulletPath);
            ConfigureSpriteImport(HealthFrameCyanPath);
            ConfigureSpriteImport(HealthFillLimePath);
            ConfigureSpriteImport(WeaponFrameCyanPath);
            ConfigureSpriteImport(PadlockCyanPath);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ConceptArtUIBuilder] No Canvas found in scene!");
                return;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            // 3. Style Top-Left Health Bar to match Concept Art
            StyleHealthBar(canvas, uiLayer, font);

            // 4. Style Top-Left Weapon Container to match Concept Art
            StyleWeaponContainer(canvas, uiLayer, font);

            // 5. Style Top-Center Wave Header to match Concept Art
            StyleWaveHeader(canvas, uiLayer, font);

            // 6. Style Post-Wave Upgrade Modal to match Concept Art (3 Cyber Cards, glowing brackets, icons)
            StylePerkModal(canvas, uiLayer, font);

            // 7. Add Cyan Padlock to the center crate if present
            StyleCenterCratePadlock();

            // 8. Save Scene
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            // Capture screenshot for AI inspection
            GameViewScreenshot.Capture(false);

            Debug.Log("[ConceptArtUIBuilder] UI successfully transformed to match the Concept Art!");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Concept Art Matched!",
                    "UI successfully updated to match the concept art!\n\n" +
                    "• Glowing Cyan Health Bar with Lime-Green 70/100 fill\n" +
                    "• Sleek Cyan-Border Weapon Card with PISTOL and '∞'\n" +
                    "• Top-Center Wave 1 + ENEMIES: 3 + Weapon Drop subtitle\n" +
                    "• Glowing Yellow [ CHOOSE AN UPGRADE ] cyber brackets\n" +
                    "• 3 Sci-Fi Upgrade Cards with Cyan, Purple, and Gold notched neon frames\n" +
                    "• Custom Glowing Icons: Swift Stride (Shoe), Flash Step (Runner), Rapid Trigger (Bullet)\n" +
                    "• Holographic Cyan Padlock on the Center Crate",
                    "OK"
                );
            }
        }

        private static void EnsureSpritesDirectory()
        {
            if (!Directory.Exists(SpritesDir))
            {
                Directory.CreateDirectory(SpritesDir);
            }
        }

        private static void ConfigureSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        #region Procedural Texture Generators

        private static void GenerateAllTextures()
        {
            if (File.Exists(CardCyanPath) && File.Exists(IconShoePath)) return;

            // Cards: 340 x 460
            GenerateCardTexture(CardCyanPath, new Color(0f, 0.94f, 1f), new Color(0.04f, 0.07f, 0.12f));
            GenerateCardTexture(CardPurplePath, new Color(0.72f, 0.22f, 1f), new Color(0.09f, 0.05f, 0.14f));
            GenerateCardTexture(CardYellowPath, new Color(1f, 0.82f, 0.08f), new Color(0.12f, 0.09f, 0.04f));

            // Icons: 140 x 140
            GenerateShoeIcon(IconShoePath, new Color(0f, 0.94f, 1f));
            GenerateRunnerIcon(IconRunnerPath, new Color(0.75f, 0.28f, 1f));
            GenerateBulletIcon(IconBulletPath, new Color(1f, 0.84f, 0.12f));

            // Health & Weapon:
            GenerateCyberFrame(HealthFrameCyanPath, 380, 52, new Color(0f, 0.94f, 1f), new Color(0.04f, 0.07f, 0.11f, 0.92f));
            GenerateHealthFill(HealthFillLimePath, 360, 42);
            GenerateCyberFrame(WeaponFrameCyanPath, 300, 44, new Color(0f, 0.94f, 1f), new Color(0.04f, 0.07f, 0.11f, 0.92f));

            // Padlock: 64 x 64
            GeneratePadlock(PadlockCyanPath, new Color(0f, 0.94f, 1f));
        }

        private static void GenerateCardTexture(string path, Color neonColor, Color bgBase)
        {
            int w = 340;
            int h = 460;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float rx = w * 0.5f - 8f;
            float ry = h * 0.5f - 8f;

            for (int y = 0; y < h; y++)
            {
                float dy = Mathf.Abs(y - cy);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - cx);

                    // Cyber Notch: cut corners at 4 corners
                    float cornerCut = (dx - (rx - 28f)) + (dy - (ry - 28f));
                    // Top / bottom tab notch
                    bool topBottomNotch = (dy > ry - 14f) && (dx > 32f && dx < 62f);

                    float distToBorder = Mathf.Max(dx - rx, dy - ry);
                    if (cornerCut > 0f) distToBorder = Mathf.Max(distToBorder, cornerCut * 0.7f);
                    if (topBottomNotch) distToBorder = Mathf.Max(distToBorder, 4f);

                    Color pixel = Color.clear;

                    if (distToBorder <= 0f)
                    {
                        // Inside card body
                        // Subtle scanline pattern
                        float scanline = (y % 4 == 0) ? 0.93f : 1.0f;
                        // Vertical subtle gradient
                        float vGrad = Mathf.Lerp(0.85f, 1.1f, (float)y / h);
                        Color fill = bgBase * vGrad * scanline;
                        fill.a = 0.96f;

                        // Inner neon border line (at dist -7f)
                        float innerLine = Mathf.Abs(distToBorder - (-7f));
                        if (innerLine < 1.4f)
                        {
                            float t = 1f - (innerLine / 1.4f);
                            fill = Color.Lerp(fill, neonColor, t * 0.85f);
                        }

                        // Outer border line (at dist 0f)
                        if (distToBorder > -3.5f)
                        {
                            float t = Mathf.InverseLerp(-3.5f, 0f, distToBorder);
                            fill = Color.Lerp(fill, Color.Lerp(neonColor, Color.white, 0.45f), t);
                        }

                        pixel = fill;
                    }
                    else if (distToBorder < 6.5f)
                    {
                        // Outer bloom neon glow
                        float glow = Mathf.Exp(-distToBorder * 0.65f);
                        pixel = neonColor * glow;
                        pixel.a = glow * 0.9f;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateCyberFrame(string path, int w, int h, Color neonColor, Color bgBase)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float rx = w * 0.5f - 4f;
            float ry = h * 0.5f - 4f;

            for (int y = 0; y < h; y++)
            {
                float dy = Mathf.Abs(y - cy);
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float cornerCut = (dx - (rx - 10f)) + (dy - (ry - 10f));
                    float dist = Mathf.Max(dx - rx, dy - ry);
                    if (cornerCut > 0f) dist = Mathf.Max(dist, cornerCut * 0.7f);

                    Color pixel = Color.clear;
                    if (dist <= 0f)
                    {
                        Color fill = bgBase;
                        if (dist > -2.5f)
                        {
                            float t = Mathf.InverseLerp(-2.5f, 0f, dist);
                            fill = Color.Lerp(fill, Color.Lerp(neonColor, Color.white, 0.5f), t);
                        }
                        pixel = fill;
                    }
                    else if (dist < 4.5f)
                    {
                        float glow = Mathf.Exp(-dist * 0.8f);
                        pixel = neonColor * glow;
                        pixel.a = glow * 0.85f;
                    }
                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateHealthFill(string path, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color limeStart = new Color(0.42f, 0.95f, 0.28f, 1f); // Vibrant lime
            Color limeEnd = new Color(0.24f, 0.85f, 0.35f, 1f);

            for (int y = 0; y < h; y++)
            {
                float yGrad = Mathf.Lerp(0.85f, 1.15f, (float)y / h);
                for (int x = 0; x < w; x++)
                {
                    float xRatio = (float)x / w;
                    Color c = Color.Lerp(limeStart, limeEnd, xRatio) * yGrad;
                    c.a = 1f;

                    // Rounded right cap
                    if (x > w - 12)
                    {
                        float dist = (x - (w - 12));
                        if (dist > 10f) c.a = 0f;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateShoeIcon(string path, Color neon)
        {
            int s = 140;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            DrawPlateBackground(tex, s, neon, new Color(0.04f, 0.08f, 0.15f, 0.95f));

            // Draw Athletic Sneaker & Speed Lines
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color baseCol = tex.GetPixel(x, y);

                    // Sole base (x: 48..105, y: 44..52)
                    bool isSole = (x >= 48 && x <= 104 && y >= 44 && y <= 52);
                    // Shoe Upper
                    bool isUpper = false;
                    if (x >= 54 && x <= 102 && y >= 52 && y <= 66) isUpper = true;
                    // Collar / ankle lift
                    if (x >= 50 && x <= 72 && y >= 64 && y <= 78) isUpper = true;
                    // Toe slope
                    if (x > 88 && y > 58 + (x - 88) * 0.9f) isUpper = false;

                    // Speed Streaks behind heel (x: 20..46)
                    bool isStreak = false;
                    if (x >= 22 && x <= 44 && (y == 48 || y == 49 || y == 58 || y == 59 || y == 68 || y == 69)) isStreak = true;

                    if (isSole || isUpper || isStreak)
                    {
                        Color glow = Color.Lerp(neon, Color.white, 0.55f);
                        baseCol = Color.Lerp(baseCol, glow, 0.95f);
                        tex.SetPixel(x, y, baseCol);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateRunnerIcon(string path, Color neon)
        {
            int s = 140;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            DrawPlateBackground(tex, s, neon, new Color(0.10f, 0.04f, 0.16f, 0.95f));

            // Dynamic Sprinting Runner Silhouette + Motion Lines
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color baseCol = tex.GetPixel(x, y);

                    // Head: circle at (78, 86), r = 9
                    float headDist = Vector2.Distance(new Vector2(x, y), new Vector2(78, 86));
                    bool isHead = headDist <= 9f;

                    // Torso (tilted 45 deg)
                    bool isTorso = (x >= 60 && x <= 76 && y >= 60 && y <= 78 && Mathf.Abs((x - 68) - (y - 68) * 0.6f) < 8f);
                    // Forward leg & foot
                    bool isFrontLeg = (x >= 70 && x <= 88 && y >= 40 && y <= 62 && Mathf.Abs((x - 70) - (62 - y) * 0.7f) < 7f);
                    bool isFrontFoot = (x >= 84 && x <= 98 && y >= 36 && y <= 42);
                    // Back leg & foot
                    bool isBackLeg = (x >= 42 && x <= 62 && y >= 50 && y <= 68 && Mathf.Abs((62 - x) - (y - 50) * 0.8f) < 7f);
                    bool isBackFoot = (x >= 32 && x <= 44 && y >= 66 && y <= 74);
                    // Arms
                    bool isArms = (x >= 54 && x <= 88 && y >= 62 && y <= 74 && Mathf.Abs((x - 70) + (y - 68) * 0.5f) < 6f);

                    // Speed motion lines behind runner
                    bool isStreak = (x >= 20 && x <= 48 && (y == 48 || y == 49 || y == 58 || y == 59 || y == 68 || y == 69 || y == 78 || y == 79));

                    if (isHead || isTorso || isFrontLeg || isFrontFoot || isBackLeg || isBackFoot || isArms || isStreak)
                    {
                        Color glow = Color.Lerp(neon, Color.white, 0.5f);
                        baseCol = Color.Lerp(baseCol, glow, 0.95f);
                        tex.SetPixel(x, y, baseCol);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void GenerateBulletIcon(string path, Color neon)
        {
            int s = 140;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            DrawPlateBackground(tex, s, neon, new Color(0.14f, 0.10f, 0.04f, 0.95f));

            // Bullet flying to the right with speed streaks
            for (int y = 0; y < s; y++)
            {
                float dy = Mathf.Abs(y - 70);
                for (int x = 0; x < s; x++)
                {
                    Color baseCol = tex.GetPixel(x, y);

                    // Bullet cartridge body: (x: 65..92, dy <= 11)
                    bool isBody = (x >= 65 && x <= 92 && dy <= 10f);
                    // Rounded bullet tip: (x: 92..108, dy <= curved tip)
                    bool isTip = false;
                    if (x > 92 && x <= 108)
                    {
                        float tipR = (108 - x) * 0.65f;
                        if (dy <= tipR) isTip = true;
                    }

                    // Motion trails trailing left: (x: 22..58)
                    bool isStreak = false;
                    if (x >= 24 && x <= 58 && (y == 62 || y == 63 || y == 70 || y == 71 || y == 78 || y == 79)) isStreak = true;
                    if (x >= 34 && x <= 54 && (y == 55 || y == 56 || y == 85 || y == 86)) isStreak = true;

                    if (isBody || isTip || isStreak)
                    {
                        Color glow = Color.Lerp(neon, Color.white, 0.5f);
                        baseCol = Color.Lerp(baseCol, glow, 0.95f);
                        tex.SetPixel(x, y, baseCol);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void DrawPlateBackground(Texture2D tex, int s, Color neon, Color plateBg)
        {
            float c = s * 0.5f;
            float r = s * 0.5f - 14f;

            for (int y = 0; y < s; y++)
            {
                float dy = Mathf.Abs(y - c);
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(x - c);
                    float dist = Mathf.Max(dx - r, dy - r);
                    // Rounded corners
                    if (dx > r - 12f && dy > r - 12f)
                    {
                        dist = Vector2.Distance(new Vector2(dx, dy), new Vector2(r - 12f, r - 12f)) - 12f;
                    }

                    Color pixel = Color.clear;
                    if (dist <= 0f)
                    {
                        Color fill = plateBg;
                        // Border line at dist 0
                        if (dist > -2.5f)
                        {
                            float t = Mathf.InverseLerp(-2.5f, 0f, dist);
                            fill = Color.Lerp(fill, Color.Lerp(neon, Color.white, 0.4f), t);
                        }
                        pixel = fill;
                    }
                    else if (dist < 4f)
                    {
                        float glow = Mathf.Exp(-dist * 0.9f);
                        pixel = neon * glow;
                        pixel.a = glow * 0.8f;
                    }
                    tex.SetPixel(x, y, pixel);
                }
            }
        }

        private static void GeneratePadlock(string path, Color neon)
        {
            int s = 64;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    Color pixel = Color.clear;

                    // Shackle loop: arch from x: 22..42, y: 34..52, thickness = 3
                    float distToArchCenter = Vector2.Distance(new Vector2(x, y), new Vector2(32, 38));
                    bool isShackle = (distToArchCenter >= 8f && distToArchCenter <= 12f && y >= 38) ||
                                     ((x >= 20 && x <= 24 || x >= 40 && x <= 44) && y >= 30 && y <= 38);

                    // Body: rounded rect x: 18..46, y: 14..34
                    bool isBody = (x >= 18 && x <= 46 && y >= 14 && y <= 34);

                    // Keyhole: x: 30..34, y: 20..26
                    bool isKeyhole = (Mathf.Abs(x - 32) <= 1 && y >= 20 && y <= 26) ||
                                     (Vector2.Distance(new Vector2(x, y), new Vector2(32, 26)) <= 2.5f);

                    if (isShackle || (isBody && !isKeyhole))
                    {
                        pixel = Color.Lerp(neon, Color.white, 0.4f);
                        pixel.a = 0.95f;
                    }
                    else if (isBody && isKeyhole)
                    {
                        pixel = new Color(0.02f, 0.05f, 0.1f, 0.95f);
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        #endregion

        #region UI Styling Methods

        private static void StyleHealthBar(Canvas canvas, int uiLayer, TMP_FontAsset font)
        {
            Transform container = canvas.transform.Find("[Player_HUD]/Health_Container");
            if (container == null) return;

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(70f, -45f);
            rect.sizeDelta = new Vector2(360f, 48f);

            Image frameImg = container.GetComponent<Image>();
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFrameCyanPath);
            if (frameSprite != null) frameImg.sprite = frameSprite;
            frameImg.color = Color.white;

            Transform fillTrans = container.Find("Health_Fill");
            if (fillTrans != null)
            {
                Image fillImg = fillTrans.GetComponent<Image>();
                Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFillLimePath);
                if (fillSprite != null) fillImg.sprite = fillSprite;
                fillImg.color = Color.white;
            }

            Transform textTrans = container.Find("Health_Text");
            if (textTrans != null)
            {
                TextMeshProUGUI tmp = textTrans.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = "70 / 100";
                tmp.fontSize = 20f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
        }

        private static void StyleWeaponContainer(Canvas canvas, int uiLayer, TMP_FontAsset font)
        {
            Transform container = canvas.transform.Find("[Player_HUD]/Weapon_Container");
            if (container == null) return;

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(70f, -100f);
            rect.sizeDelta = new Vector2(300f, 44f);

            Image bg = container.GetComponent<Image>();
            Sprite weaponFrame = AssetDatabase.LoadAssetAtPath<Sprite>(WeaponFrameCyanPath);
            if (weaponFrame != null) bg.sprite = weaponFrame;
            bg.color = Color.white;

            Transform nameTrans = container.Find("Weapon_Name");
            if (nameTrans != null)
            {
                TextMeshProUGUI tmp = nameTrans.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = "PISTOL";
                tmp.fontSize = 20f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white;
            }

            Transform ammoTrans = container.Find("Ammo_Count");
            if (ammoTrans != null)
            {
                TextMeshProUGUI tmp = ammoTrans.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = "<color=#00F0FF>∞</color>";
                tmp.fontSize = 24f;
                tmp.fontStyle = FontStyles.Bold;
            }
        }

        private static void StyleWaveHeader(Canvas canvas, int uiLayer, TMP_FontAsset font)
        {
            Transform container = canvas.transform.Find("[Player_HUD]/Wave_Header_Container");
            if (container == null) return;

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, -42f);
            rect.sizeDelta = new Vector2(360f, 74f);

            Transform waveTrans = container.Find("Wave_Title_Text");
            if (waveTrans != null)
            {
                TextMeshProUGUI tmp = waveTrans.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = "<color=#00F0FF>WAVE 1</color>";
                tmp.fontSize = 22f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            Transform enemyTrans = container.Find("Enemies_Remaining_Text");
            if (enemyTrans != null)
            {
                TextMeshProUGUI tmp = enemyTrans.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = "ENEMIES: <color=#FF3366>3</color>";
                tmp.fontSize = 17f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            // Subtitle: WEAPON DROP: COMBAT SHOTGUN
            Transform dropTrans = container.Find("Weapon_Drop_Subtitle");
            GameObject dropObj = dropTrans != null ? dropTrans.gameObject : new GameObject("Weapon_Drop_Subtitle");
            if (dropTrans == null) dropObj.transform.SetParent(container, false);
            dropObj.layer = uiLayer;

            RectTransform dropRect = GetOrAddComponent<RectTransform>(dropObj);
            dropRect.anchorMin = new Vector2(0f, -0.4f);
            dropRect.anchorMax = new Vector2(1f, 0f);
            dropRect.offsetMin = Vector2.zero;
            dropRect.offsetMax = Vector2.zero;

            TextMeshProUGUI dropTmp = GetOrAddComponent<TextMeshProUGUI>(dropObj);
            if (font != null) dropTmp.font = font;
            dropTmp.text = "<color=#38BDF8>WEAPON DROP: COMBAT SHOTGUN</color>";
            dropTmp.fontSize = 14f;
            dropTmp.fontStyle = FontStyles.Bold;
            dropTmp.alignment = TextAlignmentOptions.Center;
            dropTmp.raycastTarget = false;
        }

        private static void StylePerkModal(Canvas canvas, int uiLayer, TMP_FontAsset font)
        {
            Transform modalTrans = canvas.transform.Find("[Perk_Selection_Modal]");
            if (modalTrans == null) return;
            GameObject modalObj = modalTrans.gameObject;
            modalObj.SetActive(true); // make visible for editing & matching concept art

            // 1. Style Header Title: [ CHOOSE AN UPGRADE ] in glowing yellow
            Transform titleTrans = modalObj.transform.Find("Title_Text");
            if (titleTrans != null)
            {
                RectTransform tRect = titleTrans.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.5f, 0.85f);
                tRect.anchorMax = new Vector2(0.5f, 0.85f);
                tRect.anchoredPosition = new Vector2(0f, 0f);
                tRect.sizeDelta = new Vector2(700f, 60f);

                TextMeshProUGUI tmp = titleTrans.GetComponent<TextMeshProUGUI>();
                if (font != null) tmp.font = font;
                tmp.text = "<color=#FFD700><b>[ CHOOSE AN UPGRADE ]</b></color>";
                tmp.fontSize = 38f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            // 2. Configure 3 Perk Cards:
            // Card 1: Cyan (Swift Stride)
            // Card 2: Purple (Flash Step)
            // Card 3: Yellow (Rapid Trigger)
            string[] cardNames = { "Perk_Card_1", "Perk_Card_2", "Perk_Card_3" };
            string[] framePaths = { CardCyanPath, CardPurplePath, CardYellowPath };
            string[] iconPaths = { IconShoePath, IconRunnerPath, IconBulletPath };
            string[] titles = { "Swift Stride", "Flash Step", "Rapid Trigger" };
            string[] descs =
            {
                "+20% Movement Speed across all terrain.",
                "-0.4s Dash Cooldown for nimble evasions.",
                "+25% Faster Attack Rate permanently."
            };
            float[] xPos = { -360f, 0f, 360f };

            for (int i = 0; i < 3; i++)
            {
                Transform cardTrans = modalObj.transform.Find(cardNames[i]);
                if (cardTrans == null) continue;
                GameObject cardObj = cardTrans.gameObject;

                RectTransform cRect = cardObj.GetComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0.5f, 0.44f);
                cRect.anchorMax = new Vector2(0.5f, 0.44f);
                cRect.pivot = new Vector2(0.5f, 0.5f);
                cRect.anchoredPosition = new Vector2(xPos[i], 0f);
                cRect.sizeDelta = new Vector2(310f, 440f);

                // Card Frame Image
                Image cardBg = cardObj.GetComponent<Image>();
                Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePaths[i]);
                if (frameSprite != null) cardBg.sprite = frameSprite;
                cardBg.color = Color.white;

                // Icon Object: replace text icon with actual graphic sprite
                Transform iconTrans = cardObj.transform.Find("Icon");
                if (iconTrans != null)
                {
                    RectTransform iRect = iconTrans.GetComponent<RectTransform>();
                    iRect.anchorMin = new Vector2(0.5f, 0.72f);
                    iRect.anchorMax = new Vector2(0.5f, 0.72f);
                    iRect.pivot = new Vector2(0.5f, 0.5f);
                    iRect.sizeDelta = new Vector2(110f, 110f);

                    // Add or configure Image component for icon
                    Image iconImg = GetOrAddComponent<Image>(iconTrans.gameObject);
                    Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
                    if (iconSprite != null) iconImg.sprite = iconSprite;
                    iconImg.color = Color.white;
                    iconImg.raycastTarget = false;

                    // Disable text on icon if present to avoid boxes
                    TextMeshProUGUI iconTmp = iconTrans.GetComponent<TextMeshProUGUI>();
                    if (iconTmp != null) iconTmp.text = "";
                }

                // Title Text
                Transform titleT = cardObj.transform.Find("Title");
                if (titleT != null)
                {
                    RectTransform tRect = titleT.GetComponent<RectTransform>();
                    tRect.anchorMin = new Vector2(0.5f, 0.42f);
                    tRect.anchorMax = new Vector2(0.5f, 0.42f);
                    tRect.sizeDelta = new Vector2(280f, 40f);

                    TextMeshProUGUI tmp = titleT.GetComponent<TextMeshProUGUI>();
                    if (font != null) tmp.font = font;
                    tmp.text = titles[i];
                    tmp.fontSize = 24f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                }

                // Description Text
                Transform descT = cardObj.transform.Find("Desc");
                if (descT != null)
                {
                    RectTransform dRect = descT.GetComponent<RectTransform>();
                    dRect.anchorMin = new Vector2(0.5f, 0.22f);
                    dRect.anchorMax = new Vector2(0.5f, 0.22f);
                    dRect.sizeDelta = new Vector2(260f, 75f);

                    TextMeshProUGUI tmp = descT.GetComponent<TextMeshProUGUI>();
                    if (font != null) tmp.font = font;
                    tmp.text = descs[i];
                    tmp.fontSize = 17f;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.85f, 0.88f, 0.94f);
                }
            }
        }

        private static void StyleCenterCratePadlock()
        {
            GameObject crate = GameObject.Find("Crate_Camp_1");
            if (crate == null) crate = GameObject.Find("Crate_Grove_1");
            if (crate == null)
            {
                var allCrates = Object.FindObjectsByType<MiniShooterGame.Combat.DestructibleCrate>(FindObjectsSortMode.None);
                if (allCrates.Length > 0) crate = allCrates[0].gameObject;
            }

            if (crate != null)
            {
                Transform lockTrans = crate.transform.Find("Padlock_Indicator");
                GameObject lockObj = lockTrans != null ? lockTrans.gameObject : new GameObject("Padlock_Indicator");
                if (lockTrans == null) lockObj.transform.SetParent(crate.transform, false);

                lockObj.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                lockObj.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

                SpriteRenderer sr = GetOrAddComponent<SpriteRenderer>(lockObj);
                Sprite padSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PadlockCyanPath);
                if (padSprite != null) sr.sprite = padSprite;
                sr.sortingOrder = 160;
                sr.color = Color.white;
            }
        }

        #endregion

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
