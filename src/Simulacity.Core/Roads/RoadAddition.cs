namespace Simulacity.Core.Roads;

/// <summary>What <see cref="RoadNetwork.AddRoad"/> changed in the graph (decision 0011).</summary>
/// <param name="Segments">The new road's segments, in order from its start node to its end node.</param>
/// <param name="Splits">The existing segments the road met, each replaced by its pieces.</param>
/// <param name="NewNodes">The nodes added where the road meets other roads.</param>
public sealed record RoadAddition(
    IReadOnlyList<RoadSegment> Segments,
    IReadOnlyList<RoadSplit> Splits,
    IReadOnlyList<RoadNode> NewNodes);

/// <summary>An existing segment that was removed and replaced by pieces where a new road met it.</summary>
/// <param name="Removed">The ID of the segment that no longer exists.</param>
/// <param name="Pieces">Its replacements, in order from its start node to its end node.</param>
public sealed record RoadSplit(SegmentId Removed, IReadOnlyList<RoadSegment> Pieces);
