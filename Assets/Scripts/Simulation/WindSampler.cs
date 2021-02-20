using UnityEngine;

public class WindSampler
{
    public float _WindFrequency;
    public float _WindShiftSpeed;
    public float _WindStrength;
    public Texture2D _WindTex;
    public Vector4 _WindSpherePositionAndRadius;

    public Texture2D _WindBuffer;
    public Vector3 _WindBufferCenter;
    public float _WindBufferRange;
    public float _DynamicWindStrength;
    public float _DynamicWindRadius;

    private float sampleBuffer(float x, float y)
    {
        float windBufferU = ((x - _WindBufferCenter.x) / _WindBufferRange) * 0.5f + 0.5f;
        float windBufferV = ((y - _WindBufferCenter.z) / _WindBufferRange) * 0.5f + 0.5f;
        if (windBufferU < 0 || windBufferU > 1 || windBufferV < 0 || windBufferV > 1) {
            return 0;
        } else {
            return _WindBuffer.GetPixelBilinear(windBufferU, windBufferV, 0).r * 2 - 1;
        }
    }

    public Vector3 WindVelocity(Vector3 position)
    {
        Vector3 distVec = (Vector3)_WindSpherePositionAndRadius - position;
        bool isInWindSphereInfluence = _WindSpherePositionAndRadius.w < 0 ||
            distVec.sqrMagnitude < _WindSpherePositionAndRadius.w * _WindSpherePositionAndRadius.w;
        float u = position.x * _WindFrequency + Time.timeSinceLevelLoad * _WindShiftSpeed;
        float v = position.z * _WindFrequency + Time.timeSinceLevelLoad * _WindShiftSpeed;
        Color wind = _WindTex.GetPixelBilinear(u, v, 0);
        Vector3 staticWind = isInWindSphereInfluence ? new Vector3(wind.r, 0, wind.g) * wind.b : Vector3.zero;

#if CURL_WIND
        const float dx = 0.1f;
        const float scale = 0.5f / dx;
        float dPdx = scale * (sampleBuffer(position.x + dx, position.z) - sampleBuffer(position.x - dx, position.z));
        float dPdy = scale * (sampleBuffer(position.x, position.z + dx) - sampleBuffer(position.x, position.z - dx));
        Vector2 curl = new Vector2(dPdy, -dPdx);
        Vector3 dynamicWind = new Vector3(curl.normalized.x, 0, curl.normalized.y) * curl.magnitude;
#else
        u = ((position.x - _WindBufferCenter.x) / _WindBufferRange) * 0.5f + 0.5f;
        v = ((position.z - _WindBufferCenter.z) / _WindBufferRange) * 0.5f + 0.5f;
        if (u < 0 || u > 1 || v < 0 || v > 1) {
            wind = Color.black;
        } else {
            wind = _WindBuffer.GetPixelBilinear(u, v, 0);
        }
        Vector3 dynamicWind = new Vector3(wind.r, 0, wind.g) * _DynamicWindStrength;
#endif

        return staticWind + dynamicWind;
    }
}
