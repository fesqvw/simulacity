using Simulacity.Core.Geometry;
using Simulacity.Core.Land;

namespace Simulacity.Tests;

public class LandGridTests
{
    [Fact]
    public void A_new_grid_is_filled_with_the_given_land_type()
    {
        var grid = new LandGrid(3, 2, 10, LandType.Water);

        Assert.Equal(3, grid.Width);
        Assert.Equal(2, grid.Height);
        Assert.Equal(10, grid.CellSize);
        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Assert.Equal(LandType.Water, grid[x, y]);
            }
        }
    }

    [Fact]
    public void Setting_a_cell_changes_only_that_cell()
    {
        var grid = new LandGrid(3, 3, 10);

        grid[2, 1] = LandType.Water;

        Assert.Equal(LandType.Water, grid[2, 1]);
        Assert.Equal(LandType.Ground, grid[1, 2]);
        Assert.Equal(LandType.Ground, grid[1, 1]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 2)]
    public void Cells_outside_the_grid_are_rejected(int x, int y)
    {
        var grid = new LandGrid(3, 2, 10);

        Assert.False(grid.Contains(x, y));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid[x, y]);
    }

    [Theory]
    [InlineData(0, 2, 10)]
    [InlineData(2, 0, 10)]
    [InlineData(2, 2, 0)]
    [InlineData(2, 2, -5)]
    [InlineData(2, 2, double.NaN)]
    [InlineData(2, 2, double.PositiveInfinity)]
    public void Invalid_dimensions_are_rejected(int width, int height, double cellSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LandGrid(width, height, cellSize));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(9.99, 9.99, 0, 0)]
    [InlineData(10, 0, 1, 0)]
    [InlineData(25, 15, 2, 1)]
    public void A_world_position_maps_to_the_cell_that_contains_it(double px, double py, int expectedX, int expectedY)
    {
        var grid = new LandGrid(3, 2, 10);

        Assert.True(grid.TryGetCell(new Point2(px, py), out int x, out int y));
        Assert.Equal((expectedX, expectedY), (x, y));
    }

    [Theory]
    [InlineData(-0.1, 0)]
    [InlineData(0, -0.1)]
    [InlineData(30, 0)]
    [InlineData(0, 20)]
    [InlineData(double.NaN, 0)]
    public void A_world_position_off_the_grid_has_no_cell(double px, double py)
    {
        var grid = new LandGrid(3, 2, 10);

        Assert.False(grid.TryGetCell(new Point2(px, py), out _, out _));
    }
}
