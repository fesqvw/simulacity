using Simulacity.Core.Geometry;

namespace Simulacity.Core.Roads;

/// <summary>
/// The road graph: nodes (intersections and dead ends) joined by segments (decision 0007).
/// </summary>
public sealed class RoadNetwork
{
    private readonly Dictionary<NodeId, RoadNode> _nodes = [];
    private readonly Dictionary<SegmentId, RoadSegment> _segments = [];
    private readonly Dictionary<NodeId, List<SegmentId>> _segmentsAtNode = [];
    private int _nextNodeId = 1;
    private int _nextSegmentId = 1;

    /// <summary>All nodes in the graph.</summary>
    public IReadOnlyCollection<RoadNode> Nodes => _nodes.Values;

    /// <summary>All segments in the graph.</summary>
    public IReadOnlyCollection<RoadSegment> Segments => _segments.Values;

    /// <summary>Adds a node at a world position.</summary>
    public RoadNode AddNode(Point2 position)
    {
        if (!position.IsFinite)
        {
            throw new ArgumentException("The position must have finite coordinates.", nameof(position));
        }

        var node = new RoadNode(new NodeId(_nextNodeId++), position);
        _nodes.Add(node.Id, node);
        _segmentsAtNode.Add(node.Id, []);
        return node;
    }

    /// <summary>Adds a segment between two existing, different nodes.</summary>
    /// <param name="start">The node the segment starts at.</param>
    /// <param name="end">The node the segment ends at.</param>
    /// <param name="shape">The segment's shape. A straight line if omitted.</param>
    public RoadSegment AddSegment(NodeId start, NodeId end, SegmentShape? shape = null)
    {
        RequireNode(start, nameof(start));
        RequireNode(end, nameof(end));
        if (start == end)
        {
            throw new ArgumentException($"A segment must join two different nodes, but both ends are {start}.", nameof(end));
        }

        var segment = new RoadSegment(new SegmentId(_nextSegmentId++), start, end, shape ?? PolylineShape.Straight);
        _segments.Add(segment.Id, segment);
        _segmentsAtNode[start].Add(segment.Id);
        _segmentsAtNode[end].Add(segment.Id);
        return segment;
    }

    /// <summary>Removes a segment. Its nodes stay in the graph.</summary>
    /// <returns><see langword="true"/> if the segment existed.</returns>
    public bool RemoveSegment(SegmentId id)
    {
        if (!_segments.Remove(id, out RoadSegment? segment))
        {
            return false;
        }

        _segmentsAtNode[segment.Start].Remove(id);
        _segmentsAtNode[segment.End].Remove(id);
        return true;
    }

    /// <summary>The node with the given ID.</summary>
    /// <exception cref="KeyNotFoundException">There is no such node.</exception>
    public RoadNode GetNode(NodeId id) =>
        _nodes.TryGetValue(id, out RoadNode? node) ? node : throw new KeyNotFoundException($"There is no node {id}.");

    /// <summary>The segment with the given ID.</summary>
    /// <exception cref="KeyNotFoundException">There is no such segment.</exception>
    public RoadSegment GetSegment(SegmentId id) =>
        _segments.TryGetValue(id, out RoadSegment? segment) ? segment : throw new KeyNotFoundException($"There is no segment {id}.");

    /// <summary>The segments that start or end at a node.</summary>
    /// <exception cref="KeyNotFoundException">There is no such node.</exception>
    public IReadOnlyList<SegmentId> SegmentsAt(NodeId id) =>
        _segmentsAtNode.TryGetValue(id, out List<SegmentId>? segments) ? segments : throw new KeyNotFoundException($"There is no node {id}.");

    /// <summary>A segment's shape as a polyline from its start node to its end node.</summary>
    /// <exception cref="KeyNotFoundException">There is no such segment.</exception>
    public IReadOnlyList<Point2> PolylineOf(SegmentId id)
    {
        RoadSegment segment = GetSegment(id);
        return segment.Shape.ToPolyline(_nodes[segment.Start].Position, _nodes[segment.End].Position);
    }

    /// <summary>A segment's length along its shape, in metres.</summary>
    /// <exception cref="KeyNotFoundException">There is no such segment.</exception>
    public double LengthOf(SegmentId id) => Polyline.Length(PolylineOf(id));

    private void RequireNode(NodeId id, string parameterName)
    {
        if (!_nodes.ContainsKey(id))
        {
            throw new ArgumentException($"There is no node {id}.", parameterName);
        }
    }
}
