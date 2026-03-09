using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutterController : InvenBase
{
    public override ToolType Type => ToolType.Cutter;

    public LayerMask woodMask;
    public float cutRange = 3.0f;
    public float smallScale = 0.1f;     // 작아질 크기
    public int pieces = 5;              // 몇 조각으로 나눌지
    public float scatterRadius = 0.25f; // 조각 퍼지는 범위
    public float popForce = 2.0f;       // 조각 튀는 힘
    

    bool clicked;
    int brokenLayer = 3;

    public override void Tick()
    {
        if (Input.GetMouseButtonDown(0) && !clicked)
        {
            clicked = true;
            TryCutWood();
        }

        if (Input.GetMouseButtonUp(0))
        {
            clicked = false;
        }
    }

    void TryCutWood()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, cutRange, woodMask, QueryTriggerInteraction.Ignore))
            return;

        GameObject target = hit.collider.gameObject;

        // 이미 잘린 조각 또 자르는 거 방지
        if (target.GetComponentInParent<CutterPiece>() != null)
            return;

        GameObject root = target.transform.root.gameObject;
        CutIntoPieces(root, hit.point, hit.normal);
    }

    void CutIntoPieces(GameObject root, Vector3 hitPoint, Vector3 hitNormal)
    {
        SetEnabledRecursively(root, false);

        Vector3 basePos = root.transform.position;
        Quaternion baseRot = root.transform.rotation;

        // 원본 기준으로 weight 나누기
        float rootWeight = 1f;
        ClearBox rootClear = root.GetComponent<ClearBox>();
        if (rootClear != null) rootWeight = Mathf.Max(0f, rootClear.weight);

        // 조각 하나당 게이지 반영
        float pieceWeight = (pieces > 0) ? (rootWeight / pieces) : rootWeight;
        pieceWeight *= 0.2f;

        for (int i = 0; i < pieces; i++)
        {
            // 조각 생성
            GameObject piece = Instantiate(root, basePos, baseRot);

            SetEnabledRecursively(piece, true);

            // 레이어 변경
            SetLayerRecursively(piece, brokenLayer);

            if (piece.GetComponent<CutterPiece>() == null)
                piece.AddComponent<CutterPiece>();

            ClearBox cb = piece.GetComponent<ClearBox>();
            if (cb == null) cb = piece.AddComponent<ClearBox>();
            cb.weight = pieceWeight;

            // 크기 줄이기
            piece.transform.localScale = Vector3.one * smallScale;

            // 흩뿌리기
            Vector3 scatter = Random.insideUnitSphere * scatterRadius;
            scatter.y = Mathf.Abs(scatter.y) * 0.3f;
            piece.transform.position = hitPoint + scatter;

            var rootRb = piece.GetComponent<Rigidbody>();
            if (rootRb != null) Destroy(rootRb);

            Rigidbody firstChildRb = null;

            // 조각에 Rigidbody 장착
            foreach (Collider col in piece.GetComponentsInChildren<Collider>(true))
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb == null) rb = col.gameObject.AddComponent<Rigidbody>();

                rb.isKinematic = false;
                rb.useGravity = true;

                if (firstChildRb == null) firstChildRb = rb;
            }

            if (firstChildRb != null)
            {
                Vector3 dir = (hitNormal + Random.insideUnitSphere * 0.3f).normalized;
                firstChildRb.AddForce(dir * popForce, ForceMode.Impulse);
            }
        }

        // 원본 제거
        Destroy(root);
    }
    
    // 컴포넌트 추가
    void SetEnabledRecursively(GameObject obj, bool on)
    {
        var rends = obj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
            rends[i].enabled = on;

        var cols = obj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = on;
    }

    // 레이어 변경
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
    }
}

public class CutterPiece : MonoBehaviour { }
