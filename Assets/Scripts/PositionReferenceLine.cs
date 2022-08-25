using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PositionReferenceLine : MonoBehaviour
{
    [SerializeField] private AnimationCurve lineWidthByHeight;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LineRenderer lineTarget;

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, 200))
        {
            float lineWidth = lineWidthByHeight.Evaluate(transform.position.y - hitInfo.point.y);

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, hitInfo.point + new Vector3(0, 0.1f, 0));
            lineRenderer.widthMultiplier = lineWidth;

            lineTarget.gameObject.SetActive(true);
            lineTarget.transform.position = hitInfo.point;
            lineTarget.transform.rotation = Quaternion.identity;
            lineTarget.widthMultiplier = lineWidth;
        }
        else
        {
            lineTarget.gameObject.SetActive(false);
            lineRenderer.enabled = false;
        }
    }
}
