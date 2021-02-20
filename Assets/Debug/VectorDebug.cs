using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class VectorDebug : MonoBehaviour
{
    [SerializeField]
    Vector3 step = new Vector3(1, 1, 1);

    Bounds sampleArea;
    MeshRenderer meshRenderer;
    GameRenderer gameRenderer;

    private void OnEnable()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        gameRenderer = FindObjectOfType<GameRenderer>();
    }

    private void Update()
    {
        sampleArea = meshRenderer.bounds;
    }

    private void OnDrawGizmos()
    {
        for (float x = sampleArea.min.x; x < sampleArea.max.x; x += step.x) {
            for (float y = sampleArea.min.y; y < sampleArea.max.y; y += step.y) {
                for (float z = sampleArea.min.z; z < sampleArea.max.z; z += step.z) {
                    Vector3 position = new Vector3(x, y, z);
                    Vector3 wind = gameRenderer.windSampler.WindVelocity(position);
                    Vector3 end = position + Vector3.ClampMagnitude(wind, 1);
                    Gizmos.color = wind.magnitude * 0.1f * Color.white;
                    Gizmos.DrawSphere(position, 0.05f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(position, end);
                }
            }
        }
    }
}
