namespace Simulacity.Core.Geometry;

/// <summary>
/// A point in world coordinates, in metres: x points east, y points north and z points up.
/// z is the elevation above a fixed datum, such as sea level (decision 0009).
/// </summary>
public readonly record struct Point3(double X, double Y, double Z)
{
    /// <summary>Whether all three coordinates are finite numbers (not NaN or infinity).</summary>
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);

    /// <summary>The straight-line distance to another point in 3D, in metres.</summary>
    public double DistanceTo(Point3 other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    /// <inheritdoc/>
    public override string ToString() => FormattableString.Invariant($"({X}, {Y}, {Z})");
}
