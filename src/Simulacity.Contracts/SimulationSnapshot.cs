namespace Simulacity.Contracts;

/// <summary>
/// Everything a renderer needs to draw the world, sent in full after every change (decision 0010).
/// </summary>
/// <remarks>
/// A renderer keeps no state of its own, so any single snapshot is enough to draw the whole world.
/// These types mirror the simulation's state without referencing <c>Simulacity.Core</c>.
/// </remarks>
/// <param name="Tick">The number of steps simulated so far.</param>
/// <param name="Land">The land under the city.</param>
/// <param name="Roads">The road graph.</param>
public sealed record SimulationSnapshot(long Tick, LandSnapshot Land, RoadsSnapshot Roads);

/// <summary>A position in world coordinates, in metres: x east, y north and z up (decision 0009).</summary>
public readonly record struct Position(double X, double Y, double Z);

/// <summary>What a land cell is made of.</summary>
public enum LandKind
{
    /// <summary>Dry land.</summary>
    Ground,

    /// <summary>Open water.</summary>
    Water,
}

/// <summary>The land grid and its terrain heights.</summary>
/// <param name="Width">The number of cells from west to east.</param>
/// <param name="Height">The number of cells from south to north.</param>
/// <param name="CellSize">The side of a cell, in metres. Cell (0, 0) starts at the world origin.</param>
/// <param name="Cells">
/// One land type per cell, row by row from the south: cell (x, y) is at index y × <paramref name="Width"/> + x.
/// </param>
/// <param name="CornerElevations">
/// The ground elevation at each cell corner, in metres, row by row from the south:
/// corner (i, j) is at index j × (<paramref name="Width"/> + 1) + i.
/// </param>
public sealed record LandSnapshot(
    int Width,
    int Height,
    double CellSize,
    IReadOnlyList<LandKind> Cells,
    IReadOnlyList<double> CornerElevations);

/// <summary>The road graph, with nodes and segments sorted by ID.</summary>
public sealed record RoadsSnapshot(IReadOnlyList<RoadNodeSnapshot> Nodes, IReadOnlyList<RoadSegmentSnapshot> Segments);

/// <summary>A road node: an intersection or a dead end.</summary>
public sealed record RoadNodeSnapshot(int Id, Position Position);

/// <summary>A road segment between two nodes.</summary>
/// <param name="Id">The segment's ID.</param>
/// <param name="Start">The ID of the node the segment starts at.</param>
/// <param name="End">The ID of the node the segment ends at.</param>
/// <param name="Points">
/// The segment as a polyline to draw, from the start node's position to the end node's.
/// Curved shapes are already sampled into points (decision 0010).
/// </param>
/// <param name="Stretches">
/// The same polyline split where the road goes onto a bridge, into a tunnel or back onto the ground,
/// in order from the start node (decision 0012).
/// </param>
public sealed record RoadSegmentSnapshot(
    int Id, int Start, int End, IReadOnlyList<Position> Points, IReadOnlyList<RoadStretchSnapshot> Stretches);

/// <summary>Whether a stretch of road is on the ground, a bridge or a tunnel.</summary>
public enum RoadLevelKind
{
    /// <summary>On the ground.</summary>
    Ground,

    /// <summary>Above the ground or water.</summary>
    Bridge,

    /// <summary>Below the ground or water.</summary>
    Tunnel,
}

/// <summary>A stretch of a road segment at one level, as a polyline to draw.</summary>
public sealed record RoadStretchSnapshot(RoadLevelKind Level, IReadOnlyList<Position> Points);
