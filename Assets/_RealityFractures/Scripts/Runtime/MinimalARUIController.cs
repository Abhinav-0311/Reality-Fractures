using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealityFractures
{
    public sealed class MinimalARUIController : MonoBehaviour
    {
        [Header("Game State Reference")]
        [SerializeField] private GameStateController gameState;

        [Header("AR HUD Prompt Elements")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text progressText;

        [Header("Pause Overlay Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject inGameSettingsPanel;

        [Header("AR HUD & Pause Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button pastButton;
        [SerializeField] private Button presentButton;
        [SerializeField] private Button futureButton;

        [SerializeField] private ARPlacementController placementController;

        private void Reset()
        {
            gameState = FindFirstObjectByType<GameStateController>();
            placementController = FindFirstObjectByType<ARPlacementController>();
        }

        private void Start()
        {
            if (placementController == null)
            {
                placementController = FindFirstObjectByType<ARPlacementController>();
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            if (inGameSettingsPanel != null) inGameSettingsPanel.SetActive(false);

            if (zoomInButton != null && placementController != null)
            {
                zoomInButton.onClick.RemoveAllListeners();
                zoomInButton.onClick.AddListener(placementController.ZoomIn);
            }
            if (zoomOutButton != null && placementController != null)
            {
                zoomOutButton.onClick.RemoveAllListeners();
                zoomOutButton.onClick.AddListener(placementController.ZoomOut);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(TogglePause);
            }
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(ResumeGame);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OpenInGameSettings);
            }
            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveAllListeners();
                closeSettingsButton.onClick.AddListener(CloseInGameSettings);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartGame);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }
            if (pastButton != null && gameState != null)
            {
                pastButton.onClick.RemoveAllListeners();
                pastButton.onClick.AddListener(() => {
                    gameState.SelectPastLayer();
                    PlayEraSound(TimeLayer.Past);
                });
            }
            if (presentButton != null && gameState != null)
            {
                presentButton.onClick.RemoveAllListeners();
                presentButton.onClick.AddListener(() => {
                    gameState.SelectPresentLayer();
                    PlayEraSound(TimeLayer.Present);
                });
            }
            if (futureButton != null && gameState != null)
            {
                futureButton.onClick.RemoveAllListeners();
                futureButton.onClick.AddListener(() => {
                    gameState.SelectFutureLayer();
                    PlayEraSound(TimeLayer.Future);
                });
            }
        }

        private void PlayEraSound(TimeLayer layer)
        {
            ProceduralAudioFX audio = FindFirstObjectByType<ProceduralAudioFX>();
            if (audio != null)
            {
                audio.PlayTimeShiftSound(layer);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inGameSettingsPanel != null && inGameSettingsPanel.activeSelf)
                {
                    CloseInGameSettings();
                }
                else
                {
                    TogglePause();
                }
            }

            if (overrideStatusTimer > 0f)
            {
                overrideStatusTimer -= Time.deltaTime;
                if (overrideStatusTimer <= 0f && gameState != null)
                {
                    OnStateChanged(gameState.CurrentState);
                }
            }
        }

        private float overrideStatusTimer = 0f;
        private TemporalPuzzleController puzzleController;

        private void OnEnable()
        {
            if (gameState != null)
            {
                gameState.StateChanged += OnStateChanged;
                gameState.ProgressChanged += OnProgressChanged;
                OnStateChanged(gameState.CurrentState);
                OnProgressChanged(TimeLayer.Past, gameState.CollectedFragments, gameState.TotalFragments);
            }

            puzzleController = FindFirstObjectByType<TemporalPuzzleController>();
            if (puzzleController != null)
            {
                puzzleController.PuzzleStatusUpdated += OnPuzzleStatusUpdated;
            }
        }

        private void OnDisable()
        {
            if (gameState != null)
            {
                gameState.StateChanged -= OnStateChanged;
                gameState.ProgressChanged -= OnProgressChanged;
            }

            if (puzzleController != null)
            {
                puzzleController.PuzzleStatusUpdated -= OnPuzzleStatusUpdated;
            }
        }

        private void OnPuzzleStatusUpdated(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                overrideStatusTimer = 4.0f;
            }
        }

        private void OnStateChanged(RealityFracturesState state)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = state switch
            {
                RealityFracturesState.Scanning => "[CHRONOS-WEAVER] Scan a flat surface to locate the anomaly...",
                RealityFracturesState.ReadyToPlace => "[CHRONOS-WEAVER] Tap surface to anchor the Reality Fracture",
                RealityFracturesState.PastActive => "ERA: ANCIENT PAST | Recover the Amber Chrono-Core",
                RealityFracturesState.PresentActive => "ERA: SHATTERED PRESENT | Recover the Emerald Chrono-Core",
                RealityFracturesState.FutureActive => "ERA: CRYSTALLINE FUTURE | Recover the Cyan Chrono-Core",
                RealityFracturesState.Stabilized => "ALL TIMELINES SYNCED | Synchronizing core harmonic...",
                RealityFracturesState.Complete => "REALITY STABILIZED | Rift successfully sealed!",
                _ => string.Empty
            };
        }

        private void OnProgressChanged(TimeLayer layer, int collected, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"{collected}/{total}";
            }
        }

        // --- PAUSE MENU LOGIC ---

        public void TogglePause()
        {
            if (pausePanel == null) return;

            bool willPause = !pausePanel.activeSelf;
            pausePanel.SetActive(willPause);
            if (willPause)
            {
                pausePanel.transform.SetAsLastSibling();
            }

            if (!willPause && inGameSettingsPanel != null)
            {
                inGameSettingsPanel.SetActive(false);
            }
        }

        public void ResumeGame()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (inGameSettingsPanel != null) inGameSettingsPanel.SetActive(false);
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OpenInGameSettings()
        {
            if (inGameSettingsPanel != null)
            {
                inGameSettingsPanel.SetActive(true);
                inGameSettingsPanel.transform.SetAsLastSibling();
            }
        }

        public void CloseInGameSettings()
        {
            if (inGameSettingsPanel != null) inGameSettingsPanel.SetActive(false);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("1_MainMenu");
        }
    }
}
