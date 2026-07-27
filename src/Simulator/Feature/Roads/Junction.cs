using System.Numerics;

public sealed class Junction : Feature
{
    public Vector3 position;
    public List<Guid> connectedSegmentIds = new();
}