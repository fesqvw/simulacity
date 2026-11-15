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
        SimulationSnapshot snapshot = Snapshot(AllGround, Flat, Road(1, (0, 5, 0), (40, 5, 0)));

        Assert.Equal(["........", "o######o"], MapView.Draw(snapshot, 100, 100));
    }

    [Theory]
    [InlineData(5, "o======o")]
    [InlineData(-5, "o::::::o")]
    [InlineData(0.5, "o######o")]
    public void A_road_above_or_below_the_ground_is_a_bridge_or_a_tunnel(double deck, string expected)
    {
        SimulationSnapshot snapshot = Snapshot(AllGround, Flat, Road(1, (0, 15, 0), (10, 15, deck), (30, 15, deck), (40, 15, 0)));

        Assert.Equal([expected, "........"], MapView.Draw(snapshot, 100, 100));
    }

    [Fact]
    public void Bridges_and_tunnels_follow_the_terrain_not_the_datum()
    {
        double[] raised = [.. Enumerable.Repeat(20.0, 15)];
        SimulationSnapshot snapshot = Snapshot(AllGround, raised, Road(1, (0, 15, 20), (10, 15, 5), (30, 15, 5), (40, 15, 20)));

        Assert.Equal("o::::::o", MapView.Draw(snapshot, 100, 100)[0]);
    }

    [Fact]
    public void A_bridge_is_drawn_over_the_road_it_crosses_and_a_tunnel_under_it()
    {
        SimulationSnapshot snapshot = Snapshot(
            AllGround,
            Flat,
            Road(1, (0, 15, 0), (40, 15, 0)),
            Road(2, (12, 30, 8), (12, -10, 8)), // Nodes off the map, so only the roads show.
            Road(3, (27, 30, -8), (27, -10, -8)));

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
        SimulationSnapshot snapshot = Snapshot(AllGround, Flat, Road(1, (-50, 5, 0), (-10, 5, 0)));

        Assert.Equal(["........", "........"], MapView.Draw(snapshot, 100, 100));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10, 0, 4)]
    [InlineData(5, 5, 3)]
    [InlineData(40, 20, 0)]
    public void Ground_elevation_is_interpolated_between_corners(double x, double y, double expected)
    {
        double[] corners = [.. Flat];
        corners[1] = 4;     // Corner (1, 0).
        corners[5 + 1] = 8; // Corner (1, 1).
        var land = new LandSnapshot(4, 2, 10, AllGround, corners);

        Assert.Equal(expected, MapView.GroundElevation(land, x, y)!.Value, precision: 9);
    }

    [Fact]
    public void There_is_no_ground_elevation_off_the_map()
    {
        Assert.Null(MapView.GroundElevation(new LandSnapshot(4, 2, 10, AllGround, Flat), 41, 5));
    }

    private static SimulationSnapshot Snapshot(
        IReadOnlyList<LandKind> cells,
        IReadOnlyList<double> corners,
        params (RoadNodeSnapshot[] Nodes, RoadSegmentSnapshot Segment)[] roads) =>
        new(
            0,
            new LandSnapshot(4, 2, 10, cells, corners),
            new RoadsSnapshot([.. roads.SelectMany(road => road.Nodes)], [.. roads.Select(road => road.Segment)]));

    /// <summary>A road through the given points, with a node at each end.</summary>
    private static (RoadNodeSnapshot[] Nodes, RoadSegmentSnapshot Segment) Road(int id, params (double X, double Y, double Z)[] points)
    {
        Position[] positions = [.. points.Select(point => new Position(point.X, point.Y, point.Z))];
        RoadNodeSnapshot[] nodes = [new(id * 2, positions[0]), new((id * 2) + 1, positions[^1])];
        return (nodes, new RoadSegmentSnapshot(id, nodes[0].Id, nodes[1].Id, positions));
    }
}
