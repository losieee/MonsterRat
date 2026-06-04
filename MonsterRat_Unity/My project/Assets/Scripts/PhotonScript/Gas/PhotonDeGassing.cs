using UnityEngine;
using Fusion;

public class PhotonDeGassing : InvenBase
{
    public override ToolType Type => ToolType.DeGassing;

    public float distance = 5f;
    public float shrinkSpeed = 1.2f;
    public LayerMask gasLayerMask;
    public LayerMask pipeGasLayerMask;

    private PipeSmallGasObject currentGas;

    public override void Tick()
    {
        if (interactor == null) return;
        if (interactor.cam == null) return;

        PipeSmallGasObject targetPipeGas = null;

        if (Input.GetMouseButton(0))
        {
            Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);
            Debug.DrawRay(ray.origin, ray.direction * distance, Color.cyan, 0.5f);

            if (Physics.Raycast(ray, out RaycastHit hit, distance, gasLayerMask, QueryTriggerInteraction.Collide))
            {
                SmallGasObject targetGas = hit.collider.GetComponentInParent<SmallGasObject>();

                if (targetGas != null)
                    targetGas.SuckGas(Time.deltaTime * shrinkSpeed);
            }

            if (Physics.Raycast(ray, out RaycastHit pipeHit, distance, pipeGasLayerMask, QueryTriggerInteraction.Collide))
            {
                targetPipeGas = pipeHit.collider.GetComponentInParent<PipeSmallGasObject>();
            }
        }

        if (targetPipeGas != currentGas)
        {
            if (currentGas != null)
                currentGas.PlayGas();

            currentGas = targetPipeGas;

            if (currentGas != null)
                currentGas.StopGas();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentGas != null)
            {
                currentGas.PlayGas();
                currentGas = null;
            }
        }
    }
}