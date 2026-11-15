using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;

namespace Simulacity.Core;

/// <summary>
/// The city being simulated: its land and roads, and later its buildings. It doesn't know which
/// simulation technique is running (decision 0008).
/// </summary>
/// <remarks>
/// Change the world through its own methods, which keep the rules that involve both land and roads:
/// water can only be crossed on a bridge or in a tunnel (decision 0012). <see cref="Land"/> and
/// <see cref="Roads"/> can be changed directly, but then nothing checks those rules.
/// </remarks>
public sealed class World(LandGrid land)
{
    /// <summary>The land under the city.</summary>
    public LandGrid Land { get; } = land ?? throw new ArgumentNullException(nameof(land));

    /// <summary>The road graph.</summary>
    public RoadNetwork Roads { get; } = new();

    /// <summary>
    /// Adds a road and joins it to the roads it crosses at the same height (decision 0011). Where it
    /// runs above or below the terrain it's a bridge or a tunnel (decision 0012).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The road can't be added, for example because it would cross water at ground level. Nothing is changed.
    /// </exception>
    public RoadAddition AddRoad(NodeId start, NodeId end, SegmentShape? shape = null)
    {
        if (Roads.ContainsNode(start) && Roads.ContainsNode(end) && start != end)
        {
            IReadOnlyList<Point3> polyline = (shape ?? PolylineShape.Straight)
                .ToPolyline(Roads.GetNode(start).Position, Roads.GetNode(end).Position);
            if (RoadProfile.TryFindGroundLevelWater(Land, polyline, out Point3 where))
            {
                throw new ArgumentException(
                    $"The road would cross water at ground level near {Describe(where)}. " +
                    $"Raise it more than {RoadProfile.GradeTolerance} m above the water for a bridge, " +
                    $"or lower it more than {RoadProfile.GradeTolerance} m below the bed for a tunnel.");
            }
        }

        return Roads.AddRoad(start, end, shape);
    }

    /// <summary>A segment split into stretches on the ground, on bridges and in tunnels (decision 0012).</summary>
    /// <exception cref="KeyNotFoundException">There is no such segment.</exception>
    public IReadOnlyList<RoadStretch> StretchesOf(SegmentId id) => RoadProfile.Stretches(Land, Roads.PolylineOf(id));

    /// <summary>
    /// Sets every cell in the rectangle between two corner cells, inclusive, to a land type.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A cell is outside the grid.</exception>
    /// <exception cref="InvalidOperationException">
    /// A road would then run at ground level over water. Nothing is changed.
    /// </exception>
    public void SetLand(int x1, int y1, int x2, int y2, LandType type)
    {
        if (!Land.Contains(x1, y1) || !Land.Contains(x2, y2))
        {
            throw new ArgumentOutOfRangeException(null, $"The grid's cells go from (0, 0) to ({Land.Width - 1}, {Land.Height - 1}).");
        }

        (int west, int east) = (Math.Min(x1, x2), Math.Max(x1, x2));
        (int south, int north) = (Math.Min(y1, y2), Math.Max(y1, y2));
        var previous = new LandType[east - west + 1, north - south + 1];
        for (int y = south; y <= north; y++)
        {
            for (int x = west; x <= east; x++)
            {
                previous[x - west, y - south] = Land[x, y];
                Land[x, y] = type;
            }
        }

        RevertIfRoadInWater(() =>
        {
            for (int y = south; y <= north; y++)
            {
                for (int x = west; x <= east; x++)
                {
                    Land[x, y] = previous[x - west, y - south];
                }
            }
        });
    }

    /// <summary>Sets the ground elevation at a cell corner, in metres.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The corner is outside the grid.</exception>
    /// <exception cref="ArgumentException">The elevation isn't a finite number.</exception>
    /// <exception cref="InvalidOperationException">
    /// A road would then run at ground level over water. Nothing is changed.
    /// </exception>
    public void SetCornerElevation(int i, int j, double elevation) => SetCornerElevations([(i, j, elevation)]);

    /// <summary>
    /// Sets the ground elevation at several cell corners at once, in metres, as one change: either
    /// all of them are set or none are.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A corner is outside the grid. Nothing is changed.</exception>
    /// <exception cref="ArgumentException">An elevation isn't a finite number. Nothing is changed.</exception>
    /// <exception cref="InvalidOperationException">
    /// A road would then run at ground level over water. Nothing is changed.
    /// </exception>
    public void SetCornerElevations(IEnumerable<(int I, int J, double Elevation)> corners)
    {
        ArgumentNullException.ThrowIfNull(corners);

        var previous = new List<(int I, int J, double Elevation)>();
        void Revert()
        {
            for (int k = previous.Count - 1; k >= 0; k--)
            {
                Land.SetCornerElevation(previous[k].I, previous[k].J, previous[k].Elevation);
            }
        }

        try
        {
            foreach ((int i, int j, double elevation) in corners)
            {
                double old = Land.GetCornerElevation(i, j);
                Land.SetCornerElevation(i, j, elevation);
                previous.Add((i, j, old));
            }
        }
        catch (ArgumentException)
        {
            Revert();
            throw;
        }

        RevertIfRoadInWater(Revert);
    }

    private void RevertIfRoadInWater(Action revert)
    {
        foreach (RoadSegment segment in Roads.Segments.OrderBy(segment => segment.Id.Value))
        {
            if (RoadProfile.TryFindGroundLevelWater(Land, Roads.PolylineOf(segment.Id), out Point3 where))
            {
                revert();
                throw new InvalidOperationException(
                    $"Road {segment.Id} would then run at ground level through water near {Describe(where)}. " +
                    "Water can only be crossed on a bridge or in a tunnel.");
            }
        }
    }

    private static string Describe(Point3 point) =>
        FormattableString.Invariant($"({point.X:0.##}, {point.Y:0.##})");
}
