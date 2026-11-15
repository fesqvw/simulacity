namespace Simulacity.Core.Geometry;

/// <summary>Helpers for polylines: chains of straight lines through a list of points.</summary>
public static class Polyline
{
    /// <summary>The total length of the polyline, in metres. Zero for fewer than two points.</summary>
    public static double Length(IReadOnlyList<Point2> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        double length = 0;
        for (int i = 1; i < points.Count; i++)
        {
            length += points[i - 1].DistanceTo(points[i]);
        }

        return length;
    }
}
