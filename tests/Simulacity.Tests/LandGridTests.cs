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

        Assert.True(grid.TryGetCell(new Point3(px, py, 0), out int x, out int y));
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

        Assert.False(grid.TryGetCell(new Point3(px, py, 0), out _, out _));
    }

    [Fact]
    public void A_position_elevation_does_not_change_its_cell()
    {
        var grid = new LandGrid(3, 2, 10);

        Assert.True(grid.TryGetCell(new Point3(15, 5, -30), out int x, out int y));
        Assert.Equal((1, 0), (x, y));
    }

    [Fact]
    public void A_new_grid_is_flat_at_the_given_elevation()
    {
        var grid = new LandGrid(2, 2, 10, elevation: 12.5);

        Assert.Equal(12.5, grid.GetCornerElevation(0, 0));
        Assert.Equal(12.5, grid.GetCornerElevation(2, 2));
        Assert.True(grid.TryGetGroundElevation(new Point3(7, 13, 0), out double elevation));
        Assert.Equal(12.5, elevation, precision: 9);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10, 0, 10)]
    [InlineData(5, 0, 5)]
    [InlineData(0, 10, 20)]
    [InlineData(10, 10, 30)]
    [InlineData(5, 5, 15)]
    [InlineData(2.5, 7.5, 17.5)]
    public void Ground_elevation_is_interpolated_bilinearly_within_a_cell(double px, double py, double expected)
    {
        var grid = new LandGrid(1, 1, 10);
        grid.SetCornerElevation(1, 0, 10);
        grid.SetCornerElevation(0, 1, 20);
        grid.SetCornerElevation(1, 1, 30);

        Assert.True(grid.TryGetGroundElevation(new Point3(px, py, 0), out double elevation));
        Assert.Equal(expected, elevation, precision: 9);
    }

    [Fact]
    public void Ground_elevation_is_defined_on_the_far_edges_of_the_grid()
    {
        var grid = new LandGrid(2, 2, 10);
        grid.SetCornerElevation(2, 2, 8);

        Assert.True(grid.TryGetGroundElevation(new Point3(20, 20, 0), out double elevation));
        Assert.Equal(8, elevation, precision: 9);
    }

    [Theory]
    [InlineData(-0.1, 0)]
    [InlineData(20.1, 0)]
    [InlineData(0, 20.1)]
    [InlineData(double.NaN, 0)]
    public void A_position_off_the_grid_has_no_ground_elevation(double px, double py)
    {
        var grid = new LandGrid(2, 2, 10);

        Assert.False(grid.TryGetGroundElevation(new Point3(px, py, 0), out _));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(3, 0)]
    [InlineData(0, 3)]
    public void Corners_outside_the_grid_are_rejected(int i, int j)
    {
        var grid = new LandGrid(2, 2, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => grid.GetCornerElevation(i, j));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetCornerElevation(i, j, 1));
    }

    [Fact]
    public void Elevations_must_be_finite()
    {
        var grid = new LandGrid(2, 2, 10);

        Assert.Throws<ArgumentException>(() => grid.SetCornerElevation(0, 0, double.NaN));
        Assert.Throws<ArgumentException>(() => new LandGrid(2, 2, 10, elevation: double.PositiveInfinity));
    }
}
