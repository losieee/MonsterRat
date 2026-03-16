using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections;
using UnityEngine.SceneManagement;

public class TutorialDialogueSystem : MonoBehaviour
{
    [Header("Resources 경로")]
    public string csvResourcePath = "Dialogues/DialogueDB";

    public Text dialogueText;
    public int startIndex = 0;              // 시작 인덱스
    public int endIndexValue = -100;        // 끝날 인덱스
    public GameObject choicePanel;

    [Header("글자 페이드 인/아웃")]
    public bool autoPlay = true;
    public float fadeInTime = 0.35f;
    public float fadeOutTime = 0.35f;
    public float betweenDelay = 0.15f;      // 사이 텀

    [Header("텍스트 유지 시간")]
    public float baseStayTime = 1.0f;       // 기본 시간
    public float secondsPerChar = 0.04f;    // 글자 1개당 추가 시간
    public float minStayTime = 1.2f;        // 최소 시간
    public float maxStayTime = 6.0f;        // 최대 시간

    // 자동 진행 멈출 인덱스
    public List<int> pauseIndices = new List<int>() { };

    // 멈출 때 외부에서 호출 할거 
    public event Action<int> OnPausedAtIndex;
    public event Action<int> OnLineShown;

    // 내부 데이터
    private readonly Dictionary<int, Line> _lines = new Dictionary<int, Line>();
    private int _currentIndex;
    private Coroutine _playCo;
    private bool _isPausedByIndex = false;
    private int _pausedIndex = int.MinValue;

    [Serializable]
    private class Line
    {
        public int index;
        public string dialogue;
        public int nextIndex;
    }

    private void Awake()
    {
        LoadCsvFromResources();

        if (dialogueText != null)
        {
            var c = dialogueText.color;
            c.a = 0f;
            dialogueText.color = c;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void GoToTutorial()
    {
        choicePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(WaitStart());
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("ZZin_Main_Lobby");
    }

    public void StartDialogue(int index)
    {
        _currentIndex = index;
        ShowCurrent();
    }

    // 자동 진행 시작
    public void StartAuto()
    {
        _isPausedByIndex = false;
        if (_playCo != null) StopCoroutine(_playCo);
        _playCo = StartCoroutine(PlayRoutine());
    }

    // 자동 진행 중지
    public void StopAuto()
    {
        if (_playCo != null)
        {
            StopCoroutine(_playCo);
            _playCo = null;
        }
    }

    // 멈춘 후 재시작
    public void ResumeAuto()
    {
        if (!_isPausedByIndex) return;

        if (_lines.TryGetValue(_currentIndex, out var line))
        {
            if (line.nextIndex != endIndexValue)
                _currentIndex = line.nextIndex;
        }

        _isPausedByIndex = false;
        StartAuto();
    }

    // 현재 해당하는 줄 표시
    private void ShowCurrent()
    {
        if (!_lines.TryGetValue(_currentIndex, out var line)) return;

        if (dialogueText != null)
            dialogueText.text = line.dialogue;

        OnLineShown?.Invoke(_currentIndex);
    }

    // 텍스트 자동 진행 루틴
    private IEnumerator PlayRoutine()
    {
        while (true)
        {
            if (!_lines.TryGetValue(_currentIndex, out var line))
                yield break;

            // 현재 줄 텍스트 세팅
            ShowCurrent();

            // 페이드 인
            yield return FadeTextAlpha(0f, 1f, fadeInTime);

            // 특정 인덱스면 멈춤
            if (pauseIndices.Contains(_currentIndex))
            {
                _isPausedByIndex = true;
                _pausedIndex = _currentIndex;
                StopAuto();         // 멈춤

                // 멈췄음을 외부에 알림
                OnPausedAtIndex?.Invoke(_currentIndex);
                yield break;
            }

            // 읽을 시간 유지
            float stay = CalcStayTime(line.dialogue);
            yield return new WaitForSeconds(stay);

            // 페이드 아웃
            yield return FadeTextAlpha(1f, 0f, fadeOutTime);

            // 다음 대사 딜레이
            if (betweenDelay > 0f)
                yield return new WaitForSeconds(betweenDelay);

            // 끝이면 종료
            if (line.nextIndex == endIndexValue)
                yield break;

            // 다음으로 이동
            _currentIndex = line.nextIndex;
        }
    }

    // 텍스트 길이 기반 유지시간 계산
    private float CalcStayTime(string text)
    {
        if (string.IsNullOrEmpty(text))
            return minStayTime;

        int len = text.Trim().Length;

        // base + (글자수 * 글자당 시간)
        float t = baseStayTime + (len * secondsPerChar);

        // 너무 짧거나 길지 않게 제한
        return Mathf.Clamp(t, minStayTime, maxStayTime);
    }

    // 텍스트 페이드 조절
    private IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        if (dialogueText == null) yield break;

        // duration이 0이면 즉시 적용
        if (duration <= 0f)
        {
            var c0 = dialogueText.color;
            c0.a = to;
            dialogueText.color = c0;
            yield break;
        }

        float t = 0f;
        Color c = dialogueText.color;

        c.a = from;
        dialogueText.color = c;

        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);

            // 부드럽게 보간
            float a = Mathf.SmoothStep(from, to, f);

            c = dialogueText.color;
            c.a = a;
            dialogueText.color = c;

            yield return null;
        }

        // 마지막 값 고정
        c = dialogueText.color;
        c.a = to;
        dialogueText.color = c;
    }

    // 텍스트 -> index로 변환
    private void LoadCsvFromResources()
    {
        TextAsset csvAsset = Resources.Load<TextAsset>(csvResourcePath);
        if (csvAsset == null) return;

        var table = CsvParser.Parse(csvAsset.text);

        if (table.Count <= 1) return;

        // 헤더에서 컬럼 위치 찾기
        var header = table[0];
        int colIndex = header.IndexOf("index");
        int colDialogue = header.IndexOf("dialogue");
        int colNext = header.IndexOf("nextIndex");

        if (colIndex < 0 || colDialogue < 0 || colNext < 0) return;

        _lines.Clear();

        for (int r = 1; r < table.Count; r++)
        {
            var row = table[r];
            if (row == null || row.Count == 0) continue;

            if (colIndex >= row.Count) continue;
            if (!int.TryParse(row[colIndex].Trim(), out int id)) continue;

            string text = (colDialogue < row.Count) ? row[colDialogue] : "";
            int next = endIndexValue;
            if (colNext < row.Count) int.TryParse(row[colNext].Trim(), out next);

            _lines[id] = new Line
            {
                index = id,
                dialogue = text,
                nextIndex = next
            };
        }
    }

    private IEnumerator WaitStart()
    {
        TutorialScreenFader.Instance.StartFade(1f, 0f, blockInput: true);

        yield return new WaitForSeconds(1f);

        StartDialogue(startIndex);
        if (autoPlay)
            StartAuto();

        yield return StartCoroutine(FadeDialogueOnly(0f, 1f, 0.6f));

        yield return new WaitForSeconds(1.5f);

        TutorialScreenFader.Instance.FadeIn(2f);

        yield return new WaitForSeconds(1f);

        
    }

    private IEnumerator FadeDialogueOnly(float from, float to, float duration)
    {
        if (dialogueText == null) yield break;

        float t = 0f;
        var c = dialogueText.color;
        c.a = from;
        dialogueText.color = c;

        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            c = dialogueText.color;
            c.a = Mathf.SmoothStep(from, to, f);
            dialogueText.color = c;
            yield return null;
        }

        c = dialogueText.color;
        c.a = to;
        dialogueText.color = c;
    }


    private static class CsvParser
    {
        public static List<List<string>> Parse(string csv)
        {
            var table = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();

            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        row.Add(field.ToString());
                        field.Clear();
                    }
                    else if (c == '\r')
                    {
                        continue;
                    }
                    else if (c == '\n')
                    {
                        row.Add(field.ToString());
                        field.Clear();

                        table.Add(row);
                        row = new List<string>();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
            }

            row.Add(field.ToString());
            table.Add(row);

            if (table.Count > 0)
            {
                var last = table[table.Count - 1];
                if (last.Count == 1 && string.IsNullOrWhiteSpace(last[0]))
                    table.RemoveAt(table.Count - 1);
            }

            return table;
        }
    }
}