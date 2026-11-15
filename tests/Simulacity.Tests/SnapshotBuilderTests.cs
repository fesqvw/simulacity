using Simulacity.Contracts;
using Simulacity.Core;
using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;
using Simulacity.Simulator;

namespace Simulacity.Tests;

public class SnapshotBuilderTests
{
    private readonly Simulation _simulation = new(new World(new LandGrid(3, 2, 10)));

    [Fact]
    public void An_empty_world_gives_its_tick_land_and_no_roads()
    {
        _simulation.Step();

        SimulationSnapshot snapshot = SnapshotBuilder.From(_simulation);

        Assert.Equal(1, snapshot.Tick);
        Assert.Equal((3, 2, 10.0), (snapshot.Land.Width, snapshot.Land.Height, snapshot.Land.CellSize));
        Assert.Equal(6, snapshot.Land.Cells.Count);
        Assert.Equal(12, snapshot.Land.CornerElevations.Count);
        Assert.Empty(snapshot.Roads.Nodes);
        Assert.Empty(snapshot.Roads.Segments);
    }

    [Fact]
    public void Cells_are_listed_row_by_row_from_the_south_west()
    {
        _simulation.World.Land[2, 0] = LandType.Water;
        _simulation.World.Land[0, 1] = LandType.Water;

        LandSnapshot land = SnapshotBuilder.From(_simulation).Land;

        Assert.Equal(
            [LandKind.Ground, LandKind.Ground, LandKind.Water, LandKind.Water, LandKind.Ground, LandKind.Ground],
            land.Cells);
    }

    [Fact]
    public void Corner_elevations_are_listed_row_by_row_from_the_south_west()
    {
        _simulation.World.Land.SetCornerElevation(3, 0, 4);
        _simulation.World.Land.SetCornerElevation(0, 1, -1);
        _simulation.World.Land.SetCornerElevation(2, 2, 9);

        LandSnapshot land = SnapshotBuilder.From(_simulation).Land;

        Assert.Equal([0, 0, 0, 4, -1, 0, 0, 0, 0, 0, 9, 0], land.CornerElevations);
    }

    [Fact]
    public void Roads_are_sent_as_drawable_polylines_sorted_by_id()
    {
        RoadNetwork roads = _simulation.World.Roads;
        RoadNode a = roads.AddNode(new Point3(0, 0, 0));
        RoadNode b = roads.AddNode(new Point3(20, 0, 0));
        RoadNode c = roads.AddNode(new Point3(20, 20, 5));
        RoadSegment removed = roads.AddSegment(a.Id, b.Id);
        RoadSegment bridge = roads.AddSegment(b.Id, c.Id, new PolylineShape([new Point3(20, 10, 8)]));
        RoadSegment straight = roads.AddSegment(a.Id, b.Id);
        roads.RemoveSegment(removed.Id);

        RoadsSnapshot snapshot = SnapshotBuilder.From(_simulation).Roads;

        Assert.Equal(
            [
                new RoadNodeSnapshot(1, new Position(0, 0, 0)),
                new RoadNodeSnapshot(2, new Position(20, 0, 0)),
                new RoadNodeSnapshot(3, new Position(20, 20, 5)),
            ],
            snapshot.Nodes);
        Assert.Collection(
            snapshot.Segments,
            segment =>
            {
                Assert.Equal((bridge.Id.Value, 2, 3), (segment.Id, segment.Start, segment.End));
                Assert.Equal([new Position(20, 0, 0), new Position(20, 10, 8), new Position(20, 20, 5)], segment.Points);
            },
            segment =>
            {
                Assert.Equal((straight.Id.Value, 1, 2), (segment.Id, segment.Start, segment.End));
                Assert.Equal([new Position(0, 0, 0), new Position(20, 0, 0)], segment.Points);
            });
    }

    [Fact]
    public void A_snapshot_is_a_copy_that_later_changes_do_not_affect()
    {
        SimulationSnapshot before = SnapshotBuilder.From(_simulation);

        _simulation.World.Land[0, 0] = LandType.Water;
        _simulation.World.Roads.AddNode(new Point3(1, 1, 0));

        Assert.Equal(LandKind.Ground, before.Land.Cells[0]);
        Assert.Empty(before.Roads.Nodes);
    }
}
