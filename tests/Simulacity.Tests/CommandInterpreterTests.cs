using Simulacity.Contracts;
using Simulacity.Core;
using Simulacity.Core.Land;
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
        Assert.Equal([new SimulationSnapshot(1)], _publisher.Published);
    }

    [Fact]
    public async Task Step_with_a_count_advances_that_many_ticks_and_publishes_once()
    {
        await _interpreter.ExecuteAsync("  STEP   5 ");

        Assert.Equal(5, _simulation.Tick);
        Assert.Equal([new SimulationSnapshot(5)], _publisher.Published);
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
