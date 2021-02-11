#ifndef _LIGHTING_H
#define _LIGHTING_H

// Include this if you are doing a lit shader. This includes lighting shader variables,
// lighting and shadow functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "PowerSpheres.hlsl"

struct LightingInput
{ // vertex data
    float2 uv;
    float4 positionWSAndFogFactor; // xyz: positionWS, w: vertex fog factor
    half3 normalWS;

#if _NORMALMAP
    half3 tangentWS;
    half3 bitangentWS;
#endif

#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord; // compute shadow coord per-vertex for the main light
#endif
    float4 positionCS;
};

struct SurfaceInput
{ // sampled surface data
    half3 albedo;
    half3 specular;
    half metallic;
    half smoothness;
    half3 normalTS;
    half3 emission;
    half occlusion;
    half alpha;
    half clearCoatMask;
    half clearCoatSmoothness;
};

half4 ResolveLighting(LightingInput input, SurfaceInput surfaceData)
{
    float3 positionWS = input.positionWSAndFogFactor.xyz;

    ColorSphereInfluence colorSphereInfluence = getColorSphereInfluence(surfaceData.albedo, positionWS, _ColorSpherePositionAndRadius);
    surfaceData.albedo = lerp(surfaceData.albedo, colorSphereInfluence.albedoSwap, _ColorSpherePositionAndRadius.w > 0);
    surfaceData.emission = colorSphereInfluence.base.boundaryColor;

    SphereInfluence windSphereInfluence = getSphereInfluence(half3(0, 0, 1), positionWS, _WindSpherePositionAndRadius);
    surfaceData.albedo += windSphereInfluence.boundaryColor;

    SphereInfluence greenSphereInfluence = getSphereInfluence(half3(0, 1, 0), positionWS, _GreenSpherePositionAndRadius);
    surfaceData.albedo += greenSphereInfluence.boundaryColor;

#if _NORMALMAP
    half3 normalWS = TransformTangentToWorld(surfaceData.normalTS,
    half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
#else
    half3 normalWS = input.normalWS;
#endif
    normalWS = normalize(normalWS);

#ifdef LIGHTMAP_ON
    // Normal is required in case Directional lightmaps are baked
    half3 bakedGI = SampleLightmap(input.uvLM, normalWS);
#else
    // Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
    // are also defined in case you want to sample some terms per-vertex.
    half3 bakedGI = SampleSH(normalWS);
#endif

    half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);

    // BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
    // It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
    // below with your own.
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    // Light struct is provide by LWRP to abstract light shader variables.
    // It contains light direction, color, distanceAttenuation and shadowAttenuation.
    // LWRP take different shading approaches depending on light and platform.
    // You should never reference light shader variables in your shader, instead use the GetLight
    // funcitons to fill this Light struct.
#ifdef _MAIN_LIGHT_SHADOWS
    // Main light is the brightest directional light.
    // It is shaded outside the light loop and it has a specific set of variables and shading path
    // so we can be as fast as possible in the case when there's only a single directional light
    // You can pass optionally a shadowCoord (computed per-vertex). If so, shadowAttenuation will be
    // computed.
    Light mainLight = GetMainLight(input.shadowCoord);
#else
    Light mainLight = GetMainLight();
#endif

    // Mix diffuse GI with environment reflections.
    half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);

    // LightingPhysicallyBased computes direct light contribution.
    color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);

    // Additional lights loop
#ifdef _ADDITIONAL_LIGHTS

    // Returns the amount of lights affecting the object being renderer.
    // These lights are culled per-object in the forward renderer
    int additionalLightsCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightsCount; ++i)
    {
        // Similar to GetMainLight, but it takes a for-loop index. This figures out the
        // per-object light index and samples the light buffer accordingly to initialized the
        // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
        Light light = GetAdditionalLight(i, positionWS);

        // Same functions used to shade the main light.
        color += LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
    }
#endif
    // Emission
    color += surfaceData.emission;

    float fogFactor = input.positionWSAndFogFactor.w;

    // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
    // with a custom one.
    color = MixFog(color, fogFactor);
    return half4(color, surfaceData.alpha);
}

#endif /* _LIGHTING_H */
