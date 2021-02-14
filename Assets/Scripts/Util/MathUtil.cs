using UnityEngine;

public static class MathUtil
{
    public static int DivideByMultiple(int value, int alignment)
    {
        return (value + alignment - 1) / alignment;
    }

    // Generates the first n items in the 2,3 Halton sequence.
    public static Vector2[] GenerateHalton23(uint length)
    {
        Vector2[] halton = new Vector2[length];

        int n2 = 0, d2 = 1;
        int n3 = 0, d3 = 1;
        for (int i = 0; i < length; i++) {
            int x2 = d2 - n2;
            if (x2 == 1) {
                n2 = 1;
                d2 *= 2;
            } else {
                int y2 = d2 / 2;
                while (x2 <= y2) {
                    y2 /= 2;
                }
                n2 = 3 * y2 - x2;
            }
            int x3 = d3 - n3;
            if (x3 == 1) {
                n3 = 1;
                d3 *= 3;
            } else {
                int y3 = d3 / 3;
                while (x3 <= y3) {
                    y3 /= 3;
                }
                n3 = 4 * y3 - x3;
            }

            halton[i] = new Vector2(n2 / (float)d2, n3 / (float)d3);
        }

        return halton;
    }
}
