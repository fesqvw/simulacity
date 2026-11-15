using Simulacity.Core;
using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;
using Simulacity.Simulator;

namespace Simulacity.Tests;

/// <summary>The commands used to build maps, and map scripts (decision 0013).</summary>
public class MapCommandTests
{
    private readonly RecordingPublisher _publisher = new();
    private readonly CommandInterpreter _interpreter;

    public MapCommandTests() => _interpreter = new CommandInterpreter(new Simulation(new World(new LandGrid(10, 10, 10))), _publisher);

    private World World => _interpreter.Simulation.World;

    [Fact]
    public async Task New_starts_an_empty_map_of_the_given_size()
    {
        await _interpreter.ExecuteAsync("node 5 5");
        await _interpreter.ExecuteAsync("step");

        CommandResult result = await _interpreter.ExecuteAsync("new 30 20 5");

        Assert.True(result.Succeeded);
        Assert.Equal((30, 20, 5.0), (World.Land.Width, World.Land.Height, World.Land.CellSize));
        Assert.Empty(World.Roads.Nodes);
        Assert.Equal(0, _interpreter.Simulation.Tick);
        Assert.Equal(30, _publisher.Published[^1].Land.Width);
    }

    [Theory]
    [InlineData("new")]
    [InlineData("new 0 10")]
    [InlineData("new 10 10 0")]
    [InlineData("new 10000 10")]
    public async Task New_with_invalid_sizes_keeps_the_map(string line)
    {
        CommandResult result = await _interpreter.ExecuteAsync(line);

        Assert.False(result.Succeeded);
        Assert.Equal(10, World.Land.Width);
    }

    [Fact]
    public async Task Road_ends_can_be_points_that_reuse_or_add_nodes()
    {
        await _interpreter.ExecuteAsync("node 10 10");

        CommandResult result = await _interpreter.ExecuteAsync("road 10,10 90,10");

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(2, World.Roads.Nodes.Count);
        RoadSegment road = Assert.Single(World.Roads.Segments);
        Assert.Equal(new NodeId(1), road.Start);
        Assert.Contains("Added node n2 at (90, 10, 0).", result.Output);
    }

    [Fact]
    public async Task A_road_ending_on_another_road_by_point_makes_a_t_junction()
    {
        await _interpreter.ExecuteAsync("road 10,50 90,50");

        await _interpreter.ExecuteAsync("road 50,10 50,50");

        RoadNode junction = Assert.Single(World.Roads.Nodes, node => node.Position == new Point3(50, 50, 0));
        Assert.Equal(3, World.Roads.SegmentsAt(junction.Id).Count);
    }

    [Fact]
    public async Task A_refused_road_takes_back_the_nodes_it_added_for_its_ends()
    {
        await _interpreter.ExecuteAsync("land water 5 0 5 9");

        CommandResult result = await _interpreter.ExecuteAsync("road 10,50 90,50");

        Assert.False(result.Succeeded);
        Assert.Empty(World.Roads.Nodes);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Height_sets_a_rectangle_of_corners()
    {
        CommandResult result = await _interpreter.ExecuteAsync("height 2 3 4 5 7.5");

        Assert.Equal("Set the ground height at 9 corners from (2, 3) to (4, 5) to 7.5 m.", result.Output);
        Assert.Equal(7.5, World.Land.GetCornerElevation(4, 5));
        Assert.Equal(0, World.Land.GetCornerElevation(5, 5));
    }

    [Fact]
    public async Task Hill_raises_a_smooth_bump_that_adds_to_the_ground()
    {
        await _interpreter.ExecuteAsync("height 0 0 10 10 2");

        CommandResult result = await _interpreter.ExecuteAsync("hill 50 50 40 10");

        Assert.True(result.Succeeded, result.Output);
        Assert.Equal(12, World.Land.GetCornerElevation(5, 5), precision: 9);
        Assert.Equal(7, World.Land.GetCornerElevation(5, 7), precision: 9); // Halfway out: half the height.
        Assert.Equal(2, World.Land.GetCornerElevation(5, 9), precision: 9); // At the radius: nothing.
    }

    [Fact]
    public async Task Raising_a_riverbed_up_to_a_bridge_deck_is_refused()
    {
        await _interpreter.ExecuteAsync("land water 4 0 5 9");
        await _interpreter.ExecuteAsync("road 10,50 90,50 35,50,3 65,50,3");

        CommandResult result = await _interpreter.ExecuteAsync("hill 50 50 30 3");

        Assert.False(result.Succeeded);
        Assert.Equal(0, World.Land.GetCornerElevation(5, 5));
    }

    [Fact]
    public async Task A_script_ignores_comments_and_blank_lines_and_publishes_once()
    {
        ScriptResult result = await _interpreter.RunScriptAsync(
            ["# A comment", "", "new 20 20  # with a comment after it", "road 10,10 100,10", "   "],
            "test");

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(2, result.Commands);
        Assert.Single(_publisher.Published);
        Assert.Single(World.Roads.Segments);
    }

    [Fact]
    public async Task A_script_stops_at_the_first_failing_line_and_says_which()
    {
        ScriptResult result = await _interpreter.RunScriptAsync(["node 5 5", "bogus", "node 6 6"], "test");

        Assert.False(result.Succeeded);
        Assert.StartsWith("test, line 2: Unknown command 'bogus'", result.Message);
        Assert.Single(World.Roads.Nodes);
        Assert.Single(_publisher.Published); // What the earlier lines did is still sent.
    }

    [Fact]
    public async Task Load_runs_a_script_file()
    {
        string path = Path.Combine(Path.GetTempPath(), $"simulacity-{Guid.NewGuid():N}.txt");
        await File.WriteAllLinesAsync(path, ["new 5 5", "node 10 10"]);
        try
        {
            CommandResult result = await _interpreter.ExecuteAsync($"load {path}");

            Assert.True(result.Succeeded, result.Output);
            Assert.Equal(5, World.Land.Width);
            Assert.Single(World.Roads.Nodes);
            Assert.Single(_publisher.Published);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task A_script_that_loads_itself_stops_instead_of_looping()
    {
        string path = Path.Combine(Path.GetTempPath(), $"simulacity-{Guid.NewGuid():N}.txt");
        await File.WriteAllLinesAsync(path, [$"load {path}"]);
        try
        {
            CommandResult result = await _interpreter.ExecuteAsync($"load {path}");

            Assert.False(result.Succeeded);
            Assert.Contains("more than 8 deep", result.Output);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Load_reports_a_missing_file()
    {
        CommandResult result = await _interpreter.ExecuteAsync("load no-such-map.txt");

        Assert.False(result.Succeeded);
        Assert.StartsWith("Can't read 'no-such-map.txt'", result.Output);
    }
}
