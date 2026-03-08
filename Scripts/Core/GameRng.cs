using System;

public sealed class GameRng
{
    private uint _state;

    public GameRng()
    {
        _state = 2463534242u;
    }

    public GameRng(int seed)
    {
        _state = seed == 0 ? 2463534242u : (uint)seed;
    }

    public uint State
    {
        get => _state;
        set => _state = value == 0 ? 2463534242u : value;
    }

    public int NextInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
        }

        var span = (uint)(maxInclusive - minInclusive + 1);
        return minInclusive + (int)(NextUInt() % span);
    }

    public double NextDouble()
    {
        return NextUInt() / (double)uint.MaxValue;
    }

    public T Choose<T>(System.Collections.Generic.IReadOnlyList<T> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Cannot choose from empty list.");
        }

        return list[NextInt(0, list.Count - 1)];
    }

    private uint NextUInt()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x == 0 ? 2463534242u : x;
        return _state;
    }
}
