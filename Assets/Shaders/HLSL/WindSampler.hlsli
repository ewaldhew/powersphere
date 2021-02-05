#ifndef _WIND_SAMPLER_H
#define _WIND_SAMPLER_H

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_WindTex);
SAMPLER(sampler_WindTex);

CBUFFER_START(UnityPerMaterial)
    float _WindFrequency;
    float _WindShiftSpeed;
    float4 _WindTex_ST;
CBUFFER_END

float3 WindVelocity(float3 pos_WS)
{
    float2 uv = TRANSFORM_TEX(pos_WS.xz, _WindTex) * _WindFrequency
        + float2(0, _Time.y * _WindShiftSpeed);
    float3 wind = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, uv, 0).xyz;
    return float3(wind.x, 0, wind.y) * wind.z;
}

#endif
