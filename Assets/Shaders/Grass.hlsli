#ifndef _GRASS_HLSLI
#define _GRASS_HLSLI


// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
// Extended discussion on this function can be found at the following link:
// https://forum.unity.com/threads/am-i-over-complicating-this-random-function.454887/#post-2949326
// Returns a number in the 0...1 range.
float rand(float3 co)
{
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

// Construct a rotation matrix that rotates around the provided axis, sourced from:
// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
float3x3 AngleAxis3x3(float angle, float3 axis)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
    );
}

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define NUM_BLADES 5
#define NUM_SEGMENTS 4

float _Radius;
float _Height;
float _HeightJitter;
float _Width;
float _Lean;
float4 _Color;

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
    float4 pos = float4(basePos + mul(transform, offset), 1);
    float3 localNormal_TS = normalize(float3(0, -1, (offset.y - prevOffset.y) / (offset.z - prevOffset.z)));
    OUT.pos_WS = pos;
    OUT.pos_CS = TransformWorldToHClip(pos);
    OUT.normal_WS = mul(transform, localNormal_TS);
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

    for (int blade = 0; blade < NUM_BLADES; blade++) {
        float r = blade / (float)NUM_BLADES * TWO_PI;
        float sinr, cosr;
        sincos(r, sinr, cosr);
        float3 bladeBasePos = pos_WS.xyz + (tangent * -sinr + bitangent * cosr) * _Radius;

        float3x3 facingMatrix = AngleAxis3x3(rand(bladeBasePos.zxy) * TWO_PI, float3(0, 0, 1));
        float3x3 transform = mul(tangentToWorldMatrix, facingMatrix);

        float bladeLean = _Lean;
        float bladeHeight = _Height + (rand(bladeBasePos) * 2.0f - 1.0f) * _HeightJitter;
        float bladeWidth = _Width;

        float3 prevOffset = float3(0, 0, -1);
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
        }
        triStream.Append(outputToVertexStream(OUT, bladeBasePos, float3(0, bladeLean, bladeHeight), prevOffset, transform));
        triStream.RestartStrip();
    }
}

float4 frag(geomOut IN, float facing : VFACE) : SV_Target
{
    InputData lightingInput;
    lightingInput.positionWS = IN.pos_WS;

    float3 N = IN.normal_WS * sign(facing);
    float NdotL = saturate(dot(N, _MainLightPosition.xyz));
    float4 col = float4(_Color + NdotL * _MainLightColor.rgb, 1);
    return col;
}

#endif /* _GRASS_HLSLI */
