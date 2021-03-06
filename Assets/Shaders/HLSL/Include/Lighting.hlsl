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
};

// Computed lighting parameters
struct SurfaceBRDFParams
{
    half3 albedo;
    half3 diffuseColor;
    half3 specular;
    half smoothness;
    half reflectivity;
    half3 normal;
};

void CollectSurfaceData(half3 albedo, half metallic, half3 specular, half smoothness, inout half alpha, out SurfaceBRDFParams brdfParams)
{
#ifdef _SPECULAR_SETUP
    half reflectivity = ReflectivitySpecular(specular);
    half oneMinusReflectivity = 1.0 - reflectivity;
    half3 brdfDiffuse = albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
    half3 brdfSpecular = specular;
#else
    half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
    half reflectivity = 1.0 - oneMinusReflectivity;
    half3 brdfDiffuse = albedo * oneMinusReflectivity;
    half3 brdfSpecular = lerp(kDieletricSpec.rgb, albedo, metallic);
#endif

    brdfParams = (SurfaceBRDFParams)0;
    brdfParams.albedo = albedo;
    brdfParams.diffuseColor = brdfDiffuse;
    brdfParams.specular = brdfSpecular;
    brdfParams.reflectivity = reflectivity;
    brdfParams.smoothness = smoothness;
}

half3 LambertianDiffuse(Light light, SurfaceBRDFParams brdfParams, half3 viewDir)
{
    float LdotN = saturate(dot(light.direction, brdfParams.normal));

    half3 lightColor = light.shadowAttenuation * light.distanceAttenuation * light.color;
    half3 lambertianColor = LdotN * brdfParams.diffuseColor * lightColor * INV_PI;
    return lambertianColor;
}

half3 CookTorranceSpecular(Light light, SurfaceBRDFParams brdfParams, half3 viewDir)
{
    float3 halfVec = SafeNormalize(viewDir + light.direction);
    float LdotH = saturate(dot(light.direction, halfVec));
    float NdotH = saturate(dot(brdfParams.normal, halfVec));

    float D = 0.0; // distribution
    {
        D = pow(NdotH, brdfParams.smoothness);
        D *= (brdfParams.smoothness + 1) * 0.5 * INV_PI;
    }

    float FV = 0.0; // fresnel*visibility
    {
        FV = 1 / (LdotH * LdotH * LdotH);
    }

    half3 specularColor = brdfParams.specular * D * FV;
    return specularColor;
}

half3 LightingFunc(Light light, SurfaceBRDFParams brdfParams, half3 viewDir)
{
    half3 lambertianColor = LambertianDiffuse(light, brdfParams, viewDir);
    half3 specularColor = CookTorranceSpecular(light, brdfParams, viewDir);

    half3 color = lambertianColor + specularColor;

    return color;
}

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

    // BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
    // It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
    // below with your own.
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    SurfaceBRDFParams surfData;
    CollectSurfaceData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, surfData);
    surfData.normal = normalWS;

    // Direct light contribution
    half3 color = LightingFunc(mainLight, surfData, viewDirectionWS);

    // Mix diffuse GI with environment reflections.
    color += GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);

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
