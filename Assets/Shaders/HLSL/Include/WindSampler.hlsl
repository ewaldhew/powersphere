#ifndef _WIND_SAMPLER_H
#define _WIND_SAMPLER_H

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Util.hlsl"
#include "PowerSpheres.hlsl"

TEXTURE2D(_WindTex);
SAMPLER(sampler_WindTex);

CBUFFER_START(UnityPerMaterial)
    float _WindFrequency;
    float _WindShiftSpeed;
    float _AnimTime;
CBUFFER_END

float3 WindVelocity(float3 pos_WS)
{
    bool isInWindSphereInfluence = IsWithinSphere(pos_WS, _WindSpherePositionAndRadius);
    float2 uv = pos_WS.xz * _WindFrequency + _AnimTime * _WindShiftSpeed;
    float3 wind = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, uv, 0).xyz;
    return isInWindSphereInfluence * float3(wind.x, 0, wind.y) * wind.z;
}

#endif
