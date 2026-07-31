public sealed class RiverSegment : LinearFeature
{
    public RiverAttributes attributes = new();
    WaterType type = WaterType.River;
    public Guid StartJunctionId;
    public Guid EndJunctionId;
}