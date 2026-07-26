using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealityFractures
{
    /// <summary>
    /// Handles the AR 3D Puzzle Mode transition:
    /// - Uses standard shaders (NEVER magenta pink!).
    /// - Spawns Riddle Stones (Past), Sliding Cat (Present), and Alien Tetrahedron (Future).
    /// - Clean UI with 0 overlap (Back button below Zoom buttons, Solve button above Bottom bar).
    /// </summary>
    public sealed class Temporal3DPuzzleTransitionController : MonoBehaviour
    {
        private static Temporal3DPuzzleTransitionController instance;

        private GameObject puzzleContainer;
        private GameObject active3DPuzzleInstance;
        private GameObject backButtonObj;
        private GameObject solveButtonObj;
        private Text statusText;
        private Action onPuzzleSolvedCallback;
        private bool isPuzzleActive = false;
        private bool isSolved = false;
        private TimeLayer activeLayer;

        // Interactive 3D rotation state
        private bool isDragging = false;
        private Vector3 lastMousePos;

        public static void EnterPuzzleMode(TimeLayer layer, Action onSolved)
        {
            EnsureEventSystem();

            if (instance == null)
            {
                GameObject go = new("Temporal3DPuzzleTransitionController_Singleton");
                instance = go.AddComponent<Temporal3DPuzzleTransitionController>();
            }

            instance.StartPuzzleMode(layer, onSolved);
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esGo = new("AR_EventSystem");
                es = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private void StartPuzzleMode(TimeLayer layer, Action onSolved)
        {
            if (isPuzzleActive) return;

            activeLayer = layer;
            onPuzzleSolvedCallback = onSolved;
            isPuzzleActive = true;
            isSolved = false;

            // Step 1: Temporarily hide the main AR table/world so player focuses on 3D puzzle
            FractureWorldController worldCtrl = FindFirstObjectByType<FractureWorldController>();
            if (worldCtrl != null)
            {
                worldCtrl.gameObject.SetActive(false);
            }

            // Step 2: Ensure UI Back Button, Solve Button, and Status Text exist
            CreatePuzzleUI();
            if (backButtonObj != null) backButtonObj.SetActive(true);
            if (solveButtonObj != null) solveButtonObj.SetActive(true);

            // Step 3: Play audio transition chime
            var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
            if (audioFX != null)
            {
                audioFX.PlayOrbCollectSound();
            }

            // Step 4: Spawn the corresponding 3D Puzzle Model with rich Standard metallic materials
            Spawn3DPuzzleModel(layer);
        }

        public void ReturnToMainWorld()
        {
            if (!isPuzzleActive) return;

            isPuzzleActive = false;
            isSolved = false;

            // Destroy active 3D puzzle
            if (active3DPuzzleInstance != null)
            {
                Destroy(active3DPuzzleInstance);
                active3DPuzzleInstance = null;
            }

            // Hide UI buttons and status text
            if (backButtonObj != null) backButtonObj.SetActive(false);
            if (solveButtonObj != null) solveButtonObj.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);

            // Unhide main AR world
            FractureWorldController worldCtrl = FindFirstObjectByType<FractureWorldController>(FindObjectsInactive.Include);
            if (worldCtrl != null)
            {
                worldCtrl.gameObject.SetActive(true);
            }
        }

        private void Spawn3DPuzzleModel(TimeLayer layer)
        {
            string assetPath = layer switch
            {
                TimeLayer.Past => "Assets/_RealityFractures/Art/3DModels/Puzzles/Past_RiddleStones/riddle_stones_-_mystery_puzzle.fbx",
                TimeLayer.Present => "Assets/_RealityFractures/Art/3DModels/Puzzles/Present_SlidingCat/sliding_cat_puzzle.fbx",
                _ => "Assets/_RealityFractures/Art/3DModels/Puzzles/Future_AlienTetrahedron/alien_tetrahedron_puzzle.fbx"
            };

#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            }
#else
            GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
#endif

            if (puzzleContainer == null)
            {
                puzzleContainer = new GameObject("AR_3DPuzzleContainer");
            }

            // Position container 45cm in front of camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                puzzleContainer.transform.position = mainCam.transform.position + mainCam.transform.forward * 0.45f;
                puzzleContainer.transform.rotation = Quaternion.LookRotation(mainCam.transform.forward, Vector3.up);
            }
            else
            {
                puzzleContainer.transform.position = new Vector3(0f, 0.10f, 0.45f);
                puzzleContainer.transform.rotation = Quaternion.identity;
            }

            active3DPuzzleInstance = (GameObject)Instantiate(prefab, puzzleContainer.transform);
            active3DPuzzleInstance.name = layer + "_3D_Puzzle_Model";
            active3DPuzzleInstance.transform.localPosition = Vector3.zero;
            active3DPuzzleInstance.transform.localRotation = Quaternion.Euler(-30f, 180f, 0f);

            // Create rich metallic Standard shader material (NEVER use URP Lit, prevents pink!)
            Color baseColor = layer switch
            {
                TimeLayer.Past => new Color(0.85f, 0.65f, 0.25f, 1f),    // Rich Ancient Runes Bronze Gold
                TimeLayer.Present => new Color(0.20f, 0.90f, 0.55f, 1f), // Polished Quantum Jade Emerald
                _ => new Color(0.18f, 0.82f, 0.98f, 1f)                  // Cyber Crystalline Azure
            };
            Color emitColor = baseColor * 0.35f;

            Shader standardShader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            Material puzzleMat = new Material(standardShader);
            puzzleMat.name = layer + "_3D_Puzzle_RichMat";
            puzzleMat.color = baseColor;
            if (puzzleMat.HasProperty("_Color")) puzzleMat.SetColor("_Color", baseColor);
            if (puzzleMat.HasProperty("_EmissionColor"))
            {
                puzzleMat.EnableKeyword("_EMISSION");
                puzzleMat.SetColor("_EmissionColor", emitColor);
            }
            if (puzzleMat.HasProperty("_Glossiness")) puzzleMat.SetFloat("_Glossiness", 0.70f);
            if (puzzleMat.HasProperty("_Metallic")) puzzleMat.SetFloat("_Metallic", 0.55f);

            Renderer[] renderers = active3DPuzzleInstance.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(active3DPuzzleInstance.transform.position, Vector3.zero);
            bool hasBounds = false;
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = puzzleMat;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds && bounds.size.magnitude > 0.001f)
            {
                float targetSize = 0.30f; // Prominent 30cm interactive 3D model
                float scaleFactor = targetSize / bounds.size.magnitude;
                active3DPuzzleInstance.transform.localScale *= scaleFactor;
            }

            // Attach guaranteed SphereCollider to root so tapping model ALWAYS works
            SphereCollider sphereCol = active3DPuzzleInstance.AddComponent<SphereCollider>();
            sphereCol.radius = 0.65f;
            sphereCol.isTrigger = false;

            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
                string puzzleTitle = layer switch
                {
                    TimeLayer.Past => "🏛️ ANCIENT RIDDLE STONES | Swipe/Drag to Rotate & Align Runes",
                    TimeLayer.Present => "⚡ QUANTUM SLIDING CAT | Swipe/Drag to Synchronize Phase",
                    _ => "💎 ALIEN TETRAHEDRON | Swipe/Drag to Decrypt Cyber Lock"
                };
                statusText.text = puzzleTitle + "\n(Swipe to Rotate 3D Model | Press 'SOLVE & COLLECT FRAGMENT' below when aligned!)";
            }
        }

        private void Update()
        {
            if (!isPuzzleActive || active3DPuzzleInstance == null || isSolved) return;

            // Allow user to rotate 3D puzzle by dragging with mouse / touch
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging)
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                active3DPuzzleInstance.transform.Rotate(Vector3.up, -delta.x * 0.45f, Space.World);
                active3DPuzzleInstance.transform.Rotate(Vector3.right, delta.y * 0.45f, Space.World);
                lastMousePos = Input.mousePosition;
            }
            else
            {
                // Gentle mystical levitation rotation when idle
                active3DPuzzleInstance.transform.Rotate(Vector3.up, 12f * Time.deltaTime, Space.World);
            }

            // Tap on 3D puzzle model to trigger a stone ring spin / mechanical sound
            if (Input.GetMouseButtonUp(0) && !isDragging)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.transform.IsChildOf(active3DPuzzleInstance.transform) || hit.transform == active3DPuzzleInstance.transform)
                    {
                        var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
                        if (audioFX != null) audioFX.PlayOrbCollectSound();
                        active3DPuzzleInstance.transform.Rotate(Vector3.up, 45f, Space.Self);
                    }
                }
            }
        }

        public void OnPuzzleSolved()
        {
            if (isSolved) return;

            isSolved = true;
            if (statusText != null)
            {
                statusText.text = "✨ CONDUIT ALIGNED! RETURNING TO MAIN WORLD TO COLLECT FRAGMENT... ✨";
                statusText.color = new Color(1.0f, 0.9f, 0.3f, 1f);
            }

            var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
            if (audioFX != null)
            {
                audioFX.PlayPuzzleSolveSound();
            }

            StartCoroutine(SolvedTransitionRoutine());
        }

        private IEnumerator SolvedTransitionRoutine()
        {
            // Spin model in triumph
            float elapsed = 0f;
            while (elapsed < 1.3f)
            {
                elapsed += Time.deltaTime;
                if (active3DPuzzleInstance != null)
                {
                    active3DPuzzleInstance.transform.Rotate(Vector3.up, 240f * Time.deltaTime, Space.World);
                }
                yield return null;
            }

            ReturnToMainWorld();
            onPuzzleSolvedCallback?.Invoke();
        }

        private void CreatePuzzleUI()
        {
            if (backButtonObj != null && solveButtonObj != null) return;

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new("AR_PuzzleUICanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // 1. "◄ BACK TO MAIN WORLD" button at Top-Left BELOW Zoom buttons (Y = 0.73 to 0.81, X = 0.03 to 0.22)
            if (backButtonObj == null)
            {
                backButtonObj = new GameObject("BackToMainWorldBtn");
                backButtonObj.transform.SetParent(canvas.transform, false);
                RectTransform backRect = backButtonObj.AddComponent<RectTransform>();
                backRect.anchorMin = new Vector2(0.03f, 0.73f);
                backRect.anchorMax = new Vector2(0.22f, 0.81f);
                backRect.offsetMin = Vector2.zero;
                backRect.offsetMax = Vector2.zero;

                Image bg = backButtonObj.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

                Outline outl = backButtonObj.AddComponent<Outline>();
                outl.effectColor = new Color(0.3f, 0.85f, 1.0f, 0.9f);
                outl.effectDistance = new Vector2(2f, 2f);

                Button btn = backButtonObj.AddComponent<Button>();
                btn.onClick.AddListener(ReturnToMainWorld);

                GameObject txtObj = new("BackText");
                txtObj.transform.SetParent(backButtonObj.transform, false);
                RectTransform txtRect = txtObj.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;

                Text txt = txtObj.AddComponent<Text>();
                txt.text = "◄  BACK TO MAIN WORLD";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 17;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;

                backButtonObj.SetActive(false);
            }

            // 2. "✨ SOLVE PUZZLE & COLLECT FRAGMENT" button safely ABOVE Bottom Bar (Y = 0.16 to 0.24, X = 0.28 to 0.72)
            if (solveButtonObj == null)
            {
                solveButtonObj = new GameObject("SolveAndCollectBtn");
                solveButtonObj.transform.SetParent(canvas.transform, false);
                RectTransform solveRect = solveButtonObj.AddComponent<RectTransform>();
                solveRect.anchorMin = new Vector2(0.28f, 0.16f);
                solveRect.anchorMax = new Vector2(0.72f, 0.24f);
                solveRect.offsetMin = Vector2.zero;
                solveRect.offsetMax = Vector2.zero;

                Image sbg = solveButtonObj.AddComponent<Image>();
                sbg.color = new Color(0.12f, 0.38f, 0.22f, 0.98f);

                Outline soutl = solveButtonObj.AddComponent<Outline>();
                soutl.effectColor = new Color(0.3f, 1.0f, 0.5f, 0.95f);
                soutl.effectDistance = new Vector2(2.5f, 2.5f);

                Button sbtn = solveButtonObj.AddComponent<Button>();
                sbtn.onClick.AddListener(OnPuzzleSolved);

                GameObject stxtObj = new("SolveText");
                stxtObj.transform.SetParent(solveButtonObj.transform, false);
                RectTransform stxtRect = stxtObj.AddComponent<RectTransform>();
                stxtRect.anchorMin = Vector2.zero;
                stxtRect.anchorMax = Vector2.one;

                Text stxt = stxtObj.AddComponent<Text>();
                stxt.text = "✨  SOLVE PUZZLE & COLLECT FRAGMENT";
                stxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                stxt.fontSize = 19;
                stxt.alignment = TextAnchor.MiddleCenter;
                stxt.color = Color.white;

                solveButtonObj.SetActive(false);
            }

            // 3. Crisp White Status Instruction Text at Top Center (Y = 0.80 to 0.89)
            if (statusText == null)
            {
                GameObject statusObj = new("PuzzleStatusText");
                statusObj.transform.SetParent(canvas.transform, false);
                RectTransform statusRect = statusObj.AddComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0.12f, 0.80f);
                statusRect.anchorMax = new Vector2(0.88f, 0.89f);
                statusText = statusObj.AddComponent<Text>();
                statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statusText.fontSize = 20;
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.color = Color.white;
                statusText.gameObject.SetActive(false);
            }
        }
    }
}
