using Simulacity.Core.Geometry;

namespace Simulacity.Core.Roads;

/// <summary>
/// The shape of a road segment between its two nodes. Kept separate from the graph so that more
/// shapes, such as splines, can be added later without changing the topology (decision 0009).
/// </summary>
public abstract class SegmentShape
{
    /// <summary>
    /// The shape as a polyline from <paramref name="start"/> to <paramref name="end"/>, for anything
    /// that needs straight lines (lengths, intersection tests, rendering).
    /// </summary>
    public abstract IReadOnlyList<Point3> ToPolyline(Point3 start, Point3 end);
}

/// <summary>A polyline shape: straight lines from the start node, through its points, to the end node.</summary>
public sealed class PolylineShape : SegmentShape
{
    private readonly Point3[] _points;

    /// <summary>Creates a polyline shape through the given points, in order from the start node.</summary>
    public PolylineShape(IEnumerable<Point3> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        _points = [.. points];
        if (Array.Exists(_points, point => !point.IsFinite))
        {
            throw new ArgumentException("Every point must have finite coordinates.", nameof(points));
        }
    }

    /// <summary>A straight line between the two nodes, with no points in between.</summary>
    public static PolylineShape Straight { get; } = new([]);

    /// <summary>The points between the two nodes, in order from the start node.</summary>
    public IReadOnlyList<Point3> Points => _points;

    /// <inheritdoc/>
    public override IReadOnlyList<Point3> ToPolyline(Point3 start, Point3 end) => [start, .. _points, end];
}
