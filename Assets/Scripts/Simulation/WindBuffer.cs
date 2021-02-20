using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class WindBuffer : MonoBehaviour
{
    public const int Width = 256;
    public const int Height = 256;

    [SerializeField]
    GameState gameState;

    [SerializeField,
        Tooltip("How many world units to extend in each direction")]
    int range = 16;

    [SerializeField]
    ComputeShader windPotentialUpdate;
    private int windPotentialUpdateKernelIndex;
    private readonly int inputTexSlot = Shader.PropertyToID("PreviousResult");
    private readonly int outputTexSlot = Shader.PropertyToID("Result");
    private readonly int playerPositionChangeConst = Shader.PropertyToID("positionChange");
    private readonly int texSizeConst = Shader.PropertyToID("texSize");
    private readonly int worldToTexScaleConst = Shader.PropertyToID("worldToTexScale");
    private readonly int dynamicWindRadiusConst = Shader.PropertyToID("_DynamicWindRadius");

    public Vector3 CenterPosition { get { return gameState.player.transform.position; } }
    public int Range { get { return range; } }
    public Vector2 WorldToTexScale { get; private set; }
    public Texture2D WindPotential { get; private set; }

    private RenderTexture output1;
    private RenderTexture output2;

    private void OnEnable()
    {
#if CURL_WIND
        var rtFmt = RenderTextureFormat.R16;
#else
        var rtFmt = RenderTextureFormat.ARGBFloat;
#endif
        output1 = new RenderTexture(Width, Height, 1, rtFmt, RenderTextureReadWrite.Linear) {
            name = "WindPotential1",
            filterMode = FilterMode.Bilinear,
            enableRandomWrite = true,
        };
        output1.Create();

        output2 = new RenderTexture(output1) {
            name = "WindPotential2",
        };
        output2.Create();

#if CURL_WIND
        var currRt = RenderTexture.active;
        RenderTexture.active = output1;
        GL.Clear(false, true, new Color(0.5f, 0, 0, 0));
        RenderTexture.active = output2;
        GL.Clear(false, true, new Color(0.5f, 0, 0, 0));
        RenderTexture.active = currRt;
#endif

        windPotentialUpdateKernelIndex = windPotentialUpdate.FindKernel("CSMain");

        WorldToTexScale = new Vector2(
            Width * 0.5f / Range,
            Height * 0.5f / Range
        );

#if CURL_WIND
        var texFmt = TextureFormat.R16;
#else
        var texFmt = TextureFormat.RGBAFloat;
#endif
        WindPotential = new Texture2D(Width, Height, texFmt, false, true) {
            filterMode = FilterMode.Bilinear,
        };
    }
    private void OnDisable()
    {
        output1.Release();
        output1 = null;

        output2.Release();
        output2 = null;
    }

    private void LateUpdate()
    {
        Vector3 playerPositionChange = gameState.player.velocity * Time.deltaTime;

        windPotentialUpdate.SetFloats(playerPositionChangeConst, new float[2] {
            playerPositionChange.x * WorldToTexScale.x,
            playerPositionChange.z * WorldToTexScale.y,
        });
        windPotentialUpdate.SetInts(texSizeConst, new int[2] { Width, Height });
        windPotentialUpdate.SetFloats(worldToTexScaleConst, new float[2] {
            WorldToTexScale.x, WorldToTexScale.y
        });
        windPotentialUpdate.SetFloat(dynamicWindRadiusConst, gameState.dynamicWindRadius);

        windPotentialUpdate.SetTexture(windPotentialUpdateKernelIndex, inputTexSlot, output2);
        windPotentialUpdate.SetTexture(windPotentialUpdateKernelIndex, outputTexSlot, output1);

        windPotentialUpdate.Dispatch(
            windPotentialUpdateKernelIndex,
            MathUtil.DivideByMultiple(Width, 8),
            MathUtil.DivideByMultiple(Height, 8),
            1
        );

        // The below does not work! No CPU-GPU sync
        // See: https://answers.unity.com/questions/1271693/
        //Graphics.CopyTexture(output1, WindPotential);

        // Instead we have to do this...
        var currRt = RenderTexture.active;
        RenderTexture.active = output1;
        WindPotential.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        WindPotential.Apply();
        RenderTexture.active = currRt;

        var temp = output1;
        output1 = output2;
        output2 = temp;
    }
}
