using UnityEngine;

public class EyeController : MonoBehaviour
{
    [Header("깜빡임 설정 (Blink)")]
    public GameObject eyeObject; // 눈 오브젝트 (또는 눈 메시)
    public float visibleDuration = 3.0f; // 나타나 있는 시간
    public float invisibleDuration = 0.15f; // 사라져 있는 시간
    private float timer;
    private bool isVisible = true;

    [Header("둥둥 움직임 설정 (Floating)")]
    public float floatAmplitude = 0.1f; // 위아래 움직임 범위
    public float floatSpeed = 0.1f;     // 움직임 속도
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
        timer = visibleDuration;
    }

    void Update()
    {
        HandleBlink();
        HandleFloat();
    }

    // 눈이 사라졌다 나타났다 하는 로직
    void HandleBlink()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            isVisible = !isVisible;
            eyeObject.SetActive(isVisible); // 오브젝트를 끄고 켬으로써 깜빡임 구현

            // 다음 상태에 맞는 타이머 설정
            timer = isVisible ? visibleDuration : invisibleDuration;
        }
    }

    // 위아래로 둥둥 움직이는 로직
    void HandleFloat()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
    }
}