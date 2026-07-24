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

        private void Reset()
        {
            gameState = FindFirstObjectByType<GameStateController>();
        }

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (inGameSettingsPanel != null) inGameSettingsPanel.SetActive(false);
        }

        private void Update()
        {
            // Android System Back Button in AR Scene toggles Pause Panel
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void OnEnable()
        {
            if (gameState == null)
            {
                return;
            }

            gameState.StateChanged += OnStateChanged;
            gameState.ProgressChanged += OnProgressChanged;
            OnStateChanged(gameState.CurrentState);
            OnProgressChanged(TimeLayer.Past, gameState.CollectedFragments, gameState.TotalFragments);
        }

        private void OnDisable()
        {
            if (gameState == null)
            {
                return;
            }

            gameState.StateChanged -= OnStateChanged;
            gameState.ProgressChanged -= OnProgressChanged;
        }

        private void OnStateChanged(RealityFracturesState state)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = state switch
            {
                RealityFracturesState.Scanning => "Find a flat surface",
                RealityFracturesState.ReadyToPlace => "Tap to open the fracture",
                RealityFracturesState.PastActive => "Collect the Past fragment",
                RealityFracturesState.PresentActive => "Collect the Present fragment",
                RealityFracturesState.FutureActive => "Collect the Future fragment",
                RealityFracturesState.Stabilized => "Reality is stabilizing...",
                RealityFracturesState.Complete => "Reality Stabilized",
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
            if (inGameSettingsPanel != null) inGameSettingsPanel.SetActive(true);
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
