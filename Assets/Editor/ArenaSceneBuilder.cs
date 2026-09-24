using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniShooterGame.Core;
using MiniShooterGame.UI;

namespace MiniShooterGame.Editor
{
    /// <summary>
    /// Editor utility to construct the complete Cyber Midnight Arena stage.
    /// Sets up the high-resolution battlemap background, outer boundary walls,
    /// corner server tower colliders, tactical cover barricades, zoomed-in camera configuration,
    /// and a complete cyber Player HUD layout.
    /// Accessible via menu: Tools > Build Cyber Arena Stage
    /// </summary>
    public static class ArenaSceneBuilder
    {
        private const string BattlemapPath = "Assets/_Game/Sprites/Environment/Arena_CyberMidnight.png";
        private const string HealthFramePath = "Assets/_Game/Sprites/UI/Health_Frame.png";
        private const string HealthFillPath = "Assets/_Game/Sprites/UI/Health_Fill.png";
        private const string VignettePath = "Assets/_Game/Sprites/UI/Vignette_Red.png";

        private const string StageRootName = "[Arena_Stage]";
        private const string ObstacleTag = "Obstacle";

        [MenuItem("Tools/Build Cyber Arena Stage", priority = 10)]
        [MenuItem("Tools/Architecture/Build Cyber Arena Stage", priority = 102)]
        public static void BuildArenaStage()
        {
            EnsureObstacleTagExists();

            // 1. Load the Arena Sprite
            Sprite arenaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BattlemapPath);
            if (arenaSprite == null)
            {
                Debug.LogError($"[ArenaSceneBuilder] Battlemap sprite not found at: '{BattlemapPath}'. Please verify the asset exists.");
                EditorUtility.DisplayDialog("Error", $"Could not find battlemap sprite at:\n{BattlemapPath}", "OK");
                return;
            }

            // 2. Find or Create Root [Arena_Stage]
            GameObject stageRoot = GameObject.Find(StageRootName);
            if (stageRoot == null)
            {
                stageRoot = new GameObject(StageRootName);
                Undo.RegisterCreatedObjectUndo(stageRoot, "Create Arena Stage");
            }
            stageRoot.transform.position = Vector3.zero;
            stageRoot.transform.rotation = Quaternion.identity;
            stageRoot.transform.localScale = Vector3.one;

            // 3. Setup Background Sprite Renderer
            SetupBackground(stageRoot, arenaSprite);

            // 4. Setup Perimeter Boundary Colliders
            SetupPerimeterBoundaries(stageRoot);

            // 5. Setup Tactical Cover & Obstacle Colliders
            SetupTacticalCover(stageRoot);

            // 6. Configure Camera (Zoomed-in Action View)
            ConfigureCamera();

            // 7. Setup Complete Player HUD
            SetupHUD();

            // Mark scene dirty and select stage
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = stageRoot;

            Debug.Log("[ArenaSceneBuilder] Cyber Midnight Arena Stage & Player HUD constructed successfully!");
            EditorUtility.DisplayDialog(
                "Arena Builder",
                "Cyber Midnight Arena Stage & HUD built successfully!\n\n" +
                "• Background rendered with Sorting Order -50\n" +
                "• Outer perimeter boundary walls generated\n" +
                "• 4 Corner server towers + 4 tactical barricades placed with 'Obstacle' tag\n" +
                "• Camera zoomed to intense size (6.0) with arena bounds clamping\n" +
                "• Cyber Player HUD generated (Health bar, Wave banner, Low-HP Vignette)",
                "OK"
            );
        }

        private static void SetupBackground(GameObject stageRoot, Sprite sprite)
        {
            Transform bgTransform = stageRoot.transform.Find("Arena_Background");
            GameObject bgObj;
            if (bgTransform == null)
            {
                bgObj = new GameObject("Arena_Background");
                bgObj.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(bgObj, "Create Arena Background");
            }
            else
            {
                bgObj = bgTransform.gameObject;
            }

            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.identity;
            bgObj.transform.localScale = Vector3.one;

            SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = bgObj.AddComponent<SpriteRenderer>();

            sr.sprite = sprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -50; // Guaranteed to render behind all gameplay elements
        }

        private static void SetupPerimeterBoundaries(GameObject stageRoot)
        {
            Transform boundRootTransform = stageRoot.transform.Find("[Boundaries]");
            GameObject boundRoot;
            if (boundRootTransform == null)
            {
                boundRoot = new GameObject("[Boundaries]");
                boundRoot.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(boundRoot, "Create Boundaries Root");
            }
            else
            {
                boundRoot = boundRootTransform.gameObject;
            }

            boundRoot.transform.localPosition = Vector3.zero;

            // Dimensions calibrated to 20.48 x 20.48 battlemap with inner playable boundary
            CreateOrUpdateBoxCollider(boundRoot, "Wall_North", new Vector2(0f, 9.8f), new Vector2(20.48f, 1.2f));
            CreateOrUpdateBoxCollider(boundRoot, "Wall_South", new Vector2(0f, -9.8f), new Vector2(20.48f, 1.2f));
            CreateOrUpdateBoxCollider(boundRoot, "Wall_East", new Vector2(9.8f, 0f), new Vector2(1.2f, 20.48f));
            CreateOrUpdateBoxCollider(boundRoot, "Wall_West", new Vector2(-9.8f, 0f), new Vector2(1.2f, 20.48f));
        }

        private static void SetupTacticalCover(GameObject stageRoot)
        {
            Transform obsRootTransform = stageRoot.transform.Find("[Obstacles]");
            GameObject obsRoot;
            if (obsRootTransform == null)
            {
                obsRoot = new GameObject("[Obstacles]");
                obsRoot.transform.SetParent(stageRoot.transform);
                Undo.RegisterCreatedObjectUndo(obsRoot, "Create Obstacles Root");
            }
            else
            {
                obsRoot = obsRootTransform.gameObject;
            }

            obsRoot.transform.localPosition = Vector3.zero;

            // 1. Four Corner Octagonal / Circular Server Towers
            CreateOrUpdateCircleCollider(obsRoot, "Tower_TopLeft", new Vector2(-5.8f, 5.8f), 1.25f);
            CreateOrUpdateCircleCollider(obsRoot, "Tower_TopRight", new Vector2(5.8f, 5.8f), 1.25f);
            CreateOrUpdateCircleCollider(obsRoot, "Tower_BottomLeft", new Vector2(-5.8f, -5.8f), 1.25f);
            CreateOrUpdateCircleCollider(obsRoot, "Tower_BottomRight", new Vector2(5.8f, -5.8f), 1.25f);

            // 2. Four Tactical Wall Barricades / Cover Crates
            CreateOrUpdateBoxCollider(obsRoot, "Barricade_West", new Vector2(-5.2f, 0f), new Vector2(1.1f, 2.6f));
            CreateOrUpdateBoxCollider(obsRoot, "Barricade_East", new Vector2(5.2f, 0f), new Vector2(1.1f, 2.6f));
            CreateOrUpdateBoxCollider(obsRoot, "Barricade_North", new Vector2(0f, 4.8f), new Vector2(2.6f, 1.1f));
            CreateOrUpdateBoxCollider(obsRoot, "Barricade_South", new Vector2(0f, -4.8f), new Vector2(2.6f, 1.1f));
        }

        private static void CreateOrUpdateBoxCollider(GameObject parent, string name, Vector2 localPos, Vector2 size)
        {
            Transform child = parent.transform.Find(name);
            GameObject obj;
            if (child == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent.transform);
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            }
            else
            {
                obj = child.gameObject;
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

        private static void CreateOrUpdateCircleCollider(GameObject parent, string name, Vector2 localPos, float radius)
        {
            Transform child = parent.transform.Find(name);
            GameObject obj;
            if (child == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent.transform);
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            }
            else
            {
                obj = child.gameObject;
            }

            obj.tag = ObstacleTag;
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            CircleCollider2D circle = obj.GetComponent<CircleCollider2D>();
            if (circle == null) circle = obj.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.isTrigger = false;
        }

        private static void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Undo.RecordObject(cam, "Configure Camera Orthographic Size");
            cam.orthographic = true;
            cam.orthographicSize = 3.5f; // 2x Zoom: Action close-up framing, player & enemies large and detailed

            if (cam.TryGetComponent<CameraFollow2D>(out var follow))
            {
                Undo.RecordObject(follow, "Configure CameraFollow2D Bounds");
                SerializedObject serializedFollow = new SerializedObject(follow);
                serializedFollow.FindProperty("clampToBounds").boolValue = true;
                // Calibrated for 3.5 zoom on 16:9 display so battlemap fills screen with ZERO void borders
                serializedFollow.FindProperty("minBounds").vector2Value = new Vector2(-2.4f, -5.1f);
                serializedFollow.FindProperty("maxBounds").vector2Value = new Vector2(2.4f, 5.1f);
                serializedFollow.FindProperty("enableLookAhead").boolValue = true;
                serializedFollow.FindProperty("lookAheadDistance").floatValue = 1.0f;
                serializedFollow.ApplyModifiedProperties();
            }
        }

        private static void SetupHUD()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Ensure CanvasScaler is set to 1920x1080 landscape
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Find or create [Player_HUD] container
            Transform hudTransform = canvas.transform.Find("[Player_HUD]");
            GameObject hudObj;
            if (hudTransform == null)
            {
                hudObj = new GameObject("[Player_HUD]");
                hudObj.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(hudObj, "Create Player HUD");
            }
            else
            {
                hudObj = hudTransform.gameObject;
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

            // Setup Top-Center Wave Display
            SetupWaveDisplay(hudObj);

            // Setup Low Health Vignette
            SetupVignette(hudObj);
        }

        private static void SetupHealthBar(GameObject hudParent)
        {
            Transform containerTrans = hudParent.transform.Find("Health_Container");
            GameObject container;
            if (containerTrans == null)
            {
                container = new GameObject("Health_Container");
                container.transform.SetParent(hudParent.transform, false);
            }
            else
            {
                container = containerTrans.gameObject;
            }

            RectTransform rect = container.GetComponent<RectTransform>();
            if (rect == null) rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -40f);
            rect.sizeDelta = new Vector2(340f, 46f);

            // Frame Background
            Image frameImg = container.GetComponent<Image>();
            if (frameImg == null) frameImg = container.AddComponent<Image>();
            frameImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFramePath);
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;

            // Fill Bar
            Transform fillTrans = container.transform.Find("Health_Fill");
            GameObject fillObj;
            if (fillTrans == null)
            {
                fillObj = new GameObject("Health_Fill");
                fillObj.transform.SetParent(container.transform, false);
            }
            else
            {
                fillObj = fillTrans.gameObject;
            }

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            if (fillRect == null) fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.02f, 0.12f);
            fillRect.anchorMax = new Vector2(0.98f, 0.88f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImg = fillObj.GetComponent<Image>();
            if (fillImg == null) fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFillPath);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = new Color(0.1f, 1f, 0.5f);

            // Wire to PlayerHUD
            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("healthFillImage").objectReferenceValue = fillImg;
            serializedHUD.ApplyModifiedProperties();
        }

        private static void SetupWaveDisplay(GameObject hudParent)
        {
            Transform waveTrans = hudParent.transform.Find("Wave_Display");
            GameObject waveObj;
            if (waveTrans == null)
            {
                waveObj = new GameObject("Wave_Display");
                waveObj.transform.SetParent(hudParent.transform, false);
            }
            else
            {
                waveObj = waveTrans.gameObject;
            }

            RectTransform rect = waveObj.GetComponent<RectTransform>();
            if (rect == null) rect = waveObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -30f);
            rect.sizeDelta = new Vector2(300f, 60f);

            TextMeshProUGUI tmp = waveObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = waveObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<color=#00E5FF>WAVE 1</color>";
            tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center;

            // Wire to PlayerHUD
            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("waveTitleText").objectReferenceValue = tmp;
            serializedHUD.ApplyModifiedProperties();
        }

        private static void SetupVignette(GameObject hudParent)
        {
            Transform vigTrans = hudParent.transform.Find("LowHealth_Vignette");
            GameObject vigObj;
            if (vigTrans == null)
            {
                vigObj = new GameObject("LowHealth_Vignette");
                vigObj.transform.SetParent(hudParent.transform, false);
            }
            else
            {
                vigObj = vigTrans.gameObject;
            }

            RectTransform rect = vigObj.GetComponent<RectTransform>();
            if (rect == null) rect = vigObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = vigObj.GetComponent<Image>();
            if (img == null) img = vigObj.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VignettePath);
            img.color = new Color(1f, 0f, 0.1f, 0.8f);
            img.raycastTarget = false;

            CanvasGroup group = vigObj.GetComponent<CanvasGroup>();
            if (group == null) group = vigObj.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            // Wire to PlayerHUD
            SerializedObject serializedHUD = new SerializedObject(hudParent.GetComponent<PlayerHUD>());
            serializedHUD.FindProperty("lowHealthVignette").objectReferenceValue = group;
            serializedHUD.ApplyModifiedProperties();
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
