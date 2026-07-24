using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealityFractures
{
    public class AppFlowController : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string splashSceneName = "0_Splash";
        [SerializeField] private string mainMenuSceneName = "1_MainMenu";
        [SerializeField] private string arGameSceneName = "2_ARGame";
        [SerializeField] private float splashDurationSeconds = 2.0f;

        [Header("Main Menu UI Panels (1_MainMenu)")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject quitConfirmationPanel;

        [Header("Settings Toggles")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle vibrationToggle;

        private static AppFlowController instance;
        public static AppFlowController Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == splashSceneName)
            {
                StartCoroutine(RunSplashSequence());
            }
            else if (currentScene == mainMenuSceneName)
            {
                InitializeMainMenuUI();
            }
        }

        private void Update()
        {
            // Android System Back Button handling
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleAndroidBackButton();
            }
        }

        private IEnumerator RunSplashSequence()
        {
            yield return new WaitForSeconds(splashDurationSeconds);
            LoadMainMenu();
        }

        private void InitializeMainMenuUI()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

            // Restore sound settings
            bool soundOn = PlayerPrefs.GetInt("RF_SoundEnabled", 1) == 1;
            AudioListener.pause = !soundOn;
            if (soundToggle != null) soundToggle.isOn = soundOn;
        }

        public void LoadARGame()
        {
            SceneManager.LoadScene(arGameSceneName);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void OpenQuitConfirmation()
        {
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(true);
        }

        public void CloseQuitConfirmation()
        {
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        }

        public void ToggleSound(bool isEnabled)
        {
            AudioListener.pause = !isEnabled;
            PlayerPrefs.SetInt("RF_SoundEnabled", isEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey("RF_FracturePlaced");
            PlayerPrefs.DeleteKey("RF_LastEra");
            PlayerPrefs.Save();
            Debug.Log("[AppFlowController] Player progress reset.");
        }

        public void ConfirmQuitApp()
        {
            Debug.Log("[AppFlowController] Exiting Application...");
            Application.Quit();
        }

        private void HandleAndroidBackButton()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == mainMenuSceneName)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else if (quitConfirmationPanel != null && quitConfirmationPanel.activeSelf)
                {
                    CloseQuitConfirmation();
                }
                else
                {
                    OpenQuitConfirmation();
                }
            }
        }
    }
}
