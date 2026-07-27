public sealed class SplineCurve
{
    public Guid Id = Guid.NewGuid();
    public SplineType Type;
    public bool Closed;
    public List<SplinePoint> Points = new();
}