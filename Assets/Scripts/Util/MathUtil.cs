public static class MathUtil
{
    public static int DivideByMultiple(int value, int alignment)
    {
        return (value + alignment - 1) / alignment;
    }
}
