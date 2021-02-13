#define GROUP_WIDTH 64

#if PASS == 0
#define GROUP_SIZE_X GROUP_WIDTH
#define GROUP_SIZE_Y 1
#else
#define GROUP_SIZE_X 1
#define GROUP_SIZE_Y GROUP_WIDTH
#endif

#include "Include/EdgeFilter.hlsl"

Texture2D<float> NoiseTex;
SamplerState samplerNoiseTex;

Texture2D<float4> gCameraOutput;
Texture2D<float4> gPostInput;
RWTexture2D<float4> gPostOutput;

cbuffer UniformParams
{
    int screenWidth;
    int screenHeight;
};

[numthreads(GROUP_SIZE_X, GROUP_SIZE_Y, 1)]
void main(uint3 dispatchId : SV_DispatchThreadID, uint3 threadId : SV_GroupThreadID, uint3 groupId : SV_GroupID)
{
    uint2 pixelCoord = dispatchId.xy;
    float2 uv = pixelCoord / float2(screenWidth, screenHeight);
    float noise = NoiseTex.SampleLevel(samplerNoiseTex, uv, 0) * 2 - 1;
    uint2 samplePoint = round(pixelCoord + noise * 10);

    float d = filterKernelDerivative(samplePoint, gPostInput);
    float a = filterKernelAverage(samplePoint, gPostInput);

#if PASS == 0
    gPostOutput[pixelCoord] = float4(d, a, 0, 1);
#else
    gPostOutput[pixelCoord] = float4(a, d, 0, 1);
#endif

#if PASS == 1
    float darkenFactor = length(gPostOutput[pixelCoord].rg) * 50;
    float brightenFactor = 2;

    float4 baseColor = gCameraOutput[samplePoint] * brightenFactor;
    float4 quantColor = floor(sqrt(baseColor) * 10) * .1;

    gPostOutput[pixelCoord] = quantColor * (1 - darkenFactor);
#endif
}
