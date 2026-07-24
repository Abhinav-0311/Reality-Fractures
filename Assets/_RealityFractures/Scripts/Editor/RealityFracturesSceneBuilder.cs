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
        private const string SplashScenePath = "Assets/_RealityFractures/Scenes/0_Splash.unity";
        private const string MainMenuScenePath = "Assets/_RealityFractures/Scenes/1_MainMenu.unity";
        private const string ARGameScenePath = "Assets/_RealityFractures/Scenes/2_ARGame.unity";

        [MenuItem("Reality Fractures/Open Main Menu Scene")]
        public static void OpenMainMenuScene()
        {
            SetWorkingDirectoryToProjectRoot();
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Reality Fractures/Open AR Game Scene")]
        public static void OpenARGameScene()
        {
            SetWorkingDirectoryToProjectRoot();
            EditorSceneManager.OpenScene(ARGameScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Reality Fractures/Force Refresh Project Window")]
        public static void ForceRefreshWindow()
        {
            SetWorkingDirectoryToProjectRoot();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem("Reality Fractures/Build All 3 Scenes & Prefabs")]
        public static void BuildAllScenes()
        {
            SetWorkingDirectoryToProjectRoot();

            // 1. Create and force refresh asset database folders FIRST
            EnsureAssetFolders();

            // 2. Load generated high-res sci-fi artwork JPG sprite and panel background
            Sprite menuBgArt = LoadMainMenuBackgroundSprite();
            Sprite panelBgSprite = CreatePanelBackgroundTexture();

            // 3. Build each scene individually and flush to disk immediately
            CreateAndSaveSplashScene();
            CreateAndSaveMainMenuScene(panelBgSprite, menuBgArt);
            CreateAndSaveARGameScene(panelBgSprite);

            // 4. Register in build settings
            RegisterScenesInBuildSettings();

            // 5. Force asset database save & refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorApplication.RepaintProjectWindow();
            EditorApplication.RepaintHierarchyWindow();

            // 6. Open Main Menu scene in Unity Editor
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            string splashDiskPath = Path.Combine(Application.dataPath, "_RealityFractures/Scenes/0_Splash.unity");
            string menuDiskPath = Path.Combine(Application.dataPath, "_RealityFractures/Scenes/1_MainMenu.unity");
            string arDiskPath = Path.Combine(Application.dataPath, "_RealityFractures/Scenes/2_ARGame.unity");

            Debug.Log($"[RealityFracturesSceneBuilder] Finished BuildAllScenes!\n" +
                      $"Menu JPG Art Loaded: {menuBgArt != null}\n" +
                      $"0_Splash.unity Exists: {File.Exists(splashDiskPath)} ({splashDiskPath})\n" +
                      $"1_MainMenu.unity Exists: {File.Exists(menuDiskPath)} ({menuDiskPath})\n" +
                      $"2_ARGame.unity Exists: {File.Exists(arDiskPath)} ({arDiskPath})");
        }

        private static void SetWorkingDirectoryToProjectRoot()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Directory.SetCurrentDirectory(projectRoot);
        }

        private static void EnsureAssetFolders()
        {
            string basePhysical = Application.dataPath + "/_RealityFractures";
            EnsurePhysicalFolder(basePhysical);
            EnsurePhysicalFolder(basePhysical + "/Scenes");
            EnsurePhysicalFolder(basePhysical + "/Prefabs");
            EnsurePhysicalFolder(basePhysical + "/Art");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!AssetDatabase.IsValidFolder("Assets/_RealityFractures"))
            {
                AssetDatabase.CreateFolder("Assets", "_RealityFractures");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_RealityFractures/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_RealityFractures", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_RealityFractures/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_RealityFractures", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_RealityFractures/Art"))
            {
                AssetDatabase.CreateFolder("Assets/_RealityFractures", "Art");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsurePhysicalFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static Sprite LoadMainMenuBackgroundSprite()
        {
            string[] candidates = new string[]
            {
                "Assets/_RealityFractures/Art/UI_MainMenu_BG.jpg",
                "Assets/_RealityFractures/Art/UI_MainMenu_BG.png"
            };

            foreach (string relPath in candidates)
            {
                string absPath = Path.Combine(Application.dataPath, relPath.Substring(7));
                if (File.Exists(absPath))
                {
                    AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceSynchronousImport);
                    TextureImporter importer = AssetImporter.GetAtPath(relPath) as TextureImporter;
                    if (importer != null)
                    {
                        bool dirty = false;
                        if (importer.textureType != TextureImporterType.Sprite)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            dirty = true;
                        }
                        if (dirty)
                        {
                            importer.SaveAndReimport();
                        }
                    }
                    Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(relPath);
                    if (s != null) return s;
                }
            }
            return null;
        }

        private static Sprite CreatePanelBackgroundTexture()
        {
            string relPath = "Assets/_RealityFractures/Art/UI_Panel_BG.png";
            string absPath = Path.Combine(Application.dataPath, "_RealityFractures/Art/UI_Panel_BG.png");

            if (!File.Exists(absPath))
            {
                Texture2D tex = new(256, 256, TextureFormat.RGBA32, false);
                Color darkBorder = new(0.2f, 0.5f, 0.8f, 0.9f);
                Color darkCenter = new(0.06f, 0.07f, 0.12f, 0.92f);

                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        bool isBorder = x < 4 || x > 251 || y < 4 || y > 251;
                        tex.SetPixel(x, y, isBorder ? darkBorder : darkCenter);
                    }
                }
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(absPath, bytes);
                Object.DestroyImmediate(tex);

                AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = AssetImporter.GetAtPath(relPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(relPath);
        }

        private static void CreateAndSaveSplashScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObj = new("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
            cameraObj.AddComponent<AudioListener>();

            GameObject canvasObj = new("Splash Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Text title = CreateText("TitleText", canvasObj.transform, new Vector2(0f, 40f), 44, TextAnchor.MiddleCenter);
            title.text = "REALITY FRACTURES";
            title.color = new Color(0.36f, 0.86f, 0.9f, 1f);

            Text subtitle = CreateText("SubtitleText", canvasObj.transform, new Vector2(0f, -20f), 20, TextAnchor.MiddleCenter);
            subtitle.text = "Temporal Spatial AR Prototype";
            subtitle.color = new Color(0.8f, 0.8f, 0.85f, 0.8f);

            GameObject appFlowObj = new("App Flow Controller");
            appFlowObj.AddComponent<AppFlowController>();

            SaveSceneAsset(scene, SplashScenePath);
        }

        private static void CreateAndSaveMainMenuScene(Sprite panelBgSprite, Sprite menuBgArt)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObj = new("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.12f, 1f);
            cameraObj.AddComponent<AudioListener>();

            GameObject canvasObj = new("Main Menu Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Android vertical portrait resolution
            canvasObj.AddComponent<GraphicRaycaster>();

            // Fullscreen Sci-Fi Artwork Background for Android (.jpg)
            if (menuBgArt != null)
            {
                GameObject artBgObj = new("Artwork Background JPG");
                artBgObj.transform.SetParent(canvasObj.transform, false);
                artBgObj.transform.SetAsFirstSibling();

                RectTransform bgRect = artBgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.anchoredPosition = Vector2.zero;

                Image bgImg = artBgObj.AddComponent<Image>();
                bgImg.sprite = menuBgArt;
                bgImg.color = Color.white;
            }

            // Title Header
            Text title = CreateText("TitleText", canvasObj.transform, new Vector2(0f, 400f), 52, TextAnchor.MiddleCenter);
            title.text = "REALITY FRACTURES";
            title.color = new Color(0.36f, 0.86f, 0.9f, 1f);

            Text subtitle = CreateText("SubtitleText", canvasObj.transform, new Vector2(0f, 330f), 24, TextAnchor.MiddleCenter);
            subtitle.text = "Temporal Spatial AR Prototype";
            subtitle.color = new Color(0.7f, 0.85f, 0.95f, 0.9f);

            // Main Menu Panel
            GameObject mainPanel = CreatePanel("Main Menu Panel", canvasObj.transform, panelBgSprite, new Vector2(480f, 400f));
            Button startBtn = CreateButton("Start Game Button", mainPanel.transform, new Vector2(0f, 80f), "START NEW FRACTURE");
            Button settingsBtn = CreateButton("Settings Button", mainPanel.transform, new Vector2(0f, -10f), "SETTINGS");
            Button quitBtn = CreateButton("Quit Button", mainPanel.transform, new Vector2(0f, -100f), "QUIT");

            // Settings Panel
            GameObject settingsPanel = CreatePanel("Settings Panel", canvasObj.transform, panelBgSprite, new Vector2(560f, 520f));
            settingsPanel.SetActive(false);
            CreateText("SettingsTitle", settingsPanel.transform, new Vector2(0f, 200f), 32, TextAnchor.MiddleCenter).text = "SETTINGS";
            
            Toggle soundToggle = CreateToggle("Sound Toggle", settingsPanel.transform, new Vector2(0f, 110f), "Master Sound Effects");
            Toggle sfxToggle = CreateToggle("SFX Toggle", settingsPanel.transform, new Vector2(0f, 40f), "Ambient Audio FX");
            Toggle vfxToggle = CreateToggle("VFX Toggle", settingsPanel.transform, new Vector2(0f, -30f), "High Quality Visual FX");
            
            Button resetProgressBtn = CreateButton("Reset Button", settingsPanel.transform, new Vector2(0f, -110f), "RESET PROGRESS");
            Button closeSettingsBtn = CreateButton("Close Settings Button", settingsPanel.transform, new Vector2(0f, -185f), "BACK");

            // Quit Confirmation Panel
            GameObject quitPanel = CreatePanel("Quit Panel", canvasObj.transform, panelBgSprite, new Vector2(480f, 280f));
            quitPanel.SetActive(false);
            CreateText("QuitTitle", quitPanel.transform, new Vector2(0f, 60f), 28, TextAnchor.MiddleCenter).text = "Exit Application?";
            Button confirmQuitBtn = CreateButton("Confirm Quit Button", quitPanel.transform, new Vector2(-100f, -40f), "YES");
            Button cancelQuitBtn = CreateButton("Cancel Quit Button", quitPanel.transform, new Vector2(100f, -40f), "NO");

            // AppFlowController
            GameObject appFlowObj = new("App Flow Controller");
            AppFlowController appFlow = appFlowObj.AddComponent<AppFlowController>();

            SerializedObject serializedFlow = new(appFlow);
            serializedFlow.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
            serializedFlow.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serializedFlow.FindProperty("quitConfirmationPanel").objectReferenceValue = quitPanel;
            serializedFlow.FindProperty("soundToggle").objectReferenceValue = soundToggle;
            serializedFlow.FindProperty("sfxToggle").objectReferenceValue = sfxToggle;
            serializedFlow.FindProperty("vfxToggle").objectReferenceValue = vfxToggle;
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
            sfxToggle.onValueChanged.AddListener(appFlow.ToggleSFX);
            vfxToggle.onValueChanged.AddListener(appFlow.ToggleVFX);

            SaveSceneAsset(scene, MainMenuScenePath);
        }

        private static void CreateAndSaveARGameScene(Sprite panelBgSprite)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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

            CreateARGameUI(stateController, panelBgSprite);

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            SaveSceneAsset(scene, ARGameScenePath);
        }

        private static void SaveSceneAsset(Scene scene, string relativePath)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);

            bool saved = EditorSceneManager.SaveScene(activeScene, relativePath, false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string fullPathOnDisk = Path.Combine(Application.dataPath, relativePath.Substring(7)).Replace('\\', '/');
            Debug.Log($"[SceneBuilder] Saved ({relativePath}) | Save: {saved} | DiskExists: {File.Exists(fullPathOnDisk)}");
        }

        private static void CreateARGameUI(GameStateController stateController, Sprite panelBgSprite)
        {
            GameObject canvasObject = new("Minimal AR UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObject.AddComponent<GraphicRaycaster>();

            Text status = CreateText("Status", canvasObject.transform, new Vector2(0f, -120f), 32, TextAnchor.MiddleCenter);
            Text progress = CreateText("Progress", canvasObject.transform, new Vector2(0f, -180f), 24, TextAnchor.MiddleCenter);

            // Pause Overlay Panel
            GameObject pausePanel = CreatePanel("Pause Panel", canvasObject.transform, panelBgSprite, new Vector2(480f, 380f));
            pausePanel.SetActive(false);
            CreateText("PauseTitle", pausePanel.transform, new Vector2(0f, 130f), 36, TextAnchor.MiddleCenter).text = "GAME PAUSED";
            Button resumeBtn = CreateButton("Resume Button", pausePanel.transform, new Vector2(0f, 50f), "RESUME");
            Button restartBtn = CreateButton("Restart Button", pausePanel.transform, new Vector2(0f, -30f), "RESTART");
            Button mainMenuBtn = CreateButton("Main Menu Button", pausePanel.transform, new Vector2(0f, -110f), "MAIN MENU");

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

        private static GameObject CreatePanel(string name, Transform parent, Sprite bgSprite, Vector2 size)
        {
            GameObject panelObj = new(name);
            panelObj.transform.SetParent(parent, false);
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            Image img = panelObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.06f, 0.08f, 0.15f, 0.95f);
            return panelObj;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPos, string labelText)
        {
            GameObject buttonObj = new(name);
            buttonObj.transform.SetParent(parent, false);
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 65f);
            rect.anchoredPosition = anchoredPos;

            Image img = buttonObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.45f, 0.75f, 1f);

            Button btn = buttonObj.AddComponent<Button>();

            Text text = CreateText("Label", buttonObj.transform, Vector2.zero, 22, TextAnchor.MiddleCenter);
            text.text = labelText;
            text.color = Color.white;

            return btn;
        }

        private static Toggle CreateToggle(string name, Transform parent, Vector2 anchoredPos, string labelText)
        {
            GameObject toggleObj = new(name);
            toggleObj.transform.SetParent(parent, false);
            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 50f);
            rect.anchoredPosition = anchoredPos;

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            Text text = CreateText("Label", toggleObj.transform, Vector2.zero, 22, TextAnchor.MiddleCenter);
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
            rect.sizeDelta = new Vector2(700f, 60f);
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
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            Material material = new(shader);
            material.name = name;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
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
    }
}
