using System.Collections;
using UnityEngine;

public class GasCamFollow : MonoBehaviour
{
    public TutorialDialogueSystem dialogue;
    public TutorialManager tutorialManager;
    public Transform camPivot;
    public GameObject gasAction1;
    public GameObject gasAction2;
    public GameObject spannerPos;
    public GameObject playerHand;
    public GameObject clearGauge;
    public GameObject pollutionGauge;
    public GameObject inven;

    public MonoBehaviour[] cameraControlScripts;

    public bool triggered;
    Transform mainCamTr;
    Transform originalParent;
    Vector3 originalPos;
    Quaternion originalRot;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (tutorialManager != null)
            tutorialManager.NotifyGasCamEntered();
        playerHand.SetActive(false);

        clearGauge.SetActive(false);
        pollutionGauge.SetActive(false);
        inven.SetActive(false);

        StartCoroutine(Co_FadeSnapLock());
    }

    IEnumerator Co_FadeSnapLock()
    {
        if (camPivot == null) yield break;
        if (TutorialScreenFader.Instance == null) yield break;

        Camera cam = Camera.main;
        if (cam == null) yield break;

        mainCamTr = cam.transform;

        // 원래 상태 저장
        originalParent = mainCamTr.parent;
        originalPos = mainCamTr.position;
        originalRot = mainCamTr.rotation;

        TutorialScreenFader.Instance.FadeOut(0.5f);
        yield return new WaitForSeconds(0.5f);

        if (cameraControlScripts != null)
        {
            foreach (var s in cameraControlScripts)
                if (s != null) s.enabled = false;
        }

        mainCamTr.SetParent(camPivot, worldPositionStays: false);
        mainCamTr.localPosition = Vector3.zero;
        mainCamTr.localRotation = Quaternion.identity;

        TutorialScreenFader.Instance.FadeIn(1f);
        gasAction1.SetActive(true);
        gasAction2.SetActive(true);
        spannerPos.SetActive(true);

        yield return new WaitForSeconds(4f);

        TutorialScreenFader.Instance.FadeOut(0.5f);
        yield return new WaitForSeconds(0.5f);
        RestoreCamera();
        TutorialScreenFader.Instance.FadeIn(0.5f);
        yield return new WaitForSeconds(0.5f);

        dialogue.ResumeAuto();
        playerHand.SetActive(true);
        gameObject.SetActive(false);
        clearGauge.SetActive(true);
        pollutionGauge.SetActive(true);
    }

    // 카메라 원위치
    public void RestoreCamera()
    {
        if (mainCamTr == null) return;

        // 부모 위치 기준으로 복귀
        mainCamTr.SetParent(originalParent, worldPositionStays: false);
        mainCamTr.localPosition = new Vector3(0f, 0.5f, 0f);
        mainCamTr.localRotation = Quaternion.identity;

        if (cameraControlScripts != null)
        {
            foreach (var s in cameraControlScripts)
                if (s != null) s.enabled = true;
        }
    }
}