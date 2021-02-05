using UnityEngine;

public static class TextureCreator
{
    private static int TextureIndex = 0;

    public static Texture2D GetTexture2D(int size, string name)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBAFloat, true);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;
        return tex;
    }

    public static Texture2D PerlinClouds(int size, int seed)
    {
        Texture2D tex = GetTexture2D(size, "PerlinClouds" + TextureIndex++);
        float step = 1.0f / size;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Vector2 uv = new Vector2((x + 0.5f) * step, (y + 0.5f) * step);
                Vector3 samplepos = new Vector3(uv.x - 0.5f, seed*uv.x + uv.y - 0.5f, 0.0f);
                tex.SetPixel(x, y, Color.white * (Noise.Eval2DFrac(samplepos.x, samplepos.y, 10, 4) * 0.5f + 0.5f));
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D PerlinCurl(int size, int seed)
    {
        Texture2D tex = GetTexture2D(size, "PerlinCurl" + TextureIndex++);
        float step = 1.0f / size;
        float[] values = new float[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Vector2 uv = new Vector2((x + 0.5f) * step, (y + 0.5f) * step);
                Vector3 samplepos = new Vector3(uv.x - 0.5f, seed*uv.x + uv.y - 0.5f, 0.0f);
                values[x + y * size] = Noise.Eval2DFrac(samplepos.x, samplepos.y, 8, 2, 1) * 0.5f + 0.5f;
            }
        }
        float getValue(int x, int y)
        {
            int xwrap = (x + size) % size;
            int ywrap = (y + size) % size;
            return values[xwrap + ywrap * size];
        };
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                // centered-difference
                float dFdx = (getValue(x + 1, y) - getValue(x - 1, y)) / (2 * step);
                float dFdy = (getValue(x, y + 1) - getValue(x, y - 1)) / (2 * step);
                Vector2 curl = new Vector2(dFdy, -dFdx);
                float speed = curl.magnitude;
                curl.Normalize();
                tex.SetPixel(x, y, new Color(curl.x, curl.y, speed));
            }
        }
        tex.Apply();
        return tex;
    }
}
