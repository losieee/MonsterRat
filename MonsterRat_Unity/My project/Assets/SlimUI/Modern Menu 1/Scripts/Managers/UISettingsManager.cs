using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using UnityEngine.SceneManagement;

namespace SlimUI.ModernMenu
{
    // json으로 변환시킬 데이터 클래스
    [System.Serializable]
    public class GameSettingsData
    {
        public float musicVolume = 1.0f;
        public float effectVolume = 1.0f;
        public float xSensitivity = 1.0f;
        public float ySensitivity = 1.0f;
        public float mouseSmoothing = 0.0f;

        public int normalDifficulty = 1;
        public int hardCoreDifficulty = 0;

        public int showHUD = 1;
        public int toolTips = 1;

        public int shadows = 2; // 0: Off, 1: Low, 2: High
        public int mobileShadows = 2;

        public int vSyncCount = 1;
        public int inverted = 0;
        public int motionBlur = 1;
        public int ambientOcclusion = 1;
        public int cameraEffects = 1;
        public int textures = 2; // 0: Low, 1: Med, 2: High

        public int mobileMuteSfx = 0;
        public int mobileMuteMusic = 0;
    }

    public class UISettingsManager : MonoBehaviour
    {
        public static UISettingsManager Instance;

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
        public GameObject difficultynormaltext;
        public GameObject difficultynormaltextLINE;
        public GameObject difficultyhardcoretext;
        public GameObject difficultyhardcoretextLINE;
        public GameObject exitPanel;

        [Header("CONTROLS SETTINGS")]
        public GameObject invertmousetext;

        [Header("AUDIO SETTINGS")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        // sliders
        public Slider musicSlider;
        public Slider effectSlider;
        public GameObject sensitivityXSlider;
        public GameObject sensitivityYSlider;
        public GameObject mouseSmoothSlider;

        // JSON 데이터 관리 객체
        private GameSettingsData currentSettings;

        private bool isInitializing = false;

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            isInitializing = true;

            // 시작 시 JSON 파일 불러오기 
            LoadSettings();

            if (settingsPanel != null) settingsPanel.SetActive(false);

            ApplySettingsToUIAndEngine();

            isInitializing = false;
        }

        public void Update()
        {
            if (GameInputLock.IsLocked) return;

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

        // ★ 수정됨: PhotonInventory처럼 PlayerPrefs에 JSON 형식으로 읽고 쓰기
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

        public void LeaveTutorial()
        {
            SceneManager.LoadScene("ZZin_Main_Lobby");
        }

        public void SaveSettings()
        {
            if (isInitializing) return;

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
                if (slider != null)
                {
                    currentSettings.xSensitivity = slider.value;
                }
            }

            if (sensitivityYSlider != null)
            {
                Slider slider = sensitivityYSlider.GetComponent<Slider>();
                if (slider != null)
                {
                    currentSettings.ySensitivity = slider.value;
                }
            }

            if (mouseSmoothSlider != null)
            {
                Slider slider = mouseSmoothSlider.GetComponent<Slider>();
                if (slider != null)
                {
                    currentSettings.mouseSmoothing = slider.value;
                }
            }

            // ★ 수정됨: PhotonInventory 방식으로 JSON 데이터를 레지스트리에 저장!
            if (currentSettings != null)
            {
                string jsonString = JsonUtility.ToJson(currentSettings, true);
                PlayerPrefs.SetString("MasterGameSettings", jsonString);
                PlayerPrefs.Save();
            }
        }

        private void ApplySettingsToUIAndEngine()
        {
            if (currentSettings.normalDifficulty == 1)
            {
                difficultynormaltextLINE.gameObject.SetActive(true);
                difficultyhardcoretextLINE.gameObject.SetActive(false);
            }
            else
            {
                difficultyhardcoretextLINE.gameObject.SetActive(true);
                difficultynormaltextLINE.gameObject.SetActive(false);
            }

            if (musicSlider) musicSlider.SetValueWithoutNotify(currentSettings.musicVolume);
            if (effectSlider) effectSlider.SetValueWithoutNotify(currentSettings.effectVolume);

            if (bgmSource != null) bgmSource.volume = currentSettings.musicVolume;
            if (sfxSource != null) sfxSource.volume = currentSettings.effectVolume;

            if (sensitivityXSlider) sensitivityXSlider.GetComponent<Slider>().value = currentSettings.xSensitivity;
            if (sensitivityYSlider) sensitivityYSlider.GetComponent<Slider>().value = currentSettings.ySensitivity;
            if (mouseSmoothSlider) mouseSmoothSlider.GetComponent<Slider>().value = currentSettings.mouseSmoothing;

            if (fullscreentext) fullscreentext.GetComponent<TMP_Text>().text = Screen.fullScreen ? "on" : "off";
            if (showhudtext) showhudtext.GetComponent<TMP_Text>().text = currentSettings.showHUD == 0 ? "off" : "on";
            if (tooltipstext) tooltipstext.GetComponent<TMP_Text>().text = currentSettings.toolTips == 0 ? "off" : "on";

            if (platform == Platform.Desktop)
            {
                if (currentSettings.shadows == 0) ShadowsOff();
                else if (currentSettings.shadows == 1) ShadowsLow();
                else if (currentSettings.shadows == 2) ShadowsHigh();
            }

            QualitySettings.vSyncCount = currentSettings.vSyncCount;
            if (vsynctext) vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";
            if (invertmousetext) invertmousetext.GetComponent<TMP_Text>().text = currentSettings.inverted == 0 ? "off" : "on";
            if (motionblurtext) motionblurtext.GetComponent<TMP_Text>().text = currentSettings.motionBlur == 0 ? "off" : "on";
            if (ambientocclusiontext) ambientocclusiontext.GetComponent<TMP_Text>().text = currentSettings.ambientOcclusion == 0 ? "off" : "on";

            if (currentSettings.textures == 0) TexturesLow();
            else if (currentSettings.textures == 1) TexturesMed();
            else if (currentSettings.textures == 2) TexturesHigh();
        }

        public void FullScreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (fullscreentext) fullscreentext.GetComponent<TMP_Text>().text = Screen.fullScreen ? "on" : "off";
        }

        public void MusicSlider() { if (!isInitializing) SaveSettings(); }
        public void EffectSlider() { if (!isInitializing) SaveSettings(); }
        public void SensitivityXSlider() { SaveSettings(); }
        public void SensitivityYSlider() { SaveSettings(); }
        public void SensitivitySmoothing() { SaveSettings(); }

        public void ShowHUD()
        {
            currentSettings.showHUD = currentSettings.showHUD == 0 ? 1 : 0;
            if (showhudtext) showhudtext.GetComponent<TMP_Text>().text = currentSettings.showHUD == 0 ? "off" : "on";
            SaveSettings();
        }

        public void ToolTips()
        {
            currentSettings.toolTips = currentSettings.toolTips == 0 ? 1 : 0;
            if (tooltipstext) tooltipstext.GetComponent<TMP_Text>().text = currentSettings.toolTips == 0 ? "off" : "on";
            SaveSettings();
        }

        public void NormalDifficulty()
        {
            if (difficultyhardcoretextLINE) difficultyhardcoretextLINE.gameObject.SetActive(false);
            if (difficultynormaltextLINE) difficultynormaltextLINE.gameObject.SetActive(true);
            currentSettings.normalDifficulty = 1;
            currentSettings.hardCoreDifficulty = 0;
            SaveSettings();
        }

        public void HardcoreDifficulty()
        {
            if (difficultyhardcoretextLINE) difficultyhardcoretextLINE.gameObject.SetActive(true);
            if (difficultynormaltextLINE) difficultynormaltextLINE.gameObject.SetActive(false);
            currentSettings.normalDifficulty = 0;
            currentSettings.hardCoreDifficulty = 1;
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

        public void vsync()
        {
            currentSettings.vSyncCount = currentSettings.vSyncCount == 0 ? 1 : 0;
            QualitySettings.vSyncCount = currentSettings.vSyncCount;
            if (vsynctext) vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";
            SaveSettings();
        }

        public void InvertMouse()
        {
            currentSettings.inverted = currentSettings.inverted == 0 ? 1 : 0;
            if (invertmousetext) invertmousetext.GetComponent<TMP_Text>().text = currentSettings.inverted == 0 ? "off" : "on";
            SaveSettings();
        }

        public void MotionBlur()
        {
            currentSettings.motionBlur = currentSettings.motionBlur == 0 ? 1 : 0;
            if (motionblurtext) motionblurtext.GetComponent<TMP_Text>().text = currentSettings.motionBlur == 0 ? "off" : "on";
            SaveSettings();
        }

        public void AmbientOcclusion()
        {
            currentSettings.ambientOcclusion = currentSettings.ambientOcclusion == 0 ? 1 : 0;
            if (ambientocclusiontext) ambientocclusiontext.GetComponent<TMP_Text>().text = currentSettings.ambientOcclusion == 0 ? "off" : "on";
            SaveSettings();
        }

        public void CameraEffects()
        {
            currentSettings.cameraEffects = currentSettings.cameraEffects == 0 ? 1 : 0;
            if (cameraeffectstext) cameraeffectstext.GetComponent<TMP_Text>().text = currentSettings.cameraEffects == 0 ? "off" : "on";
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

        public float XSensitivity => currentSettings != null ? currentSettings.xSensitivity : 1f;
        public float YSensitivity => currentSettings != null ? currentSettings.ySensitivity : 1f;
        public float MouseSmoothing => currentSettings != null ? currentSettings.mouseSmoothing : 0f;
        public float EffectVolume => currentSettings != null ? currentSettings.effectVolume : 1f;
        public float MusicVolume => currentSettings != null ? currentSettings.musicVolume : 1f;
    }
}