#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#undef _NORMALMAP
#include "Include/Lighting.hlsl"

#include "Include/Util.hlsl"
#include "Include/WindSampler.hlsl"
#include "Include/PowerSpheres.hlsl"

#define MAX_NUM_BLADES 5
#define MAX_NUM_SEGMENTS 4

// instanced props
float _Radius;
float _Height;
float _HeightJitter;
float _Width;
float _Lean;
float4 _Color;

// uniform
float3 _PlayerPosition;
float _GrassSquashRadius;
float _GrassSquashStrength;
float4 _GrassLOD;

struct appdata
{
    float4 pos_OS : POSITION;
    float3 normal_OS : NORMAL;
    float4 tangent_OS : TANGENT;
};

struct vertOut
{
    float4 pos_WS : SV_POSITION;
    float3 normal_WS : NORMAL;
    float3 tangent_WS : TANGENT;
    float3 bitangent_WS : TANGENT1;
};

struct geomOut
{
    float4 pos_CS : SV_POSITION;
    float4 pos_WS : TEXCOORD1;
    float4 center_WS : POSITION_WS;
    float3 normal_WS : NORMAL;
    float2 uv : TEXCOORD;
#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord : TEXCOORD2;
#endif
};

void vert(appdata IN, out vertOut OUT)
{
    VertexNormalInputs tbn = GetVertexNormalInputs(IN.normal_OS, IN.tangent_OS);

    OUT.pos_WS = float4(TransformObjectToWorld(IN.pos_OS.xyz), 1);
    OUT.normal_WS = tbn.normalWS;
    OUT.tangent_WS = tbn.tangentWS;
    OUT.bitangent_WS = tbn.bitangentWS;
}

geomOut outputToVertexStream(geomOut base, float3 basePos, float3 offset, float3 prevOffset, float3x3 transform)
{
    geomOut OUT = base;
    float4 pos = float4(basePos + mul(offset, transform), 1);
    float3 localNormal_TS = normalize(float3(0, -1, (offset.y - prevOffset.y) / (offset.z - prevOffset.z)));
    OUT.pos_WS = pos;
    OUT.pos_CS = TransformWorldToHClip(pos.xyz);
    OUT.normal_WS = mul(localNormal_TS, transform);
    OUT.uv = float2(0, offset.x);
#ifdef _MAIN_LIGHT_SHADOWS
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    OUT.shadowCoord = ComputeNormalizedDeviceCoordinatesWithZ(pos_CS);
#else
    OUT.shadowCoord = TransformWorldToShadowCoord(pos);
#endif
#endif
    return OUT;
}

[maxvertexcount(MAX_NUM_BLADES * (MAX_NUM_SEGMENTS * 2 + 1))]
void geom(point vertOut IN[1], inout TriangleStream<geomOut> triStream)
{
    if (!IsWithinSphere(IN[0].pos_WS.xyz, _ColorSpherePositionAndRadius) ||
        !IsWithinSphere(IN[0].pos_WS.xyz, _GreenSpherePositionAndRadius)) {
        return;
    }

    float4 pos_WS = IN[0].pos_WS;
    float3 normal = IN[0].normal_WS;
    float3 tangent = IN[0].tangent_WS;
    float3 bitangent = IN[0].bitangent_WS;

    float3x3 tangentToWorldMatrix = float3x3(
        tangent.x, bitangent.x, normal.x,
        tangent.y, bitangent.y, normal.y,
        tangent.z, bitangent.z, normal.z
    );

    geomOut OUT;
    OUT.center_WS = pos_WS;

    // calculate LOD factor
    float distanceFromCamera = distance(GetCameraPositionWS(), pos_WS);
    float lodFactor1 = 1.0f - saturate((distanceFromCamera - _GrassLOD.x) / _GrassLOD.y);
    float lodFactor2 = 1.0f - saturate((distanceFromCamera - _GrassLOD.z) / _GrassLOD.w);

    const int numBlades = max(1, ceil(lodFactor1 * MAX_NUM_BLADES));
    const int numSegments = max(1, ceil(lodFactor2 * MAX_NUM_SEGMENTS));

    [unroll]
    for (int blade = 0; blade < numBlades; blade++) {
        float r = blade / (float)numBlades * TWO_PI;
        float sinr, cosr;
        sincos(r, sinr, cosr);
        float3 bladeBasePos = pos_WS.xyz + (tangent * -sinr + bitangent * cosr) * _Radius;

        float distanceFromSquasher = distance(_PlayerPosition.xz, bladeBasePos.xz);
        float squashFactor = 1.0f - saturate(distanceFromSquasher / _GrassSquashRadius); // Can sample from texture also
        float3 squashDirection = -cross(_PlayerPosition - bladeBasePos, normal); // Can sample derivative of texture
        float3x3 squashingMatrix = AngleAxis3x3(_GrassSquashStrength * squashFactor, squashDirection);

        float3x3 facingMatrix = AngleAxis3x3(rand(bladeBasePos.zxy) * TWO_PI, float3(0, 0, 1));

        float3 wind = WindVelocity(bladeBasePos);
        wind = normalize(wind);
        float windFactor = saturate(abs(dot(wind, bitangent)));
        float3 bendAxis = cross(wind, normal);
        float3x3 windBendMatrix = dot(wind, wind) > 0
            ? AngleAxis3x3(_WindStrength * windFactor * 0.05 * PI, bendAxis)
            : (float3x3)IdentityMatrix;

        float3x3 transform = mul(facingMatrix, tangentToWorldMatrix);

        float bladeLean = _Lean;
        float bladeHeight = _Height + (rand(bladeBasePos) * 2.0f - 1.0f) * _HeightJitter;
        float bladeWidth = _Width;

        float3 prevOffset = float3(0, 0, -1);
        [unroll]
        for (int segment = 0; segment < numSegments; segment++) {
            float t = segment / (float)numSegments;
            float3 offset = float3(
                (1 - t) * bladeWidth,
                t * t * bladeLean,
                t * bladeHeight
            );

            triStream.Append(outputToVertexStream(OUT, bladeBasePos, offset, prevOffset, transform));
            triStream.Append(outputToVertexStream(OUT, bladeBasePos, offset * float3(-1, 1, 1), prevOffset, transform));
            prevOffset = offset;

            if (segment == 0) {
                transform = mul(transform, windBendMatrix);
            }
            if (segment == 1) {
                transform = mul(transform, squashingMatrix);
            }
        }
        triStream.Append(outputToVertexStream(OUT, bladeBasePos, float3(0, bladeLean, bladeHeight), prevOffset, transform));
        triStream.RestartStrip();
    }
}

half4 frag(geomOut IN, float facing : VFACE) : SV_Target
{
    LightingInput lightingInput;
    lightingInput.uv = IN.uv;
    lightingInput.positionWSAndFogFactor = float4(IN.pos_WS.xyz, 0); // xyz: positionWS, w: vertex fog factor
    lightingInput.normalWS = IN.normal_WS * sign(facing);
    lightingInput.positionCS = IN.pos_CS;
#ifdef _MAIN_LIGHT_SHADOWS
    lightingInput.shadowCoord = IN.shadowCoord;
#endif

    SurfaceInput surfaceInput;
    surfaceInput.albedo = _Color;
    surfaceInput.specular = 0;
    surfaceInput.metallic = 0;
    surfaceInput.smoothness = 1;
    surfaceInput.normalTS = float3(0, 0, 1);
    surfaceInput.emission = 0;
    surfaceInput.occlusion = 1;
    surfaceInput.alpha = 1;

    return ResolveLighting(lightingInput, surfaceInput);
}
