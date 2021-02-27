#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

uint _Debug;

#undef _NORMALMAP
#include "Include/Lighting.hlsl"

#include "Include/Util.hlsl"
#include "Include/PowerSpheres.hlsl"

TEXTURE2D(_SurfaceMap);
SAMPLER(sampler_SurfaceMap);

struct appdata
{
    float4 pos_OS : POSITION;
    float3 normal_OS : NORMAL;
    float4 tangent_OS : TANGENT;
    float2 uv : TEXCOORD;
};

struct vertOut
{
    float2 uv : TEXCOORD;
    float4 pos_CS : SV_POSITION;
    float4 pos_SS : TEXCOORD1;
    float3 pos_WS : TEXCOORD2;
    float3 normal_WS : NORMAL;
    float3 tangent_WS : TANGENT;
    float3 bitangent_WS : TANGENT1;
};

float GetWaterHeight(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_SurfaceMap, sampler_SurfaceMap, uv, 0).r;
}

float3 ComputeNormal(float2 uv)
{
    const float eps = 0.01;
    const float inveps = 0.5f / eps;

    float2 x0 = uv - float2(eps, 0);
    float2 x1 = uv + float2(eps, 0);
    float2 y0 = uv - float2(0, eps);
    float2 y1 = uv + float2(0, eps);

    float fx0 = GetWaterHeight(x0);
    float fx1 = GetWaterHeight(x1);
    float fy0 = GetWaterHeight(y0);
    float fy1 = GetWaterHeight(y1);

    float dfdx = (fx1 - fx0) * inveps;
    float dfdy = (fy1 - fy0) * inveps;

    return normalize(float3(-dfdx, -dfdy, 1));
}

void vert(appdata IN, out vertOut OUT)
{
    float3 positionOS = IN.pos_OS.xyz;

    float offset = GetWaterHeight(IN.uv);
    positionOS += offset * IN.normal_OS;

    VertexPositionInputs vtx = GetVertexPositionInputs(positionOS);
    OUT.pos_CS = vtx.positionCS;
    OUT.pos_SS = vtx.positionNDC;
    OUT.pos_WS = vtx.positionWS;

    VertexNormalInputs tbn = GetVertexNormalInputs(IN.normal_OS, IN.tangent_OS);
    OUT.normal_WS = tbn.normalWS;
    OUT.tangent_WS = tbn.tangentWS;
    OUT.bitangent_WS = tbn.bitangentWS;
    OUT.uv = IN.uv;
}

half4 frag(vertOut IN) : SV_Target
{
    if (_Debug == 1) {
        half2 pm = GetWaterHeight(IN.uv).xx * float2(1, -1) * 0.5;
        return half4(saturate(pm.x), 0, saturate(pm.y), 1);
    }
    if (_Debug == 2) {
        half2 pm = SAMPLE_TEXTURE2D_LOD(_SurfaceMap, sampler_SurfaceMap, IN.uv, 0).gg * float2(1, -1) * 0.5;
        return half4(saturate(pm.x), 0, saturate(pm.y), 1);
    }

    clip(IsWithinSphere(IN.pos_WS.xyz, _WaterSpherePositionAndRadius) ? 1 : -1);

    float3x3 tangentToWorldMatrix = float3x3(IN.tangent_WS, IN.bitangent_WS, IN.normal_WS);
    float3 normalTS = ComputeNormal(IN.uv);
    float3 newNormalOS = mul(normalTS, tangentToWorldMatrix);
    float3 normal_WS = TransformObjectToWorldNormal(newNormalOS);

    float2 screenPos = IN.pos_SS.xy / IN.pos_SS.w;
    float depth = LinearEyeDepth(SampleSceneDepth(screenPos).r, _ZBufferParams);
    float waterDepth = LinearEyeDepth(IN.pos_CS.z, _ZBufferParams);

    half3 color = _BaseColor.rgb;
    half alpha = _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
    color *= alpha;
#endif
#ifdef _SHADER_TARGET_2_0
    alpha = OutputAlpha(alpha, _Surface);
#endif

    half4 diffuseColor = depth - waterDepth < 0.1 ? 1 : half4(color, alpha);

    Light mainLight = GetMainLight();

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv, surfaceData);
    SurfaceBRDFParams surfData;
    CollectSurfaceData(1, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, alpha, surfData);
    surfData.normal = normal_WS;

    half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.pos_WS);

    half3 specularColor = CookTorranceSpecular(mainLight, surfData, viewDirectionWS);

    float specularBlendFactor = dot(specularColor, specularColor);
    return half4(diffuseColor.rgb + specularColor.rgb, lerp(diffuseColor.a, 1, specularBlendFactor));
}
