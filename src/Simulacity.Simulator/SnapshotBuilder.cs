using Simulacity.Contracts;
using Simulacity.Core;
using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;

namespace Simulacity.Simulator;

/// <summary>
/// Copies the simulation's state into a <see cref="SimulationSnapshot"/> for renderers (decision 0010).
/// </summary>
public static class SnapshotBuilder
{
    /// <summary>Builds a snapshot of the whole world as it is now.</summary>
    public static SimulationSnapshot From(Simulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        return new SimulationSnapshot(simulation.Tick, From(simulation.World.Land), From(simulation.World));
    }

    private static LandSnapshot From(LandGrid land)
    {
        var cells = new LandKind[land.Width * land.Height];
        for (int y = 0; y < land.Height; y++)
        {
            for (int x = 0; x < land.Width; x++)
            {
                cells[(y * land.Width) + x] = land[x, y] switch
                {
                    LandType.Ground => LandKind.Ground,
                    LandType.Water => LandKind.Water,
                    LandType other => throw new InvalidOperationException($"Land type {other} has no snapshot equivalent."),
                };
            }
        }

        int cornersAcross = land.Width + 1;
        var elevations = new double[cornersAcross * (land.Height + 1)];
        for (int j = 0; j <= land.Height; j++)
        {
            for (int i = 0; i <= land.Width; i++)
            {
                elevations[(j * cornersAcross) + i] = land.GetCornerElevation(i, j);
            }
        }

        return new LandSnapshot(land.Width, land.Height, land.CellSize, cells, elevations);
    }

    private static RoadsSnapshot From(World world)
    {
        RoadNetwork roads = world.Roads;
        RoadNodeSnapshot[] nodes =
        [
            .. roads.Nodes
                .OrderBy(node => node.Id.Value)
                .Select(node => new RoadNodeSnapshot(node.Id.Value, ToPosition(node.Position))),
        ];

        RoadSegmentSnapshot[] segments =
        [
            .. roads.Segments
                .OrderBy(segment => segment.Id.Value)
                .Select(segment => new RoadSegmentSnapshot(
                    segment.Id.Value,
                    segment.Start.Value,
                    segment.End.Value,
                    [.. roads.PolylineOf(segment.Id).Select(ToPosition)],
                    [.. world.StretchesOf(segment.Id).Select(ToStretch)])),
        ];

        return new RoadsSnapshot(nodes, segments);
    }

    private static RoadStretchSnapshot ToStretch(RoadStretch stretch) =>
        new(
            stretch.Level switch
            {
                RoadLevel.Ground => RoadLevelKind.Ground,
                RoadLevel.Bridge => RoadLevelKind.Bridge,
                RoadLevel.Tunnel => RoadLevelKind.Tunnel,
                RoadLevel other => throw new InvalidOperationException($"Road level {other} has no snapshot equivalent."),
            },
            [.. stretch.Points.Select(ToPosition)]);

    private static Position ToPosition(Point3 point) => new(point.X, point.Y, point.Z);
}
