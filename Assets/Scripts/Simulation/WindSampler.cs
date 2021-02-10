using UnityEngine;

public class WindSampler
{
    public float _WindFrequency;
    public float _WindShiftSpeed;
    public Texture2D _WindTex;
    public Vector4 _WindSpherePositionAndRadius;

    public Vector3 WindVelocity(Vector3 position)
    {
        Vector3 distVec = (Vector3)_WindSpherePositionAndRadius - position;
        bool isInWindSphereInfluence = _WindSpherePositionAndRadius.w < 0 ||
            distVec.sqrMagnitude < _WindSpherePositionAndRadius.w * _WindSpherePositionAndRadius.w;
        float u = position.x * _WindFrequency + Time.timeSinceLevelLoad * _WindShiftSpeed;
        float v = position.z * _WindFrequency + Time.timeSinceLevelLoad * _WindShiftSpeed;
        Color wind = _WindTex.GetPixelBilinear(u, v, 0);
        return isInWindSphereInfluence ? new Vector3(wind.r, 0, wind.g) * wind.b : Vector3.zero;
    }
}
