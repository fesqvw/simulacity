using Simulacity.Core.Geometry;

namespace Simulacity.Core.Roads;

/// <summary>
/// The road graph: nodes (intersections and dead ends) joined by segments (decision 0009).
/// </summary>
/// <remarks>
/// <see cref="AddRoad"/> keeps the graph connected where roads cross at the same height
/// (decision 0011). <see cref="AddSegment"/> adds a segment exactly as given and doesn't look for
/// crossings.
/// </remarks>
public sealed class RoadNetwork
{
    /// <summary>
    /// How far apart two roads' heights can be where they cross in plan and still meet, in metres.
    /// Further apart than this, one passes over the other (decision 0011).
    /// </summary>
    public const double MeetingHeightTolerance = 1.0;

    /// <summary>
    /// How close two points must be in plan to count as the same place, in metres. A road that ends
    /// this close to another road meets it (decision 0011).
    /// </summary>
    public const double SnapDistance = 0.01;

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
    public RoadNode AddNode(Point3 position)
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
        RequireEnds(start, end);
        return AddSegmentUnchecked(start, end, shape ?? PolylineShape.Straight);
    }

    /// <summary>
    /// Adds a road between two existing, different nodes, joining it to every road it crosses or
    /// touches at the same height (decision 0011). Where it meets a road away from that road's
    /// nodes, a node is added there, and both roads are split at it. Split segments are replaced by
    /// new ones with new IDs.
    /// </summary>
    /// <param name="start">The node the road starts at.</param>
    /// <param name="end">The node the road ends at.</param>
    /// <param name="shape">The road's shape. A straight line if omitted.</param>
    /// <exception cref="ArgumentException">
    /// A node doesn't exist, both ends are the same node, or the road would run along an existing
    /// road at the same height. Nothing is changed.
    /// </exception>
    public RoadAddition AddRoad(NodeId start, NodeId end, SegmentShape? shape = null)
    {
        RequireEnds(start, end);
        shape ??= PolylineShape.Straight;
        IReadOnlyList<Point3> road = shape.ToPolyline(_nodes[start].Position, _nodes[end].Position);
        double[] roadDistances = PlanDistances(road);

        // First find every meeting, so that an overlap is reported before anything changes.
        List<Meeting> meetings = FindMeetings(road, roadDistances);

        var newNodes = new List<RoadNode>();
        var roadCuts = new List<Cut>();
        var cutsBySegment = new Dictionary<SegmentId, List<Cut>>();
        foreach (Meeting meeting in meetings)
        {
            RoadSegment other = _segments[meeting.Other];
            NodeId? roadEnd = EndNear(meeting.RoadDistance, roadDistances[^1], start, end);
            NodeId? otherEnd = EndNear(meeting.OtherDistance, meeting.OtherLength, other.Start, other.End);
            if (roadEnd is { } a && otherEnd is { } b && a != b)
            {
                // Two different nodes in the same place. Joining them would mean merging nodes.
                continue;
            }

            NodeId node = roadEnd ?? otherEnd ?? NodeAt(meeting, newNodes);
            roadCuts.Add(new Cut(meeting.RoadDistance, node));
            if (!cutsBySegment.TryGetValue(meeting.Other, out List<Cut>? cuts))
            {
                cutsBySegment.Add(meeting.Other, cuts = []);
            }

            cuts.Add(new Cut(meeting.OtherDistance, node));
        }

        var splits = new List<RoadSplit>();
        foreach ((SegmentId id, List<Cut> cuts) in cutsBySegment.OrderBy(pair => pair.Key.Value))
        {
            RoadSegment other = _segments[id];
            IReadOnlyList<Point3> polyline = PolylineOf(id);
            double[] distances = PlanDistances(polyline);
            List<Cut> inner = InnerCuts(cuts, distances[^1]);
            if (inner.Count == 0)
            {
                continue;
            }

            RemoveSegment(id);
            splits.Add(new RoadSplit(id, AddPieces(other.Start, other.End, polyline, distances, inner)));
        }

        List<RoadSegment> segments = AddPieces(start, end, road, roadDistances, InnerCuts(roadCuts, roadDistances[^1]));
        return new RoadAddition(segments, splits, newNodes);
    }

    private RoadSegment AddSegmentUnchecked(NodeId start, NodeId end, SegmentShape shape)
    {
        var segment = new RoadSegment(new SegmentId(_nextSegmentId++), start, end, shape);
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

    /// <summary>Whether the graph has a node with the given ID.</summary>
    public bool ContainsNode(NodeId id) => _nodes.ContainsKey(id);

    /// <summary>Whether the graph has a segment with the given ID.</summary>
    public bool ContainsSegment(SegmentId id) => _segments.ContainsKey(id);

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
    public IReadOnlyList<Point3> PolylineOf(SegmentId id)
    {
        RoadSegment segment = GetSegment(id);
        return segment.Shape.ToPolyline(_nodes[segment.Start].Position, _nodes[segment.End].Position);
    }

    /// <summary>A segment's length along its shape, in metres.</summary>
    /// <exception cref="KeyNotFoundException">There is no such segment.</exception>
    public double LengthOf(SegmentId id) => Polyline.Length(PolylineOf(id));

    /// <summary>Every place where existing segments meet the new road at the same height.</summary>
    private List<Meeting> FindMeetings(IReadOnlyList<Point3> road, double[] roadDistances)
    {
        var meetings = new List<Meeting>();
        foreach (RoadSegment segment in _segments.Values.OrderBy(segment => segment.Id.Value))
        {
            IReadOnlyList<Point3> other = PolylineOf(segment.Id);
            double[] otherDistances = PlanDistances(other);
            for (int i = 1; i < road.Count; i++)
            {
                for (int j = 1; j < other.Count; j++)
                {
                    PlanContact contact = PlanContact.Between(road[i - 1], road[i], other[j - 1], other[j], SnapDistance);
                    if (contact.Kind == PlanContactKind.None)
                    {
                        continue;
                    }

                    Point3 onRoad = Lerp(road[i - 1], road[i], contact.T);
                    Point3 onOther = Lerp(other[j - 1], other[j], contact.U);
                    if (Math.Abs(onRoad.Z - onOther.Z) > MeetingHeightTolerance)
                    {
                        continue;
                    }

                    if (contact.Kind == PlanContactKind.Overlap)
                    {
                        throw new ArgumentException(
                            $"The road would run along {segment.Id} at the same height near {onRoad}. Roads can cross or meet, but not overlap.");
                    }

                    meetings.Add(new Meeting(
                        segment.Id,
                        Lerp(roadDistances[i - 1], roadDistances[i], contact.T),
                        Lerp(otherDistances[j - 1], otherDistances[j], contact.U),
                        otherDistances[^1],
                        onRoad,
                        onOther));
                }
            }
        }

        return meetings;
    }

    /// <summary>A node added by this road near the meeting, or else a new one between the two roads' heights.</summary>
    private NodeId NodeAt(Meeting meeting, List<RoadNode> newNodes)
    {
        foreach (RoadNode node in newNodes)
        {
            double dx = node.Position.X - meeting.OnRoad.X;
            double dy = node.Position.Y - meeting.OnRoad.Y;
            if ((dx * dx) + (dy * dy) <= SnapDistance * SnapDistance
                && Math.Abs(node.Position.Z - meeting.OnRoad.Z) <= MeetingHeightTolerance)
            {
                return node.Id;
            }
        }

        RoadNode added = AddNode(meeting.OnRoad with { Z = (meeting.OnRoad.Z + meeting.OnOther.Z) / 2 });
        newNodes.Add(added);
        return added.Id;
    }

    /// <summary>Adds the pieces of a polyline between its end nodes and the cuts, in order.</summary>
    private List<RoadSegment> AddPieces(NodeId start, NodeId end, IReadOnlyList<Point3> polyline, double[] distances, List<Cut> cuts)
    {
        List<Cut> stops = [new Cut(0, start), .. cuts, new Cut(distances[^1], end)];
        var pieces = new List<RoadSegment>();
        for (int k = 1; k < stops.Count; k++)
        {
            Cut from = stops[k - 1], to = stops[k];
            if (from.Node == to.Node)
            {
                continue;
            }

            var points = new List<Point3>();
            for (int v = 1; v < polyline.Count - 1; v++)
            {
                if (distances[v] > from.Distance + SnapDistance && distances[v] < to.Distance - SnapDistance)
                {
                    points.Add(polyline[v]);
                }
            }

            pieces.Add(AddSegmentUnchecked(from.Node, to.Node, points.Count == 0 ? PolylineShape.Straight : new PolylineShape(points)));
        }

        return pieces;
    }

    /// <summary>The cuts away from both ends, in order along the polyline, one per place.</summary>
    private static List<Cut> InnerCuts(List<Cut> cuts, double length)
    {
        var inner = new List<Cut>();
        foreach (Cut cut in cuts.Where(cut => cut.Distance > SnapDistance && cut.Distance < length - SnapDistance).OrderBy(cut => cut.Distance))
        {
            if (inner.Count > 0 && (inner[^1].Node == cut.Node || cut.Distance - inner[^1].Distance <= SnapDistance))
            {
                continue;
            }

            inner.Add(cut);
        }

        return inner;
    }

    private static NodeId? EndNear(double distance, double length, NodeId start, NodeId end) =>
        distance <= SnapDistance ? start : distance >= length - SnapDistance ? end : null;

    /// <summary>The distance in plan from the first point to each point of a polyline.</summary>
    private static double[] PlanDistances(IReadOnlyList<Point3> polyline)
    {
        var distances = new double[polyline.Count];
        for (int i = 1; i < polyline.Count; i++)
        {
            double dx = polyline[i].X - polyline[i - 1].X;
            double dy = polyline[i].Y - polyline[i - 1].Y;
            distances[i] = distances[i - 1] + Math.Sqrt((dx * dx) + (dy * dy));
        }

        return distances;
    }

    private static Point3 Lerp(Point3 a, Point3 b, double t) =>
        new(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t), Lerp(a.Z, b.Z, t));

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private void RequireEnds(NodeId start, NodeId end)
    {
        RequireNode(start, nameof(start));
        RequireNode(end, nameof(end));
        if (start == end)
        {
            throw new ArgumentException($"A segment must join two different nodes, but both ends are {start}.", nameof(end));
        }
    }

    private void RequireNode(NodeId id, string parameterName)
    {
        if (!_nodes.ContainsKey(id))
        {
            throw new ArgumentException($"There is no node {id}.", parameterName);
        }
    }

    /// <summary>Where the new road meets an existing segment, measured in plan along both.</summary>
    private readonly record struct Meeting(
        SegmentId Other, double RoadDistance, double OtherDistance, double OtherLength, Point3 OnRoad, Point3 OnOther);

    /// <summary>A place to split a polyline, and the node to split it at.</summary>
    private readonly record struct Cut(double Distance, NodeId Node);
}
