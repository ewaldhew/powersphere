#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Include/Util.hlsl"

float4 _BaseColor;
float _Cutoff;

float _WallGlowRadius;
float3 _PlayerPosition;

struct appdata
{
    float4 pos_OS : POSITION;
    float3 normal_OS : NORMAL;
    float4 tangent_OS : TANGENT;
};

struct vertOut
{
    float4 pos_CS : SV_POSITION;
    float4 pos_WS : TEXCOORD0;
    float3 normal_WS : NORMAL;
};

void vert(appdata IN, out vertOut OUT)
{
    OUT.pos_WS = float4(TransformObjectToWorld(IN.pos_OS.xyz), 1);
    OUT.pos_CS = TransformWorldToHClip(OUT.pos_WS.xyz);
    OUT.normal_WS = TransformObjectToWorldNormal(IN.normal_OS);
}

half4 frag(vertOut IN) : SV_Target
{
    half distanceFromPlayer = distance(_PlayerPosition, IN.pos_WS.xyz);
    half glowFactor = saturate(distanceFromPlayer / _WallGlowRadius) * TWO_PI + 0.0001f;
    half falloff = sin(glowFactor) / glowFactor;

    half3 color = _BaseColor.rgb;
    half alpha = falloff * falloff * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
    color *= alpha;
#endif
#ifdef _SHADER_TARGET_2_0
    alpha = OutputAlpha(alpha, _Surface);
#endif

    return half4(color, alpha);
}
