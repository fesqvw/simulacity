public abstract class AreaFeature : Feature
{
    public SplineCurve Boundary = new(); // must be Closed = true
}