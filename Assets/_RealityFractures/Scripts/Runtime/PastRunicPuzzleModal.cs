using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealityFractures
{
    /// <summary>
    /// Transforms the Past World puzzle into a AAA archaeological Aztec/Mayan Concentric Relic Astrolabe
    /// with procedural circular transparent stone gear sprites, true circular layout, and zero UI overlap.
    /// </summary>
    public sealed class PastRunicPuzzleModal : MonoBehaviour
    {
        private static PastRunicPuzzleModal instance;

        private GameObject modalPanel;
        private Text titleText;
        private Text statusText;
        private RectTransform[] ringTransforms = new RectTransform[3];
        private Image[] ringImages = new Image[3];
        private Image[] alignmentJewels = new Image[3];

        // Each ring rotates in 90-degree increments (0, 1, 2, 3 -> 0°, 90°, 180°, 270°)
        // Target is index 0 (0 degrees: North aligned!)
        private int[] currentRingState = new int[3];
        private readonly int[] targetRingState = new int[] { 0, 0, 0 };

        private readonly string[] ringNames = new string[] {
            "☀️ SOLARIS (Outer Gear)",
            "🌙 LUNARIS (Middle Ring)",
            "⭐ ASTRALIS (Core Medallion)"
        };

        private readonly Color[] jewelColors = new Color[] {
            new Color(1.0f, 0.85f, 0.25f, 1f), // Sun: Glowing Gold
            new Color(0.75f, 0.90f, 1.0f, 1f), // Moon: Celestial Silver
            new Color(1.0f, 0.65f, 0.20f, 1f)  // Star: Quantum Amber
        };

        private Action onSolvedCallback;
        private bool isSolved = false;
        private bool[] isSpinning = new bool[3];

        // Procedural Circular Sprites cached
        private static Sprite outerRingSprite;
        private static Sprite middleRingSprite;
        private static Sprite innerCoreSprite;
        private static Sprite jewelSprite;

        public static void Show(Action onSolved)
        {
            EnsureEventSystem();

            if (instance == null)
            {
                GameObject go = new("PastRunicPuzzleModal_Singleton");
                instance = go.AddComponent<PastRunicPuzzleModal>();
            }
            instance.OpenModal(onSolved);
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esGo = new("AR_EventSystem");
                es = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Debug.Log("[PastRunicPuzzleModal] Created EventSystem + StandaloneInputModule for UI clicks.");
            }
        }

        private void OpenModal(Action onSolved)
        {
            onSolvedCallback = onSolved;
            isSolved = false;

            // Generate procedural circular stone sprites if not cached
            EnsureProceduralSprites();

            // Start scrambled so player must rotate all 3 rings
            currentRingState[0] = 1; // 90 deg off
            currentRingState[1] = 3; // 270 deg off
            currentRingState[2] = 2; // 180 deg off

            if (modalPanel == null)
            {
                CreateUI();
            }

            UpdateRingUI(true);
            modalPanel.SetActive(true);
        }

        public void Close()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        public void ResetRings()
        {
            if (isSolved) return;

            currentRingState[0] = 1;
            currentRingState[1] = 3;
            currentRingState[2] = 2;
            UpdateRingUI(true);

            var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
            if (audioFX != null)
            {
                audioFX.PlayOrbCollectSound();
            }
        }

        private void OnRingClicked(int ringIndex)
        {
            if (isSolved || isSpinning[ringIndex]) return;

            currentRingState[ringIndex] = (currentRingState[ringIndex] + 1) % 4;
            StartCoroutine(SpinRingRoutine(ringIndex));

            var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
            if (audioFX != null)
            {
                audioFX.PlayOrbCollectSound();
            }

            CheckSolution();
        }

        private IEnumerator SpinRingRoutine(int ringIndex)
        {
            isSpinning[ringIndex] = true;
            RectTransform rt = ringTransforms[ringIndex];
            if (rt != null)
            {
                float startZ = rt.localEulerAngles.z;
                float targetZ = startZ - 90f; // Spin 90 degrees clockwise per tap
                float elapsed = 0f;
                float duration = 0.32f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    // Add slight overshoot/settle for mechanical stone gear feel
                    float curve = Mathf.Sin(t * Mathf.PI * 0.5f);
                    float curZ = Mathf.Lerp(startZ, targetZ, curve);
                    rt.localRotation = Quaternion.Euler(0f, 0f, curZ);
                    yield return null;
                }
                rt.localRotation = Quaternion.Euler(0f, 0f, targetZ);
            }
            isSpinning[ringIndex] = false;
            UpdateRingUI(false);
        }

        private void UpdateRingUI(bool setRotationInstant)
        {
            for (int i = 0; i < 3; i++)
            {
                int state = currentRingState[i];
                if (setRotationInstant && ringTransforms[i] != null)
                {
                    float angle = -90f * state;
                    ringTransforms[i].localRotation = Quaternion.Euler(0f, 0f, angle);
                }

                if (alignmentJewels[i] != null)
                {
                    bool isAligned = (state == 0);
                    alignmentJewels[i].color = isAligned ? jewelColors[i] : new Color(0.2f, 0.18f, 0.15f, 0.5f);
                    alignmentJewels[i].transform.localScale = isAligned ? Vector3.one * 1.25f : Vector3.one * 0.9f;
                }
            }

            if (statusText != null && !isSolved)
            {
                statusText.text = "Rotate the 3 Concentric Stone Gears until all 3 Alignment Jewels light up Gold (▼)";
                statusText.color = new Color(0.95f, 0.85f, 0.6f, 1f);
            }
        }

        private void CheckSolution()
        {
            if (currentRingState[0] == 0 &&
                currentRingState[1] == 0 &&
                currentRingState[2] == 0)
            {
                isSolved = true;
                if (statusText != null)
                {
                    statusText.text = "✨ ANCIENT CONDUIT ALIGNED! AZTEC RELIC ENERGY SURGING... ✨";
                    statusText.color = new Color(1.0f, 0.88f, 0.3f, 1f);
                }

                for (int i = 0; i < 3; i++)
                {
                    if (ringImages[i] != null)
                    {
                        ringImages[i].color = new Color(1.0f, 0.9f, 0.5f, 1f);
                    }
                }

                var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
                if (audioFX != null)
                {
                    audioFX.PlayPuzzleSolveSound();
                }

                StartCoroutine(SolvedRoutine());
            }
        }

        private IEnumerator SolvedRoutine()
        {
            yield return new WaitForSeconds(1.4f);
            Close();
            onSolvedCallback?.Invoke();
        }

        #region Procedural Circular Sprite Generator (No Square Rectangles!)

        private static void EnsureProceduralSprites()
        {
            if (outerRingSprite != null && middleRingSprite != null && innerCoreSprite != null && jewelSprite != null)
                return;

            outerRingSprite = CreateRingSprite(512, 175, 245, 12, new Color(0.38f, 0.30f, 0.18f, 1f), new Color(0.95f, 0.78f, 0.25f, 1f));
            middleRingSprite = CreateRingSprite(512, 105, 168, 8, new Color(0.25f, 0.28f, 0.32f, 1f), new Color(0.75f, 0.88f, 0.98f, 1f));
            innerCoreSprite = CreateRingSprite(512, 0, 96, 4, new Color(0.42f, 0.25f, 0.10f, 1f), new Color(1.0f, 0.65f, 0.20f, 1f));
            jewelSprite = CreateJewelSprite(128);
        }

        private static Sprite CreateRingSprite(int size, float innerRadius, float outerRadius, int teethCount, Color stoneColor, Color goldColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Transparent outside the ring
                    if (dist < innerRadius - 2f || dist > outerRadius + 2f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // Smooth edge anti-aliasing
                    float alpha = 1f;
                    if (dist < innerRadius)
                        alpha = Mathf.Clamp01(dist - (innerRadius - 2f)) * 0.5f;
                    else if (dist > outerRadius)
                        alpha = Mathf.Clamp01((outerRadius + 2f) - dist) * 0.5f;

                    // Aztec geometric teeth / carved runic notches
                    float notch = Mathf.Sin(angle * teethCount) * 6f;
                    bool isBorder = (dist < innerRadius + 12f + notch) || (dist > outerRadius - 12f - notch);
                    bool isNorthMarker = (dist >= innerRadius && dist <= outerRadius) && (angle > Mathf.PI * 0.42f && angle < Mathf.PI * 0.58f);
                    bool isTick = (Mathf.Abs(Mathf.Sin(angle * 2f)) < 0.08f) && (dist > innerRadius + (outerRadius - innerRadius) * 0.4f);

                    Color pixelCol = isNorthMarker ? goldColor * 1.3f :
                                     (isBorder || isTick) ? goldColor :
                                     stoneColor;

                    // Add subtle stone texture variation
                    float noise = Mathf.Sin(x * 0.15f) * Mathf.Cos(y * 0.15f) * 0.08f;
                    pixelCol = new Color(
                        Mathf.Clamp01(pixelCol.r + noise),
                        Mathf.Clamp01(pixelCol.g + noise),
                        Mathf.Clamp01(pixelCol.b + noise),
                        alpha
                    );

                    pixels[y * size + x] = pixelCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateJewelSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float glow = 1f - (dist / radius);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, glow * 0.95f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        #endregion

        private void CreateUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new("RunicPuzzleCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            modalPanel = new GameObject("PastRunicModalPanel");
            modalPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = modalPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Full-screen mystical vignette backdrop
            Image bg = modalPanel.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.02f, 0.04f, 0.92f);

            // Main Aztec Stone Slab Window (Clean boundaries, plenty of breathing room)
            GameObject winObj = new("AztecSlabWindow");
            winObj.transform.SetParent(modalPanel.transform, false);
            RectTransform winRect = winObj.AddComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.14f, 0.08f);
            winRect.anchorMax = new Vector2(0.86f, 0.92f);
            winRect.offsetMin = Vector2.zero;
            winRect.offsetMax = Vector2.zero;

            Image winBg = winObj.AddComponent<Image>();
            winBg.color = new Color(0.10f, 0.09f, 0.07f, 0.98f); // Dark Mayan temple stone

            Outline outline = winObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.65f, 0.20f, 0.90f);
            outline.effectDistance = new Vector2(4f, 4f);

            // Title Header (Top 12%)
            GameObject titleObj = new("TitleHeader");
            titleObj.transform.SetParent(winObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.88f);
            titleRect.anchorMax = new Vector2(0.95f, 0.96f);
            titleText = titleObj.AddComponent<Text>();
            titleText.text = "🏛️  ANCIENT CONCENTRIC RELIC ASTROLABE  🏛️";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.95f, 0.80f, 0.30f, 1f);

            // Subtitle / Instruction Text
            GameObject statusObj = new("InstructionStatus");
            statusObj.transform.SetParent(winObj.transform, false);
            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.80f);
            statusRect.anchorMax = new Vector2(0.95f, 0.88f);
            statusText = statusObj.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 18;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.9f, 0.8f, 0.6f, 1f);

            // North Golden Indicator Arrow (▼) + 3 Jewel Lights
            GameObject pointerObj = new("NorthPointerHeader");
            pointerObj.transform.SetParent(winObj.transform, false);
            RectTransform ptrRect = pointerObj.AddComponent<RectTransform>();
            ptrRect.anchorMin = new Vector2(0.30f, 0.74f);
            ptrRect.anchorMax = new Vector2(0.70f, 0.81f);

            Text ptrText = pointerObj.AddComponent<Text>();
            ptrText.text = "▼   SACRED NORTH ALIGNMENT   ▼";
            ptrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ptrText.fontSize = 20;
            ptrText.alignment = TextAnchor.MiddleCenter;
            ptrText.color = new Color(1.0f, 0.85f, 0.3f, 1f);

            // 3 Alignment Jewel Status Lights (Show visual progress at a glance)
            GameObject jewelContainer = new("JewelBar");
            jewelContainer.transform.SetParent(winObj.transform, false);
            RectTransform jRect = jewelContainer.AddComponent<RectTransform>();
            jRect.anchorMin = new Vector2(0.40f, 0.69f);
            jRect.anchorMax = new Vector2(0.60f, 0.74f);
            HorizontalLayoutGroup jLayout = jewelContainer.AddComponent<HorizontalLayoutGroup>();
            jLayout.childControlWidth = false;
            jLayout.childControlHeight = false;
            jLayout.childForceExpandWidth = false;
            jLayout.childForceExpandHeight = false;
            jLayout.spacing = 35;
            jLayout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 3; i++)
            {
                GameObject jObj = new("Jewel_" + i);
                jObj.transform.SetParent(jewelContainer.transform, false);
                RectTransform jRT = jObj.AddComponent<RectTransform>();
                jRT.sizeDelta = new Vector2(22f, 22f);
                Image jImg = jObj.AddComponent<Image>();
                jImg.sprite = jewelSprite;
                jImg.color = jewelColors[i];
                alignmentJewels[i] = jImg;
            }

            // CENTRAL ASTROLABE AREA (True Square Aspect-Ratio Invariant Container!)
            GameObject astrolabeCenter = new("AstrolabeSquareCenter");
            astrolabeCenter.transform.SetParent(winObj.transform, false);
            RectTransform astroRect = astrolabeCenter.AddComponent<RectTransform>();
            astroRect.anchorMin = new Vector2(0.15f, 0.18f);
            astroRect.anchorMax = new Vector2(0.85f, 0.69f);
            astroRect.offsetMin = Vector2.zero;
            astroRect.offsetMax = Vector2.zero;

            // AspectRatioFitter ensures our concentric circles are ALWAYS PERFECT 1:1 CIRCLES!
            AspectRatioFitter fitter = astrolabeCenter.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            // RING 0: OUTER RELIC STONE GEAR
            CreateConcentricRing(0, astrolabeCenter.transform, 0.0f, 1.0f, outerRingSprite);

            // RING 1: MIDDLE SILVER LUNAR GEAR
            CreateConcentricRing(1, astrolabeCenter.transform, 0.18f, 0.82f, middleRingSprite);

            // RING 2: INNER AMBER STAR MEDALLION
            CreateConcentricRing(2, astrolabeCenter.transform, 0.38f, 0.62f, innerCoreSprite);

            // GOLDEN SEPARATOR LINE above bottom buttons
            GameObject sepObj = new("BottomSeparatorLine");
            sepObj.transform.SetParent(winObj.transform, false);
            RectTransform sepRect = sepObj.AddComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0.10f, 0.16f);
            sepRect.anchorMax = new Vector2(0.90f, 0.165f);
            Image sepImg = sepObj.AddComponent<Image>();
            sepImg.color = new Color(0.85f, 0.65f, 0.2f, 0.6f);

            // ZERO-OVERLAP BOTTOM CONTROL BAR (Y = 0.03 to 0.13)
            CreateBottomButton(winObj.transform, "ResetBtn", new Vector2(0.14f, 0.035f), new Vector2(0.44f, 0.125f),
                "↺   RESET RINGS", new Color(0.25f, 0.20f, 0.10f, 1f), new Color(0.95f, 0.78f, 0.25f, 1f), ResetRings);

            CreateBottomButton(winObj.transform, "CloseBtn", new Vector2(0.56f, 0.035f), new Vector2(0.86f, 0.125f),
                "✕   CLOSE ALTAR", new Color(0.35f, 0.14f, 0.14f, 1f), new Color(0.95f, 0.40f, 0.30f, 1f), Close);
        }

        private void CreateConcentricRing(int index, Transform parent, float anchorMinVal, float anchorMaxVal, Sprite ringSprite)
        {
            GameObject ringObj = new("ConcentricRing_" + index);
            ringObj.transform.SetParent(parent, false);

            RectTransform ringRT = ringObj.AddComponent<RectTransform>();
            ringRT.anchorMin = new Vector2(anchorMinVal, anchorMinVal);
            ringRT.anchorMax = new Vector2(anchorMaxVal, anchorMaxVal);
            ringRT.offsetMin = Vector2.zero;
            ringRT.offsetMax = Vector2.zero;
            ringTransforms[index] = ringRT;

            Image ringImg = ringObj.AddComponent<Image>();
            ringImages[index] = ringImg;
            ringImg.sprite = ringSprite; // TRUE TRANSPARENT CIRCULAR GEAR! NO SQUARE BOXES!
            ringImg.type = Image.Type.Simple;
            ringImg.preserveAspect = true;

            Button ringBtn = ringObj.AddComponent<Button>();
            int idx = index;
            ringBtn.onClick.AddListener(() => OnRingClicked(idx));
        }

        private static void CreateBottomButton(Transform parent, string goName, Vector2 anchorMin, Vector2 anchorMax,
            string label, Color bgColor, Color outlineColor, Action onClick)
        {
            GameObject btnObj = new(goName);
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = anchorMin;
            btnRect.anchorMax = anchorMax;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Outline outl = btnObj.AddComponent<Outline>();
            outl.effectColor = outlineColor;
            outl.effectDistance = new Vector2(2f, 2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtObj = new("LabelText");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
    }
}
