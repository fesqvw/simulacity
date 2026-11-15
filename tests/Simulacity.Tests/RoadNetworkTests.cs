using Simulacity.Core.Geometry;
using Simulacity.Core.Roads;

namespace Simulacity.Tests;

public class RoadNetworkTests
{
    private readonly RoadNetwork _roads = new();

    [Fact]
    public void Nodes_get_distinct_ids_and_keep_their_position()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));
        RoadNode b = _roads.AddNode(new Point2(100, 50));

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(new Point2(100, 50), _roads.GetNode(b.Id).Position);
        Assert.Equal(2, _roads.Nodes.Count);
    }

    [Fact]
    public void A_segment_joins_two_nodes_and_is_listed_at_both()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));
        RoadNode b = _roads.AddNode(new Point2(100, 0));

        RoadSegment segment = _roads.AddSegment(a.Id, b.Id);

        Assert.Equal(segment, _roads.GetSegment(segment.Id));
        Assert.Equal([segment.Id], _roads.SegmentsAt(a.Id));
        Assert.Equal([segment.Id], _roads.SegmentsAt(b.Id));
    }

    [Fact]
    public void A_segment_without_a_shape_is_a_straight_line()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));
        RoadNode b = _roads.AddNode(new Point2(30, 40));

        RoadSegment segment = _roads.AddSegment(a.Id, b.Id);

        Assert.Equal([new Point2(0, 0), new Point2(30, 40)], _roads.PolylineOf(segment.Id));
        Assert.Equal(50, _roads.LengthOf(segment.Id), precision: 9);
    }

    [Fact]
    public void A_polyline_segment_runs_from_its_start_node_through_its_points_to_its_end_node()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));
        RoadNode b = _roads.AddNode(new Point2(100, 100));

        RoadSegment segment = _roads.AddSegment(a.Id, b.Id, new PolylineShape([new Point2(100, 0)]));

        Assert.Equal([new Point2(0, 0), new Point2(100, 0), new Point2(100, 100)], _roads.PolylineOf(segment.Id));
        Assert.Equal(200, _roads.LengthOf(segment.Id), precision: 9);
    }

    [Fact]
    public void A_segment_needs_two_existing_different_nodes()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));

        Assert.Throws<ArgumentException>(() => _roads.AddSegment(a.Id, a.Id));
        Assert.Throws<ArgumentException>(() => _roads.AddSegment(a.Id, new NodeId(999)));
        Assert.Throws<ArgumentException>(() => _roads.AddSegment(new NodeId(999), a.Id));
        Assert.Empty(_roads.Segments);
    }

    [Fact]
    public void Positions_and_points_must_be_finite()
    {
        Assert.Throws<ArgumentException>(() => _roads.AddNode(new Point2(double.NaN, 0)));
        Assert.Throws<ArgumentException>(() => new PolylineShape([new Point2(0, double.PositiveInfinity)]));
    }

    [Fact]
    public void Removing_a_segment_keeps_its_nodes()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));
        RoadNode b = _roads.AddNode(new Point2(100, 0));
        RoadSegment segment = _roads.AddSegment(a.Id, b.Id);

        Assert.True(_roads.RemoveSegment(segment.Id));

        Assert.False(_roads.RemoveSegment(segment.Id));
        Assert.Empty(_roads.Segments);
        Assert.Empty(_roads.SegmentsAt(a.Id));
        Assert.Equal(2, _roads.Nodes.Count);
        Assert.Throws<KeyNotFoundException>(() => _roads.GetSegment(segment.Id));
    }

    [Fact]
    public void Ids_are_not_reused_after_a_removal()
    {
        RoadNode a = _roads.AddNode(new Point2(0, 0));
        RoadNode b = _roads.AddNode(new Point2(100, 0));
        RoadSegment first = _roads.AddSegment(a.Id, b.Id);
        _roads.RemoveSegment(first.Id);

        RoadSegment second = _roads.AddSegment(a.Id, b.Id);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Unknown_ids_are_reported()
    {
        Assert.Throws<KeyNotFoundException>(() => _roads.GetNode(new NodeId(1)));
        Assert.Throws<KeyNotFoundException>(() => _roads.SegmentsAt(new NodeId(1)));
        Assert.Throws<KeyNotFoundException>(() => _roads.PolylineOf(new SegmentId(1)));
    }
}
