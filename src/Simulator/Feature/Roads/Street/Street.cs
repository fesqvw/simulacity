public sealed class Street
{
    public Guid roadSegmentId;

    public StreetAttributes attributes = new();
    
    public StreetCrossSection crossSection = new();
}