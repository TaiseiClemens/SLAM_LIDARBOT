using System;

public readonly struct ChunkKey : IEquatable<ChunkKey>
{
    public readonly int X;
    public readonly int Y;

    public ChunkKey(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(ChunkKey other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is ChunkKey other && Equals(other); // TODO: What is going on here?
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}