using System.Numerics;

public sealed class SplinePoint
{
    public Vector3 Position;
    public Vector3 InTangent;
    public Vector3 OutTangent;        
    
    public SplinePoint(Vector3 position, Vector3 innerTangent, Vector3 outerTangent)
        {
            Position = position;
            InTangent = innerTangent;
            OutTangent = outerTangent;
        }
}
