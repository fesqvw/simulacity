public sealed class Street : Feature
{
    public Guid roadSegmentId;

    public StreetAttributes attributes = new();
    
    public StreetCrossSection crossSection = new();
}