using Simulacity.Core.Geometry;
using Simulacity.Core.Land;

namespace Simulacity.Core.Roads;

/// <summary>Whether a stretch of road runs on the ground, above it or below it (decision 0012).</summary>
public enum RoadLevel
{
    /// <summary>On the ground: within <see cref="RoadProfile.GradeTolerance"/> of the terrain.</summary>
    Ground,

    /// <summary>A bridge: higher than the terrain by more than the tolerance.</summary>
    Bridge,

    /// <summary>A tunnel: lower than the terrain by more than the tolerance.</summary>
    Tunnel,
}

/// <summary>A stretch of road at one level, as a polyline.</summary>
/// <param name="Level">Whether the stretch is on the ground, a bridge or a tunnel.</param>
/// <param name="Points">The stretch's points, from where it starts to where it ends.</param>
public sealed record RoadStretch(RoadLevel Level, IReadOnlyList<Point3> Points);

/// <summary>
/// Works out where a road is on the ground, a bridge or a tunnel by comparing its height with the
/// terrain under it (decision 0012). Nothing about this is stored on the road.
/// </summary>
public static class RoadProfile
{
    /// <summary>
    /// How far above or below the terrain a road can be and still be on the ground, in metres.
    /// Further up it's a bridge, and further down a tunnel.
    /// </summary>
    public const double GradeTolerance = 1.0;

    /// <summary>Stretches of water shorter than this, in metres, are ignored, so a road can meet the bank.</summary>
    private const double Snap = RoadNetwork.SnapDistance;

    /// <summary>The level of a point of road. Off the map there's no terrain, so it counts as on the ground.</summary>
    public static RoadLevel LevelAt(LandGrid land, Point3 point)
    {
        ArgumentNullException.ThrowIfNull(land);

        if (!land.TryGetGroundElevation(point, out double ground))
        {
            return RoadLevel.Ground;
        }

        return point.Z > ground + GradeTolerance ? RoadLevel.Bridge
            : point.Z < ground - GradeTolerance ? RoadLevel.Tunnel
            : RoadLevel.Ground;
    }

    /// <summary>
    /// Splits a road's polyline into stretches at one level each, in order. Where the level changes
    /// part-way along a straight piece, the stretches meet at the point where it changes.
    /// </summary>
    public static IReadOnlyList<RoadStretch> Stretches(LandGrid land, IReadOnlyList<Point3> polyline)
    {
        ArgumentNullException.ThrowIfNull(land);
        ArgumentNullException.ThrowIfNull(polyline);
        if (polyline.Count == 0)
        {
            return [];
        }

        var stretches = new List<RoadStretch>();
        RoadLevel level = LevelAt(land, polyline[0]);
        var points = new List<Point3> { polyline[0] };
        for (int i = 1; i < polyline.Count; i++)
        {
            Point3 a = polyline[i - 1], b = polyline[i];
            double[] samples = Samples(land, a, b);
            for (int k = 1; k < samples.Length; k++)
            {
                double low = samples[k - 1];
                RoadLevel target = LevelAt(land, Lerp(a, b, samples[k]));

                // A few changes can fall between two samples, so keep going until the level matches.
                for (int changes = 0; level != target && changes < 3; changes++)
                {
                    double boundary = Boundary(land, a, b, low, samples[k], level);
                    Point3 where = Lerp(a, b, boundary);
                    points.Add(where);
                    stretches.Add(new RoadStretch(level, points));
                    level = LevelAt(land, where);
                    points = [where];
                    low = boundary;
                }
            }

            points.Add(b);
        }

        stretches.Add(new RoadStretch(level, points));
        return stretches;
    }

    /// <summary>
    /// Finds a place where a road would run at ground level over water, which isn't allowed: water
    /// has to be crossed on a bridge or in a tunnel (decision 0012). A road can touch the water's edge.
    /// </summary>
    /// <returns><see langword="true"/>, and the place, if there is one.</returns>
    public static bool TryFindGroundLevelWater(LandGrid land, IReadOnlyList<Point3> polyline, out Point3 where)
    {
        ArgumentNullException.ThrowIfNull(land);
        ArgumentNullException.ThrowIfNull(polyline);

        for (int i = 1; i < polyline.Count; i++)
        {
            Point3 a = polyline[i - 1], b = polyline[i];
            double planLength = PlanLength(a, b);
            if (planLength < Snap)
            {
                continue;
            }

            double[] samples = Samples(land, a, b);
            double slack = Snap / planLength;
            for (int k = 1; k < samples.Length; k++)
            {
                double t0 = samples[k - 1], t1 = samples[k];
                if (t1 - t0 <= 2 * slack)
                {
                    continue;
                }

                // Each sample interval lies inside one cell, so check it just inside its ends and in the middle.
                foreach (double t in (ReadOnlySpan<double>)[t0 + slack, (t0 + t1) / 2, t1 - slack])
                {
                    Point3 point = Lerp(a, b, t);
                    if (IsInWater(land, point) && LevelAt(land, point) == RoadLevel.Ground)
                    {
                        where = point;
                        return true;
                    }
                }
            }
        }

        where = default;
        return false;
    }

    /// <summary>
    /// Whether a point is in water, rather than on dry land or on the edge between the two: every
    /// cell within half the snap distance of it has to be water.
    /// </summary>
    private static bool IsInWater(LandGrid land, Point3 point)
    {
        const double half = Snap / 2;
        foreach ((double dx, double dy) in (ReadOnlySpan<(double, double)>)[(-half, -half), (half, -half), (-half, half), (half, half)])
        {
            if (!land.TryGetCell(point with { X = point.X + dx, Y = point.Y + dy }, out int x, out int y)
                || land[x, y] != LandType.Water)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Where to look along a straight piece, from 0 to 1: its ends, every place it crosses a cell
    /// edge, and enough points in between that none are more than a quarter of a cell apart.
    /// </summary>
    private static double[] Samples(LandGrid land, Point3 a, Point3 b)
    {
        double planLength = PlanLength(a, b);
        if (planLength < 1e-9)
        {
            return [0, 1];
        }

        var samples = new List<double> { 0, 1 };
        int steps = (int)Math.Ceiling(planLength / (land.CellSize / 4));
        for (int k = 1; k < steps; k++)
        {
            samples.Add((double)k / steps);
        }

        AddGridCrossings(samples, a.X, b.X, land.CellSize);
        AddGridCrossings(samples, a.Y, b.Y, land.CellSize);
        samples.Sort();
        return [.. samples];
    }

    private static void AddGridCrossings(List<double> samples, double from, double to, double cellSize)
    {
        if (from == to)
        {
            return;
        }

        double low = Math.Min(from, to), high = Math.Max(from, to);
        for (double line = Math.Ceiling(low / cellSize) * cellSize; line < high; line += cellSize)
        {
            if (line > low)
            {
                samples.Add((line - from) / (to - from));
            }
        }
    }

    /// <summary>Narrows down where the level stops being <paramref name="level"/> between two places on a piece.</summary>
    private static double Boundary(LandGrid land, Point3 a, Point3 b, double low, double high, RoadLevel level)
    {
        for (int iteration = 0; iteration < 30; iteration++)
        {
            double middle = (low + high) / 2;
            if (LevelAt(land, Lerp(a, b, middle)) == level)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return high;
    }

    private static double PlanLength(Point3 a, Point3 b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static Point3 Lerp(Point3 a, Point3 b, double t) =>
        new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t), a.Z + ((b.Z - a.Z) * t));
}
