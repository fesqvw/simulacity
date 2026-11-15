namespace Simulacity.Core.Roads;

/// <summary>The stable ID of a <see cref="RoadNode"/>.</summary>
public readonly record struct NodeId(int Value)
{
    /// <inheritdoc/>
    public override string ToString() => $"n{Value}";
}

/// <summary>The stable ID of a <see cref="RoadSegment"/>.</summary>
public readonly record struct SegmentId(int Value)
{
    /// <inheritdoc/>
    public override string ToString() => $"s{Value}";
}
