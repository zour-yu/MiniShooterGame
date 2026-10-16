using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using MiniShooterGame.UI;
using MiniShooterGame.Player;
using MiniShooterGame.Core;

namespace MiniShooterGame.Editor
{
    [InitializeOnLoad]
    public static class GameHUDBuilder
    {
        private const string HealthFramePath = "Assets/_Game/Sprites/UI/Health_Frame.png";
        private const string HealthFillPath = "Assets/_Game/Sprites/UI/Health_Fill.png";
        private const string DashButtonPath = "Assets/_Game/Sprites/UI/Button_Dash.png";
        private const string VignettePath = "Assets/_Game/Sprites/UI/Vignette_Red.png";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const string AutoBuildFlag = "MiniShooterGame_HUD_Polished_v1";

        static GameHUDBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(AutoBuildFlag, false))
                {
                    SessionState.SetBool(AutoBuildFlag, true);
                    BuildHUD(false);
                }
            };
        }

        [MenuItem("Tools/Build and Polish Complete Game HUD", priority = 5)]
        public static void BuildHUDMenu()
        {
            BuildHUD(true);
        }

        public static void BuildHUD(bool showDialog = false)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0) uiLayer = 5;

            // 1. Ensure EventSystem exists
            EnsureEventSystem(uiLayer);

            // 2. Find or create UI Canvas
            Canvas canvas = EnsureCanvas(uiLayer);
            if (canvas == null)
            {
                Debug.LogError("[GameHUDBuilder] Failed to find or create Canvas!");
                return;
            }

            // 3. Find or create root [Player_HUD]
            Transform hudTransform = canvas.transform.Find("[Player_HUD]");
            GameObject hudObj = hudTransform != null ? hudTransform.gameObject : new GameObject("[Player_HUD]");
            if (hudTransform == null)
            {
                hudObj.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(hudObj, "Create Player HUD");
            }
            hudObj.layer = uiLayer;

            RectTransform hudRect = GetOrAddComponent<RectTransform>(hudObj);
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            PlayerHUD playerHUD = GetOrAddComponent<PlayerHUD>(hudObj);

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            // 4. Build Top-Left Health Bar (with numeric text & catchup bar)
            var (fillImg, catchupImg, healthTxt) = BuildHealthBar(hudObj, uiLayer, fontAsset);

            // 5. Build Top-Left Weapon & Ammo Badge (below health bar)
            var (weaponTxt, ammoTxt) = BuildWeaponBadge(hudObj, uiLayer, fontAsset);

            // 6. Build Top-Center Wave Header & Enemy Remaining Display
            var (waveTxt, enemiesTxt) = BuildWaveAndEnemyHeader(hudObj, uiLayer, fontAsset);

            // 7. Build/Configure Bottom-Right Dash Button & Radial Cooldown Fill
            Image dashRadial = BuildDashButton(canvas, uiLayer);

            // 8. Build Fullscreen Low-Health Vignette
            CanvasGroup vignetteGroup = BuildLowHealthVignette(hudObj, uiLayer);

            // 9. Wire all references into PlayerHUD
            SerializedObject soHUD = new SerializedObject(playerHUD);
            soHUD.FindProperty("healthFillImage").objectReferenceValue = fillImg;
            soHUD.FindProperty("healthCatchupImage").objectReferenceValue = catchupImg;
            soHUD.FindProperty("healthText").objectReferenceValue = healthTxt;
            soHUD.FindProperty("dashCooldownRadialFill").objectReferenceValue = dashRadial;
            soHUD.FindProperty("waveTitleText").objectReferenceValue = waveTxt;
            soHUD.FindProperty("enemiesRemainingText").objectReferenceValue = enemiesTxt;
            soHUD.FindProperty("lowHealthVignette").objectReferenceValue = vignetteGroup;
            soHUD.FindProperty("lowHealthThreshold").floatValue = 0.35f;
            soHUD.FindProperty("weaponNameText").objectReferenceValue = weaponTxt;
            soHUD.FindProperty("ammoCountText").objectReferenceValue = ammoTxt;

            // Configure dynamic health gradient (Red -> Amber -> Emerald)
            SerializedProperty gradProp = soHUD.FindProperty("healthGradient");
            SetupHealthGradient(gradProp);
            soHUD.ApplyModifiedProperties();

            // 10. Ensure all children of Canvas are recursively assigned to UI layer
            SetLayerRecursively(canvas.gameObject, uiLayer);

            // 11. Mark scene dirty & save
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            // Capture preview screenshot if available
            GameViewScreenshot.Capture(false);

            Debug.Log("[GameHUDBuilder] Complete Cyber/Island Player HUD successfully constructed, styled, and wired!");
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Game HUD Builder",
                    "Complete Player HUD constructed and polished!\n\n" +
                    "• Top-Left: Sliced Health Bar + Animated Catchup + '100 / 100 HP' Counter\n" +
                    "• Top-Left: Weapon Badge ('PISTOL') + Ammo Counter ('∞')\n" +
                    "• Top-Center: Styled Wave Title ('WAVE 1') + Enemies Remaining ('ENEMIES: 6')\n" +
                    "• Bottom-Right: 130px Mobile Dash Button + Radial Cooldown Dark Sweep\n" +
                    "• Full-Screen: Low-Health Red Pulse Vignette (HP < 35%)\n" +
                    "• Mobile Safe Area: 70px side padding prevents notch/bezel clipping\n" +
                    "• All elements assigned to UI Layer (5) on Canvas (Sorting Order 100)",
                    "OK"
                );
            }
        }

        private static Canvas EnsureCanvas(int uiLayer)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("UI_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            canvas.gameObject.layer = uiLayer;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvas.gameObject);
            return canvas;
        }

        private static void EnsureEventSystem(int uiLayer)
        {
            var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length == 0)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.layer = uiLayer;
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }
            else if (eventSystems.Length > 1)
            {
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    Undo.DestroyObjectImmediate(eventSystems[i].gameObject);
                }
            }
        }

        private static (Image fill, Image catchup, TextMeshProUGUI text) BuildHealthBar(GameObject hudParent, int uiLayer, TMP_FontAsset font)
        {
            Transform containerTrans = hudParent.transform.Find("Health_Container");
            GameObject container = containerTrans != null ? containerTrans.gameObject : new GameObject("Health_Container");
            if (containerTrans == null) container.transform.SetParent(hudParent.transform, false);
            container.layer = uiLayer;

            RectTransform rect = GetOrAddComponent<RectTransform>(container);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            // Safe area: 70px from left edge, 45px from top edge
            rect.anchoredPosition = new Vector2(70f, -45f);
            rect.sizeDelta = new Vector2(360f, 48f);

            Image frameImg = GetOrAddComponent<Image>(container);
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFramePath);
            if (frameSprite != null)
            {
                frameImg.sprite = frameSprite;
                frameImg.type = Image.Type.Sliced;
            }
            frameImg.color = Color.white;

            // 1. Trailing Catchup Fill
            Transform catchupTrans = container.transform.Find("Health_Catchup");
            GameObject catchupObj = catchupTrans != null ? catchupTrans.gameObject : new GameObject("Health_Catchup");
            if (catchupTrans == null) catchupObj.transform.SetParent(container.transform, false);
            catchupObj.layer = uiLayer;

            RectTransform catchupRect = GetOrAddComponent<RectTransform>(catchupObj);
            catchupRect.anchorMin = new Vector2(0.02f, 0.12f);
            catchupRect.anchorMax = new Vector2(0.98f, 0.88f);
            catchupRect.offsetMin = Vector2.zero;
            catchupRect.offsetMax = Vector2.zero;

            Image catchupImg = GetOrAddComponent<Image>(catchupObj);
            Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFillPath);
            if (fillSprite != null)
            {
                catchupImg.sprite = fillSprite;
                catchupImg.type = Image.Type.Filled;
                catchupImg.fillMethod = Image.FillMethod.Horizontal;
                catchupImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            catchupImg.color = new Color(1f, 0.45f, 0.15f, 0.8f);

            // 2. Foreground Dynamic Fill
            Transform fillTrans = container.transform.Find("Health_Fill");
            GameObject fillObj = fillTrans != null ? fillTrans.gameObject : new GameObject("Health_Fill");
            if (fillTrans == null) fillObj.transform.SetParent(container.transform, false);
            fillObj.layer = uiLayer;

            RectTransform fillRect = GetOrAddComponent<RectTransform>(fillObj);
            fillRect.anchorMin = new Vector2(0.02f, 0.12f);
            fillRect.anchorMax = new Vector2(0.98f, 0.88f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImg = GetOrAddComponent<Image>(fillObj);
            if (fillSprite != null)
            {
                fillImg.sprite = fillSprite;
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
            fillImg.color = new Color(0.1f, 1f, 0.5f, 1f);

            // 3. Numeric Health Text (e.g. 100 / 100 HP)
            Transform textTrans = container.transform.Find("Health_Text");
            GameObject textObj = textTrans != null ? textTrans.gameObject : new GameObject("Health_Text");
            if (textTrans == null) textObj.transform.SetParent(container.transform, false);
            textObj.layer = uiLayer;

            RectTransform textRect = GetOrAddComponent<RectTransform>(textObj);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(textObj);
            if (font != null) tmp.font = font;
            tmp.text = "100 / 100 HP";
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return (fillImg, catchupImg, tmp);
        }

        private static (TextMeshProUGUI name, TextMeshProUGUI ammo) BuildWeaponBadge(GameObject hudParent, int uiLayer, TMP_FontAsset font)
        {
            Transform containerTrans = hudParent.transform.Find("Weapon_Container");
            GameObject container = containerTrans != null ? containerTrans.gameObject : new GameObject("Weapon_Container");
            if (containerTrans == null) container.transform.SetParent(hudParent.transform, false);
            container.layer = uiLayer;

            RectTransform rect = GetOrAddComponent<RectTransform>(container);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(70f, -100f);
            rect.sizeDelta = new Vector2(300f, 44f);

            Image bg = GetOrAddComponent<Image>(container);
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.88f);

            // Weapon Name Text
            Transform nameTrans = container.transform.Find("Weapon_Name");
            GameObject nameObj = nameTrans != null ? nameTrans.gameObject : new GameObject("Weapon_Name");
            if (nameTrans == null) nameObj.transform.SetParent(container.transform, false);
            nameObj.layer = uiLayer;

            RectTransform nameRect = GetOrAddComponent<RectTransform>(nameObj);
            nameRect.anchorMin = new Vector2(0.08f, 0f);
            nameRect.anchorMax = new Vector2(0.65f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            TextMeshProUGUI nameTmp = GetOrAddComponent<TextMeshProUGUI>(nameObj);
            if (font != null) nameTmp.font = font;
            nameTmp.text = "PISTOL";
            nameTmp.fontSize = 20f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = new Color(0.35f, 0.85f, 1f);
            nameTmp.raycastTarget = false;

            // Ammo Count Text
            Transform ammoTrans = container.transform.Find("Ammo_Count");
            GameObject ammoObj = ammoTrans != null ? ammoTrans.gameObject : new GameObject("Ammo_Count");
            if (ammoTrans == null) ammoObj.transform.SetParent(container.transform, false);
            ammoObj.layer = uiLayer;

            RectTransform ammoRect = GetOrAddComponent<RectTransform>(ammoObj);
            ammoRect.anchorMin = new Vector2(0.65f, 0f);
            ammoRect.anchorMax = new Vector2(0.94f, 1f);
            ammoRect.offsetMin = Vector2.zero;
            ammoRect.offsetMax = Vector2.zero;

            TextMeshProUGUI ammoTmp = GetOrAddComponent<TextMeshProUGUI>(ammoObj);
            if (font != null) ammoTmp.font = font;
            ammoTmp.text = "∞";
            ammoTmp.fontSize = 24f;
            ammoTmp.fontStyle = FontStyles.Bold;
            ammoTmp.alignment = TextAlignmentOptions.MidlineRight;
            ammoTmp.color = new Color(1f, 0.85f, 0.2f);
            ammoTmp.raycastTarget = false;

            return (nameTmp, ammoTmp);
        }

        private static (TextMeshProUGUI wave, TextMeshProUGUI enemies) BuildWaveAndEnemyHeader(GameObject hudParent, int uiLayer, TMP_FontAsset font)
        {
            Transform containerTrans = hudParent.transform.Find("Wave_Header_Container");
            GameObject container = containerTrans != null ? containerTrans.gameObject : new GameObject("Wave_Header_Container");
            if (containerTrans == null) container.transform.SetParent(hudParent.transform, false);
            container.layer = uiLayer;

            RectTransform rect = GetOrAddComponent<RectTransform>(container);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            // Safe area: 45px from top
            rect.anchoredPosition = new Vector2(0f, -42f);
            rect.sizeDelta = new Vector2(380f, 72f);

            Image bg = GetOrAddComponent<Image>(container);
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.85f);

            // Wave Title
            Transform waveTrans = container.transform.Find("Wave_Title_Text");
            GameObject waveObj = waveTrans != null ? waveTrans.gameObject : new GameObject("Wave_Title_Text");
            if (waveTrans == null) waveObj.transform.SetParent(container.transform, false);
            waveObj.layer = uiLayer;

            RectTransform waveRect = GetOrAddComponent<RectTransform>(waveObj);
            waveRect.anchorMin = new Vector2(0f, 0.46f);
            waveRect.anchorMax = new Vector2(1f, 1f);
            waveRect.offsetMin = Vector2.zero;
            waveRect.offsetMax = Vector2.zero;

            TextMeshProUGUI waveTmp = GetOrAddComponent<TextMeshProUGUI>(waveObj);
            if (font != null) waveTmp.font = font;
            waveTmp.text = "<color=#00E5FF>WAVE 1</color>";
            waveTmp.fontSize = 24f;
            waveTmp.fontStyle = FontStyles.Bold;
            waveTmp.alignment = TextAlignmentOptions.Center;
            waveTmp.raycastTarget = false;

            // Enemies Remaining Text
            Transform enemyTrans = container.transform.Find("Enemies_Remaining_Text");
            GameObject enemyObj = enemyTrans != null ? enemyTrans.gameObject : new GameObject("Enemies_Remaining_Text");
            if (enemyTrans == null) enemyObj.transform.SetParent(container.transform, false);
            enemyObj.layer = uiLayer;

            RectTransform enemyRect = GetOrAddComponent<RectTransform>(enemyObj);
            enemyRect.anchorMin = new Vector2(0f, 0f);
            enemyRect.anchorMax = new Vector2(1f, 0.48f);
            enemyRect.offsetMin = Vector2.zero;
            enemyRect.offsetMax = Vector2.zero;

            TextMeshProUGUI enemyTmp = GetOrAddComponent<TextMeshProUGUI>(enemyObj);
            if (font != null) enemyTmp.font = font;
            enemyTmp.text = "ENEMIES: <color=#FF3366>6</color>";
            enemyTmp.fontSize = 18f;
            enemyTmp.fontStyle = FontStyles.Bold;
            enemyTmp.alignment = TextAlignmentOptions.Center;
            enemyTmp.color = Color.white;
            enemyTmp.raycastTarget = false;

            // Remove deprecated standalone Wave_Display if present
            Transform oldWave = hudParent.transform.Find("Wave_Display");
            if (oldWave != null && oldWave != containerTrans)
            {
                Object.DestroyImmediate(oldWave.gameObject);
            }

            return (waveTmp, enemyTmp);
        }

        private static Image BuildDashButton(Canvas canvas, int uiLayer)
        {
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            Button dashBtn = null;
            foreach (var b in buttons)
            {
                if (b.name.ToLower().Contains("dash"))
                {
                    dashBtn = b;
                    break;
                }
            }

            if (dashBtn == null)
            {
                GameObject btnObj = new GameObject("Button_Dash");
                btnObj.transform.SetParent(canvas.transform, false);
                dashBtn = btnObj.AddComponent<Button>();
                Undo.RegisterCreatedObjectUndo(btnObj, "Create Dash Button");
            }

            dashBtn.gameObject.layer = uiLayer;
            RectTransform rt = GetOrAddComponent<RectTransform>(dashBtn.gameObject);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // Safe area: 150px from right & bottom
            rt.anchoredPosition = new Vector2(-150f, 150f);
            rt.sizeDelta = new Vector2(130f, 130f);

            Image btnImg = GetOrAddComponent<Image>(dashBtn.gameObject);
            Sprite dashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DashButtonPath);
            if (dashSprite != null)
            {
                btnImg.sprite = dashSprite;
            }
            btnImg.color = Color.white;

            // Dash Radial Cooldown Overlay (Child Image)
            Transform radialTrans = dashBtn.transform.Find("Dash_Cooldown_Radial");
            GameObject radialObj = radialTrans != null ? radialTrans.gameObject : new GameObject("Dash_Cooldown_Radial");
            if (radialTrans == null) radialObj.transform.SetParent(dashBtn.transform, false);
            radialObj.layer = uiLayer;

            RectTransform radialRect = GetOrAddComponent<RectTransform>(radialObj);
            radialRect.anchorMin = Vector2.zero;
            radialRect.anchorMax = Vector2.one;
            radialRect.offsetMin = Vector2.zero;
            radialRect.offsetMax = Vector2.zero;

            Image radialImg = GetOrAddComponent<Image>(radialObj);
            if (dashSprite != null) radialImg.sprite = dashSprite;
            radialImg.type = Image.Type.Filled;
            radialImg.fillMethod = Image.FillMethod.Radial360;
            radialImg.fillOrigin = (int)Image.Origin360.Top;
            radialImg.fillClockwise = true;
            radialImg.fillAmount = 0f;
            radialImg.color = new Color(0f, 0f, 0f, 0.7f);
            radialImg.raycastTarget = false;

            // Wire Click to PlayerDash if in scene
            PlayerDash playerDash = Object.FindFirstObjectByType<PlayerDash>();
            if (playerDash != null)
            {
                dashBtn.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(dashBtn.onClick, playerDash.TriggerDash);
            }

            return radialImg;
        }

        private static CanvasGroup BuildLowHealthVignette(GameObject hudParent, int uiLayer)
        {
            Transform vigTrans = hudParent.transform.Find("LowHealth_Vignette");
            GameObject vigObj = vigTrans != null ? vigTrans.gameObject : new GameObject("LowHealth_Vignette");
            if (vigTrans == null) vigObj.transform.SetParent(hudParent.transform, false);
            vigObj.layer = uiLayer;

            RectTransform rect = GetOrAddComponent<RectTransform>(vigObj);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = GetOrAddComponent<Image>(vigObj);
            Sprite vigSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
            if (vigSprite != null) img.sprite = vigSprite;
            img.color = new Color(1f, 0f, 0.1f, 0.85f);
            img.raycastTarget = false;

            CanvasGroup group = GetOrAddComponent<CanvasGroup>(vigObj);
            group.alpha = 0f;
            return group;
        }

        private static void SetupHealthGradient(SerializedProperty gradProp)
        {
            if (gradProp == null) return;
            SerializedProperty numColors = gradProp.FindPropertyRelative("m_NumColorKeys");
            SerializedProperty numAlphas = gradProp.FindPropertyRelative("m_NumAlphaKeys");
            if (numColors != null) numColors.intValue = 3;
            if (numAlphas != null) numAlphas.intValue = 2;

            SerializedProperty key0 = gradProp.FindPropertyRelative("key0");
            SerializedProperty key1 = gradProp.FindPropertyRelative("key1");
            SerializedProperty key2 = gradProp.FindPropertyRelative("key2");
            SerializedProperty ctime0 = gradProp.FindPropertyRelative("ctime0");
            SerializedProperty ctime1 = gradProp.FindPropertyRelative("ctime1");
            SerializedProperty ctime2 = gradProp.FindPropertyRelative("ctime2");

            if (key0 != null) key0.colorValue = new Color(1f, 0.2f, 0.2f); // Red
            if (ctime0 != null) ctime0.intValue = 0;

            if (key1 != null) key1.colorValue = new Color(1f, 0.85f, 0.1f); // Amber
            if (ctime1 != null) ctime1.intValue = 32767;

            if (key2 != null) key2.colorValue = new Color(0.1f, 1f, 0.5f); // Emerald
            if (ctime2 != null) ctime2.intValue = 65535;

            SerializedProperty atime0 = gradProp.FindPropertyRelative("atime0");
            SerializedProperty atime1 = gradProp.FindPropertyRelative("atime1");
            if (atime0 != null) atime0.intValue = 0;
            if (atime1 != null) atime1.intValue = 65535;
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
