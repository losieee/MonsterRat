using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PipeBroken : MonoBehaviour
{
    public Image crackImage;

    private Material mat;

    void Start()
    {
        mat = crackImage.material;
        mat.SetFloat("_Reveal", 0f);

        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.2f);

        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.3f);

        yield return new WaitForSeconds(1f);
        mat.SetFloat("_Reveal", 0.5f);

        Destroy(gameObject);
    }
}
