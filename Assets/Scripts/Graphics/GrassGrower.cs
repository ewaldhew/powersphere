using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassGrower : MonoBehaviour
{
    const int MAX_CLUSTERS = 200;
    const int MAX_GRID_CELLS = 4;

    [SerializeField, Range(1, MAX_CLUSTERS),
        Tooltip("Number of grass clusters to generate")]
    public int grassDensityPerCell = 100;

    [SerializeField, Min(1),
        Tooltip("Number of subdivisions per axis to use")]
    public Vector2Int gridSize = Vector2Int.one;

    private int prevGrassDensity = 0;
    private Vector2[] halton;

    private void Start()
    {
        halton = GenerateHalton23(MAX_CLUSTERS * MAX_GRID_CELLS);
    }

    private void Update()
    {
        int grassDensity = grassDensityPerCell * gridSize.x * gridSize.y;

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

        int ind = 0;
        Vector2 uvStep = Vector2.one / gridSize;
        for (int x = 0; x < gridSize.x; x++) {
            for (int y = 0; y < gridSize.y; y++) {
                Vector2 uvStart = uvStep * new Vector2(x, y);
                for (int i = 0; i < grassDensityPerCell; i++) {
                    Vector2 uvRel = GetHalton(i, x * gridSize.y + y);
                    Vector2 uv = uvStart + uvRel * uvStep;
                    indices[ind] = ind;
                    vertices[ind] = new Vector3(uv.x - 0.5f, 0, uv.y - 0.5f);
                    uvCoords[ind] = uv;
                    normals[ind] = normal;
                    tangents[ind] = tangent;
                    ind++;
                }
            }
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh = new Mesh();
        mesh.name = "Grass Root Positions";
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        mesh.uv = uvCoords;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.RecalculateBounds(); // XXX: Does not account for grass height!

        prevGrassDensity = grassDensity;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(1, 0, 1));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0, 1));

        Vector2 gridStep = Vector2.one / gridSize;
        // horizontal lines
        for (int i = 1; i < gridSize.x; i++) {
            float x = i * gridStep.x - 0.5f;
            Gizmos.DrawLine(new Vector3(x, 0, -.5f), new Vector3(x, 0, .5f));
        }

        // draw the vertical lines
        for (int i = 1; i < gridSize.y; i++) {
            float y = i * gridStep.y - 0.5f;
            Gizmos.DrawLine(new Vector3(-.5f, 0, y), new Vector3(.5f, 0, y));
        }
    }

    private Vector2 GetHalton(int index, int skip)
    {
        if (skip < MAX_GRID_CELLS) {
            // sequentially from the skip-th block
            return halton[skip * MAX_CLUSTERS + index];
        } else {
            // add skip to the front, take every 7th element wrapping around
            return halton[(index * 7 + skip) % halton.Length];
        }
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
