#ifndef _EDGE_FILTER_H
#define _EDGE_FILTER_H

#if PASS == 0
static const int2 kDirection = int2(1, 0);
inline uint flattenIndex(uint2 id) { return id.x; }
#else
static const int2 kDirection = int2(0, 1);
inline uint flattenIndex(uint2 id)
{
    return id.y;
}
#endif

#define KERNEL_SIZE 1
static const float derivativeKernel[KERNEL_SIZE * 2 + 1] =
{
    1,
    0,
    -1
};
static const float averageKernel[KERNEL_SIZE * 2 + 1] =
{
    1,
    1,
    1
};

float4 sampleOffset(Texture2D tex, uint2 sampleLocation, int2 offset)
{
    return tex[sampleLocation + offset];
}

 // x derivative is used for x direction
float filterKernelDerivative(uint2 pixelCenter, Texture2D inputTex)
{
    float derivative = 0;
    for (int i = -KERNEL_SIZE; i <= KERNEL_SIZE; ++i) {
        float sample = sampleOffset(inputTex, pixelCenter, kDirection * i).r;
        derivative += derivativeKernel[i + KERNEL_SIZE] * sample;
    }

    return derivative;
}

 // y average is used for x direction
float filterKernelAverage(uint2 pixelCenter, Texture2D inputTex)
{
    float average = 0;
    for (int i = -KERNEL_SIZE; i <= KERNEL_SIZE; ++i) {
        float sample = sampleOffset(inputTex, pixelCenter, kDirection * i).r;
        average += averageKernel[i + KERNEL_SIZE] * sample;
    }

    return average;
}

#endif // _EDGE_FILTER_H
