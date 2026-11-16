using Simulacity.Core.Geometry;
using Simulacity.Core.Roads;

namespace Simulacity.Tests;

public class RoadCrossingTests
{
    private readonly RoadNetwork _roads = new();

    private NodeId Node(double x, double y, double z = 0) => _roads.AddNode(new Point3(x, y, z)).Id;

    [Fact]
    public void A_road_that_crosses_nothing_is_one_segment()
    {
        NodeId a = Node(0, 0), b = Node(100, 0);

        RoadAddition addition = _roads.AddRoad(a, b);

        RoadSegment segment = Assert.Single(addition.Segments);
        Assert.Equal((a, b), (segment.Start, segment.End));
        Assert.Empty(addition.Splits);
        Assert.Empty(addition.NewNodes);
    }

    [Fact]
    public void Roads_that_cross_at_the_same_height_meet_at_a_new_node()
    {
        NodeId west = Node(0, 50), east = Node(100, 50);
        SegmentId first = Assert.Single(_roads.AddRoad(west, east).Segments).Id;
        NodeId south = Node(40, 0), north = Node(40, 100);

        RoadAddition addition = _roads.AddRoad(south, north);

        RoadNode junction = Assert.Single(addition.NewNodes);
        Assert.Equal(new Point3(40, 50, 0), junction.Position);

        Assert.Equal(2, addition.Segments.Count);
        Assert.Equal((south, junction.Id), (addition.Segments[0].Start, addition.Segments[0].End));
        Assert.Equal((junction.Id, north), (addition.Segments[1].Start, addition.Segments[1].End));

        RoadSplit split = Assert.Single(addition.Splits);
        Assert.Equal(first, split.Removed);
        Assert.False(_roads.ContainsSegment(first));
        Assert.Equal((west, junction.Id), (split.Pieces[0].Start, split.Pieces[0].End));
        Assert.Equal((junction.Id, east), (split.Pieces[1].Start, split.Pieces[1].End));

        Assert.Equal(4, _roads.Segments.Count);
        Assert.Equal(4, _roads.SegmentsAt(junction.Id).Count);
        Assert.Equal(40, _roads.LengthOf(split.Pieces[0].Id), precision: 9);
        Assert.Equal(60, _roads.LengthOf(split.Pieces[1].Id), precision: 9);
    }

    [Fact]
    public void A_road_passes_over_another_on_a_bridge_without_meeting_it()
    {
        NodeId west = Node(0, 50), east = Node(100, 50);
        SegmentId below = Assert.Single(_roads.AddRoad(west, east).Segments).Id;
        NodeId south = Node(40, 0), north = Node(40, 100);
        var bridge = new PolylineShape([new Point3(40, 30, 6), new Point3(40, 70, 6)]);

        RoadAddition addition = _roads.AddRoad(south, north, bridge);

        Assert.Single(addition.Segments);
        Assert.Empty(addition.Splits);
        Assert.Empty(addition.NewNodes);
        Assert.True(_roads.ContainsSegment(below));
    }

    [Fact]
    public void Heights_within_the_tolerance_still_meet_halfway()
    {
        NodeId west = Node(0, 50), east = Node(100, 50);
        _roads.AddRoad(west, east);
        NodeId south = Node(40, 0, 0.5), north = Node(40, 100, 0.5);

        RoadAddition addition = _roads.AddRoad(south, north);

        Assert.Equal(0.25, Assert.Single(addition.NewNodes).Position.Z, precision: 9);
    }

    [Fact]
    public void A_road_that_ends_on_another_makes_a_t_junction_at_its_own_end_node()
    {
        NodeId west = Node(0, 50), east = Node(100, 50);
        _roads.AddRoad(west, east);
        NodeId south = Node(30, 0), onRoad = Node(30, 50);

        RoadAddition addition = _roads.AddRoad(south, onRoad);

        Assert.Empty(addition.NewNodes);
        Assert.Single(addition.Segments);
        RoadSplit split = Assert.Single(addition.Splits);
        Assert.Equal(onRoad, split.Pieces[0].End);
        Assert.Equal(onRoad, split.Pieces[1].Start);
        Assert.Equal(3, _roads.SegmentsAt(onRoad).Count);
    }

    [Fact]
    public void A_road_that_passes_through_another_roads_end_node_is_split_there()
    {
        NodeId south = Node(30, 0), deadEnd = Node(30, 50);
        SegmentId spur = Assert.Single(_roads.AddRoad(south, deadEnd).Segments).Id;
        NodeId west = Node(0, 50), east = Node(100, 50);

        RoadAddition addition = _roads.AddRoad(west, east);

        Assert.Empty(addition.NewNodes);
        Assert.Empty(addition.Splits);
        Assert.True(_roads.ContainsSegment(spur));
        Assert.Equal(2, addition.Segments.Count);
        Assert.Equal(deadEnd, addition.Segments[0].End);
        Assert.Equal(3, _roads.SegmentsAt(deadEnd).Count);
    }

    [Fact]
    public void Roads_that_share_an_end_node_are_not_split()
    {
        NodeId centre = Node(50, 50), east = Node(100, 50), north = Node(50, 100);
        _roads.AddRoad(centre, east);

        RoadAddition addition = _roads.AddRoad(centre, north);

        Assert.Single(addition.Segments);
        Assert.Empty(addition.Splits);
        Assert.Empty(addition.NewNodes);
    }

    [Fact]
    public void A_road_through_an_existing_junction_joins_it_instead_of_adding_a_node()
    {
        NodeId west = Node(0, 50), east = Node(100, 50);
        _roads.AddRoad(west, east);
        RoadNode junction = Assert.Single(_roads.AddRoad(Node(50, 0), Node(50, 100)).NewNodes);
        NodeId southWest = Node(0, 0), northEast = Node(100, 100);

        RoadAddition addition = _roads.AddRoad(southWest, northEast);

        Assert.Empty(addition.NewNodes);
        Assert.Empty(addition.Splits);
        Assert.Equal(2, addition.Segments.Count);
        Assert.Equal(junction.Id, addition.Segments[0].End);
        Assert.Equal(6, _roads.SegmentsAt(junction.Id).Count);
    }

    [Fact]
    public void A_road_that_crosses_several_roads_is_split_at_each_in_order()
    {
        SegmentId first = Assert.Single(_roads.AddRoad(Node(20, 0), Node(20, 100)).Segments).Id;
        SegmentId second = Assert.Single(_roads.AddRoad(Node(60, 0), Node(60, 100)).Segments).Id;
        NodeId west = Node(0, 50), east = Node(100, 50);

        RoadAddition addition = _roads.AddRoad(west, east);

        Assert.Equal(2, addition.NewNodes.Count);
        Assert.Equal([first, second], addition.Splits.Select(split => split.Removed));
        Assert.Equal([20, 40, 40], addition.Segments.Select(segment => Math.Round(_roads.LengthOf(segment.Id), 9)));
        Assert.Equal(west, addition.Segments[0].Start);
        Assert.Equal(east, addition.Segments[^1].End);
    }

    [Fact]
    public void Polyline_points_go_to_the_piece_they_belong_to()
    {
        NodeId west = Node(0, 0), east = Node(100, 0);
        var bend = new PolylineShape([new Point3(0, 50, 0), new Point3(100, 50, 0)]);
        RoadSegment loop = Assert.Single(_roads.AddRoad(west, east, bend).Segments);
        NodeId south = Node(30, 25), north = Node(30, 75);

        RoadSplit split = Assert.Single(_roads.AddRoad(south, north).Splits);

        Assert.Equal(loop.Id, split.Removed);
        Assert.Equal(
            [new Point3(0, 0, 0), new Point3(0, 50, 0), new Point3(30, 50, 0)],
            _roads.PolylineOf(split.Pieces[0].Id));
        Assert.Equal(
            [new Point3(30, 50, 0), new Point3(100, 50, 0), new Point3(100, 0, 0)],
            _roads.PolylineOf(split.Pieces[1].Id));
    }

    [Fact]
    public void A_road_that_crosses_another_at_one_of_its_bends_gets_one_node()
    {
        NodeId west = Node(0, 0), east = Node(100, 0);
        _roads.AddRoad(west, east, new PolylineShape([new Point3(50, 50, 0)]));

        RoadAddition addition = _roads.AddRoad(Node(0, 50), Node(100, 50));

        RoadNode junction = Assert.Single(addition.NewNodes);
        Assert.Equal(new Point3(50, 50, 0), junction.Position);
        Assert.Equal(2, Assert.Single(addition.Splits).Pieces.Count);
        Assert.Equal(2, addition.Segments.Count);
    }

    [Fact]
    public void A_road_that_would_run_along_another_is_refused_and_changes_nothing()
    {
        NodeId a = Node(0, 0), b = Node(100, 0);
        _roads.AddRoad(a, b);
        NodeId c = Node(50, 0), d = Node(150, 0);
        int nodes = _roads.Nodes.Count;

        Assert.Throws<ArgumentException>(() => _roads.AddRoad(c, d));

        Assert.Single(_roads.Segments);
        Assert.Equal(nodes, _roads.Nodes.Count);
    }

    [Fact]
    public void A_road_that_continues_another_in_a_straight_line_just_shares_the_node()
    {
        NodeId a = Node(0, 0), b = Node(100, 0), c = Node(200, 0);
        _roads.AddRoad(a, b);

        RoadAddition addition = _roads.AddRoad(b, c);

        Assert.Single(addition.Segments);
        Assert.Empty(addition.Splits);
    }

    [Fact]
    public void Add_segment_does_not_look_for_crossings()
    {
        _roads.AddSegment(Node(0, 50), Node(100, 50));

        _roads.AddSegment(Node(50, 0), Node(50, 100));

        Assert.Equal(2, _roads.Segments.Count);
        Assert.Equal(4, _roads.Nodes.Count);
    }
}
