using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterSimulation : MonoBehaviour
{
    public const int Width = 256;
    public const int Height = 256;

    [SerializeField]
    GameState gameState;

    [SerializeField, Tooltip(
        "A texture indicating the inside of the water surface. Any value not equal to 1 " +
        "in this texture (red channel) will not count as water for the simulation, and " +
        "waves will reflect off the edges of the area with value 1. ")]
    RenderTexture waterMaskTexture;

    [SerializeField]
    ComputeShader waterSimulationCS;
    private int waterSimulationKernelIndex;
    private readonly int inputTexSlot = Shader.PropertyToID("gInput");
    private readonly int outputTexSlot = Shader.PropertyToID("gOutput");
    private readonly int boundaryMaskTexSlot = Shader.PropertyToID("boundaryMask");
    private readonly int playerPositionConst = Shader.PropertyToID("_PlayerPosition");
    private readonly int texSizeConst = Shader.PropertyToID("_TexSize");

    public Texture2D WaterSurfaceData { get; private set; }

    private Mesh waterMesh;
    private MeshRenderer waterMeshRenderer;
    private readonly int surfaceMapTexSlot = Shader.PropertyToID("_SurfaceMap");

    private RenderTexture[] buffers = new RenderTexture[2];
    private int swap = 0;

    [SerializeField, Range(-1, 2),
        Tooltip("0: off. 1: visualize waves. 2: visualize potential. -1: reset water.")]
    int _Debug = 0;

    private void Start()
    {
        if (waterMaskTexture.width != Width || waterMaskTexture.height != Height) {
            Debug.LogError(
                "The provided water boundary mask should be at the same resolution " +
                "as the water surface simulation!");
        }
    }

    private void OnEnable()
    {
        var rtFmt = RenderTextureFormat.ARGBFloat;

        buffers[0] = new RenderTexture(Width, Height, 1, rtFmt, RenderTextureReadWrite.Linear) {
            name = "WaterSurface1",
            filterMode = FilterMode.Point,
            enableRandomWrite = true,
        };
        buffers[0].Create();

        buffers[1] = new RenderTexture(buffers[0]) {
            name = "WaterSurface2",
        };
        buffers[1].Create();

        waterSimulationKernelIndex = waterSimulationCS.FindKernel("CSMain");

        var texFmt = TextureFormat.RGBAFloat;
        WaterSurfaceData = new Texture2D(Width, Height, texFmt, true, true) {
            name = "WaterSurfaceSRVTex",
            filterMode = FilterMode.Trilinear,
        };

        waterMesh = GetComponent<MeshFilter>().mesh;
        waterMeshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnDisable()
    {
        foreach (var buffer in buffers) {
            buffer.Release();
        }
    }

    private void LateUpdate()
    {
        Vector3 offset = gameState.player.transform.position - transform.position;
        bool hasMoved = gameState.player.velocity.magnitude > 0;
        bool onWaterSurface = Mathf.Abs(offset.y) < gameState.player.height * 0.5;

        int texX = -1;
        int texY = -1;
        if (hasMoved && onWaterSurface) {
            texX = Mathf.RoundToInt((-offset.x / waterMesh.bounds.size.x / transform.localScale.x + 0.5f) * (Width - 1));
            texY = Mathf.RoundToInt((-offset.z / waterMesh.bounds.size.z / transform.localScale.z + 0.5f) * (Height - 1));
        }
        waterSimulationCS.SetInts(playerPositionConst, new int[2] { texX, texY });
        waterSimulationCS.SetInts(texSizeConst, new int[2] { Width, Height });
        waterSimulationCS.SetInt("_Debug", _Debug);

        waterSimulationCS.SetTexture(waterSimulationKernelIndex, boundaryMaskTexSlot, waterMaskTexture);
        waterSimulationCS.SetTexture(waterSimulationKernelIndex, inputTexSlot, buffers[swap]);
        waterSimulationCS.SetTexture(waterSimulationKernelIndex, outputTexSlot, buffers[1 - swap]);

        waterSimulationCS.Dispatch(
            waterSimulationKernelIndex,
            MathUtil.DivideByMultiple(Width, 8),
            MathUtil.DivideByMultiple(Height, 8),
            1
        );

        var currRt = RenderTexture.active;
        RenderTexture.active = buffers[1 - swap];
        WaterSurfaceData.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        WaterSurfaceData.Apply();
        RenderTexture.active = currRt;

        waterMeshRenderer.material.SetTexture(surfaceMapTexSlot, WaterSurfaceData);
        waterMeshRenderer.material.SetInt("_Debug", _Debug);

        swap = 1 - swap;
    }
}
