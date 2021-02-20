#ifndef _WIND_SAMPLER_H
#define _WIND_SAMPLER_H

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Util.hlsl"
#include "PowerSpheres.hlsl"

TEXTURE2D(_WindTex);
SAMPLER(sampler_WindTex);

TEXTURE2D(_WindBuffer);
SAMPLER(sampler_WindBuffer);

CBUFFER_START(UnityPerMaterial)
    float _WindFrequency;
    float _WindShiftSpeed;
    float _WindStrength;
    float _AnimTime;

    float3 _WindBufferCenter;
    float _WindBufferRange;
    float _DynamicWindStrength;
CBUFFER_END

float3 WindVelocity(float3 pos_WS)
{
    bool isInWindSphereInfluence = IsWithinSphere(pos_WS, _WindSpherePositionAndRadius);
    float2 uv = pos_WS.xz * _WindFrequency + _AnimTime * _WindShiftSpeed;
    float3 wind = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, uv, 0).xyz;
    float3 staticWind = isInWindSphereInfluence * float3(wind.x, 0, wind.y) * wind.z;

    uv = ((pos_WS.xz - _WindBufferCenter.xz) / _WindBufferRange) * 0.5f + 0.5f;
    wind = SAMPLE_TEXTURE2D_LOD(_WindBuffer, sampler_WindBuffer, uv, 0).xyz;
    float clampUv = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);
    float3 dynamicWind = clampUv * float3(wind.x, 0, wind.y) * _DynamicWindStrength;

    return staticWind + dynamicWind;
}

#endif
