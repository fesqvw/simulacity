namespace Simulacity.Core.Geometry;

/// <summary>How two straight pieces touch in plan, ignoring z.</summary>
internal enum PlanContactKind
{
    /// <summary>They don't touch.</summary>
    None,

    /// <summary>They cross or touch at a single point.</summary>
    Point,

    /// <summary>They lie along each other for more than the snap distance.</summary>
    Overlap,
}

/// <summary>
/// Where two straight pieces touch in plan. <see cref="T"/> and <see cref="U"/> are how far along the
/// first and the second piece the contact is, from 0 at their start to 1 at their end. For an
/// overlap they give the middle of the shared stretch.
/// </summary>
internal readonly record struct PlanContact(PlanContactKind Kind, double T, double U)
{
    private static readonly PlanContact NoContact = new(PlanContactKind.None, 0, 0);

    /// <summary>
    /// Finds where the piece from <paramref name="a"/> to <paramref name="b"/> touches the piece from
    /// <paramref name="c"/> to <paramref name="d"/> in plan. Points closer than <paramref name="snap"/>
    /// metres count as touching, so a road that ends on another one is found even with rounding.
    /// Pieces shorter than the snap distance in plan, such as vertical ones, touch nothing.
    /// </summary>
    public static PlanContact Between(Point3 a, Point3 b, Point3 c, Point3 d, double snap)
    {
        double rx = b.X - a.X, ry = b.Y - a.Y;
        double sx = d.X - c.X, sy = d.Y - c.Y;
        double rLength = Math.Sqrt((rx * rx) + (ry * ry));
        double sLength = Math.Sqrt((sx * sx) + (sy * sy));
        if (rLength < snap || sLength < snap)
        {
            return NoContact;
        }

        double qx = c.X - a.X, qy = c.Y - a.Y;
        double denominator = (rx * sy) - (ry * sx);
        if (Math.Abs(denominator) <= 1e-12 * rLength * sLength)
        {
            return Parallel(rx, ry, rLength, sx, sy, sLength, qx, qy, snap);
        }

        double t = ((qx * sy) - (qy * sx)) / denominator;
        double u = ((qx * ry) - (qy * rx)) / denominator;
        double tSlack = snap / rLength, uSlack = snap / sLength;
        if (t < -tSlack || t > 1 + tSlack || u < -uSlack || u > 1 + uSlack)
        {
            return NoContact;
        }

        return new PlanContact(PlanContactKind.Point, Math.Clamp(t, 0, 1), Math.Clamp(u, 0, 1));
    }

    private static PlanContact Parallel(
        double rx, double ry, double rLength, double sx, double sy, double sLength, double qx, double qy, double snap)
    {
        // How far c is from the line through a and b.
        double offset = Math.Abs((qx * ry) - (qy * rx)) / rLength;
        if (offset > snap)
        {
            return NoContact;
        }

        // Where c and d fall along the first piece, then the part of that range inside it.
        double r2 = rLength * rLength;
        double tc = ((qx * rx) + (qy * ry)) / r2;
        double td = tc + (((sx * rx) + (sy * ry)) / r2);
        double low = Math.Max(0, Math.Min(tc, td));
        double high = Math.Min(1, Math.Max(tc, td));
        double shared = (high - low) * rLength;
        if (shared < -snap)
        {
            return NoContact;
        }

        double t = Math.Clamp((low + high) / 2, 0, 1);

        // The same point measured along the second piece.
        double px = (t * rx) - qx, py = (t * ry) - qy;
        double u = Math.Clamp(((px * sx) + (py * sy)) / (sLength * sLength), 0, 1);
        return new PlanContact(shared > snap ? PlanContactKind.Overlap : PlanContactKind.Point, t, u);
    }
}
