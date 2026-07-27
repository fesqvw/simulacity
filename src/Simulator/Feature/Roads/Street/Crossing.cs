public sealed class Crossing : Feature
{
    // Position along this segment.
    public float DistanceAlongSegment;
    public bool ConnectsLeftToRight = true;
    public bool StopsVehicles;
    public float Width;
}