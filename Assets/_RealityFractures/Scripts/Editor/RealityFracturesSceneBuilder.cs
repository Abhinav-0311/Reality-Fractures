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
        private const string ScenePath = "Assets/_RealityFractures/Scenes/RealityFractures_ARScene.unity";

        [MenuItem("Reality Fractures/Create AR MVP Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RealityFractures_ARScene";

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
            GameObject fracturePrefab = PrefabUtility.SaveAsPrefabAsset(fracturePrototype, "Assets/_RealityFractures/Prefabs/FractureRoot_Prototype.prefab");
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

            CreateUi(stateController);

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
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
            GameObject root = new("FractureRoot_Prototype");
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

        private static void CreateUi(GameStateController stateController)
        {
            GameObject canvasObject = new("Minimal AR UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            Text status = CreateText("Status", canvasObject.transform, new Vector2(0f, -90f), 28, TextAnchor.MiddleCenter);
            Text progress = CreateText("Progress", canvasObject.transform, new Vector2(0f, -132f), 22, TextAnchor.MiddleCenter);

            MinimalARUIController ui = canvasObject.AddComponent<MinimalARUIController>();
            SerializedObject uiSerialized = new(ui);
            uiSerialized.FindProperty("gameState").objectReferenceValue = stateController;
            uiSerialized.FindProperty("statusText").objectReferenceValue = status;
            uiSerialized.FindProperty("progressText").objectReferenceValue = progress;
            uiSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, int size, TextAnchor anchor)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(720f, 52f);
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
    }
}
