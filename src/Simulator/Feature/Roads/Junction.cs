using System.Numerics;

public sealed class Junction : Feature
{
    public Vector3 Position;
    public List<Guid> ConnectedSegmentIds = new();
}