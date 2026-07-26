using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RealityFractures
{
    public sealed class PresentFrequencyModal : MonoBehaviour
    {
        private static PresentFrequencyModal instance;

        private GameObject modalPanel;
        private Text titleText;
        private Text statusText;
        private Text[] dialTexts = new Text[3];
        private Image[] dialImages = new Image[3];

        private int[] currentDials = new int[3];
        private readonly int[] targetDials = new int[] { 1, 2, 2 }; // 1: 528Hz, 2: 180 DEG, 2: OPTIMAL FLUX
        private readonly string[][] dialSymbols = new string[][] {
            new string[] { "FREQ:\n432 Hz", "FREQ:\n528 Hz", "FREQ:\n639 Hz" },
            new string[] { "PHASE:\n0° DEG", "PHASE:\n90° DEG", "PHASE:\n180° DEG" },
            new string[] { "FLUX:\nLOW", "FLUX:\nMID", "FLUX:\nOPTIMAL" }
        };
        private readonly Color[] neonColors = new Color[] {
            new Color(0.3f, 0.95f, 0.5f, 1f), // Bright Emerald
            new Color(0.2f, 0.85f, 0.65f, 1f),// Cyan-Green
            new Color(0.4f, 1.0f, 0.3f, 1f)   // Quantum Lime
        };

        private Action onSolvedCallback;
        private bool isSolved = false;
        private static Sprite circularDialSprite;

        public static void Show(Action onSolved)
        {
            EnsureEventSystem();

            if (instance == null)
            {
                GameObject go = new("PresentFrequencyModal_Singleton");
                instance = go.AddComponent<PresentFrequencyModal>();
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
            }
        }

        private void OpenModal(Action onSolved)
        {
            onSolvedCallback = onSolved;
            isSolved = false;

            if (circularDialSprite == null)
            {
                circularDialSprite = CreateCircleSprite(256, new Color(0.06f, 0.18f, 0.10f, 1f), new Color(0.3f, 0.95f, 0.5f, 1f));
            }

            // Start with unsolved sequence
            currentDials[0] = 0;
            currentDials[1] = 0;
            currentDials[2] = 1;

            if (modalPanel == null)
            {
                CreateUI();
            }

            UpdateDialUI();
            modalPanel.SetActive(true);
        }

        public void Close()
        {
            if (modalPanel != null)
            {
                modalPanel.SetActive(false);
            }
        }

        private void OnDialClicked(int index)
        {
            if (isSolved) return;

            currentDials[index] = (currentDials[index] + 1) % 3;
            UpdateDialUI();

            var audioFX = FindFirstObjectByType<ProceduralAudioFX>();
            if (audioFX != null)
            {
                audioFX.PlayOrbCollectSound();
            }

            CheckSolution();
        }

        private void UpdateDialUI()
        {
            for (int i = 0; i < 3; i++)
            {
                int val = currentDials[i];
                if (dialTexts[i] != null)
                {
                    dialTexts[i].text = dialSymbols[i][val];
                    dialTexts[i].color = neonColors[i];
                }
                if (dialImages[i] != null)
                {
                    dialImages[i].color = Color.Lerp(neonColors[i], Color.black, 0.70f);
                }
            }

            if (statusText != null && !isSolved)
            {
                statusText.text = "Target Frequency:   528 Hz  |  180° DEG  |  OPTIMAL FLUX";
                statusText.color = new Color(0.5f, 0.95f, 0.7f, 1f);
            }
        }

        private void CheckSolution()
        {
            if (currentDials[0] == targetDials[0] &&
                currentDials[1] == targetDials[1] &&
                currentDials[2] == targetDials[2])
            {
                isSolved = true;
                if (statusText != null)
                {
                    statusText.text = "⚡ FREQUENCY LOCKED! STABILIZING REALITY... ⚡";
                    statusText.color = new Color(0.3f, 1.0f, 0.5f, 1f);
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
            yield return new WaitForSeconds(1.2f);
            Close();
            onSolvedCallback?.Invoke();
        }

        private static Sprite CreateCircleSprite(int size, Color fillColor, Color rimColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            float outerRad = size * 0.46f;
            float innerRad = size * 0.41f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > outerRad)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else if (dist > innerRad)
                    {
                        pixels[y * size + x] = rimColor;
                    }
                    else
                    {
                        pixels[y * size + x] = fillColor;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void CreateUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new("PresentPuzzleCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            modalPanel = new GameObject("PresentModalPanel");
            modalPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = modalPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image bg = modalPanel.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.05f, 0.03f, 0.92f);

            GameObject winObj = new("PresentWindow");
            winObj.transform.SetParent(modalPanel.transform, false);
            RectTransform winRect = winObj.AddComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.14f, 0.12f);
            winRect.anchorMax = new Vector2(0.86f, 0.88f);
            winRect.offsetMin = Vector2.zero;
            winRect.offsetMax = Vector2.zero;

            Image winBg = winObj.AddComponent<Image>();
            winBg.color = new Color(0.04f, 0.12f, 0.08f, 0.98f);

            Outline outline = winObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.95f, 0.6f, 0.8f);
            outline.effectDistance = new Vector2(3f, 3f);

            GameObject titleObj = new("Title");
            titleObj.transform.SetParent(winObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.84f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleText = titleObj.AddComponent<Text>();
            titleText.text = "⚡  TEMPORAL RIFT SYNCHRONIZER  ⚡";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 32;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.3f, 0.95f, 0.6f, 1f);

            GameObject statusObj = new("Status");
            statusObj.transform.SetParent(winObj.transform, false);
            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.72f);
            statusRect.anchorMax = new Vector2(0.95f, 0.82f);
            statusText = statusObj.AddComponent<Text>();
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 20;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.5f, 0.95f, 0.7f, 1f);

            GameObject dialContainer = new("DialContainer");
            dialContainer.transform.SetParent(winObj.transform, false);
            RectTransform contRect = dialContainer.AddComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.10f, 0.24f);
            contRect.anchorMax = new Vector2(0.90f, 0.68f);
            HorizontalLayoutGroup hLayout = dialContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;
            hLayout.spacing = 30;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                GameObject dialObj = new("Dial_" + i);
                dialObj.transform.SetParent(dialContainer.transform, false);

                Image dialImg = dialObj.AddComponent<Image>();
                dialImages[i] = dialImg;
                dialImg.sprite = circularDialSprite; // Circular emerald frequency dial! Not a square box!
                dialImg.preserveAspect = true;
                dialImg.color = new Color(0.08f, 0.2f, 0.14f, 1f);

                Outline dialOutline = dialObj.AddComponent<Outline>();
                dialOutline.effectColor = new Color(0.2f, 0.9f, 0.5f, 0.6f);
                dialOutline.effectDistance = new Vector2(2f, 2f);

                Button dialBtn = dialObj.AddComponent<Button>();
                dialBtn.onClick.AddListener(() => OnDialClicked(idx));

                GameObject textObj = new("DialText");
                textObj.transform.SetParent(dialObj.transform, false);
                RectTransform txtRect = textObj.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;

                Text dText = textObj.AddComponent<Text>();
                dText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                dText.fontSize = 24;
                dText.alignment = TextAnchor.MiddleCenter;
                dialTexts[i] = dText;
            }

            GameObject closeObj = new("CloseBtn");
            closeObj.transform.SetParent(winObj.transform, false);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.38f, 0.04f);
            closeRect.anchorMax = new Vector2(0.62f, 0.15f);

            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.12f, 0.35f, 0.2f, 1f);
            Outline outl = closeObj.AddComponent<Outline>();
            outl.effectColor = new Color(0.3f, 0.95f, 0.6f, 0.8f);
            outl.effectDistance = new Vector2(2f, 2f);

            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            GameObject closeTextObj = new("CloseTxt");
            closeTextObj.transform.SetParent(closeObj.transform, false);
            RectTransform cTxtRect = closeTextObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            Text cText = closeTextObj.AddComponent<Text>();
            cText.text = "✕   CLOSE";
            cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cText.fontSize = 20;
            cText.alignment = TextAnchor.MiddleCenter;
            cText.color = Color.white;
        }
    }
}
