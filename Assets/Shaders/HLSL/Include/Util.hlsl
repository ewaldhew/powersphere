#ifndef _UTIL_H
#define _UTIL_H

static const float4x4 IdentityMatrix =
{
    1, 0, 0, 0,
    0, 1, 0, 0,
    0, 0, 1, 0,
    0, 0, 0, 1
};

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

// Construct a rotation matrix that rotates first vector onto second vector
// https://math.stackexchange.com/a/476311
float3x3 RotateTowards(float3 from, float3 to)
{
    float3 axb = cross(from, to);
    float c = dot(from, to);
    float3x3 v = float3x3(
        0, -axb.z, axb.y,
        axb.z, 0, -axb.x,
        -axb.y, axb.x, 0
    );

    return float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1) + v + mul(v, v) / (1 + c);
}


inline bool IsWithinSphere(float3 testPos, float4 sphereCenterAndRadius)
{
    float3 distVec = sphereCenterAndRadius.xyz - testPos.xyz;
    return sphereCenterAndRadius.w < 0 ||
        dot(distVec, distVec) < sphereCenterAndRadius.w * sphereCenterAndRadius.w;
}

#endif
