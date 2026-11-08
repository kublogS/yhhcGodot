public static class DeterministicSeed
{
    public static int Combine(int a, int b, int c)
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + a;
            hash = (hash * 31) + b;
            hash = (hash * 31) + c;
            return hash & int.MaxValue;
        }
    }
}
