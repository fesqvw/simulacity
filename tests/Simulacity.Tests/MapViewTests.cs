using Simulacity.Contracts;
using Simulacity.Renderer.Console;

namespace Simulacity.Tests;

/// <summary>
/// The test map is 4 × 2 cells of 10 m. With room to spare it's drawn 8 characters across and
/// 2 down: each cell is two characters wide (5 m each) and one row tall (10 m), north at the top.
/// </summary>
public class MapViewTests
{
    private static readonly LandKind[] AllGround = [.. Enumerable.Repeat(LandKind.Ground, 8)];
    private static readonly double[] Flat = new double[15];

    [Fact]
    public void Land_is_drawn_with_north_at_the_top()
    {
        LandKind[] cells = [.. AllGround];
        cells[3] = LandKind.Water; // Cell (3, 0): the south-east corner.

        Assert.Equal(["........", "......~~"], MapView.Draw(Snapshot(cells, Flat), 100, 100));
    }

    [Fact]
    public void A_road_on_the_ground_is_drawn_between_its_nodes()
    {
        SimulationSnapshot snapshot = Snapshot(AllGround, Flat, Road(1, RoadLevelKind.Ground, (0, 5, 0), (40, 5, 0)));

        Assert.Equal(["........", "o######o"], MapView.Draw(snapshot, 100, 100));
    }

    [Theory]
    [InlineData(RoadLevelKind.Bridge, "o======o")]
    [InlineData(RoadLevelKind.Tunnel, "o::::::o")]
    [InlineData(RoadLevelKind.Ground, "o######o")]
    public void Each_stretch_is_drawn_as_its_level(RoadLevelKind level, string expected)
    {
        SimulationSnapshot snapshot = Snapshot(AllGround, Flat, Road(1, level, (0, 15, 0), (40, 15, 0)));

        Assert.Equal([expected, "........"], MapView.Draw(snapshot, 100, 100));
    }

    [Fact]
    public void A_segment_can_change_level_along_its_length()
    {
        Position[] points = [new(0, 15, 0), new(20, 15, 5), new(40, 15, 0)];
        var segment = new RoadSegmentSnapshot(
            1,
            2,
            3,
            points,
            [new(RoadLevelKind.Ground, [points[0], new(12, 15, 2)]),
             new(RoadLevelKind.Bridge, [new(12, 15, 2), points[1], new(28, 15, 2)]),
             new(RoadLevelKind.Ground, [new(28, 15, 2), points[2]])]);
        RoadNodeSnapshot[] nodes = [new(2, points[0]), new(3, points[2])];
        var snapshot = new SimulationSnapshot(0, new LandSnapshot(4, 2, 10, AllGround, Flat), new RoadsSnapshot(nodes, [segment]));

        Assert.Equal("o#====#o", MapView.Draw(snapshot, 100, 100)[0]);
    }

    [Fact]
    public void A_bridge_is_drawn_over_the_road_it_crosses_and_a_tunnel_under_it()
    {
        SimulationSnapshot snapshot = Snapshot(
            AllGround,
            Flat,
            Road(1, RoadLevelKind.Ground, (0, 15, 0), (40, 15, 0)),
            Road(2, RoadLevelKind.Bridge, (12, 30, 8), (12, -10, 8)), // Nodes off the map, so only the roads show.
            Road(3, RoadLevelKind.Tunnel, (27, 30, -8), (27, -10, -8)));

        IReadOnlyList<string> map = MapView.Draw(snapshot, 100, 100);

        Assert.Equal('=', map[0][2]);
        Assert.Equal('#', map[0][5]);
        Assert.Equal(':', map[1][5]);
    }

    [Fact]
    public void A_large_map_is_scaled_down_to_fit()
    {
        var land = new LandSnapshot(300, 100, 10, [.. Enumerable.Repeat(LandKind.Ground, 30_000)], new double[301 * 101]);
        var snapshot = new SimulationSnapshot(0, land, new RoadsSnapshot([], []));

        IReadOnlyList<string> map = MapView.Draw(snapshot, 80, 30);

        Assert.InRange(map.Count, 1, 30);
        Assert.All(map, row => Assert.InRange(row.Length, 1, 80));
        Assert.Equal(map[0].Length, map[^1].Length);
    }

    [Fact]
    public void Roads_and_nodes_off_the_map_are_left_out()
    {
        SimulationSnapshot snapshot = Snapshot(AllGround, Flat, Road(1, RoadLevelKind.Ground, (-50, 5, 0), (-10, 5, 0)));

        Assert.Equal(["........", "........"], MapView.Draw(snapshot, 100, 100));
    }

    private static SimulationSnapshot Snapshot(
        IReadOnlyList<LandKind> cells,
        IReadOnlyList<double> corners,
        params (RoadNodeSnapshot[] Nodes, RoadSegmentSnapshot Segment)[] roads) =>
        new(
            0,
            new LandSnapshot(4, 2, 10, cells, corners),
            new RoadsSnapshot([.. roads.SelectMany(road => road.Nodes)], [.. roads.Select(road => road.Segment)]));

    /// <summary>A road at one level through the given points, with a node at each end.</summary>
    private static (RoadNodeSnapshot[] Nodes, RoadSegmentSnapshot Segment) Road(
        int id, RoadLevelKind level, params (double X, double Y, double Z)[] points)
    {
        Position[] positions = [.. points.Select(point => new Position(point.X, point.Y, point.Z))];
        RoadNodeSnapshot[] nodes = [new(id * 2, positions[0]), new((id * 2) + 1, positions[^1])];
        return (nodes, new RoadSegmentSnapshot(id, nodes[0].Id, nodes[1].Id, positions, [new(level, positions)]));
    }
}
