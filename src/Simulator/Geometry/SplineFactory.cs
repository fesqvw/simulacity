using System.Numerics;

public class SplineFactory
{
    public static SplineCurve FromPoints(IReadOnlyList<Vector3> points, bool closed)
    {
        SplineCurve c = new()
        {
            Points = Generate(points, closed: closed),
            Type = SplineType.CatmullRom
        };

        return c;
    }


    private const float Alpha = 0.5f;
    private const float Epsilon = 1e-5f;
    private const float TangentDelta = 1e-3f;

    static List<SplinePoint> Generate(
        IReadOnlyList<Vector3> controlPoints,
        int samplesPerSegment = 16,
        bool closed = false)
    {
        if (controlPoints == null)
            throw new ArgumentNullException(nameof(controlPoints));

        if (controlPoints.Count < 2)
            throw new ArgumentException("At least 2 control points are required.", nameof(controlPoints));

        if (samplesPerSegment < 2)
            throw new ArgumentOutOfRangeException(nameof(samplesPerSegment), "Must be at least 2.");

        var result = new List<SplinePoint>();

        if (closed)
        {
            int count = controlPoints.Count;

            for (int i = 0; i < count; i++)
            {
                Vector3 p0 = controlPoints[(i - 1 + count) % count];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[(i + 1) % count];
                Vector3 p3 = controlPoints[(i + 2) % count];

                AppendSegment(result, p0, p1, p2, p3, samplesPerSegment, includeFirstPoint: i == 0);
            }
        }
        else
        {
            if (controlPoints.Count == 2)
            {
                Vector3 dir = Vector3.Normalize(controlPoints[1] - controlPoints[0]);
                result.Add(new SplinePoint(controlPoints[0], dir, dir));
                result.Add(new SplinePoint(controlPoints[1], dir, dir));
                return result;
            }

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                Vector3 p0 = (i == 0) ? controlPoints[0] : controlPoints[i - 1];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[i + 1];
                Vector3 p3 = (i + 2 < controlPoints.Count) ? controlPoints[i + 2] : controlPoints[^1];

                AppendSegment(result, p0, p1, p2, p3, samplesPerSegment, includeFirstPoint: i == 0);
            }
        }

        return result;
    }

    static SplinePoint EvaluateSegmentWithTangents(
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
    {
        float t0 = 0f;
        float t1 = GetKnot(t0, p0, p1);
        float t2 = GetKnot(t1, p1, p2);
        float t3 = GetKnot(t2, p2, p3);

        FixKnots(ref t0, ref t1, ref t2, ref t3);

        return EvaluateWithTangents(p0, p1, p2, p3, t0, t1, t2, t3, u);
    }

    static void AppendSegment(
        List<SplinePoint> output,
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        int samplesPerSegment,
        bool includeFirstPoint)
    {
        float t0 = 0f;
        float t1 = GetKnot(t0, p0, p1);
        float t2 = GetKnot(t1, p1, p2);
        float t3 = GetKnot(t2, p2, p3);

        FixKnots(ref t0, ref t1, ref t2, ref t3);

        int start = includeFirstPoint ? 0 : 1;

        for (int i = start; i < samplesPerSegment; i++)
        {
            float u = i / (float)(samplesPerSegment - 1);
            output.Add(EvaluateWithTangents(p0, p1, p2, p3, t0, t1, t2, t3, u));
        }
    }

    private static SplinePoint EvaluateWithTangents(
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        float t0, float t1, float t2, float t3,
        float u)
    {
        u = Clamp01(u);

        Vector3 position = Evaluate(p0, p1, p2, p3, t0, t1, t2, t3, u);

        float uPrev = MathF.Max(0f, u - TangentDelta);
        float uNext = MathF.Min(1f, u + TangentDelta);

        Vector3 prev = Evaluate(p0, p1, p2, p3, t0, t1, t2, t3, uPrev);
        Vector3 next = Evaluate(p0, p1, p2, p3, t0, t1, t2, t3, uNext);

        Vector3 inner;
        Vector3 outer;

        if (u <= TangentDelta)
        {
            outer = SafeNormalize(next - position);
            inner = outer;
        }
        else if (u >= 1f - TangentDelta)
        {
            inner = SafeNormalize(position - prev);
            outer = inner;
        }
        else
        {
            inner = SafeNormalize(position - prev);
            outer = SafeNormalize(next - position);
        }

        return new SplinePoint(position, inner, outer);
    }

    private static float GetKnot(float ti, Vector3 a, Vector3 b)
    {
        float distance = Vector3.Distance(a, b);
        return ti + MathF.Pow(distance, Alpha);
    }

    private static void FixKnots(ref float t0, ref float t1, ref float t2, ref float t3)
    {
        if (MathF.Abs(t1 - t0) < Epsilon) t1 = t0 + Epsilon;
        if (MathF.Abs(t2 - t1) < Epsilon) t2 = t1 + Epsilon;
        if (MathF.Abs(t3 - t2) < Epsilon) t3 = t2 + Epsilon;
    }

    private static Vector3 Evaluate(
        Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        float t0, float t1, float t2, float t3,
        float u)
    {
        float t = Lerp(t1, t2, u);

        Vector3 A1 = Interpolate(p0, p1, t0, t1, t);
        Vector3 A2 = Interpolate(p1, p2, t1, t2, t);
        Vector3 A3 = Interpolate(p2, p3, t2, t3, t);

        Vector3 B1 = Interpolate(A1, A2, t0, t2, t);
        Vector3 B2 = Interpolate(A2, A3, t1, t3, t);

        return Interpolate(B1, B2, t1, t2, t);
    }

    private static Vector3 Interpolate(Vector3 a, Vector3 b, float ta, float tb, float t)
    {
        if (MathF.Abs(tb - ta) < Epsilon)
            return a;

        float u = (t - ta) / (tb - ta);
        return a + (b - a) * u;
    }

    private static Vector3 SafeNormalize(Vector3 v)
    {
        float lenSq = v.LengthSquared();
        if (lenSq < Epsilon * Epsilon)
            return Vector3.Zero;

        return v / MathF.Sqrt(lenSq);
    }

    private static float Clamp01(float x) => MathF.Max(0f, MathF.Min(1f, x));
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

}
