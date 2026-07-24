using System.IO;
using RealityFractures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace RealityFractures.EditorTools
{
    public static class RealityFracturesSceneBuilder
    {
        private const string ScenesFolder = "Assets/_RealityFractures/Scenes";
        private const string SplashScenePath = "Assets/_RealityFractures/Scenes/0_Splash.unity";
        private const string MainMenuScenePath = "Assets/_RealityFractures/Scenes/1_MainMenu.unity";
        private const string ARGameScenePath = "Assets/_RealityFractures/Scenes/2_ARGame.unity";

        [MenuItem("Reality Fractures/Build All 3 Scenes & Prefabs")]
        public static void BuildAllScenes()
        {
            EnsureDirectory(Path.Combine(Application.dataPath, "_RealityFractures/Scenes"));
            EnsureDirectory(Path.Combine(Application.dataPath, "_RealityFractures/Prefabs"));

            CreateSplashScene();
            CreateMainMenuScene();
            CreateARGameScene();

            RegisterScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("[RealityFracturesSceneBuilder] Successfully built all 3 scenes (0_Splash, 1_MainMenu, 2_ARGame) and updated Build Settings.");
        }

        private static void CreateSplashScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "0_Splash";

            GameObject cameraObj = new("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            cameraObj.AddComponent<AudioListener>();

            GameObject canvasObj = new("Splash Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Text title = CreateText("TitleText", canvasObj.transform, new Vector2(0f, 40f), 42, TextAnchor.MiddleCenter);
            title.text = "REALITY FRACTURES";
            title.color = new Color(0.36f, 0.86f, 0.9f, 1f);

            Text subtitle = CreateText("SubtitleText", canvasObj.transform, new Vector2(0f, -20f), 20, TextAnchor.MiddleCenter);
            subtitle.text = "Temporal Spatial AR Prototype";
            subtitle.color = new Color(0.8f, 0.8f, 0.85f, 0.8f);

            GameObject appFlowObj = new("App Flow Controller");
            appFlowObj.AddComponent<AppFlowController>();

            bool saved = EditorSceneManager.SaveScene(scene, SplashScenePath);
            Debug.Log($"[SceneBuilder] Saved Splash Scene ({SplashScenePath}): {saved}");
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "1_MainMenu";

            GameObject cameraObj = new("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            cameraObj.AddComponent<AudioListener>();

            GameObject canvasObj = new("Main Menu Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Title
            Text title = CreateText("TitleText", canvasObj.transform, new Vector2(0f, 180f), 38, TextAnchor.MiddleCenter);
            title.text = "REALITY FRACTURES";

            // Main Menu Panel
            GameObject mainPanel = CreatePanel("Main Menu Panel", canvasObj.transform);
            Button startBtn = CreateButton("Start Game Button", mainPanel.transform, new Vector2(0f, 40f), "START NEW FRACTURE");
            Button settingsBtn = CreateButton("Settings Button", mainPanel.transform, new Vector2(0f, -30f), "SETTINGS");
            Button quitBtn = CreateButton("Quit Button", mainPanel.transform, new Vector2(0f, -100f), "QUIT");

            // Settings Panel
            GameObject settingsPanel = CreatePanel("Settings Panel", canvasObj.transform);
            settingsPanel.SetActive(false);
            CreateText("SettingsTitle", settingsPanel.transform, new Vector2(0f, 120f), 28, TextAnchor.MiddleCenter).text = "SETTINGS";
            Toggle soundToggle = CreateToggle("Sound Toggle", settingsPanel.transform, new Vector2(0f, 40f), "Audio Sound Effects");
            Button resetProgressBtn = CreateButton("Reset Button", settingsPanel.transform, new Vector2(0f, -30f), "RESET PROGRESS");
            Button closeSettingsBtn = CreateButton("Close Settings Button", settingsPanel.transform, new Vector2(0f, -100f), "BACK");

            // Quit Confirmation Panel
            GameObject quitPanel = CreatePanel("Quit Panel", canvasObj.transform);
            quitPanel.SetActive(false);
            CreateText("QuitTitle", quitPanel.transform, new Vector2(0f, 60f), 26, TextAnchor.MiddleCenter).text = "Exit Application?";
            Button confirmQuitBtn = CreateButton("Confirm Quit Button", quitPanel.transform, new Vector2(-100f, -40f), "YES");
            Button cancelQuitBtn = CreateButton("Cancel Quit Button", quitPanel.transform, new Vector2(100f, -40f), "NO");

            // Controller
            GameObject appFlowObj = new("App Flow Controller");
            AppFlowController appFlow = appFlowObj.AddComponent<AppFlowController>();

            SerializedObject serializedFlow = new(appFlow);
            serializedFlow.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
            serializedFlow.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serializedFlow.FindProperty("quitConfirmationPanel").objectReferenceValue = quitPanel;
            serializedFlow.FindProperty("soundToggle").objectReferenceValue = soundToggle;
            serializedFlow.ApplyModifiedPropertiesWithoutUndo();

            // Wire Buttons
            startBtn.onClick.AddListener(appFlow.LoadARGame);
            settingsBtn.onClick.AddListener(appFlow.OpenSettings);
            closeSettingsBtn.onClick.AddListener(appFlow.CloseSettings);
            resetProgressBtn.onClick.AddListener(appFlow.ResetProgress);
            quitBtn.onClick.AddListener(appFlow.OpenQuitConfirmation);
            cancelQuitBtn.onClick.AddListener(appFlow.CloseQuitConfirmation);
            confirmQuitBtn.onClick.AddListener(appFlow.ConfirmQuitApp);
            soundToggle.onValueChanged.AddListener(appFlow.ToggleSound);

            bool saved = EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log($"[SceneBuilder] Saved Main Menu Scene ({MainMenuScenePath}): {saved}");
        }

        private static void CreateARGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "2_ARGame";

            GameObject gameState = new("Game State");
            GameStateController stateController = gameState.AddComponent<GameStateController>();

            GameObject arSession = new("AR Session");
            arSession.AddComponent<ARSession>();

            GameObject xrOriginObject = new("XR Origin");
            XROrigin xrOrigin = xrOriginObject.AddComponent<XROrigin>();

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(xrOriginObject.transform);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ARCameraManager>();
            cameraObject.AddComponent<ARCameraBackground>();
            xrOrigin.Camera = cameraObject.GetComponent<Camera>();

            ARPlaneManager planeManager = xrOriginObject.AddComponent<ARPlaneManager>();
            ARRaycastManager raycastManager = xrOriginObject.AddComponent<ARRaycastManager>();
            xrOriginObject.AddComponent<ARAnchorManager>();

            GameObject placementIndicator = CreatePlacementIndicator();
            placementIndicator.SetActive(false);

            GameObject fracturePrototype = CreateFracturePrototype();
            GameObject fracturePrefab = PrefabUtility.SaveAsPrefabAsset(fracturePrototype, "Assets/_RealityFractures/Prefabs/FractureRoot.prefab");
            Object.DestroyImmediate(fracturePrototype);

            GameObject placementSystem = new("Placement System");
            ARPlacementController placement = placementSystem.AddComponent<ARPlacementController>();
            SerializedObject placementSerialized = new(placement);
            placementSerialized.FindProperty("raycastManager").objectReferenceValue = raycastManager;
            placementSerialized.FindProperty("planeManager").objectReferenceValue = planeManager;
            placementSerialized.FindProperty("placementIndicator").objectReferenceValue = placementIndicator;
            placementSerialized.FindProperty("fracturePrefab").objectReferenceValue = fracturePrefab;
            placementSerialized.FindProperty("gameState").objectReferenceValue = stateController;
            placementSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateARGameUI(stateController);

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            bool saved = EditorSceneManager.SaveScene(scene, ARGameScenePath);
            Debug.Log($"[SceneBuilder] Saved AR Game Scene ({ARGameScenePath}): {saved}");
        }

        private static void CreateARGameUI(GameStateController stateController)
        {
            GameObject canvasObject = new("Minimal AR UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            Text status = CreateText("Status", canvasObject.transform, new Vector2(0f, -90f), 28, TextAnchor.MiddleCenter);
            Text progress = CreateText("Progress", canvasObject.transform, new Vector2(0f, -132f), 22, TextAnchor.MiddleCenter);

            // Pause Overlay Panel
            GameObject pausePanel = CreatePanel("Pause Panel", canvasObject.transform);
            pausePanel.SetActive(false);
            CreateText("PauseTitle", pausePanel.transform, new Vector2(0f, 120f), 32, TextAnchor.MiddleCenter).text = "GAME PAUSED";
            Button resumeBtn = CreateButton("Resume Button", pausePanel.transform, new Vector2(0f, 40f), "RESUME");
            Button restartBtn = CreateButton("Restart Button", pausePanel.transform, new Vector2(0f, -30f), "RESTART");
            Button mainMenuBtn = CreateButton("Main Menu Button", pausePanel.transform, new Vector2(0f, -100f), "MAIN MENU");

            MinimalARUIController ui = canvasObject.AddComponent<MinimalARUIController>();
            SerializedObject uiSerialized = new(ui);
            uiSerialized.FindProperty("gameState").objectReferenceValue = stateController;
            uiSerialized.FindProperty("statusText").objectReferenceValue = status;
            uiSerialized.FindProperty("progressText").objectReferenceValue = progress;
            uiSerialized.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            uiSerialized.ApplyModifiedPropertiesWithoutUndo();

            resumeBtn.onClick.AddListener(ui.ResumeGame);
            restartBtn.onClick.AddListener(ui.RestartGame);
            mainMenuBtn.onClick.AddListener(ui.ReturnToMainMenu);
        }

        private static GameObject CreatePlacementIndicator()
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "Placement Indicator";
            indicator.transform.localScale = new Vector3(0.18f, 0.004f, 0.18f);
            Object.DestroyImmediate(indicator.GetComponent<Collider>());

            Renderer renderer = indicator.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial("RF_Placement_Cyan", new Color(0.36f, 0.86f, 0.9f, 0.55f));
            return indicator;
        }

        private static GameObject CreateFracturePrototype()
        {
            GameObject root = new("FractureRoot");
            root.transform.localScale = Vector3.one * 0.35f;

            FractureWorldController worldController = root.AddComponent<FractureWorldController>();

            Material frameMaterial = CreateMaterial("RF_Frame_DarkStone", new Color(0.08f, 0.075f, 0.07f, 1f));
            CreateFrameSegment(root.transform, "Frame North", new Vector3(0f, 0.05f, 0.28f), new Vector3(0.62f, 0.04f, 0.045f), frameMaterial);
            CreateFrameSegment(root.transform, "Frame South", new Vector3(0f, 0.05f, -0.28f), new Vector3(0.62f, 0.04f, 0.045f), frameMaterial);
            CreateFrameSegment(root.transform, "Frame East", new Vector3(0.28f, 0.05f, 0f), new Vector3(0.045f, 0.04f, 0.62f), frameMaterial);
            CreateFrameSegment(root.transform, "Frame West", new Vector3(-0.28f, 0.05f, 0f), new Vector3(0.045f, 0.04f, 0.62f), frameMaterial);

            GameObject past = CreateWorldLayer("Past World", new Color(0.9f, 0.56f, 0.22f, 1f), TimeLayer.Past);
            GameObject present = CreateWorldLayer("Present World", new Color(0.35f, 0.68f, 0.45f, 1f), TimeLayer.Present);
            GameObject future = CreateWorldLayer("Future World", new Color(0.64f, 0.9f, 0.94f, 1f), TimeLayer.Future);
            past.transform.SetParent(root.transform, false);
            present.transform.SetParent(root.transform, false);
            future.transform.SetParent(root.transform, false);
            present.SetActive(false);
            future.SetActive(false);

            SerializedObject worldSerialized = new(worldController);
            worldSerialized.FindProperty("pastWorld").objectReferenceValue = past;
            worldSerialized.FindProperty("presentWorld").objectReferenceValue = present;
            worldSerialized.FindProperty("futureWorld").objectReferenceValue = future;
            worldSerialized.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreateWorldLayer(string name, Color color, TimeLayer layer)
        {
            GameObject layerRoot = new(name);
            Material material = CreateMaterial("RF_" + name.Replace(" ", "_"), color);

            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "Miniature Platform";
            platform.transform.SetParent(layerRoot.transform, false);
            platform.transform.localScale = new Vector3(0.45f, 0.035f, 0.45f);
            platform.GetComponent<Renderer>().sharedMaterial = material;

            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Time Shard";
            shard.transform.SetParent(layerRoot.transform, false);
            shard.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            shard.transform.localRotation = Quaternion.Euler(0f, 45f, 25f);
            shard.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            shard.GetComponent<Renderer>().sharedMaterial = material;

            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fragment.name = layer + " Fragment";
            fragment.transform.SetParent(layerRoot.transform, false);
            fragment.transform.localPosition = new Vector3(0.22f, 0.18f, 0.08f);
            fragment.transform.localScale = Vector3.one * 0.07f;
            fragment.GetComponent<Renderer>().sharedMaterial = CreateMaterial("RF_" + layer + "_Fragment", Color.Lerp(color, Color.white, 0.35f));

            CollectibleFragment collectible = fragment.AddComponent<CollectibleFragment>();
            SerializedObject collectibleSerialized = new(collectible);
            collectibleSerialized.FindProperty("layer").enumValueIndex = (int)layer;
            collectibleSerialized.ApplyModifiedPropertiesWithoutUndo();

            return layerRoot;
        }

        private static void CreateFrameSegment(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent, false);
            segment.transform.localPosition = position;
            segment.transform.localScale = scale;
            segment.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(segment.GetComponent<Collider>());
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panelObj = new(name);
            panelObj.transform.SetParent(parent, false);
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = panelObj.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
            return panelObj;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPos, string labelText)
        {
            GameObject buttonObj = new(name);
            buttonObj.transform.SetParent(parent, false);
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 50f);
            rect.anchoredPosition = anchoredPos;

            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.8f, 1f);

            Button btn = buttonObj.AddComponent<Button>();

            Text text = CreateText("Label", buttonObj.transform, Vector2.zero, 18, TextAnchor.MiddleCenter);
            text.text = labelText;
            text.color = Color.white;

            return btn;
        }

        private static Toggle CreateToggle(string name, Transform parent, Vector2 anchoredPos, string labelText)
        {
            GameObject toggleObj = new(name);
            toggleObj.transform.SetParent(parent, false);
            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 40f);
            rect.anchoredPosition = anchoredPos;

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            Text text = CreateText("Label", toggleObj.transform, Vector2.zero, 18, TextAnchor.MiddleCenter);
            text.text = labelText;

            return toggle;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, int size, TextAnchor anchor)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, 50f);
            rect.anchoredPosition = anchoredPosition;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Material material = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            return material;
        }

        private static void RegisterScenesInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new(SplashScenePath, true),
                new(MainMenuScenePath, true),
                new(ARGameScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static void EnsureDirectory(string fullPath)
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
}
