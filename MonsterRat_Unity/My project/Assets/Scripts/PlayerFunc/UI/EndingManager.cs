using TMPro;
using UnityEngine;
using System.Collections;
using Fusion;
using UnityEngine.SceneManagement;

public class EndingManager : NetworkBehaviour
{
    public TMP_Text ending;
    public TMP_Text playTime;

    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private string lobbySceneName = "ZZin_Main_Lobby";

    private void Start()
    {
        SetAlpha(ending, 0f);
        SetAlpha(playTime, 0f);

        if (PlayTimeManager.Instance != null)
        {
            playTime.text = FormatPlayTime(PlayTimeManager.Instance.PlayTime);
        }

        StartCoroutine(StartEnding());
    }

    IEnumerator StartEnding()
    {
        yield return StartCoroutine(FadeInText(ending, fadeDuration));

        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeInText(playTime, fadeDuration));

        yield return new WaitForSeconds(1f);

        StartCoroutine(FadeOutText(ending, fadeDuration));
        yield return StartCoroutine(FadeOutText(playTime, fadeDuration));

        yield return new WaitForSeconds(1f);

        MoveToLobby();
    }

    void MoveToLobby()
    {
        if (!HasStateAuthority) return;

        if (PlayTimeManager.Instance != null)
        {
            PlayTimeManager.Instance.StopCounting();
        }

        DeleteCurrentSaveSlot();

        Runner.LoadScene(lobbySceneName);
    }

    void DeleteCurrentSaveSlot()
    {
        int activeSlot = PlayerPrefs.GetInt("CurrentActiveSaveSlot", -1);

        if (activeSlot >= 0)
        {
            PlayerPrefs.DeleteKey("SaveSlot_" + activeSlot);
        }

        PlayerPrefs.DeleteKey("MasterWorldSave");
        PlayerPrefs.DeleteKey("IsPollutionLeft");
        PlayerPrefs.DeleteKey("SpawnInventoryOnGround");

        PlayerPrefs.Save();

        Debug.Log($"클리어 완료: SaveSlot_{activeSlot} 삭제됨");
    }

    string FormatPlayTime(float time)
    {
        int totalSeconds = Mathf.FloorToInt(time);

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"클리어 타임 : {hours:00}시간 {minutes:00}분 {seconds:00}초";
    }

    IEnumerator FadeInText(TMP_Text text, float duration)
    {
        float elapsed = 0f;
        Color color = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            text.color = color;
            yield return null;
        }

        color.a = 1f;
        text.color = color;
    }

    IEnumerator FadeOutText(TMP_Text text, float duration)
    {
        float elapsed = 0f;
        Color color = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            text.color = color;
            yield return null;
        }

        color.a = 0f;
        text.color = color;
    }

    void SetAlpha(TMP_Text text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}