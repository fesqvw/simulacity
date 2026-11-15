using Simulacity.Core.Geometry;

namespace Simulacity.Core.Land;

/// <summary>
/// The land under the city (decision 0009): a grid of square cells over x and y, each with a
/// <see cref="LandType"/>, and a heightmap with one ground elevation at each cell corner.
/// </summary>
/// <remarks>
/// Cell (0, 0) covers the square from (0, 0) to (<see cref="CellSize"/>, <see cref="CellSize"/>);
/// x grows east and y grows north. Corner (i, j) is at (i × <see cref="CellSize"/>, j × <see cref="CellSize"/>),
/// so a grid of <see cref="Width"/> × <see cref="Height"/> cells has (Width + 1) × (Height + 1) corners.
/// </remarks>
public sealed class LandGrid
{
    private readonly LandType[] _cells;
    private readonly double[] _cornerElevations;

    /// <summary>Creates a flat grid with every cell set to <paramref name="fill"/>.</summary>
    /// <param name="width">The number of cells from west to east.</param>
    /// <param name="height">The number of cells from south to north.</param>
    /// <param name="cellSize">The side of a cell, in metres.</param>
    /// <param name="fill">The land type every cell starts with.</param>
    /// <param name="elevation">The ground elevation every corner starts with, in metres.</param>
    public LandGrid(int width, int height, double cellSize, LandType fill = LandType.Ground, double elevation = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (!double.IsFinite(cellSize) || cellSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be a positive, finite number of metres.");
        }

        RequireFinite(elevation, nameof(elevation));

        Width = width;
        Height = height;
        CellSize = cellSize;
        _cells = new LandType[checked(width * height)];
        Array.Fill(_cells, fill);
        _cornerElevations = new double[checked((width + 1) * (height + 1))];
        Array.Fill(_cornerElevations, elevation);
    }

    /// <summary>The number of cells from west to east.</summary>
    public int Width { get; }

    /// <summary>The number of cells from south to north.</summary>
    public int Height { get; }

    /// <summary>The side of a cell, in metres.</summary>
    public double CellSize { get; }

    /// <summary>The land type of the cell at column <paramref name="x"/> and row <paramref name="y"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The cell is outside the grid.</exception>
    public LandType this[int x, int y]
    {
        get => _cells[CellIndex(x, y)];
        set => _cells[CellIndex(x, y)] = value;
    }

    /// <summary>Whether column <paramref name="x"/> and row <paramref name="y"/> are inside the grid.</summary>
    public bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>Finds the cell under a world position. Its elevation is ignored.</summary>
    /// <returns><see langword="true"/> if the position is over the grid.</returns>
    public bool TryGetCell(Point3 position, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y))
        {
            return false;
        }

        double column = Math.Floor(position.X / CellSize);
        double row = Math.Floor(position.Y / CellSize);
        if (column < 0 || column >= Width || row < 0 || row >= Height)
        {
            return false;
        }

        x = (int)column;
        y = (int)row;
        return true;
    }

    /// <summary>The ground elevation at corner (<paramref name="i"/>, <paramref name="j"/>), in metres.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The corner is outside the grid.</exception>
    public double GetCornerElevation(int i, int j) => _cornerElevations[CornerIndex(i, j)];

    /// <summary>Sets the ground elevation at corner (<paramref name="i"/>, <paramref name="j"/>), in metres.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The corner is outside the grid.</exception>
    /// <exception cref="ArgumentException">The elevation isn't a finite number.</exception>
    public void SetCornerElevation(int i, int j, double elevation)
    {
        int index = CornerIndex(i, j);
        RequireFinite(elevation, nameof(elevation));
        _cornerElevations[index] = elevation;
    }

    /// <summary>
    /// The ground elevation under a world position, interpolated bilinearly between the corners of
    /// its cell. The position's own elevation is ignored.
    /// </summary>
    /// <returns><see langword="true"/> if the position is over the grid, edges included.</returns>
    public bool TryGetGroundElevation(Point3 position, out double elevation)
    {
        elevation = 0;
        double u = position.X / CellSize;
        double v = position.Y / CellSize;
        if (!(u >= 0 && u <= Width && v >= 0 && v <= Height))
        {
            return false; // Off the grid, or NaN.
        }

        int i = Math.Min((int)u, Width - 1);
        int j = Math.Min((int)v, Height - 1);
        double fx = u - i;
        double fy = v - j;

        double south = Lerp(GetCornerElevation(i, j), GetCornerElevation(i + 1, j), fx);
        double north = Lerp(GetCornerElevation(i, j + 1), GetCornerElevation(i + 1, j + 1), fx);
        elevation = Lerp(south, north, fy);
        return true;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private static void RequireFinite(double elevation, string parameterName)
    {
        if (!double.IsFinite(elevation))
        {
            throw new ArgumentException("The elevation must be a finite number of metres.", parameterName);
        }
    }

    private int CellIndex(int x, int y)
    {
        if (!Contains(x, y))
        {
            throw new ArgumentOutOfRangeException(null, $"Cell ({x}, {y}) is outside the {Width} by {Height} grid.");
        }

        return (y * Width) + x;
    }

    private int CornerIndex(int i, int j)
    {
        if (i < 0 || i > Width || j < 0 || j > Height)
        {
            throw new ArgumentOutOfRangeException(null, $"Corner ({i}, {j}) is outside the {Width + 1} by {Height + 1} corners of the grid.");
        }

        return (j * (Width + 1)) + i;
    }
}
