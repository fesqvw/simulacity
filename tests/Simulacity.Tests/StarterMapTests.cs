using Simulacity.Contracts;
using Simulacity.Core;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;
using Simulacity.Simulator;

namespace Simulacity.Tests;

/// <summary>The starting map (decision 0013) loads cleanly and has a bit of everything.</summary>
public class StarterMapTests
{
    private static readonly string StarterPath = Path.Combine(AppContext.BaseDirectory, "maps", "starter.txt");

    private readonly RecordingPublisher _publisher = new();
    private readonly CommandInterpreter _interpreter;

    public StarterMapTests() => _interpreter = new CommandInterpreter(new Simulation(new World(new LandGrid(1, 1, 10))), _publisher);

    private World World => _interpreter.Simulation.World;

    [Fact]
    public async Task Every_command_in_the_starting_map_succeeds_and_it_is_published_once()
    {
        ScriptResult result = await LoadAsync();

        Assert.True(result.Succeeded, result.Message);
        Assert.Single(_publisher.Published);
        Assert.Equal((160, 80), (World.Land.Width, World.Land.Height));
    }

    [Fact]
    public async Task The_starting_map_has_water_hills_junctions_bridges_and_tunnels()
    {
        await LoadAsync();

        Assert.Equal(LandType.Water, World.Land[61, 40]); // The river.
        Assert.Equal(LandType.Water, World.Land[110, 10]); // The lake.
        Assert.True(World.Land.GetCornerElevation(125, 60) > 25); // The top of the hill.
        Assert.Contains(World.Roads.Nodes, node => World.Roads.SegmentsAt(node.Id).Count == 4); // Crossroads.
        Assert.Contains(World.Roads.Nodes, node => World.Roads.SegmentsAt(node.Id).Count == 3); // T-junctions.

        RoadLevel[] levels = [.. World.Roads.Segments.SelectMany(segment => World.StretchesOf(segment.Id)).Select(stretch => stretch.Level)];
        Assert.Contains(RoadLevel.Bridge, levels);
        Assert.Contains(RoadLevel.Tunnel, levels);
    }

    [Fact]
    public async Task The_road_over_the_hill_stays_on_the_ground_and_the_one_through_it_is_a_tunnel()
    {
        await LoadAsync();

        RoadSegment Along(double y) => World.Roads.Segments.Single(segment =>
            World.Roads.PolylineOf(segment.Id) is var points && points[0].Y == y && points[^1].Y == y && points[^1].X == 1590);

        Assert.All(World.StretchesOf(Along(680).Id), stretch => Assert.Equal(RoadLevel.Ground, stretch.Level));
        Assert.Equal(
            [RoadLevel.Ground, RoadLevel.Tunnel, RoadLevel.Ground],
            World.StretchesOf(Along(520).Id).Select(stretch => stretch.Level));
    }

    private async Task<ScriptResult> LoadAsync() =>
        await _interpreter.RunScriptAsync(await File.ReadAllLinesAsync(StarterPath), "starter.txt");
}
