using UnityEngine;

public class WindSampler
{
    public float _WindFrequency;
    public float _WindShiftSpeed;
    public Texture2D _WindTex;

    public Vector3 WindVelocity(Vector3 position)
    {
        float u = position.x * _WindFrequency;
        float v = position.z * _WindFrequency + Time.timeSinceLevelLoad * _WindShiftSpeed;
        Color wind = _WindTex.GetPixelBilinear(u, v, 0);
        return new Vector3(wind.r, 0, wind.g) * wind.b;
    }
}
