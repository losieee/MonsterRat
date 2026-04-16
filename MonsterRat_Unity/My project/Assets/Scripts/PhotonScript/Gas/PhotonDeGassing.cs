using UnityEngine;
using Fusion;

public class PhotonDeGassing : InvenBase
{
    public override ToolType Type => ToolType.DeGassing;

    public float distance = 5f;
    public float shrinkSpeed = 1.2f;
    public LayerMask gasLayerMask;

    public override void Tick()
    {
        if (Input.GetMouseButton(0))
        {
            if (interactor == null)   return; 
            if (interactor.cam == null) return; 

            Ray ray = new Ray(interactor.cam.position, interactor.cam.forward);
            Debug.DrawRay(ray.origin, ray.direction * distance, Color.cyan, 0.5f);

            if (Physics.Raycast(ray, out RaycastHit hit, distance, gasLayerMask, QueryTriggerInteraction.Ignore))
            {
                SmallGasObject targetGas = hit.collider.GetComponentInParent<SmallGasObject>();

                if (targetGas != null)
                {
                    targetGas.SuckGas(Time.deltaTime * shrinkSpeed);
                }
            }
        }
       
       
       
       
    }
}