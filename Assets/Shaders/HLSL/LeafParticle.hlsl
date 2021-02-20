#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Include/Util.hlsl"
#include "Include/PowerSpheres.hlsl"

struct AttributesParticle
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    float4 uvAndVelocityXY : TEXCOORD0;
    float4 velocityZAndCenter : TEXCOORD1;
    float4 particleSizeAndFacing : TEXCOORD2;
};

struct VaryingsParticle
{
    half4 color : COLOR;
    float2 texcoord : TEXCOORD0;

    float4 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float3 viewDirWS : TEXCOORD3;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD7;
#endif

    float3 vertexSH : TEXCOORD8; // SH
    float4 clipPos : SV_POSITION;
};

void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData output)
{
    output = (InputData)0;

    output.positionWS = input.positionWS.xyz;

    half3 viewDirWS = input.viewDirWS;
    output.normalWS = input.normalWS;

    output.normalWS = NormalizeNormalPerPixel(output.normalWS);

#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
#else
    output.shadowCoord = float4(0, 0, 0, 0);
#endif

    output.fogCoord = (half)input.positionWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = SampleSHPixel(input.vertexSH, output.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle ParticlesLitVertex(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;

    // transform by velocity
    #define EPSILON 0.01f
    float radius = input.particleSizeAndFacing.x / 2;
    float3 facingVec = input.particleSizeAndFacing.yzw;
    float3 velocityVec = float3(input.uvAndVelocityXY.zw, input.velocityZAndCenter.x);
    float3 center = input.velocityZAndCenter.yzw;

    float3 facing = normalize(float3(facingVec.x, facingVec.y, 0));
    float3x3 rotateMatrix = RotateTowards(float3(1, 0, 0), facing);

    velocityVec = (center.y > radius + EPSILON) && length(velocityVec) > 0 ? normalize(velocityVec) : float3(0, 1, 0);
    velocityVec *= sign(velocityVec.y);
    float3x3 objectToVelocityMatrix = RotateTowards(float3(0, 0, 1), velocityVec);

    float3 localVertex = input.vertex.xyz - center;
    localVertex = mul(objectToVelocityMatrix, mul(rotateMatrix, localVertex));
    float3 inputVertex = localVertex + center;
    inputVertex.y -= radius - EPSILON;

    float3 normalWS = velocityVec;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(inputVertex.xyz);
    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

    normalWS *= sign(dot(viewDirWS, normalWS));

#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.normalWS = normalWS;
    output.viewDirWS = viewDirWS;

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.positionWS.xyz = vertexInput.positionWS;
    output.positionWS.w = 0;
    output.clipPos = vertexInput.positionCS;
    output.color = input.color;

    output.texcoord = input.uvAndVelocityXY.xy;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

half4 ParticlesLitFragment(VaryingsParticle input) : SV_Target
{
    float dist = distance(_GreenSpherePositionAndRadius.xyz, input.positionWS.xyz);
    float closeness = 1 - smoothstep(0, 1, dist / _GreenSpherePositionAndRadius.w); // 1 at the center
    input.color.a *= closeness;

    float3 blendUv = float3(0, 0, 0);

    SurfaceData surfaceData;
    InitializeParticleLitSurfaceData(input.texcoord, blendUv, input.color, 0, surfaceData);

    ColorSphereInfluence colorSphereInfluence = getColorSphereInfluence(surfaceData.albedo, input.positionWS.xyz, _ColorSpherePositionAndRadius);
    surfaceData.albedo = lerp(surfaceData.albedo, colorSphereInfluence.albedoSwap, _ColorSpherePositionAndRadius.w > 0);
    surfaceData.emission = colorSphereInfluence.base.boundaryColor;

    InputData inputData = (InputData)0;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData.albedo,
    surfaceData.metallic, half3(0, 0, 0), surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);

    return color;
}
