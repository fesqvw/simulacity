using Simulacity.Core.Geometry;

namespace Simulacity.Core.Roads;

/// <summary>An intersection or a dead end in the road graph (decision 0007).</summary>
/// <param name="Id">The node's stable ID.</param>
/// <param name="Position">Where the node is, in world coordinates.</param>
public sealed record RoadNode(NodeId Id, Point2 Position);

/// <summary>A road between two nodes of the road graph (decision 0007).</summary>
/// <param name="Id">The segment's stable ID.</param>
/// <param name="Start">The node the segment starts at.</param>
/// <param name="End">The node the segment ends at.</param>
/// <param name="Shape">The segment's shape between its two nodes.</param>
public sealed record RoadSegment(SegmentId Id, NodeId Start, NodeId End, SegmentShape Shape);
