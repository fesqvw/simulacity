using Simulacity.Contracts;
using Simulacity.Renderer.Console;

namespace Simulacity.Tests;

/// <summary>Showing terrain height in the console renderer (decision 0015).</summary>
public class HeightShadingTests
{
    private static readonly LandKind[] AllGround = [.. Enumerable.Repeat(LandKind.Ground, 8)];

    /// <summary>A 4 × 2 map of 10 m cells rising from 0 m on the west edge to 60 m on the east.</summary>
    private static LandSnapshot Slope(LandKind[]? cells = null) =>
        new(4, 2, 10, cells ?? AllGround, [.. Enumerable.Range(0, 15).Select(index => (index % 5) * 15.0)]);

    [Fact]
    public void A_flat_map_has_a_single_band()
    {
        HeightShading shading = HeightShading.For(new LandSnapshot(4, 2, 10, AllGround, new double[15]));

        Assert.Equal(1, shading.Bands);
        Assert.Equal(0, shading.BandOf(0));
        Assert.Equal("Height:   0 m (flat)", shading.Legend().Text);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9.9, 0)]
    [InlineData(10, 1)]
    [InlineData(35, 3)]
    [InlineData(60, 5)]
    [InlineData(99, 5)]
    public void The_height_range_is_split_into_equal_bands(double elevation, int band)
    {
        Assert.Equal(band, HeightShading.For(Slope()).BandOf(elevation));
    }

    [Fact]
    public void The_legend_gives_where_each_band_starts()
    {
        Assert.Equal("Height:   0   10   20   30   40   50 to 60 m", HeightShading.For(Slope()).Legend().Text);
    }

    [Fact]
    public void Each_character_is_coloured_by_the_height_of_the_ground_under_it()
    {
        LandSnapshot land = Slope();
        var snapshot = new SimulationSnapshot(0, land, new RoadsSnapshot([], []));
        MapPicture picture = MapView.Render(snapshot, MapViewport.Fit(land, 100, 100), 8, 2);

        Assert.Equal(3.75, picture.Elevations[0, 0], precision: 9); // 2.5 m from the west edge.
        Assert.Equal(56.25, picture.Elevations[0, 7], precision: 9);

        string row = HeightShading.For(land).Colour(picture, 0);
        Assert.StartsWith("\u001b[48;5;22;", row); // Dark green at the bottom.
        Assert.Contains("\u001b[48;5;250;38;5;16m", row); // Light grey with dark text at the top.
        Assert.EndsWith("\u001b[0m", row);
        Assert.Equal("........", System.Text.RegularExpressions.Regex.Replace(row, "\u001b\\[[0-9;]*m", string.Empty));
    }

    [Fact]
    public void Water_stays_blue_and_off_the_map_has_no_colour()
    {
        LandKind[] cells = [.. AllGround];
        cells[4] = LandKind.Water; // Cell (0, 1), on the top row.
        LandSnapshot land = Slope(cells);
        var snapshot = new SimulationSnapshot(0, land, new RoadsSnapshot([], []));

        // A view starting 10 m west of the map, so the first two characters are off it.
        MapPicture picture = MapView.Render(snapshot, new MapViewport(-10, 20, 10), 6, 1);
        string row = HeightShading.For(land).Colour(picture, 0);

        Assert.Null(picture.LandUnder[0, 0]);
        Assert.Equal(LandKind.Water, picture.LandUnder[0, 2]);
        Assert.StartsWith("  \u001b[48;5;18;38;5;45m~~", row);
    }
}
