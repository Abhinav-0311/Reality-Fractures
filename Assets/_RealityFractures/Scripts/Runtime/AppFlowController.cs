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

        [Header("Main Menu Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelQuitButton;

        [Header("Settings Controls")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle vfxToggle;
        [SerializeField] private Text startButtonLabel;

        private static AppFlowController instance;
        public static AppFlowController Instance => instance;

        private void Awake()
        {
            instance = this;
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

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(LoadARGame);
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OpenSettings);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OpenQuitConfirmation);
            }
            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.RemoveAllListeners();
                closeSettingsButton.onClick.AddListener(CloseSettings);
            }
            if (resetProgressButton != null)
            {
                resetProgressButton.onClick.RemoveAllListeners();
                resetProgressButton.onClick.AddListener(ResetProgress);
            }
            if (confirmQuitButton != null)
            {
                confirmQuitButton.onClick.RemoveAllListeners();
                confirmQuitButton.onClick.AddListener(ConfirmQuitApp);
            }
            if (cancelQuitButton != null)
            {
                cancelQuitButton.onClick.RemoveAllListeners();
                cancelQuitButton.onClick.AddListener(CloseQuitConfirmation);
            }
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.RemoveAllListeners();
                soundToggle.onValueChanged.AddListener(ToggleSound);
            }
            if (sfxToggle != null)
            {
                sfxToggle.onValueChanged.RemoveAllListeners();
                sfxToggle.onValueChanged.AddListener(ToggleSFX);
            }
            if (vfxToggle != null)
            {
                vfxToggle.onValueChanged.RemoveAllListeners();
                vfxToggle.onValueChanged.AddListener(ToggleVFX);
            }

            bool hasProgress = PlayerPrefs.GetInt("RF_FracturePlaced", 0) == 1;
            if (startButtonLabel != null)
            {
                startButtonLabel.text = hasProgress ? "CONTINUE FRACTURE" : "START NEW FRACTURE";
            }

            // Restore Sound, SFX, and VFX settings
            bool soundOn = PlayerPrefs.GetInt("RF_SoundEnabled", 1) == 1;
            bool sfxOn = PlayerPrefs.GetInt("RF_SFXEnabled", 1) == 1;
            bool vfxHigh = PlayerPrefs.GetInt("RF_VFXHigh", 1) == 1;

            AudioListener.pause = !soundOn;
            if (soundToggle != null) soundToggle.isOn = soundOn;
            if (sfxToggle != null) sfxToggle.isOn = sfxOn;
            if (vfxToggle != null) vfxToggle.isOn = vfxHigh;
        }

        public void LoadARGame()
        {
            StartCoroutine(LoadSceneAsync(arGameSceneName));
        }

        public void LoadMainMenu()
        {
            StartCoroutine(LoadSceneAsync(mainMenuSceneName));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
            {
                yield return null;
            }
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

        public void ToggleSFX(bool isEnabled)
        {
            PlayerPrefs.SetInt("RF_SFXEnabled", isEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleVFX(bool isHighQuality)
        {
            PlayerPrefs.SetInt("RF_VFXHigh", isHighQuality ? 1 : 0);
            QualitySettings.SetQualityLevel(isHighQuality ? 2 : 0, true);
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
