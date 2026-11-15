using Simulacity.Contracts;
using Simulacity.Core;
using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;
using Simulacity.Simulator;

namespace Simulacity.Tests;

public class CommandInterpreterTests
{
    private readonly Simulation _simulation = new(new World(new LandGrid(4, 4, 10)));
    private readonly RecordingPublisher _publisher = new();
    private readonly CommandInterpreter _interpreter;

    public CommandInterpreterTests() => _interpreter = new CommandInterpreter(_simulation, _publisher);

    [Fact]
    public async Task Step_without_a_count_advances_one_tick_and_publishes_it()
    {
        CommandResult result = await _interpreter.ExecuteAsync("step");

        Assert.Equal(1, _simulation.Tick);
        Assert.Equal("Tick 1", result.Output);
        Assert.Equal(1, Assert.Single(_publisher.Published).Tick);
    }

    [Fact]
    public async Task Step_with_a_count_advances_that_many_ticks_and_publishes_once()
    {
        await _interpreter.ExecuteAsync("  STEP   5 ");

        Assert.Equal(5, _simulation.Tick);
        Assert.Equal(5, Assert.Single(_publisher.Published).Tick);
    }

    [Theory]
    [InlineData("step 0")]
    [InlineData("step -2")]
    [InlineData("step abc")]
    [InlineData("step 1 2")]
    public async Task Step_with_an_invalid_count_changes_nothing(string line)
    {
        CommandResult result = await _interpreter.ExecuteAsync(line);

        Assert.StartsWith("Usage: step", result.Output);
        Assert.Equal(0, _simulation.Tick);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Status_reports_the_tick_without_changing_anything()
    {
        await _interpreter.ExecuteAsync("step 3");

        CommandResult result = await _interpreter.ExecuteAsync("status");

        Assert.Equal("Tick 3", result.Output);
        Assert.Equal(3, _simulation.Tick);
        Assert.Single(_publisher.Published);
    }

    [Theory]
    [InlineData("quit")]
    [InlineData("exit")]
    public async Task Quit_and_exit_stop_the_simulator(string line)
    {
        Assert.True((await _interpreter.ExecuteAsync(line)).Quit);
    }

    [Fact]
    public async Task An_unknown_command_is_reported()
    {
        CommandResult result = await _interpreter.ExecuteAsync("fly");

        Assert.Contains("Unknown command 'fly'", result.Output);
        Assert.False(result.Quit);
    }

    [Fact]
    public async Task An_empty_line_does_nothing()
    {
        CommandResult result = await _interpreter.ExecuteAsync("   ");

        Assert.Equal(string.Empty, result.Output);
        Assert.False(result.Quit);
    }

    [Fact]
    public async Task Node_without_a_z_is_placed_on_the_ground()
    {
        _simulation.World.Land.SetCornerElevation(1, 1, 8);

        CommandResult result = await _interpreter.ExecuteAsync("node 10 10");

        Assert.Equal("Added node n1 at (10, 10, 8).", result.Output);
        Assert.Equal(new Point3(10, 10, 8), Assert.Single(_simulation.World.Roads.Nodes).Position);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Node_with_a_z_is_placed_at_that_height()
    {
        await _interpreter.ExecuteAsync("node 5 5 -12.5");

        Assert.Equal(new Point3(5, 5, -12.5), Assert.Single(_simulation.World.Roads.Nodes).Position);
    }

    [Fact]
    public async Task Node_off_the_map_needs_a_z()
    {
        CommandResult rejected = await _interpreter.ExecuteAsync("node 100 5");
        await _interpreter.ExecuteAsync("node 100 5 3");

        Assert.Contains("off the map", rejected.Output);
        Assert.Equal(new Point3(100, 5, 3), Assert.Single(_simulation.World.Roads.Nodes).Position);
    }

    [Theory]
    [InlineData("node")]
    [InlineData("node 1")]
    [InlineData("node 1 2 3 4")]
    [InlineData("node a 2")]
    [InlineData("node 1 2 NaN")]
    [InlineData("node 1,5 2")]
    public async Task Node_with_invalid_arguments_changes_nothing(string line)
    {
        CommandResult result = await _interpreter.ExecuteAsync(line);

        Assert.StartsWith("Usage: node", result.Output);
        Assert.Empty(_simulation.World.Roads.Nodes);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task Road_joins_two_nodes_through_optional_points()
    {
        await _interpreter.ExecuteAsync("node 0 0");
        await _interpreter.ExecuteAsync("node 30 30");

        CommandResult result = await _interpreter.ExecuteAsync("road n1 2 30,0 30,0,4");

        RoadNetwork roads = _simulation.World.Roads;
        RoadSegment segment = Assert.Single(roads.Segments);
        Assert.Equal("Added road s1 from n1 to n2, 64.27 m long.", result.Output);
        Assert.Equal(
            [new Point3(0, 0, 0), new Point3(30, 0, 0), new Point3(30, 0, 4), new Point3(30, 30, 0)],
            roads.PolylineOf(segment.Id));
        Assert.Equal(3, _publisher.Published.Count);
    }

    [Theory]
    [InlineData("road", "Usage: road")]
    [InlineData("road n1", "Usage: road")]
    [InlineData("road x1 n2", "Usage: road")]
    [InlineData("road n1 n9", "There is no node n9")]
    [InlineData("road n1 n1", "two different nodes")]
    [InlineData("road n1 n2 5", "isn't a point")]
    [InlineData("road n1 n2 5,5,5,5", "isn't a point")]
    [InlineData("road n1 n2 500,5", "off the map")]
    public async Task Road_with_invalid_arguments_changes_nothing(string line, string expected)
    {
        await _interpreter.ExecuteAsync("node 0 0");
        await _interpreter.ExecuteAsync("node 30 30");

        CommandResult result = await _interpreter.ExecuteAsync(line);

        Assert.Contains(expected, result.Output);
        Assert.Empty(_simulation.World.Roads.Segments);
        Assert.Equal(2, _publisher.Published.Count);
    }

    [Fact]
    public async Task Road_that_crosses_another_reports_the_junction_and_the_split()
    {
        foreach (string line in (string[])["node 0 20", "node 40 20", "road n1 n2", "node 10 0", "node 10 40"])
        {
            await _interpreter.ExecuteAsync(line);
        }

        CommandResult result = await _interpreter.ExecuteAsync("road n3 n4");

        Assert.Equal(
            """
            Added road from n3 to n4 as s4 and s5, 40 m long.
            Added junction n5 at (10, 20, 0).
            Split road s1 into s2 and s3.
            """.ReplaceLineEndings(),
            result.Output.ReplaceLineEndings());
        Assert.Equal(4, _simulation.World.Roads.Segments.Count);
    }

    [Fact]
    public async Task Road_that_would_overlap_another_is_refused()
    {
        foreach (string line in (string[])["node 0 0", "node 30 0", "road n1 n2", "node 10 0", "node 40 0"])
        {
            await _interpreter.ExecuteAsync(line);
        }

        CommandResult result = await _interpreter.ExecuteAsync("road n3 n4");

        Assert.Contains("would run along s1", result.Output);
        Assert.Single(_simulation.World.Roads.Segments);
        Assert.Equal(5, _publisher.Published.Count);
    }

    [Fact]
    public async Task Road_across_water_at_ground_level_is_refused_but_a_bridge_is_built()
    {
        foreach (string line in (string[])["land water 2 0 2 3", "node 5 15", "node 35 15"])
        {
            await _interpreter.ExecuteAsync(line);
        }

        CommandResult ford = await _interpreter.ExecuteAsync("road n1 n2");
        CommandResult bridge = await _interpreter.ExecuteAsync("road n1 n2 18,15,5 32,15,5");

        Assert.Contains("water at ground level", ford.Output);
        Assert.StartsWith("Added road s1", bridge.Output);
        Assert.Single(_simulation.World.Roads.Segments);
        Assert.Equal(4, _publisher.Published.Count);
    }

    [Fact]
    public async Task Land_and_height_changes_that_would_flood_a_road_are_refused()
    {
        foreach (string line in (string[])["node 5 15", "node 35 15", "road n1 n2"])
        {
            await _interpreter.ExecuteAsync(line);
        }

        CommandResult land = await _interpreter.ExecuteAsync("land water 2 0 2 3");

        Assert.Contains("Road s1 would then run at ground level through water", land.Output);
        Assert.Equal(LandType.Ground, _simulation.World.Land[2, 1]);
        Assert.Equal(3, _publisher.Published.Count);
    }

    [Fact]
    public async Task Remove_deletes_a_road_but_keeps_its_nodes()
    {
        await _interpreter.ExecuteAsync("node 0 0");
        await _interpreter.ExecuteAsync("node 30 0");
        await _interpreter.ExecuteAsync("road n1 n2");

        CommandResult removed = await _interpreter.ExecuteAsync("remove s1");
        CommandResult missing = await _interpreter.ExecuteAsync("remove s1");

        Assert.Equal("Removed road s1.", removed.Output);
        Assert.Contains("There is no road s1", missing.Output);
        Assert.Empty(_simulation.World.Roads.Segments);
        Assert.Equal(2, _simulation.World.Roads.Nodes.Count);
        Assert.Equal(4, _publisher.Published.Count);
    }

    [Fact]
    public async Task Roads_lists_nodes_and_segments()
    {
        Assert.Contains("no roads yet", (await _interpreter.ExecuteAsync("roads")).Output);

        await _interpreter.ExecuteAsync("node 0 0");
        await _interpreter.ExecuteAsync("node 30 40");
        await _interpreter.ExecuteAsync("road n1 n2");
        string output = (await _interpreter.ExecuteAsync("roads")).Output;

        Assert.Contains("n1 at (0, 0, 0)", output);
        Assert.Contains("n2 at (30, 40, 0)", output);
        Assert.Contains("s1 from n1 to n2, 50 m", output);
        Assert.Equal(3, _publisher.Published.Count);
    }

    [Fact]
    public async Task Land_sets_one_cell()
    {
        CommandResult result = await _interpreter.ExecuteAsync("land water 2 1");

        Assert.Equal("Set cell (2, 1) to water.", result.Output);
        Assert.Equal(LandType.Water, _simulation.World.Land[2, 1]);
        Assert.Equal(LandType.Ground, _simulation.World.Land[1, 1]);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Land_sets_a_rectangle_given_in_any_corner_order()
    {
        CommandResult result = await _interpreter.ExecuteAsync("land WATER 2 3 1 0");

        LandGrid land = _simulation.World.Land;
        Assert.Equal("Set 8 cells from (1, 0) to (2, 3) to water.", result.Output);
        for (int y = 0; y < 4; y++)
        {
            Assert.Equal(LandType.Ground, land[0, y]);
            Assert.Equal(LandType.Water, land[1, y]);
            Assert.Equal(LandType.Water, land[2, y]);
            Assert.Equal(LandType.Ground, land[3, y]);
        }
    }

    [Theory]
    [InlineData("land water", "Usage: land")]
    [InlineData("land lava 0 0", "Usage: land")]
    [InlineData("land 1 0 0", "Usage: land")]
    [InlineData("land water 0 0 1", "Usage: land")]
    [InlineData("land water 0 a", "Usage: land")]
    [InlineData("land water 4 0", "go from (0, 0) to (3, 3)")]
    [InlineData("land water 0 0 0 -1", "go from (0, 0) to (3, 3)")]
    public async Task Land_with_invalid_arguments_changes_nothing(string line, string expected)
    {
        CommandResult result = await _interpreter.ExecuteAsync(line);

        Assert.Contains(expected, result.Output);
        Assert.Empty(_publisher.Published);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Assert.Equal(LandType.Ground, _simulation.World.Land[x, y]);
            }
        }
    }

    [Fact]
    public async Task Height_sets_the_ground_height_at_a_corner()
    {
        CommandResult result = await _interpreter.ExecuteAsync("height 4 4 -2.5");

        Assert.Equal("Set the ground height at corner (4, 4) to -2.5 m.", result.Output);
        Assert.Equal(-2.5, _simulation.World.Land.GetCornerElevation(4, 4));
        Assert.Single(_publisher.Published);
    }

    [Theory]
    [InlineData("height 1 1", "Usage: height")]
    [InlineData("height 1 1 high", "Usage: height")]
    [InlineData("height 5 0 1", "go from (0, 0) to (4, 4)")]
    [InlineData("height 0 -1 1", "go from (0, 0) to (4, 4)")]
    public async Task Height_with_invalid_arguments_changes_nothing(string line, string expected)
    {
        CommandResult result = await _interpreter.ExecuteAsync(line);

        Assert.Contains(expected, result.Output);
        Assert.Empty(_publisher.Published);
    }

    private sealed class RecordingPublisher : IStatePublisher
    {
        public List<SimulationSnapshot> Published { get; } = [];

        public Task PublishAsync(SimulationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Published.Add(snapshot);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
