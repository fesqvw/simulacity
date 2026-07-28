using System.Numerics;

public sealed class GeometricIntersection
{
    public Guid StrokeA;
    public Guid StrokeB;
    public Vector3 Position;
    public float TA; // parameter on stroke A
    public float TB; // parameter on stroke B
}