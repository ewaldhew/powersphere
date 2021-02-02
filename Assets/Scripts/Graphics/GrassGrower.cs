using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassGrower : MonoBehaviour
{
    const int MAX_CLUSTERS = 200;

    [SerializeField, Range(1, MAX_CLUSTERS),
        Tooltip("Number of grass clusters to generate")]
    public int grassDensity = 100;

    private int prevGrassDensity = 0;
    private Vector2[] halton = new Vector2[MAX_CLUSTERS];

    private void Start()
    {
        halton = GenerateHalton23(MAX_CLUSTERS);
    }

    private void Update()
    {
        if (grassDensity == prevGrassDensity) {
            return;
        }

        int[] indices = new int[grassDensity];
        Vector3[] vertices = new Vector3[grassDensity];
        Vector2[] uvCoords = new Vector2[grassDensity];
        Vector3[] normals = new Vector3[grassDensity];
        Vector4[] tangents = new Vector4[grassDensity];

        Vector3 normal = Vector3.up;
        Vector4 tangent = new Vector4(1, 0, 0, -1);
        for (int i = 0; i < grassDensity; i++) {
            Vector2 uv = halton[i];
            indices[i] = i;
            vertices[i] = new Vector3(uv.x - 0.5f, 0, uv.y - 0.5f);
            uvCoords[i] = uv;
            normals[i] = normal;
            tangents[i] = tangent;
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "Grass Root Positions";
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.uv = uvCoords;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.RecalculateBounds(); // XXX: Does not account for grass height!
    }

    private Vector2[] GenerateHalton23(uint length)
    {
        Vector2[] halton = new Vector2[length];

        int n2 = 0, d2 = 1;
        int n3 = 0, d3 = 1;
        for (int i = 0; i < length; i++) {
            int x2 = d2 - n2;
            if (x2 == 1) {
                n2 = 1;
                d2 *= 2;
            } else {
                int y2 = d2 / 2;
                while (x2 <= y2) {
                    y2 /= 2;
                }
                n2 = 3 * y2 - x2;
            }
            int x3 = d3 - n3;
            if (x3 == 1) {
                n3 = 1;
                d3 *= 3;
            } else {
                int y3 = d3 / 3;
                while (x3 <= y3) {
                    y3 /= 3;
                }
                n3 = 4 * y3 - x3;
            }

            halton[i] = new Vector2(n2 / (float)d2, n3 / (float)d3);
        }

        return halton;
    }
}
