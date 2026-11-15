using Simulacity.Core;
using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;

namespace Simulacity.Tests;

/// <summary>Bridges, tunnels and water (decision 0012), on a 10 × 4 map of 10 m cells.</summary>
public class RoadProfileTests
{
    private readonly World _world = new(new LandGrid(10, 4, 10));

    private NodeId Node(double x, double y, double z = 0) => _world.Roads.AddNode(new Point3(x, y, z)).Id;

    private void DigRiver() => _world.SetLand(4, 0, 5, 3, LandType.Water); // 40 m to 60 m from the west.

    [Theory]
    [InlineData(0, RoadLevel.Ground)]
    [InlineData(1, RoadLevel.Ground)]
    [InlineData(-1, RoadLevel.Ground)]
    [InlineData(1.5, RoadLevel.Bridge)]
    [InlineData(-1.5, RoadLevel.Tunnel)]
    public void The_level_comes_from_the_height_above_the_terrain(double z, RoadLevel expected)
    {
        Assert.Equal(expected, RoadProfile.LevelAt(_world.Land, new Point3(15, 15, z)));
    }

    [Fact]
    public void Levels_follow_the_terrain_not_the_datum()
    {
        var hills = new LandGrid(2, 2, 10, elevation: 20);

        Assert.Equal(RoadLevel.Tunnel, RoadProfile.LevelAt(hills, new Point3(5, 5, 5)));
        Assert.Equal(RoadLevel.Ground, RoadProfile.LevelAt(hills, new Point3(5, 5, 20)));
    }

    [Fact]
    public void A_road_on_the_ground_is_one_stretch()
    {
        IReadOnlyList<RoadStretch> stretches = RoadProfile.Stretches(_world.Land, [new Point3(0, 5, 0), new Point3(100, 5, 0)]);

        RoadStretch stretch = Assert.Single(stretches);
        Assert.Equal(RoadLevel.Ground, stretch.Level);
        Assert.Equal([new Point3(0, 5, 0), new Point3(100, 5, 0)], stretch.Points);
    }

    [Fact]
    public void A_ramp_becomes_a_bridge_where_it_passes_the_tolerance()
    {
        Point3[] road = [new Point3(0, 5, 0), new Point3(40, 5, 8), new Point3(60, 5, 8), new Point3(100, 5, 0)];

        IReadOnlyList<RoadStretch> stretches = RoadProfile.Stretches(_world.Land, road);

        Assert.Equal([RoadLevel.Ground, RoadLevel.Bridge, RoadLevel.Ground], stretches.Select(stretch => stretch.Level));
        Assert.Equal(5, stretches[0].Points[^1].X, precision: 6); // 1 m up a ramp that climbs 8 m in 40 m.
        Assert.Equal(stretches[0].Points[^1], stretches[1].Points[0]);
        Assert.Equal(new Point3(40, 5, 8), stretches[1].Points[1]);
        Assert.Equal(95, stretches[2].Points[0].X, precision: 6);
        Assert.Equal(new Point3(100, 5, 0), stretches[2].Points[^1]);
    }

    [Fact]
    public void A_road_cannot_cross_water_at_ground_level()
    {
        DigRiver();
        NodeId west = Node(10, 15), east = Node(90, 15);

        ArgumentException error = Assert.Throws<ArgumentException>(() => _world.AddRoad(west, east));

        Assert.Contains("water at ground level", error.Message);
        Assert.Empty(_world.Roads.Segments);
    }

    [Fact]
    public void A_road_high_enough_over_water_is_a_bridge()
    {
        DigRiver();
        NodeId west = Node(10, 15), east = Node(90, 15);
        var deck = new PolylineShape([new Point3(35, 15, 6), new Point3(65, 15, 6)]);

        RoadSegment bridge = Assert.Single(_world.AddRoad(west, east, deck).Segments);

        Assert.Equal(
            [RoadLevel.Ground, RoadLevel.Bridge, RoadLevel.Ground],
            _world.StretchesOf(bridge.Id).Select(stretch => stretch.Level));
    }

    [Fact]
    public void A_road_deep_enough_under_water_is_a_tunnel()
    {
        DigRiver();
        NodeId west = Node(10, 15), east = Node(90, 15);
        var tube = new PolylineShape([new Point3(35, 15, -6), new Point3(65, 15, -6)]);

        RoadSegment tunnel = Assert.Single(_world.AddRoad(west, east, tube).Segments);

        Assert.Equal(
            [RoadLevel.Ground, RoadLevel.Tunnel, RoadLevel.Ground],
            _world.StretchesOf(tunnel.Id).Select(stretch => stretch.Level));
    }

    [Fact]
    public void A_bridge_that_comes_down_too_early_is_refused()
    {
        DigRiver();
        NodeId west = Node(10, 15), east = Node(90, 15);
        var sagging = new PolylineShape([new Point3(35, 15, 6), new Point3(55, 15, 6), new Point3(58, 15, 0.5)]);

        Assert.Throws<ArgumentException>(() => _world.AddRoad(west, east, sagging));
    }

    [Fact]
    public void A_road_can_run_along_the_bank_and_end_at_the_waters_edge()
    {
        DigRiver();

        _world.AddRoad(Node(40, 0), Node(40, 40)); // Along the river's west edge.
        _world.AddRoad(Node(10, 25), Node(40, 25)); // Up to the edge, ending on the first road.

        Assert.Equal(3, _world.Roads.Segments.Count);
    }

    [Fact]
    public void Water_cannot_be_painted_under_a_road_on_the_ground()
    {
        RoadSegment road = Assert.Single(_world.AddRoad(Node(10, 15), Node(90, 15)).Segments);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(DigRiver);

        Assert.Contains($"Road {road.Id}", error.Message);
        Assert.Equal(LandType.Ground, _world.Land[4, 0]);
        Assert.Equal(LandType.Ground, _world.Land[5, 3]);
    }

    [Fact]
    public void Water_can_be_painted_under_a_bridge()
    {
        _world.AddRoad(Node(10, 15), Node(90, 15), new PolylineShape([new Point3(35, 15, 6), new Point3(65, 15, 6)]));

        DigRiver();

        Assert.Equal(LandType.Water, _world.Land[4, 1]);
    }

    [Fact]
    public void The_riverbed_cannot_be_raised_up_to_a_bridge()
    {
        DigRiver();
        _world.AddRoad(Node(10, 15), Node(90, 15), new PolylineShape([new Point3(35, 15, 6), new Point3(65, 15, 6)]));

        Assert.Throws<InvalidOperationException>(() => _world.SetCornerElevation(5, 1, 11));

        Assert.Equal(0, _world.Land.GetCornerElevation(5, 1));
    }

    [Fact]
    public void Lowering_the_ground_under_a_road_turns_it_into_a_bridge_automatically()
    {
        RoadSegment road = Assert.Single(_world.AddRoad(Node(0, 20), Node(100, 20)).Segments);

        _world.SetCornerElevation(5, 2, -5);

        Assert.Contains(RoadLevel.Bridge, _world.StretchesOf(road.Id).Select(stretch => stretch.Level));
    }
}
