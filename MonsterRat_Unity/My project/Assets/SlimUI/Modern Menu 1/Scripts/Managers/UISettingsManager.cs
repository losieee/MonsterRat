using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace SlimUI.ModernMenu
{
    [System.Serializable]
    public class GameSettingsData
    {
        public float musicVolume = 1.0f;
        public float effectVolume = 1.0f;
        public float xSensitivity = 1.0f;
        public float ySensitivity = 1.0f;
        public float mouseSmoothing = 0.0f;

       //public int normalDifficulty = 1;
       //public int hardCoreDifficulty = 0;

        public int showHUD = 1;
        public int toolTips = 1;

        public int shadows = 2;
        public int mobileShadows = 2;

        public int vSyncCount = 1;
        public int inverted = 0;
        public int motionBlur = 1;
        public int ambientOcclusion = 1;
        public int cameraEffects = 1;
        public int textures = 2;

        // 해상도 및 안티앨리어싱
        public int resolutionIndex = -1;
        public int antiAliasing = 0;  

        public int bugPhobiaMode = 0;
    }

    public class UISettingsManager : MonoBehaviour
    {
        public static UISettingsManager Instance;

        public static event System.Action OnSettingsUpdated;

        [Header("UI PANEL REFERENCE")]
        public GameObject settingsPanel;
        public static bool isMenuOpen = false;

        [Header("MODE")]
        public bool useLocalPlayerCheck = false;
        public bool isLocalPlayer = true;
        public bool isTutorialPlayer = false;
        public NetworkRunner runner;
        public PlayerUIState state;

        [Header("UI BEHAVIOR")]
        public bool canUseEscKey = true;
        public enum Platform { Desktop, Mobile };
        public Platform platform;

        [Header("VIDEO SETTINGS")]
        public GameObject fullscreentext;
        public TMP_Dropdown resolutionDropdown;
        public GameObject ambientocclusiontext;
        public GameObject shadowofftextLINE;
        public GameObject shadowlowtextLINE;
        public GameObject shadowhightextLINE;

        
        public GameObject aaofftextLINE;
        public GameObject aa2xtextLINE;
        public GameObject aa4xtextLINE;
        public GameObject aa8xtextLINE;

        public GameObject vsynctext;
        public GameObject motionblurtext;
        public GameObject texturelowtextLINE;
        public GameObject texturemedtextLINE;
        public GameObject texturehightextLINE;
        public GameObject cameraeffectstext;

        [Header("GAME SETTINGS")]
        public GameObject showhudtext;
        public GameObject tooltipstext;
       //public GameObject difficultynormaltext;
       //public GameObject difficultynormaltextLINE;
       //public GameObject difficultyhardcoretext;
       //public GameObject difficultyhardcoretextLINE;
        public GameObject exitPanel;

        [Header("PHOBIA SETTINGS")]
        public GameObject phobiaOffLine;
        public GameObject phobiaOnLine;

        [Header("CONTROLS SETTINGS")]
        public GameObject invertmousetext;

        [Header("AUDIO SETTINGS")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        public Slider musicSlider;
        public Slider effectSlider;
        public GameObject sensitivityXSlider;
        public GameObject sensitivityYSlider;
        public GameObject mouseSmoothSlider;

        private GameSettingsData currentSettings;
        private Resolution[] resolutions;
        private bool isInitializing = false;

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            isInitializing = true;
            LoadSettings();
            InitializeResolutions();

            if (settingsPanel != null) settingsPanel.SetActive(false);

            ApplySettingsToUIAndEngine();
            isInitializing = false;
        }

        public void Update()
        {
            bool localPlayerDead = PlayerController.LocalPlayer != null && PlayerController.LocalPlayer.IsDead;

            if (GameInputLock.IsLocked && !localPlayerDead)
                return;

            if (canUseEscKey && Input.GetKeyDown(KeyCode.Escape))
            {
                if (isTutorialPlayer && state.storeOpen) return;
                if (PhotonPlayerUIState.isGlobalStoreOpen) return;

                ToggleSettingsMenu();
            }
        }

        bool CanUseMenu()
        {
            if (!useLocalPlayerCheck) return true;
            return isLocalPlayer;
        }

        public void ToggleSettingsMenu()
        {
            if (settingsPanel == null) return;
            if (!CanUseMenu()) return;

            bool isCurrentlyActive = settingsPanel.activeSelf;
            settingsPanel.SetActive(!isCurrentlyActive);

            isMenuOpen = settingsPanel.activeSelf;
            if (!isCurrentlyActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                SaveSettings();
                exitPanel.SetActive(false);
            }
        }

        private void LoadSettings()
        {
            string jsonString = PlayerPrefs.GetString("MasterGameSettings", "");
            if (!string.IsNullOrEmpty(jsonString))
            {
                currentSettings = JsonUtility.FromJson<GameSettingsData>(jsonString);
            }
            else
            {
                currentSettings = new GameSettingsData();
                SaveSettings();
            }
        }

        private void InitializeResolutions()
        {
            if (resolutionDropdown == null) return;

            Resolution[] allResolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            List<Resolution> uniqueResolutions = new List<Resolution>();
            int currentResIndex = 0;

            for (int i = 0; i < allResolutions.Length; i++)
            {
                string optionText = allResolutions[i].width + " x " + allResolutions[i].height;
                if (!options.Contains(optionText))
                {
                    options.Add(optionText);
                    uniqueResolutions.Add(allResolutions[i]);
                    if (allResolutions[i].width == Screen.currentResolution.width &&
                        allResolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResIndex = uniqueResolutions.Count - 1;
                    }
                }
            }
            resolutions = uniqueResolutions.ToArray();

            resolutionDropdown.AddOptions(options);

            if (currentSettings.resolutionIndex == -1)
            {
                currentSettings.resolutionIndex = currentResIndex;
            }

            resolutionDropdown.value = currentSettings.resolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        public void SaveSettings()
        {
            if (isInitializing) return;

            if (resolutionDropdown != null) currentSettings.resolutionIndex = resolutionDropdown.value;

            if (musicSlider != null)
            {
                currentSettings.musicVolume = musicSlider.value;
                PlayerPrefs.SetFloat("MusicVolume", currentSettings.musicVolume);
                if (bgmSource != null) bgmSource.volume = currentSettings.musicVolume;
            }

            if (effectSlider != null)
            {
                currentSettings.effectVolume = effectSlider.value;
                PlayerPrefs.SetFloat("EffectVolume", currentSettings.effectVolume);
                if (sfxSource != null) sfxSource.volume = currentSettings.effectVolume;
            }

            if (sensitivityXSlider != null)
            {
                Slider slider = sensitivityXSlider.GetComponent<Slider>();
                if (slider != null) currentSettings.xSensitivity = slider.value;
                PlayerPrefs.SetFloat("XSensitivity", currentSettings.xSensitivity);
            }

            if (sensitivityYSlider != null)
            {
                Slider slider = sensitivityYSlider.GetComponent<Slider>();
                if (slider != null) currentSettings.ySensitivity = slider.value;
                PlayerPrefs.SetFloat("YSensitivity", currentSettings.ySensitivity);
            }

            if (mouseSmoothSlider != null)
            {
                Slider slider = mouseSmoothSlider.GetComponent<Slider>();
                if (slider != null) currentSettings.mouseSmoothing = slider.value;
                PlayerPrefs.SetFloat("MouseSmoothing", currentSettings.mouseSmoothing);
            }

            if (currentSettings != null)
            {
                string jsonString = JsonUtility.ToJson(currentSettings, true);
                PlayerPrefs.SetString("MasterGameSettings", jsonString);
                PlayerPrefs.Save();
                OnSettingsUpdated?.Invoke();
            }

           // PlayerPrefs.Save();
        }

        private void ApplySettingsToUIAndEngine()
        {
            //if (currentSettings.normalDifficulty == 1)
            //{
            //    difficultynormaltextLINE.gameObject.SetActive(true);
            //    difficultyhardcoretextLINE.gameObject.SetActive(false);
            //}
            //if
            //{
            //    difficultyhardcoretextLINE.gameObject.SetActive(true);
            //    difficultynormaltextLINE.gameObject.SetActive(false);
            //}

            if (phobiaOffLine) phobiaOffLine.SetActive(currentSettings.bugPhobiaMode == 0);
            if (phobiaOnLine) phobiaOnLine.SetActive(currentSettings.bugPhobiaMode == 1);
            if (musicSlider) musicSlider.SetValueWithoutNotify(currentSettings.musicVolume);
            if (effectSlider) effectSlider.SetValueWithoutNotify(currentSettings.effectVolume);
            if (bgmSource != null) bgmSource.volume = currentSettings.musicVolume;
            if (sfxSource != null) sfxSource.volume = currentSettings.effectVolume;

            if (sensitivityXSlider) sensitivityXSlider.GetComponent<Slider>().value = currentSettings.xSensitivity;
            if (sensitivityYSlider) sensitivityYSlider.GetComponent<Slider>().value = currentSettings.ySensitivity;
            if (mouseSmoothSlider) mouseSmoothSlider.GetComponent<Slider>().value = currentSettings.mouseSmoothing;
            if (invertmousetext) invertmousetext.GetComponent<TMP_Text>().text = currentSettings.inverted == 0 ? "off" : "on";

            if (fullscreentext) fullscreentext.GetComponent<TMP_Text>().text = Screen.fullScreen ? "on" : "off";
            if (vsynctext) vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";

            if (resolutions != null && resolutions.Length > 0 && currentSettings.resolutionIndex >= 0 && currentSettings.resolutionIndex < resolutions.Length)
            {
                Resolution res = resolutions[currentSettings.resolutionIndex];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }

            if (platform == Platform.Desktop)
            {
                if (currentSettings.shadows == 0) ShadowsOff();
                else if (currentSettings.shadows == 1) ShadowsLow();
                else if (currentSettings.shadows == 2) ShadowsHigh();
            }

            QualitySettings.vSyncCount = currentSettings.vSyncCount;

            if (currentSettings.textures == 0) TexturesLow();
            else if (currentSettings.textures == 1) TexturesMed();
            else if (currentSettings.textures == 2) TexturesHigh();

             
            int aaLevel = 0;
            if (currentSettings.antiAliasing == 1) aaLevel = 2;
            else if (currentSettings.antiAliasing == 2) aaLevel = 4;
            else if (currentSettings.antiAliasing == 3) aaLevel = 8;
            QualitySettings.antiAliasing = aaLevel;
            UpdateAAUI();

            if (showhudtext) showhudtext.GetComponent<TMP_Text>().text = currentSettings.showHUD == 0 ? "off" : "on";
            if (tooltipstext) tooltipstext.GetComponent<TMP_Text>().text = currentSettings.toolTips == 0 ? "off" : "on";
            if (motionblurtext) motionblurtext.GetComponent<TMP_Text>().text = currentSettings.motionBlur == 0 ? "off" : "on";
            if (ambientocclusiontext) ambientocclusiontext.GetComponent<TMP_Text>().text = currentSettings.ambientOcclusion == 0 ? "off" : "on";
        }

        public void SetResolution(int resolutionIndex)
        {
            Resolution res = resolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            currentSettings.resolutionIndex = resolutionIndex;
            SaveSettings();
            Debug.Log($"적용된 해상도: {res.width} x {res.height}");
        }
        public void BugPhobiaModeOff()
        {
            // ★ 안전장치 추가: 데이터가 비어있으면 강제로 다시 불러옵니다.
            if (currentSettings == null) LoadSettings();

            currentSettings.bugPhobiaMode = 0;
            if (phobiaOffLine) phobiaOffLine.SetActive(true);
            if (phobiaOnLine) phobiaOnLine.SetActive(false);
            SaveSettings();
        }

        public void BugPhobiaModeOn()
        {
            // ★ 안전장치 추가: 데이터가 비어있으면 강제로 다시 불러옵니다.
            if (currentSettings == null) LoadSettings();

            currentSettings.bugPhobiaMode = 1;
            if (phobiaOffLine) phobiaOffLine.SetActive(false);
            if (phobiaOnLine) phobiaOnLine.SetActive(true);
            SaveSettings();
        }

        public void FullScreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (fullscreentext) fullscreentext.GetComponent<TMP_Text>().text = Screen.fullScreen ? "on" : "off";
            SaveSettings();
        }

        public void MusicSlider()
        {
            if (isInitializing) return;
            SaveSettings();
        }

        public void EffectSlider()
        {
            if (isInitializing) return;
            SaveSettings();
        }

        public async void LeaveRoom()
        {
            if (runner == null)
                runner = FindObjectOfType<NetworkRunner>();

            if (runner != null)
            {
                await runner.Shutdown();
                Destroy(runner.gameObject);
                runner = null;
            }

            if (settingsPanel != null) settingsPanel.SetActive(false);

            isMenuOpen = false;
            await System.Threading.Tasks.Task.Delay(200);
            SceneManager.LoadScene("ZZin_Main_Lobby");
        }
        public void vsync()
        {
            currentSettings.vSyncCount = currentSettings.vSyncCount == 0 ? 1 : 0;
            QualitySettings.vSyncCount = currentSettings.vSyncCount;
            if (vsynctext) vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";
            SaveSettings();
        }

        public void ShadowsOff()
        {
            currentSettings.shadows = 0;
            QualitySettings.shadowCascades = 0;
            QualitySettings.shadowDistance = 0;
            if (shadowofftextLINE) shadowofftextLINE.gameObject.SetActive(true);
            if (shadowlowtextLINE) shadowlowtextLINE.gameObject.SetActive(false);
            if (shadowhightextLINE) shadowhightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void ShadowsLow()
        {
            currentSettings.shadows = 1;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowDistance = 75;
            if (shadowofftextLINE) shadowofftextLINE.gameObject.SetActive(false);
            if (shadowlowtextLINE) shadowlowtextLINE.gameObject.SetActive(true);
            if (shadowhightextLINE) shadowhightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void ShadowsHigh()
        {
            currentSettings.shadows = 2;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 500;
            if (shadowofftextLINE) shadowofftextLINE.gameObject.SetActive(false);
            if (shadowlowtextLINE) shadowlowtextLINE.gameObject.SetActive(false);
            if (shadowhightextLINE) shadowhightextLINE.gameObject.SetActive(true);
            SaveSettings();
        }

        public void TexturesLow()
        {
            currentSettings.textures = 0;
            QualitySettings.globalTextureMipmapLimit = 2;
            if (texturelowtextLINE) texturelowtextLINE.gameObject.SetActive(true);
            if (texturemedtextLINE) texturemedtextLINE.gameObject.SetActive(false);
            if (texturehightextLINE) texturehightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void TexturesMed()
        {
            currentSettings.textures = 1;
            QualitySettings.globalTextureMipmapLimit = 1;
            if (texturelowtextLINE) texturelowtextLINE.gameObject.SetActive(false);
            if (texturemedtextLINE) texturemedtextLINE.gameObject.SetActive(true);
            if (texturehightextLINE) texturehightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void TexturesHigh()
        {
            currentSettings.textures = 2;
            QualitySettings.globalTextureMipmapLimit = 0;
            if (texturelowtextLINE) texturelowtextLINE.gameObject.SetActive(false);
            if (texturemedtextLINE) texturemedtextLINE.gameObject.SetActive(false);
            if (texturehightextLINE) texturehightextLINE.gameObject.SetActive(true);
            SaveSettings();
        }

         
        public void AntiAliasingOff() { currentSettings.antiAliasing = 0; QualitySettings.antiAliasing = 0; UpdateAAUI(); SaveSettings(); }
        public void AntiAliasing2x() { currentSettings.antiAliasing = 1; QualitySettings.antiAliasing = 2; UpdateAAUI(); SaveSettings(); }
        public void AntiAliasing4x() { currentSettings.antiAliasing = 2; QualitySettings.antiAliasing = 4; UpdateAAUI(); SaveSettings(); }
        public void AntiAliasing8x() { currentSettings.antiAliasing = 3; QualitySettings.antiAliasing = 8; UpdateAAUI(); SaveSettings(); }

         
        private void UpdateAAUI()
        {
            if (aaofftextLINE) aaofftextLINE.SetActive(currentSettings.antiAliasing == 0);
            if (aa2xtextLINE) aa2xtextLINE.SetActive(currentSettings.antiAliasing == 1);
            if (aa4xtextLINE) aa4xtextLINE.SetActive(currentSettings.antiAliasing == 2);
            if (aa8xtextLINE) aa8xtextLINE.SetActive(currentSettings.antiAliasing == 3);
        }

        public float XSensitivity => currentSettings != null ? currentSettings.xSensitivity : 1f;
        public float YSensitivity => currentSettings != null ? currentSettings.ySensitivity : 1f;
        public float MouseSmoothing => currentSettings != null ? currentSettings.mouseSmoothing : 0f;
        public float EffectVolume => currentSettings != null ? currentSettings.effectVolume : 1f;
        public float MusicVolume => currentSettings != null ? currentSettings.musicVolume : 0.5f;

        public int BugPhobiaMode => currentSettings != null ? currentSettings.bugPhobiaMode : 0;
    }
}