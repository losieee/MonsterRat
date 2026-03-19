using UnityEngine;
using SlimUI.ModernMenu; // 에셋의 네임스페이스 활용

public class InGamePauseMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("ESC를 눌렀을 때 켜질 전체 일시정지/옵션 캔버스나 패널")]
    public GameObject pauseMenuWrapper;

    [Tooltip("에셋의 설정(Settings) 패널들 (비디오, 오디오 등)")]
    public GameObject settingsPanel;

    private bool isMenuOpen = false;

    void Start()
    {
        // 시작할 때는 메뉴를 무조건 꺼둡니다.
        pauseMenuWrapper.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    public void OpenMenu()
    {
        isMenuOpen = true;
        pauseMenuWrapper.SetActive(true);
        settingsPanel.SetActive(true); // 옵션 패널을 바로 띄우고 싶다면 true

        // ?? 주의: Fusion 2 멀티플레이이므로 Time.timeScale = 0f; 를 사용해 
        // 게임을 멈추면 안 됩니다! (다른 플레이어들과 핑이 어긋나게 됨)

        // 마우스 커서 활성화 (FPS/TPS 게임일 경우 필요)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        pauseMenuWrapper.SetActive(false);
        settingsPanel.SetActive(false);

        // 마우스 커서 다시 숨기기 (게임 장르에 따라 수정하세요)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}