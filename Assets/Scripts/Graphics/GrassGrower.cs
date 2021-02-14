using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassGrower : MonoBehaviour
{
    const int MAX_CLUSTERS = 200;
    const int MAX_GRID_CELLS = 4;

    [SerializeField, Range(1, MAX_CLUSTERS),
        Tooltip("Number of grass clusters to generate")]
    int grassDensityPerCell = 100;

    [SerializeField, Min(1),
        Tooltip("Number of subdivisions per axis to use")]
    Vector2Int gridSize = Vector2Int.one;

    [SerializeField]
    private Texture2D grassGrowthMask;
    int grassGrowthMaskPropId = Shader.PropertyToID("_GrowthMask");
    [SerializeField]
    bool generateGrassGrowthMask;

    [Header("Geometry options")]
    [SerializeField,
        Tooltip("Cluster Radius")]
    float radius = 1;
    int radiusPropId = Shader.PropertyToID("_Radius");
    [SerializeField,
        Tooltip("Grass Blade Maximum Height")]
    float height = 1;
    int heightPropId = Shader.PropertyToID("_Height");
    [SerializeField,
        Tooltip("Amount Of Random Height Variation")]
    float heightJitter = 0.1f;
    int heightJitterPropId = Shader.PropertyToID("_HeightJitter");
    [SerializeField,
        Tooltip("Grass Blade Base Width")]
    float width = 1;
    int widthPropId = Shader.PropertyToID("_Width");
    [SerializeField,
        Tooltip("Grass Blade Lean Amount")]
    float lean = 0.4f;
    int leanPropId = Shader.PropertyToID("_Lean");
    [SerializeField,
        Tooltip("Grass Color")]
    Color grassColor = new Color(0.1f, 1, 0.1f, 1);
    int grassColorPropId = Shader.PropertyToID("_Color");

    [SerializeField,
        Tooltip("Reset to material defaults")]
    bool resetMaterialProps;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock materialProps;

    private int prevGrassDensity = 0;
    private Vector2[] halton;

    private void Start()
    {
        halton = MathUtil.GenerateHalton23(MAX_CLUSTERS * MAX_GRID_CELLS);

        meshRenderer = GetComponent<MeshRenderer>();
        materialProps = new MaterialPropertyBlock();
        GetDefaultMaterialProps();

        if (grassGrowthMask == null && generateGrassGrowthMask) {
            grassGrowthMask = TextureCreator.Perlin(256, 1337.0f, 37.0f, 1);
        }
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

    private void LateUpdate()
    {
        if (resetMaterialProps) {
            GetDefaultMaterialProps();
            resetMaterialProps = false;
        }

        materialProps.SetFloat(radiusPropId, radius);
        materialProps.SetFloat(heightPropId, height);
        materialProps.SetFloat(heightJitterPropId, heightJitter);
        materialProps.SetFloat(widthPropId, width);
        materialProps.SetFloat(leanPropId, lean);
        materialProps.SetColor(grassColorPropId, grassColor);
        if (grassGrowthMask) { materialProps.SetTexture(grassGrowthMaskPropId, grassGrowthMask); }
        meshRenderer.SetPropertyBlock(materialProps);
    }

    private void GetDefaultMaterialProps()
    {
        radius = meshRenderer.material.GetFloat(radiusPropId);
        height = meshRenderer.material.GetFloat(heightPropId);
        heightJitter = meshRenderer.material.GetFloat(heightJitterPropId);
        width = meshRenderer.material.GetFloat(widthPropId);
        lean = meshRenderer.material.GetFloat(leanPropId);
        grassColor = meshRenderer.material.GetColor(grassColorPropId);
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
}
