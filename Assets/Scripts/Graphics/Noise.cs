using UnityEngine;

public static class Noise
{
    private static float SQRT2 = Mathf.Sqrt(2);

    private static int[] hash = {
        151,160,137, 91, 90, 15,131, 13,201, 95, 96, 53,194,233,  7,225,
        140, 36,103, 30, 69,142,  8, 99, 37,240, 21, 10, 23,190,  6,148,
        247,120,234, 75,  0, 26,197, 62, 94,252,219,203,117, 35, 11, 32,
         57,177, 33, 88,237,149, 56, 87,174, 20,125,136,171,168, 68,175,
         74,165, 71,134,139, 48, 27,166, 77,146,158,231, 83,111,229,122,
         60,211,133,230,220,105, 92, 41, 55, 46,245, 40,244,102,143, 54,
         65, 25, 63,161,  1,216, 80, 73,209, 76,132,187,208, 89, 18,169,
        200,196,135,130,116,188,159, 86,164,100,109,198,173,186,  3, 64,
         52,217,226,250,124,123,  5,202, 38,147,118,126,255, 82, 85,212,
        207,206, 59,227, 47, 16, 58, 17,182,189, 28, 42,223,183,170,213,
        119,248,152,  2, 44,154,163, 70,221,153,101,155,167, 43,172,  9,
        129, 22, 39,253, 19, 98,108,110, 79,113,224,232,178,185,112,104,
        218,246, 97,228,251, 34,242,193,238,210,144, 12,191,179,162,241,
         81, 51,145,235,249, 14,239,107, 49,192,214, 31,181,199,106,157,
        184, 84,204,176,115,121, 50, 45,127,  4,150,254,138,236,205, 93,
        222,114, 67, 29, 24, 72,243,141,128,195, 78, 66,215, 61,156,180
    };

    private static int Hash(int i)
    {
        const int hashMask = 255;
        return hash[i & hashMask];
    }

    private static Vector2[] gradients2D = {
        new Vector2( 1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2( 0f, 1f),
        new Vector2( 0f,-1f),
        new Vector2( 1f, 1f).normalized,
        new Vector2(-1f, 1f).normalized,
        new Vector2( 1f,-1f).normalized,
        new Vector2(-1f,-1f).normalized
    };

    private static Vector2 Gradient(int i)
    {
        const int gradientsMask2D = 3;
        return gradients2D[i & gradientsMask2D];
    }

    /** Returns Perlin noise: Vec2 -> [-1, 1] */
    public static float Eval2D(float x, float y, int period = -1)
    {
        int Wrap(int k)
        {
            if (period < 0) {
                return k;
            } else {
                return k < 0 ? (k % period) + period : (k % period);
            }
        }

        int i = Mathf.FloorToInt(x);
        int j = Mathf.FloorToInt(y);

        int i0 = Wrap(i);
        int i1 = Wrap(i + 1);
        int j0 = Wrap(j);
        int j1 = Wrap(j + 1);

        int h0 = Hash(i0);
        int h1 = Hash(i1);

        Vector2 g00 = Gradient(Hash(h0 + j0));
        Vector2 g01 = Gradient(Hash(h0 + j1));
        Vector2 g10 = Gradient(Hash(h1 + j0));
        Vector2 g11 = Gradient(Hash(h1 + j1));

        float tx0 = x - i;
        float ty0 = y - j;
        float tx1 = tx0 - 1;
        float ty1 = ty0 - 1;

        float v00 = Vector2.Dot(g00, new Vector2(tx0, ty0));
        float v01 = Vector2.Dot(g01, new Vector2(tx0, ty1));
        float v10 = Vector2.Dot(g10, new Vector2(tx1, ty0));
        float v11 = Vector2.Dot(g11, new Vector2(tx1, ty1));

        float grad0 = Mathf.SmoothStep(v00, v10, tx0);
        float grad1 = Mathf.SmoothStep(v01, v11, tx0);
        return Mathf.SmoothStep(grad0, grad1, ty0) * Mathf.Sqrt(2);
    }

    /** Returns fractal noise from Perlin noise */
    public static float Eval2DFrac(float x, float y, int baseFreq, uint octaves, int period = -1, float persistence = 0.5f)
    {
        int frequency = baseFreq;
        float sum = Eval2D(x * frequency, y * frequency, period * frequency);
        float amplitude = 1.0f;
        float range = 1.0f;
        for (int o = 1; o < octaves; o++) {
            frequency *= 2;
            amplitude *= persistence;
            range += amplitude;
            sum += Eval2D(x * frequency, y * frequency, period * frequency) * amplitude;
        }
        return sum / range;
    }
}
