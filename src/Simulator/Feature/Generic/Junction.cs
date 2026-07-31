using System.Numerics;

public sealed class Junction : Feature
{
    public List<Guid> connectedSegmentIds = new();
    public Vector3 Position;
}