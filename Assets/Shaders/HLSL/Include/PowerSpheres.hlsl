#ifndef _POWER_SPHERES_H
#define _POWER_SPHERES_H

float4 _ColorSpherePositionAndRadius;
float4 _WindSpherePositionAndRadius;
float4 _GreenSpherePositionAndRadius;
float4 _WaterSpherePositionAndRadius;

TEXTURE2D(_NoiseTex);
SAMPLER(sampler_NoiseTex);

struct SphereInfluence
{
    half3 boundaryColor;
};

struct ColorSphereInfluence
{
    SphereInfluence base;
    half3 albedoSwap;
};


inline SphereInfluence getSphereInfluence(half3 boundaryColor, float3 positionWS, float4 positionAndRadiusWS)
{
    half3 colorBoundary = boundaryColor;

    float3 distVec = positionAndRadiusWS.xyz - positionWS;
    float3 d = normalize(distVec);
    float2 uv = float2(-atan2(d.x, d.z) * 0.5, -asin(d.y)) * INV_PI + 0.5;
    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv + _Time.x).r;

    float dist = length(distVec);
    float closeness = 1 - saturate(dist / positionAndRadiusWS.w); // 1 at the center
    closeness *= noise;

    const float cutoff = 0.02f; // min closeness
    const float width = 0.02f;
    bool isInner = closeness - width > cutoff;
    bool isOuter = closeness < cutoff;

    SphereInfluence result;
    result.boundaryColor = !isInner * !isOuter * colorBoundary;
    return result;
}

inline ColorSphereInfluence getColorSphereInfluence(half3 originalAlbedo, float3 positionWS, float4 positionAndRadiusWS)
{
    half3 colorInner = originalAlbedo;
    half3 colorOuter = half3(.05, .05, .05);
    half3 colorBoundary = 1;

    float3 distVec = positionAndRadiusWS.xyz - positionWS;
    float3 d = normalize(distVec);
    float2 uv = float2(-atan2(d.x, d.z) * 0.5, -asin(d.y)) * INV_PI + 0.5;
    float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv + _Time.x).r;
    float noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, (positionWS.xz * 0.1f) + _Time.x).r;
    noise2 *= length(distVec.xz) / positionAndRadiusWS.w;

    float dist = length(distVec);
    float closeness = 1 - saturate(dist / positionAndRadiusWS.w); // 1 at the center
    closeness *= noise;

    const float cutoff = 0.05f; // min closeness
    const float width = 0.05f;
    bool isInner = closeness - width > cutoff && noise2 < 0.5f;
    bool isOuter = closeness < cutoff;

    half3 innerColor = isInner * colorInner;
    half3 outerColor = isOuter * colorOuter;
    half3 boundaryColor = !isInner * !isOuter * colorBoundary;

    ColorSphereInfluence result;
    result.base.boundaryColor = boundaryColor;
    result.albedoSwap = innerColor + outerColor + boundaryColor;
    return result;
}

#endif /* _POWER_SPHERES_H */
