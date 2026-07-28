using System;
using System.Collections.Generic;
using System.Numerics;

public static class CatmullRomRasterizer
{
    public readonly record struct Int2(int X, int Y);

    public static HashSet<Int2> Rasterize(
        IReadOnlyList<Vector3> points,
        IReadOnlyList<float> widths,
        Func<Vector3, Vector2> projectToRaster,
        Vector2 cellSize,
        float tension = 0f,
        float flatnessInCells = 0.25f,
        float maxWidthDeltaInCells = 0.5f,
        int maxDepth = 16,
        bool closed = false)
    {
        if (points == null) throw new ArgumentNullException(nameof(points));
        if (widths == null) throw new ArgumentNullException(nameof(widths));
        if (projectToRaster == null) throw new ArgumentNullException(nameof(projectToRaster));
        if (points.Count < 2) throw new ArgumentException("Need at least 2 points.", nameof(points));
        if (widths.Count != points.Count) throw new ArgumentException("widths.Count must equal points.Count.", nameof(widths));

        var pixels = new HashSet<Int2>();
        int count = points.Count;

        int segmentCount = closed ? count : count - 1;
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 p0 = GetPoint(points, i - 1, closed);
            Vector3 p1 = GetPoint(points, i, closed);
            Vector3 p2 = GetPoint(points, i + 1, closed);
            Vector3 p3 = GetPoint(points, i + 2, closed);

            float w1 = GetWidth(widths, i, closed);
            float w2 = GetWidth(widths, i + 1, closed);

            float s = 0.5f * (1f - tension);
            Vector3 t1 = s * (p2 - p0);
            Vector3 t2 = s * (p3 - p1);

            RasterizeHermiteSpan(
                p1, p2, t1, t2,
                w1, w2,
                projectToRaster,
                cellSize,
                flatnessInCells,
                maxWidthDeltaInCells,
                maxDepth,
                pixels);
        }

        return pixels;
    }

    private static void RasterizeHermiteSpan(
        Vector3 p0, Vector3 p1,
        Vector3 t0, Vector3 t1,
        float width0, float width1,
        Func<Vector3, Vector2> projectToRaster,
        Vector2 cellSize,
        float flatnessInCells,
        float maxWidthDeltaInCells,
        int maxDepth,
        HashSet<Int2> pixels)
    {
        Vector3 b0 = p0;
        Vector3 b1 = p0 + t0 / 3f;
        Vector3 b2 = p1 - t1 / 3f;
        Vector3 b3 = p1;

        RasterizeBezierRecursive(
            b0, b1, b2, b3,
            width0, width1,
            projectToRaster,
            cellSize,
            flatnessInCells,
            maxWidthDeltaInCells,
            maxDepth,
            pixels);
    }

    private static void RasterizeBezierRecursive(
        Vector3 b0, Vector3 b1, Vector3 b2, Vector3 b3,
        float width0, float width1,
        Func<Vector3, Vector2> projectToRaster,
        Vector2 cellSize,
        float flatnessInCells,
        float maxWidthDeltaInCells,
        int depth,
        HashSet<Int2> pixels)
    {
        Vector2 p0 = projectToRaster(b0);
        Vector2 p1 = projectToRaster(b1);
        Vector2 p2 = projectToRaster(b2);
        Vector2 p3 = projectToRaster(b3);

        bool flatEnough = IsFlatEnoughProjected(p0, p1, p2, p3, cellSize, flatnessInCells);
        bool widthStableEnough = IsWidthStableEnough(width0, width1, cellSize, maxWidthDeltaInCells);

        if (depth <= 0 || (flatEnough && widthStableEnough))
        {
            RasterizeThickSegment(p0, p3, width0, width1, cellSize, pixels);
            return;
        }

        SplitBezier(
            b0, b1, b2, b3,
            out var l0, out var l1, out var l2, out var l3,
            out var r0, out var r1, out var r2, out var r3);

        float midWidth = 0.5f * (width0 + width1);

        RasterizeBezierRecursive(
            l0, l1, l2, l3,
            width0, midWidth,
            projectToRaster,
            cellSize,
            flatnessInCells,
            maxWidthDeltaInCells,
            depth - 1,
            pixels);

        RasterizeBezierRecursive(
            r0, r1, r2, r3,
            midWidth, width1,
            projectToRaster,
            cellSize,
            flatnessInCells,
            maxWidthDeltaInCells,
            depth - 1,
            pixels);
    }

    private static bool IsFlatEnoughProjected(
        Vector2 b0, Vector2 b1, Vector2 b2, Vector2 b3,
        Vector2 cellSize,
        float flatnessInCells)
    {
        float tolerance = flatnessInCells * MathF.Min(cellSize.X, cellSize.Y);

        float d1 = DistancePointToLine2D(b1, b0, b3);
        float d2 = DistancePointToLine2D(b2, b0, b3);

        return d1 <= tolerance && d2 <= tolerance;
    }

    private static bool IsWidthStableEnough(
        float width0, float width1,
        Vector2 cellSize,
        float maxWidthDeltaInCells)
    {
        float tolerance = maxWidthDeltaInCells * MathF.Min(cellSize.X, cellSize.Y);
        return MathF.Abs(width1 - width0) <= tolerance;
    }

    private static float DistancePointToLine2D(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len = ab.Length();

        if (len < 1e-6f)
            return Vector2.Distance(p, a);

        float cross = MathF.Abs(ab.X * (p.Y - a.Y) - ab.Y * (p.X - a.X));
        return cross / len;
    }

    private static void SplitBezier(
        Vector3 b0, Vector3 b1, Vector3 b2, Vector3 b3,
        out Vector3 l0, out Vector3 l1, out Vector3 l2, out Vector3 l3,
        out Vector3 r0, out Vector3 r1, out Vector3 r2, out Vector3 r3)
    {
        Vector3 m01 = (b0 + b1) * 0.5f;
        Vector3 m12 = (b1 + b2) * 0.5f;
        Vector3 m23 = (b2 + b3) * 0.5f;

        Vector3 m012 = (m01 + m12) * 0.5f;
        Vector3 m123 = (m12 + m23) * 0.5f;

        Vector3 m = (m012 + m123) * 0.5f;

        l0 = b0; l1 = m01;  l2 = m012; l3 = m;
        r0 = m;  r1 = m123; r2 = m23;  r3 = b3;
    }

    private static void RasterizeThickSegment(
        Vector2 a,
        Vector2 b,
        float width0,
        float width1,
        Vector2 cellSize,
        HashSet<Int2> pixels)
    {
        float radius0 = width0 * 0.5f;
        float radius1 = width1 * 0.5f;
        float maxRadius = MathF.Max(radius0, radius1);

        float minX = MathF.Min(a.X, b.X) - maxRadius;
        float maxX = MathF.Max(a.X, b.X) + maxRadius;
        float minY = MathF.Min(a.Y, b.Y) - maxRadius;
        float maxY = MathF.Max(a.Y, b.Y) + maxRadius;

        int ix0 = (int)MathF.Floor(minX / cellSize.X);
        int ix1 = (int)MathF.Floor(maxX / cellSize.X);
        int iy0 = (int)MathF.Floor(minY / cellSize.Y);
        int iy1 = (int)MathF.Floor(maxY / cellSize.Y);

        Vector2 ab = b - a;
        float abLenSq = ab.LengthSquared();

        for (int iy = iy0; iy <= iy1; iy++)
        {
            for (int ix = ix0; ix <= ix1; ix++)
            {
                Vector2 cellCenter = new Vector2(
                    (ix + 0.5f) * cellSize.X,
                    (iy + 0.5f) * cellSize.Y);

                float t;
                if (abLenSq < 1e-6f)
                {
                    t = 0f;
                }
                else
                {
                    t = Vector2.Dot(cellCenter - a, ab) / abLenSq;
                    t = Math.Clamp(t, 0f, 1f);
                }

                Vector2 closest = a + t * ab;
                float radius = Lerp(radius0, radius1, t);
                float dist = Vector2.Distance(cellCenter, closest);

                if (dist <= radius)
                    pixels.Add(new Int2(ix, iy));
            }
        }
    }

    private static Vector3 GetPoint(IReadOnlyList<Vector3> points, int index, bool closed)
    {
        int n = points.Count;

        if (closed)
        {
            index %= n;
            if (index < 0) index += n;
            return points[index];
        }

        if (index < 0) return points[0];
        if (index >= n) return points[n - 1];
        return points[index];
    }

    private static float GetWidth(IReadOnlyList<float> widths, int index, bool closed)
    {
        int n = widths.Count;

        if (closed)
        {
            index %= n;
            if (index < 0) index += n;
            return widths[index];
        }

        if (index < 0) return widths[0];
        if (index >= n) return widths[n - 1];
        return widths[index];
    }

    private static float Lerp(float a, float b, float t)
        => a + (b - a) * t;
}