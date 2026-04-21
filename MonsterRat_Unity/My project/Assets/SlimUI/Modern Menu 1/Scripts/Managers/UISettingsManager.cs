using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.IO; 

namespace SlimUI.ModernMenu
{

    // json으로 저장할 데이터 클래스 
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

        [Header("UI BEHAVIOR")]
        public bool canUseEscKey = true;
        public enum Platform { Desktop, Mobile };
        public Platform platform;

        // // toggle buttons   필요음슴
        // [Header("MOBILE SETTINGS")]
        // public GameObject mobileSFXtext;
        // public GameObject mobileMusictext;
        // public GameObject mobileShadowofftextLINE;
        // public GameObject mobileShadowlowtextLINE;
        // public GameObject mobileShadowhightextLINE;

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
        private string saveFilePath;

        private bool isInitializing = false;        // 초기화중에는 Json값 저장 못하게 막음

        private void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            // 저장 경로 설정 (OS별로 안전한 영구 저장 경로 자동 지정됨)
            saveFilePath = Application.persistentDataPath + "/GameSettings.json";

            isInitializing = true;

            // 시작 시 JSON 파일 불러오기 
            LoadSettings();

            // 시작할 때는 메뉴를 숨겨둡니다.
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // 불러온 데이터로 UI 및 게임 엔진 설정 업데이트
            ApplySettingsToUIAndEngine();

            isInitializing = false;

            Debug.Log("세이브 파일 실제 위치: " + Application.persistentDataPath);
        }

        public void Update()
        {
            // [수정됨] canUseEscKey가 true일 때만 ESC 키를 인식합니다.
            if (canUseEscKey && Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleSettingsMenu();
            }
        }

        // ==========================================
        // UI 토글 및 마우스 커서 관리 (멀티플레이 고려)
        // ==========================================
        public void ToggleSettingsMenu()
        {
            if (settingsPanel == null) return;

            bool isCurrentlyActive = settingsPanel.activeSelf;
            settingsPanel.SetActive(!isCurrentlyActive);

            isMenuOpen = settingsPanel.activeSelf;
            if (!isCurrentlyActive)
            {
                // 메뉴가 열렸을 때 (게임은 계속 진행되도록 Time.timeScale = 0은 쓰지 않음)
                Cursor.lockState = CursorLockMode.None; // 마우스 락 풀기
                Cursor.visible = true;                  // 마우스 보이기
            }
            else
            {
                // 메뉴가 닫혔을 때 (다시 게임 조작 상태로 복귀)
                Cursor.lockState = CursorLockMode.Locked; // 마우스 다시 화면 중앙에 고정
                Cursor.visible = false;                   // 마우스 숨기기

                // 메뉴를 닫을 때 현재 슬라이더 값들을 한 번 더 확실하게 저장
                SaveSettings();
            }
        }

        // json 세이브로드 로직
        private void LoadSettings()
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                currentSettings = JsonUtility.FromJson<GameSettingsData>(json);
               // Debug.Log("설정 파일 불러오기 성공: " + saveFilePath);
            }
            else
            {
                // 파일이 없으면 새 객체 생성 (기본값)
                currentSettings = new GameSettingsData();
                SaveSettings();
               // Debug.Log("새 설정 파일 생성됨: " + saveFilePath);
            }
        }

        public void SaveSettings()
        {
            if (isInitializing) return;

            // 1. Music Slider 저장 및 전체 볼륨 조절
            if (musicSlider != null)
            {
                currentSettings.musicVolume = musicSlider.value;
                PlayerPrefs.SetFloat("MusicVolume", currentSettings.musicVolume);

                if (bgmSource != null)
                    bgmSource.volume = currentSettings.musicVolume;
            }

            if (effectSlider != null)
            {
                currentSettings.effectVolume = effectSlider.value;
                PlayerPrefs.SetFloat("EffectVolume", currentSettings.effectVolume);

                if (sfxSource != null)
                    sfxSource.volume = currentSettings.effectVolume;
            }

            PlayerPrefs.Save();

            // 2. Sensitivity X 저장
            if (sensitivityXSlider != null)
            {
                Slider slider = sensitivityXSlider.GetComponent<Slider>();
                if (slider != null)
                {
                    currentSettings.xSensitivity = slider.value;
                    PlayerPrefs.SetFloat("XSensitivity", currentSettings.xSensitivity);
                }
            }

            // 3. Sensitivity Y 저장
            if (sensitivityYSlider != null)
            {
                Slider slider = sensitivityYSlider.GetComponent<Slider>();
                if (slider != null)
                {
                    currentSettings.ySensitivity = slider.value;
                    PlayerPrefs.SetFloat("YSensitivity", currentSettings.ySensitivity);
                }
            }

            // 4. Mouse Smoothing 저장
            if (mouseSmoothSlider != null)
            {
                Slider slider = mouseSmoothSlider.GetComponent<Slider>();
                if (slider != null)
                {
                    currentSettings.mouseSmoothing = slider.value;
                    PlayerPrefs.SetFloat("MouseSmoothing", currentSettings.mouseSmoothing);
                }
            }

            // JSON 파일로 영구 저장 (안전 장치 추가)
            if (!string.IsNullOrEmpty(saveFilePath) && currentSettings != null)
            {
                string json = JsonUtility.ToJson(currentSettings, true);
                File.WriteAllText(saveFilePath, json);
            }
        }

        // ==========================================
        // 불러온 데이터로 UI 상태와 QualitySettings 맞추기
        // ==========================================
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

            if (bgmSource != null)
                bgmSource.volume = currentSettings.musicVolume;

            if (sfxSource != null)
                sfxSource.volume = currentSettings.effectVolume;

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
            //  else if (platform == Platform.Mobile)
            //  {
            //      if (currentSettings.mobileShadows == 0) MobileShadowsOff();
            //      else if (currentSettings.mobileShadows == 1) MobileShadowsLow();
            //      else if (currentSettings.mobileShadows == 2) MobileShadowsHigh();
            //  }

            QualitySettings.vSyncCount = currentSettings.vSyncCount;
            if (vsynctext) vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";
            if (invertmousetext) invertmousetext.GetComponent<TMP_Text>().text = currentSettings.inverted == 0 ? "off" : "on";
            if (motionblurtext) motionblurtext.GetComponent<TMP_Text>().text = currentSettings.motionBlur == 0 ? "off" : "on";
            if (ambientocclusiontext) ambientocclusiontext.GetComponent<TMP_Text>().text = currentSettings.ambientOcclusion == 0 ? "off" : "on";

            if (currentSettings.textures == 0) TexturesLow();
            else if (currentSettings.textures == 1) TexturesMed();
            else if (currentSettings.textures == 2) TexturesHigh();
        }

        // ==========================================
        // UI 버튼 클릭 이벤트들 (값 변경 후 SaveSettings 호출)
        // ==========================================
        public void FullScreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (fullscreentext) fullscreentext.GetComponent<TMP_Text>().text = Screen.fullScreen ? "on" : "off";
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
        public void SensitivityXSlider() { SaveSettings(); }
        public void SensitivityYSlider() { SaveSettings(); }
        public void SensitivitySmoothing() { SaveSettings(); }

        public void ShowHUD()
        {
            currentSettings.showHUD = currentSettings.showHUD == 0 ? 1 : 0;
            if (showhudtext) showhudtext.GetComponent<TMP_Text>().text = currentSettings.showHUD == 0 ? "off" : "on";
            SaveSettings();
        }

        // public void MobileSFXMute()
        // {
        //     currentSettings.mobileMuteSfx = currentSettings.mobileMuteSfx == 0 ? 1 : 0;
        //     if (mobileSFXtext) mobileSFXtext.GetComponent<TMP_Text>().text = currentSettings.mobileMuteSfx == 0 ? "off" : "on";
        //     SaveSettings();
        // }
        //
        // public void MobileMusicMute()
        // {
        //     currentSettings.mobileMuteMusic = currentSettings.mobileMuteMusic == 0 ? 1 : 0;
        //     if (mobileMusictext) mobileMusictext.GetComponent<TMP_Text>().text = currentSettings.mobileMuteMusic == 0 ? "off" : "on";
        //     SaveSettings();
        // }

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

        // public void MobileShadowsOff()
        // {
        //     currentSettings.mobileShadows = 0;
        //     QualitySettings.shadowCascades = 0;
        //     QualitySettings.shadowDistance = 0;
        //     if (mobileShadowofftextLINE) mobileShadowofftextLINE.gameObject.SetActive(true);
        //     if (mobileShadowlowtextLINE) mobileShadowlowtextLINE.gameObject.SetActive(false);
        //     if (mobileShadowhightextLINE) mobileShadowhightextLINE.gameObject.SetActive(false);
        //     SaveSettings();
        // }
        //
        // public void MobileShadowsLow()
        // {
        //     currentSettings.mobileShadows = 1;
        //     QualitySettings.shadowCascades = 2;
        //     QualitySettings.shadowDistance = 75;
        //     if (mobileShadowofftextLINE) mobileShadowofftextLINE.gameObject.SetActive(false);
        //     if (mobileShadowlowtextLINE) mobileShadowlowtextLINE.gameObject.SetActive(true);
        //     if (mobileShadowhightextLINE) mobileShadowhightextLINE.gameObject.SetActive(false);
        //     SaveSettings();
        // }
        //
        // public void MobileShadowsHigh()
        // {
        //     currentSettings.mobileShadows = 2;
        //     QualitySettings.shadowCascades = 4;
        //     QualitySettings.shadowDistance = 100;
        //     if (mobileShadowofftextLINE) mobileShadowofftextLINE.gameObject.SetActive(false);
        //     if (mobileShadowlowtextLINE) mobileShadowlowtextLINE.gameObject.SetActive(false);
        //     if (mobileShadowhightextLINE) mobileShadowhightextLINE.gameObject.SetActive(true);
        //     SaveSettings();
        // }

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

        public float EffectVolume => currentSettings != null ? currentSettings.effectVolume : 1f;
        public float MusicVolume => currentSettings != null ? currentSettings.musicVolume : 1f;
    }
}