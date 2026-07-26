using System.IO;
using RealityFractures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.EventSystems;

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

        private static Sprite LoadSpriteAsset(string relPath)
        {
            string absPath = Path.Combine(Application.dataPath, relPath.Substring(7));
            if (!File.Exists(absPath)) return null;

            AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(relPath) is TextureImporter importer)
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(relPath);
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

            CreateEventSystem();

            GameObject canvasObj = new("Splash Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
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

            CreateEventSystem();

            GameObject canvasObj = new("Main Menu Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // Android horizontal landscape resolution
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
            Text title = CreateText("TitleText", canvasObj.transform, new Vector2(0f, 320f), 52, TextAnchor.MiddleCenter);
            title.text = "REALITY FRACTURES";
            title.color = new Color(0.36f, 0.86f, 0.9f, 1f);

            Text subtitle = CreateText("SubtitleText", canvasObj.transform, new Vector2(0f, 250f), 24, TextAnchor.MiddleCenter);
            subtitle.text = "Temporal Spatial AR Prototype";
            subtitle.color = new Color(0.7f, 0.85f, 0.95f, 0.9f);

            // Main Menu Panel
            GameObject mainPanel = CreatePanel("Main Menu Panel", canvasObj.transform, panelBgSprite, new Vector2(560f, 420f));
            Button startBtn = CreateButton("Start Game Button", mainPanel.transform, new Vector2(0f, 80f), "START NEW FRACTURE");
            Button settingsBtn = CreateButton("Settings Button", mainPanel.transform, new Vector2(0f, -10f), "SETTINGS");
            Button quitBtn = CreateButton("Quit Button", mainPanel.transform, new Vector2(0f, -100f), "QUIT");

            // Settings Panel
            GameObject settingsPanel = CreatePanel("Settings Panel", canvasObj.transform, panelBgSprite, new Vector2(640f, 520f));
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
            serializedFlow.FindProperty("startButton").objectReferenceValue = startBtn;
            serializedFlow.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            serializedFlow.FindProperty("quitButton").objectReferenceValue = quitBtn;
            serializedFlow.FindProperty("closeSettingsButton").objectReferenceValue = closeSettingsBtn;
            serializedFlow.FindProperty("resetProgressButton").objectReferenceValue = resetProgressBtn;
            serializedFlow.FindProperty("confirmQuitButton").objectReferenceValue = confirmQuitBtn;
            serializedFlow.FindProperty("cancelQuitButton").objectReferenceValue = cancelQuitBtn;
            serializedFlow.FindProperty("soundToggle").objectReferenceValue = soundToggle;
            serializedFlow.FindProperty("sfxToggle").objectReferenceValue = sfxToggle;
            serializedFlow.FindProperty("vfxToggle").objectReferenceValue = vfxToggle;
            Text startBtnText = startBtn.GetComponentInChildren<Text>();
            if (startBtnText != null)
            {
                serializedFlow.FindProperty("startButtonLabel").objectReferenceValue = startBtnText;
            }
            serializedFlow.ApplyModifiedPropertiesWithoutUndo();

            // Wire Buttons (Runtime + Persistent Editor Listeners)
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

            UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, appFlow.LoadARGame);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsBtn.onClick, appFlow.OpenSettings);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeSettingsBtn.onClick, appFlow.CloseSettings);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resetProgressBtn.onClick, appFlow.ResetProgress);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick, appFlow.OpenQuitConfirmation);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(cancelQuitBtn.onClick, appFlow.CloseQuitConfirmation);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(confirmQuitBtn.onClick, appFlow.ConfirmQuitApp);

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
            ARAnchorManager anchorManager = xrOriginObject.AddComponent<ARAnchorManager>();

            GameObject planeVisualPrototype = CreatePlaneVisual();
            GameObject planeVisualPrefab = PrefabUtility.SaveAsPrefabAsset(planeVisualPrototype, "Assets/_RealityFractures/Prefabs/PlaneVisual.prefab");
            Object.DestroyImmediate(planeVisualPrototype);
            planeManager.planePrefab = planeVisualPrefab;

            GameObject indicatorPrototype = CreatePlacementIndicator();
            GameObject placementIndicatorPrefab = PrefabUtility.SaveAsPrefabAsset(indicatorPrototype, "Assets/_RealityFractures/Prefabs/PlacementIndicator.prefab");
            Object.DestroyImmediate(indicatorPrototype);
            GameObject placementIndicator = (GameObject)PrefabUtility.InstantiatePrefab(placementIndicatorPrefab);
            placementIndicator.name = "Placement Indicator";
            placementIndicator.SetActive(false);

            CreateEnergyFragmentPrefab();

            GameObject fracturePrototype = CreateFracturePrototype();
            GameObject fracturePrefab = PrefabUtility.SaveAsPrefabAsset(fracturePrototype, "Assets/_RealityFractures/Prefabs/FractureRoot.prefab");
            Object.DestroyImmediate(fracturePrototype);

            GameObject placementSystem = new("Placement System");
            ARPlacementController placement = placementSystem.AddComponent<ARPlacementController>();
            SerializedObject placementSerialized = new(placement);
            placementSerialized.FindProperty("raycastManager").objectReferenceValue = raycastManager;
            placementSerialized.FindProperty("planeManager").objectReferenceValue = planeManager;
            placementSerialized.FindProperty("anchorManager").objectReferenceValue = anchorManager;
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
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObject.AddComponent<GraphicRaycaster>();

            CreateEventSystem();

            Text status = CreateText("Status", canvasObject.transform, new Vector2(0f, -70f), 28, TextAnchor.MiddleCenter);
            Text progress = CreateText("Progress", canvasObject.transform, new Vector2(0f, -130f), 22, TextAnchor.MiddleCenter);
            SetUIAnchor(status.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f));
            SetUIAnchor(progress.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -95f));

            // Time-Layer Shifting Buttons (Anchored to Bottom Left, Center, Right so they NEVER get cut off on mobile safe areas)
            Button pastBtn = CreateButton("Past Button", canvasObject.transform, Vector2.zero, "◄ PAST (AMBER)", new Vector2(300f, 65f));
            Button presentBtn = CreateButton("Present Button", canvasObject.transform, Vector2.zero, "PRESENT (EMERALD)", new Vector2(300f, 65f));
            Button futureBtn = CreateButton("Future Button", canvasObject.transform, Vector2.zero, "FUTURE (CYAN) ►", new Vector2(300f, 65f));
            SetUIAnchor(pastBtn.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 35f));
            SetUIAnchor(presentBtn.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 35f));
            SetUIAnchor(futureBtn.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 35f));

            pastBtn.onClick.AddListener(stateController.SelectPastLayer);
            presentBtn.onClick.AddListener(stateController.SelectPresentLayer);
            futureBtn.onClick.AddListener(stateController.SelectFutureLayer);

            // HUD Buttons
            Button pauseBtn = CreateButton("Pause Button", canvasObject.transform, Vector2.zero, "|| PAUSE", new Vector2(160f, 60f));
            SetUIAnchor(pauseBtn.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f));

            Button zoomInBtn = CreateButton("Zoom In Button", canvasObject.transform, Vector2.zero, "+ ZOOM IN", new Vector2(170f, 55f));
            Button zoomOutBtn = CreateButton("Zoom Out Button", canvasObject.transform, Vector2.zero, "- ZOOM OUT", new Vector2(170f, 55f));
            SetUIAnchor(zoomInBtn.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f));
            SetUIAnchor(zoomOutBtn.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -95f));

            // Pause Overlay Panel
            GameObject pausePanel = CreatePanel("Pause Panel", canvasObject.transform, panelBgSprite, new Vector2(560f, 440f));
            pausePanel.SetActive(false);
            CreateText("PauseTitle", pausePanel.transform, new Vector2(0f, 140f), 36, TextAnchor.MiddleCenter).text = "GAME PAUSED";
            Button resumeBtn = CreateButton("Resume Button", pausePanel.transform, new Vector2(0f, 50f), "RESUME");
            Button settingsBtn = CreateButton("Settings Button", pausePanel.transform, new Vector2(0f, -30f), "SETTINGS");
            Button mainMenuBtn = CreateButton("Main Menu Button", pausePanel.transform, new Vector2(0f, -110f), "MAIN MENU");

            // In-Game Settings Panel
            GameObject settingsPanel = CreatePanel("InGame Settings Panel", canvasObject.transform, panelBgSprite, new Vector2(600f, 460f));
            settingsPanel.SetActive(false);
            CreateText("SettingsTitle", settingsPanel.transform, new Vector2(0f, 160f), 32, TextAnchor.MiddleCenter).text = "SETTINGS";
            CreateToggle("Sound Toggle", settingsPanel.transform, new Vector2(0f, 80f), "Master Sound Effects");
            CreateToggle("SFX Toggle", settingsPanel.transform, new Vector2(0f, 10f), "Ambient Audio FX");
            CreateToggle("VFX Toggle", settingsPanel.transform, new Vector2(0f, -60f), "High Quality Visual FX");
            Button closeSettingsBtn = CreateButton("Close Settings Button", settingsPanel.transform, new Vector2(0f, -150f), "BACK");

            MinimalARUIController ui = canvasObject.AddComponent<MinimalARUIController>();
            SerializedObject uiSerialized = new(ui);
            uiSerialized.FindProperty("gameState").objectReferenceValue = stateController;
            uiSerialized.FindProperty("statusText").objectReferenceValue = status;
            uiSerialized.FindProperty("progressText").objectReferenceValue = progress;
            uiSerialized.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            uiSerialized.FindProperty("inGameSettingsPanel").objectReferenceValue = settingsPanel;
            uiSerialized.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            uiSerialized.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            uiSerialized.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            uiSerialized.FindProperty("closeSettingsButton").objectReferenceValue = closeSettingsBtn;
            uiSerialized.FindProperty("zoomInButton").objectReferenceValue = zoomInBtn;
            uiSerialized.FindProperty("zoomOutButton").objectReferenceValue = zoomOutBtn;
            uiSerialized.FindProperty("mainMenuButton").objectReferenceValue = mainMenuBtn;
            uiSerialized.FindProperty("pastButton").objectReferenceValue = pastBtn;
            uiSerialized.FindProperty("presentButton").objectReferenceValue = presentBtn;
            uiSerialized.FindProperty("futureButton").objectReferenceValue = futureBtn;
            uiSerialized.ApplyModifiedPropertiesWithoutUndo();

            pauseBtn.onClick.AddListener(ui.TogglePause);
            resumeBtn.onClick.AddListener(ui.ResumeGame);
            settingsBtn.onClick.AddListener(ui.OpenInGameSettings);
            closeSettingsBtn.onClick.AddListener(ui.CloseInGameSettings);
            mainMenuBtn.onClick.AddListener(ui.ReturnToMainMenu);

            UnityEditor.Events.UnityEventTools.AddPersistentListener(pauseBtn.onClick, ui.TogglePause);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resumeBtn.onClick, ui.ResumeGame);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsBtn.onClick, ui.OpenInGameSettings);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeSettingsBtn.onClick, ui.CloseInGameSettings);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(mainMenuBtn.onClick, ui.ReturnToMainMenu);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(pastBtn.onClick, stateController.SelectPastLayer);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(presentBtn.onClick, stateController.SelectPresentLayer);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(futureBtn.onClick, stateController.SelectFutureLayer);
        }

        private static void SetUIAnchor(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.anchoredPosition = anchoredPosition;
            }
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

        private static GameObject CreatePlaneVisual()
        {
            GameObject planeObj = new("PlaneVisual");
            planeObj.AddComponent<ARPlane>();
            planeObj.AddComponent<MeshCollider>();
            planeObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = planeObj.AddComponent<MeshRenderer>();
            planeObj.AddComponent<ARPlaneMeshVisualizer>();

            renderer.sharedMaterial = CreateMaterial("RF_PlaneVisual_Mat", new Color(0.2f, 0.8f, 0.9f, 0.25f));
            return planeObj;
        }

        private static void CreateEnergyFragmentPrefab()
        {
            GameObject fragmentPrototype = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fragmentPrototype.name = "EnergyFragment";
            fragmentPrototype.transform.localScale = Vector3.one * 0.07f;
            fragmentPrototype.GetComponent<Renderer>().sharedMaterial = CreateMaterial("RF_Fragment_Base", new Color(0.9f, 0.9f, 1f, 1f));
            fragmentPrototype.AddComponent<CollectibleFragment>();
            PrefabUtility.SaveAsPrefabAsset(fragmentPrototype, "Assets/_RealityFractures/Prefabs/EnergyFragment.prefab");
            Object.DestroyImmediate(fragmentPrototype);
        }

        private static GameObject CreateFracturePrototype()
        {
            GameObject root = new("FractureRoot");
            root.transform.localScale = Vector3.one * 0.95f;

            FractureWorldController worldController = root.AddComponent<FractureWorldController>();
            root.AddComponent<TemporalPuzzleController>();
            root.AddComponent<ProceduralAudioFX>();
            root.AddComponent<AnomalyTouchController>();

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

            string meshyFbxPath = layer switch
            {
                TimeLayer.Past => "Assets/_RealityFractures/Art/Models/Meshy/Past/Meshy_AI_Astral_Obelisk_on_a_R_0726091509_texture_fbx/Meshy_AI_Astral_Obelisk_on_a_R_0726091509_texture.fbx",
                TimeLayer.Present => "Assets/_RealityFractures/Art/Models/Meshy/Present/Meshy_AI_Emerald_Quantum_Core_0726092051_texture_fbx/Meshy_AI_Emerald_Quantum_Core_0726092051_texture.fbx",
                _ => "Assets/_RealityFractures/Art/Models/Meshy/Future/Meshy_AI_Azure_Crystal_Citadel_0726092821_texture_fbx/Meshy_AI_Azure_Crystal_Citadel_0726092821_texture.fbx"
            };

            string meshyTexPath = layer switch
            {
                TimeLayer.Past => "Assets/_RealityFractures/Art/Models/Meshy/Past/Meshy_AI_Astral_Obelisk_on_a_R_0726091509_texture_fbx/Meshy_AI_Astral_Obelisk_on_a_R_0726091509_texture.png",
                TimeLayer.Present => "Assets/_RealityFractures/Art/Models/Meshy/Present/Meshy_AI_Emerald_Quantum_Core_0726092051_texture_fbx/Meshy_AI_Emerald_Quantum_Core_0726092051_texture.png",
                _ => "Assets/_RealityFractures/Art/Models/Meshy/Future/Meshy_AI_Azure_Crystal_Citadel_0726092821_texture_fbx/Meshy_AI_Azure_Crystal_Citadel_0726092821_texture.png"
            };

            GameObject meshyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(meshyFbxPath);
            Texture2D meshyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(meshyTexPath);

            if (meshyPrefab != null)
            {
                GameObject meshyInst = (GameObject)PrefabUtility.InstantiatePrefab(meshyPrefab, layerRoot.transform);
                meshyInst.name = "Meshy " + layer + " Asset";
                meshyInst.transform.localPosition = Vector3.zero;
                meshyInst.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                meshyInst.transform.localScale = Vector3.one * 15.0f;

                Renderer[] renderers = meshyInst.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    Material m = CreateMaterial("RF_Meshy_" + layer + "_" + r.name, Color.white);
                    m.color = Color.white;
                    if (meshyTex != null)
                    {
                        m.mainTexture = meshyTex;
                    }
                    r.sharedMaterial = m;
                    if (r.gameObject.GetComponent<Collider>() == null)
                    {
                        r.gameObject.AddComponent<MeshCollider>().convex = true;
                    }
                }

                if (layer == TimeLayer.Past)
                {
                    meshyInst.AddComponent<TemporalLever>();
                }
                else if (layer == TimeLayer.Present)
                {
                    meshyInst.AddComponent<PresentChronoDevice>();
                }
                else if (layer == TimeLayer.Future)
                {
                    meshyInst.AddComponent<CyberneticTerminal>();
                }
            }

            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fragment.name = layer + " Fragment";
            fragment.transform.SetParent(layerRoot.transform, false);
            fragment.transform.localPosition = new Vector3(0f, 0.04f, -0.055f);
            fragment.transform.localScale = Vector3.one * 0.035f;
            fragment.GetComponent<Renderer>().sharedMaterial = CreateMaterial("RF_" + layer + "_Fragment", Color.Lerp(color, Color.white, 0.35f));

            CollectibleFragment collectible = fragment.AddComponent<CollectibleFragment>();
            SerializedObject collectibleSerialized = new(collectible);
            collectibleSerialized.FindProperty("layer").enumValueIndex = (int)layer;
            collectibleSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject barrierObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            barrierObj.name = layer + " Forcefield Barrier";
            barrierObj.transform.SetParent(layerRoot.transform, false);
            barrierObj.transform.localPosition = new Vector3(0f, 0.04f, -0.055f);
            barrierObj.transform.localScale = Vector3.one * 0.052f;
            barrierObj.GetComponent<Renderer>().sharedMaterial = CreateMaterial("RF_" + layer + "_Barrier_Mat", new Color(color.r, color.g, color.b, 0.45f));
            TemporalBarrier barrier = barrierObj.AddComponent<TemporalBarrier>();
            SerializedObject barrierSerialized = new(barrier);
            barrierSerialized.FindProperty("protectedLayer").enumValueIndex = (int)layer;
            barrierSerialized.ApplyModifiedPropertiesWithoutUndo();

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

        private static void CreateWorldLayerDecorations(Transform layerRoot, TimeLayer layer, Color color, Material material)
        {
            switch (layer)
            {
                case TimeLayer.Past:
                    // Create 2 ancient temple pillars and ruined arch
                    CreateDecorationCube("TemplePillarLeft", layerRoot, new Vector3(-0.25f, 0.15f, -0.1f), new Vector3(0.06f, 0.3f, 0.06f), material);
                    CreateDecorationCube("TemplePillarRight", layerRoot, new Vector3(0.25f, 0.15f, -0.1f), new Vector3(0.06f, 0.3f, 0.06f), material);
                    CreateDecorationCube("FallenArch", layerRoot, new Vector3(0f, 0.02f, -0.15f), new Vector3(0.4f, 0.04f, 0.08f), material);
                    break;
                case TimeLayer.Present:
                    // Create floating shattered debris
                    CreateDecorationCube("Debris1", layerRoot, new Vector3(-0.2f, 0.12f, 0.15f), new Vector3(0.07f, 0.07f, 0.07f), material, Quaternion.Euler(15f, 45f, 10f));
                    CreateDecorationCube("Debris2", layerRoot, new Vector3(0.18f, 0.25f, -0.12f), new Vector3(0.05f, 0.09f, 0.06f), material, Quaternion.Euler(-20f, 30f, 45f));
                    CreateDecorationCube("Debris3", layerRoot, new Vector3(-0.1f, 0.32f, -0.18f), new Vector3(0.06f, 0.05f, 0.08f), material, Quaternion.Euler(60f, 10f, -15f));
                    break;
                case TimeLayer.Future:
                    // Create cybernetic containment obelisks
                    CreateDecorationCube("ContainmentObelisk1", layerRoot, new Vector3(-0.22f, 0.18f, 0.22f), new Vector3(0.04f, 0.36f, 0.04f), material);
                    CreateDecorationCube("ContainmentObelisk2", layerRoot, new Vector3(0.22f, 0.18f, 0.22f), new Vector3(0.04f, 0.36f, 0.04f), material);
                    CreateDecorationCube("ContainmentObelisk3", layerRoot, new Vector3(0f, 0.18f, -0.28f), new Vector3(0.04f, 0.36f, 0.04f), material);
                    break;
            }
        }

        private static void CreateDecorationCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Quaternion rot = default)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = pos;
            cube.transform.localRotation = rot == default ? Quaternion.identity : rot;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObj = new("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();

            bool addedNewModule = false;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type modType = assembly.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
                if (modType != null)
                {
                    eventSystemObj.AddComponent(modType);
                    addedNewModule = true;
                    break;
                }
            }

            if (!addedNewModule)
            {
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
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

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPos, string labelText, Vector2 customSize = default)
        {
            GameObject buttonObj = new(name);
            buttonObj.transform.SetParent(parent, false);
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = customSize == default ? new Vector2(360f, 65f) : customSize;
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
            EnsureAssetFolders();
            string matPath = $"Assets/_RealityFractures/Materials/{name}.mat";
            Shader shader = Shader.Find("Standard")
                ?? Shader.Find("Legacy Shaders/Diffuse")
                ?? Shader.Find("Mobile/Diffuse")
                ?? Shader.Find("Unlit/Color");

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                existing.shader = shader;
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Material material = new(shader);
            material.name = name;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            AssetDatabase.CreateAsset(material, matPath);
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
