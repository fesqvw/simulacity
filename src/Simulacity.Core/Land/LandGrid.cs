using Simulacity.Core.Geometry;

namespace Simulacity.Core.Land;

/// <summary>
/// A grid of square land cells laid over world coordinates (decision 0007). Cell (0, 0) covers
/// the square from (0, 0) to (<see cref="CellSize"/>, <see cref="CellSize"/>); x grows east and y grows north.
/// </summary>
public sealed class LandGrid
{
    private readonly LandType[] _cells;

    /// <summary>Creates a grid with every cell set to <paramref name="fill"/>.</summary>
    /// <param name="width">The number of cells from west to east.</param>
    /// <param name="height">The number of cells from south to north.</param>
    /// <param name="cellSize">The side of a cell, in metres.</param>
    /// <param name="fill">The land type every cell starts with.</param>
    public LandGrid(int width, int height, double cellSize, LandType fill = LandType.Ground)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (!double.IsFinite(cellSize) || cellSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "The cell size must be a positive, finite number of metres.");
        }

        Width = width;
        Height = height;
        CellSize = cellSize;
        _cells = new LandType[checked(width * height)];
        Array.Fill(_cells, fill);
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
        get => _cells[IndexOf(x, y)];
        set => _cells[IndexOf(x, y)] = value;
    }

    /// <summary>Whether column <paramref name="x"/> and row <paramref name="y"/> are inside the grid.</summary>
    public bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>Finds the cell that contains a world position.</summary>
    /// <returns><see langword="true"/> if the position is on the grid.</returns>
    public bool TryGetCell(Point2 position, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (!position.IsFinite)
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

    private int IndexOf(int x, int y)
    {
        if (!Contains(x, y))
        {
            throw new ArgumentOutOfRangeException(null, $"Cell ({x}, {y}) is outside the {Width} by {Height} grid.");
        }

        return (y * Width) + x;
    }
}
