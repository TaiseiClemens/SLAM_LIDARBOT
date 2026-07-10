using System;

public readonly struct PathNode : IEquatable<PathNode>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Distance;

    public PathNode(int x, int y, int distance)
    {
        X = x;
        Y = y;
        Distance = distance;
    }

    public bool Equal(PathNode other)
    {
        return X == other.X && Y == other.Y;
    }

    public bool Equals(PathNode other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is PathNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}