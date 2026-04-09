using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.IO; // JSON 파일 저장을 위해 필요

namespace SlimUI.ModernMenu
{

    [System.Serializable]
    public class GameSettingsData
    {
        public float musicVolume = 1.0f;
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

    public class PhotonUIOption : MonoBehaviour
    {

        [Header("UI PANEL REFERENCE")]
        [Tooltip("ESC를 눌렀을 때 껐다 켜질 전체 설정 UI 패널(Canvas 또는 Panel)을 연결하세요.")]
        public GameObject settingsPanel;

        public enum Platform { Desktop, Mobile };
        public Platform platform;

        [Header("MOBILE SETTINGS")]
        public GameObject mobileSFXtext;
        public GameObject mobileMusictext;
        public GameObject mobileShadowofftextLINE;
        public GameObject mobileShadowlowtextLINE;
        public GameObject mobileShadowhightextLINE;

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

        public GameObject musicSlider;
        public GameObject sensitivityXSlider;
        public GameObject sensitivityYSlider;
        public GameObject mouseSmoothSlider;

        private GameSettingsData currentSettings;
        private string saveFilePath;

        public void Start()
        {
            saveFilePath = Application.persistentDataPath + "/GameSettings.json";

            // 시작 시 JSON 파일 불러오기 (파일이 없으면 기본값 생성)
            LoadSettings();

            // 시작할 때는 메뉴를 숨겨둡니다.
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // 불러온 데이터로 UI 및 게임 엔진 설정 업데이트
            ApplySettingsToUIAndEngine();
        }

        public void Update()
        {
            // ESC 키를 눌렀을 때 UI 껐다 켜기
            if (Input.GetKeyDown(KeyCode.Escape))
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

        // ==========================================
        // JSON 세이브 & 로드 로직
        // ==========================================
        private void LoadSettings()
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                currentSettings = JsonUtility.FromJson<GameSettingsData>(json);
                Debug.Log("설정 파일 불러오기 성공: " + saveFilePath);
            }
            else
            {
                // 파일이 없으면 새 객체 생성 (기본값)
                currentSettings = new GameSettingsData();
                SaveSettings();
                Debug.Log("새 설정 파일 생성됨: " + saveFilePath);
            }
        }

        public void SaveSettings()
        {
            // 슬라이더 값 최신화
            if (musicSlider != null) currentSettings.musicVolume = musicSlider.GetComponent<Slider>().value;
            if (sensitivityXSlider != null) currentSettings.xSensitivity = sensitivityXSlider.GetComponent<Slider>().value;
            if (sensitivityYSlider != null) currentSettings.ySensitivity = sensitivityYSlider.GetComponent<Slider>().value;
            if (mouseSmoothSlider != null) currentSettings.mouseSmoothing = mouseSmoothSlider.GetComponent<Slider>().value;

            string json = JsonUtility.ToJson(currentSettings, true); // true는 보기 좋게 줄바꿈 해줌
            File.WriteAllText(saveFilePath, json);
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

            musicSlider.GetComponent<Slider>().value = currentSettings.musicVolume;
            sensitivityXSlider.GetComponent<Slider>().value = currentSettings.xSensitivity;
            sensitivityYSlider.GetComponent<Slider>().value = currentSettings.ySensitivity;
            mouseSmoothSlider.GetComponent<Slider>().value = currentSettings.mouseSmoothing;

            if (Screen.fullScreen == true) fullscreentext.GetComponent<TMP_Text>().text = "on";
            else fullscreentext.GetComponent<TMP_Text>().text = "off";

            showhudtext.GetComponent<TMP_Text>().text = currentSettings.showHUD == 0 ? "off" : "on";
            tooltipstext.GetComponent<TMP_Text>().text = currentSettings.toolTips == 0 ? "off" : "on";

            if (platform == Platform.Desktop)
            {
                if (currentSettings.shadows == 0) ShadowsOff();
                else if (currentSettings.shadows == 1) ShadowsLow();
                else if (currentSettings.shadows == 2) ShadowsHigh();
            }
            else if (platform == Platform.Mobile)
            {
                if (currentSettings.mobileShadows == 0) MobileShadowsOff();
                else if (currentSettings.mobileShadows == 1) MobileShadowsLow();
                else if (currentSettings.mobileShadows == 2) MobileShadowsHigh();
            }

            QualitySettings.vSyncCount = currentSettings.vSyncCount;
            vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";

            invertmousetext.GetComponent<TMP_Text>().text = currentSettings.inverted == 0 ? "off" : "on";
            motionblurtext.GetComponent<TMP_Text>().text = currentSettings.motionBlur == 0 ? "off" : "on";
            ambientocclusiontext.GetComponent<TMP_Text>().text = currentSettings.ambientOcclusion == 0 ? "off" : "on";

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
            fullscreentext.GetComponent<TMP_Text>().text = Screen.fullScreen ? "on" : "off";
        }

        public void MusicSlider() { SaveSettings(); }
        public void SensitivityXSlider() { SaveSettings(); }
        public void SensitivityYSlider() { SaveSettings(); }
        public void SensitivitySmoothing() { SaveSettings(); }

        public void ShowHUD()
        {
            currentSettings.showHUD = currentSettings.showHUD == 0 ? 1 : 0;
            showhudtext.GetComponent<TMP_Text>().text = currentSettings.showHUD == 0 ? "off" : "on";
            SaveSettings();
        }

        public void MobileSFXMute()
        {
            currentSettings.mobileMuteSfx = currentSettings.mobileMuteSfx == 0 ? 1 : 0;
            mobileSFXtext.GetComponent<TMP_Text>().text = currentSettings.mobileMuteSfx == 0 ? "off" : "on";
            SaveSettings();
        }

        public void MobileMusicMute()
        {
            currentSettings.mobileMuteMusic = currentSettings.mobileMuteMusic == 0 ? 1 : 0;
            mobileMusictext.GetComponent<TMP_Text>().text = currentSettings.mobileMuteMusic == 0 ? "off" : "on";
            SaveSettings();
        }

        public void ToolTips()
        {
            currentSettings.toolTips = currentSettings.toolTips == 0 ? 1 : 0;
            tooltipstext.GetComponent<TMP_Text>().text = currentSettings.toolTips == 0 ? "off" : "on";
            SaveSettings();
        }

        public void NormalDifficulty()
        {
            difficultyhardcoretextLINE.gameObject.SetActive(false);
            difficultynormaltextLINE.gameObject.SetActive(true);
            currentSettings.normalDifficulty = 1;
            currentSettings.hardCoreDifficulty = 0;
            SaveSettings();
        }

        public void HardcoreDifficulty()
        {
            difficultyhardcoretextLINE.gameObject.SetActive(true);
            difficultynormaltextLINE.gameObject.SetActive(false);
            currentSettings.normalDifficulty = 0;
            currentSettings.hardCoreDifficulty = 1;
            SaveSettings();
        }

        public void ShadowsOff()
        {
            currentSettings.shadows = 0;
            QualitySettings.shadowCascades = 0;
            QualitySettings.shadowDistance = 0;
            shadowofftextLINE.gameObject.SetActive(true);
            shadowlowtextLINE.gameObject.SetActive(false);
            shadowhightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void ShadowsLow()
        {
            currentSettings.shadows = 1;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowDistance = 75;
            shadowofftextLINE.gameObject.SetActive(false);
            shadowlowtextLINE.gameObject.SetActive(true);
            shadowhightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void ShadowsHigh()
        {
            currentSettings.shadows = 2;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 500;
            shadowofftextLINE.gameObject.SetActive(false);
            shadowlowtextLINE.gameObject.SetActive(false);
            shadowhightextLINE.gameObject.SetActive(true);
            SaveSettings();
        }

        public void MobileShadowsOff()
        {
            currentSettings.mobileShadows = 0;
            QualitySettings.shadowCascades = 0;
            QualitySettings.shadowDistance = 0;
            mobileShadowofftextLINE.gameObject.SetActive(true);
            mobileShadowlowtextLINE.gameObject.SetActive(false);
            mobileShadowhightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void MobileShadowsLow()
        {
            currentSettings.mobileShadows = 1;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowDistance = 75;
            mobileShadowofftextLINE.gameObject.SetActive(false);
            mobileShadowlowtextLINE.gameObject.SetActive(true);
            mobileShadowhightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void MobileShadowsHigh()
        {
            currentSettings.mobileShadows = 2;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 100;
            mobileShadowofftextLINE.gameObject.SetActive(false);
            mobileShadowlowtextLINE.gameObject.SetActive(false);
            mobileShadowhightextLINE.gameObject.SetActive(true);
            SaveSettings();
        }

        public void vsync()
        {
            currentSettings.vSyncCount = currentSettings.vSyncCount == 0 ? 1 : 0;
            QualitySettings.vSyncCount = currentSettings.vSyncCount;
            vsynctext.GetComponent<TMP_Text>().text = currentSettings.vSyncCount == 0 ? "off" : "on";
            SaveSettings();
        }

        public void InvertMouse()
        {
            currentSettings.inverted = currentSettings.inverted == 0 ? 1 : 0;
            invertmousetext.GetComponent<TMP_Text>().text = currentSettings.inverted == 0 ? "off" : "on";
            SaveSettings();
        }

        public void MotionBlur()
        {
            currentSettings.motionBlur = currentSettings.motionBlur == 0 ? 1 : 0;
            motionblurtext.GetComponent<TMP_Text>().text = currentSettings.motionBlur == 0 ? "off" : "on";
            SaveSettings();
        }

        public void AmbientOcclusion()
        {
            currentSettings.ambientOcclusion = currentSettings.ambientOcclusion == 0 ? 1 : 0;
            ambientocclusiontext.GetComponent<TMP_Text>().text = currentSettings.ambientOcclusion == 0 ? "off" : "on";
            SaveSettings();
        }

        public void CameraEffects()
        {
            currentSettings.cameraEffects = currentSettings.cameraEffects == 0 ? 1 : 0;
            cameraeffectstext.GetComponent<TMP_Text>().text = currentSettings.cameraEffects == 0 ? "off" : "on";
            SaveSettings();
        }

        public void TexturesLow()
        {
            currentSettings.textures = 0;
            QualitySettings.globalTextureMipmapLimit = 2;
            texturelowtextLINE.gameObject.SetActive(true);
            texturemedtextLINE.gameObject.SetActive(false);
            texturehightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void TexturesMed()
        {
            currentSettings.textures = 1;
            QualitySettings.globalTextureMipmapLimit = 1;
            texturelowtextLINE.gameObject.SetActive(false);
            texturemedtextLINE.gameObject.SetActive(true);
            texturehightextLINE.gameObject.SetActive(false);
            SaveSettings();
        }

        public void TexturesHigh()
        {
            currentSettings.textures = 2;
            QualitySettings.globalTextureMipmapLimit = 0;
            texturelowtextLINE.gameObject.SetActive(false);
            texturemedtextLINE.gameObject.SetActive(false);
            texturehightextLINE.gameObject.SetActive(true);
            SaveSettings();
        }
    }
}