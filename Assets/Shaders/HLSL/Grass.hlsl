#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Util.hlsli"
#include "WindSampler.hlsli"

#define NUM_BLADES 5
#define NUM_SEGMENTS 4

float _Radius;
float _Height;
float _HeightJitter;
float _Width;
float _Lean;
float4 _Color;

float3 _PlayerPosition;
float _GrassSquashRadius;
float _GrassSquashStrength;

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
    return OUT;
}

[maxvertexcount(NUM_BLADES * (NUM_SEGMENTS * 2 + 1))]
void geom(point vertOut IN[1], inout TriangleStream<geomOut> triStream)
{
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

    [unroll]
    for (int blade = 0; blade < NUM_BLADES; blade++) {
        float r = blade / (float)NUM_BLADES * TWO_PI;
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
        float3x3 windBendMatrix = AngleAxis3x3(windFactor * 0.05 * PI, bendAxis);

        float3x3 transform = mul(facingMatrix, tangentToWorldMatrix);

        float bladeLean = _Lean;
        float bladeHeight = _Height + (rand(bladeBasePos) * 2.0f - 1.0f) * _HeightJitter;
        float bladeWidth = _Width;

        float3 prevOffset = float3(0, 0, -1);
        [unroll]
        for (int segment = 0; segment < NUM_SEGMENTS; segment++) {
            float t = segment / (float)NUM_SEGMENTS;
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

float4 frag(geomOut IN, float facing : VFACE) : SV_Target
{
    float3 N = IN.normal_WS * sign(facing);
    float NdotL = saturate(dot(N, _MainLightPosition.xyz));
    float4 col = _Color + float4(NdotL * _MainLightColor.rgb, 1);
    return col;
}
