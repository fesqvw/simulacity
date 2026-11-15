namespace Simulacity.Core.Geometry;

/// <summary>
/// A point in world coordinates, in metres: x points east and y points north (decision 0007).
/// </summary>
public readonly record struct Point2(double X, double Y)
{
    /// <summary>Whether both coordinates are finite numbers (not NaN or infinity).</summary>
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

    /// <summary>The straight-line distance to another point, in metres.</summary>
    public double DistanceTo(Point2 other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <inheritdoc/>
    public override string ToString() => FormattableString.Invariant($"({X}, {Y})");
}
