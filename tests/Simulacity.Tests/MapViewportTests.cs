using Simulacity.Contracts;
using Simulacity.Renderer.Console;

namespace Simulacity.Tests;

/// <summary>Moving around the map in the renderer (decision 0014). The map is 100 × 50 cells of 10 m.</summary>
public class MapViewportTests
{
    private static readonly LandSnapshot Land = new(100, 50, 10, [.. Enumerable.Repeat(LandKind.Ground, 5000)], new double[101 * 51]);

    [Fact]
    public void A_map_that_fits_at_one_row_per_cell_starts_fully_in_view()
    {
        var small = new LandSnapshot(4, 2, 10, [.. Enumerable.Repeat(LandKind.Ground, 8)], new double[15]);

        Assert.Equal(new MapViewport(0, 20, 10), MapViewport.Initial(small, 80, 20));
    }

    [Fact]
    public void A_big_map_starts_at_one_row_per_cell_centred_on_the_map()
    {
        MapViewport view = MapViewport.Initial(Land, 80, 20);

        Assert.Equal(10, view.MetresPerRow);
        Assert.Equal((500, 250), view.Centre(80, 20));
        Assert.Equal((300, 350), (view.West, view.North));
    }

    [Fact]
    public void Panning_moves_a_quarter_of_the_view()
    {
        MapViewport view = MapViewport.Initial(Land, 80, 20);

        Assert.Equal((600, 250), view.Pan(1, 0, Land, 80, 20).Centre(80, 20));
        Assert.Equal((500, 200), view.Pan(0, -1, Land, 80, 20).Centre(80, 20));
    }

    [Fact]
    public void Panning_keeps_the_middle_of_the_view_on_the_map()
    {
        MapViewport view = MapViewport.Initial(Land, 80, 20);
        for (int i = 0; i < 20; i++)
        {
            view = view.Pan(-1, 1, Land, 80, 20);
        }

        Assert.Equal((0, 500), view.Centre(80, 20));
    }

    [Fact]
    public void Zooming_keeps_the_middle_and_stops_at_four_rows_per_cell_and_at_the_whole_map()
    {
        MapViewport view = MapViewport.Initial(Land, 80, 20);

        MapViewport closer = view.Zoom(1, Land, 80, 20);
        Assert.Equal(5, closer.MetresPerRow);
        Assert.Equal((500, 250), closer.Centre(80, 20));
        Assert.Equal(2.5, view.Zoom(10, Land, 80, 20).MetresPerRow);
        Assert.Equal(MapViewport.Fit(Land, 80, 20).MetresPerRow, view.Zoom(-10, Land, 80, 20).MetresPerRow);
    }

    [Fact]
    public void Drawing_a_view_shows_only_that_part_and_leaves_off_the_map_blank()
    {
        var land = new LandSnapshot(4, 2, 10, [LandKind.Ground, LandKind.Ground, LandKind.Ground, LandKind.Water, .. Enumerable.Repeat(LandKind.Ground, 4)], new double[15]);
        Position[] points = [new(0, 5, 0), new(40, 5, 0)];
        var road = new RoadSegmentSnapshot(1, 1, 2, points, [new(RoadLevelKind.Ground, points)]);
        var snapshot = new SimulationSnapshot(0, land, new RoadsSnapshot([new(1, points[0]), new(2, points[1])], [road]));

        // From 20 m east of the west edge, 4 characters across (20 m) and 3 rows down (30 m).
        IReadOnlyList<string> map = MapView.Draw(snapshot, new MapViewport(20, 30, 10), 6, 3);

        Assert.Equal(["      ", "....  ", "###o  "], map);
    }
}
