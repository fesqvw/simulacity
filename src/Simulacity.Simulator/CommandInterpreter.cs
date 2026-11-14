using System.Globalization;
using Simulacity.Contracts;
using Simulacity.Core;

namespace Simulacity.Simulator;

/// <summary>The result of running one command line.</summary>
/// <param name="Output">Text to show the user. May be empty.</param>
/// <param name="Quit">Whether the simulator should exit.</param>
public readonly record struct CommandResult(string Output, bool Quit = false);

/// <summary>
/// Runs the simulator's commands (decision 0004). This is the only place the simulation changes,
/// and every change is published to the renderers.
/// </summary>
public sealed class CommandInterpreter(Simulation simulation, IStatePublisher publisher)
{
    /// <summary>The help text listing the available commands.</summary>
    public const string HelpText =
        """
        Commands:
          step [n]   advance the simulation by n steps (default 1)
          status     show the current state
          help       show this list
          quit       exit the simulator
        """;

    /// <summary>Runs one command line typed by the user.</summary>
    public async Task<CommandResult> ExecuteAsync(string line, CancellationToken cancellationToken = default)
    {
        string[] words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return new CommandResult(string.Empty);
        }

        string command = words[0].ToLowerInvariant();
        string[] arguments = words[1..];

        return command switch
        {
            "step" => await StepAsync(arguments, cancellationToken).ConfigureAwait(false),
            "status" => new CommandResult(Status()),
            "help" or "?" => new CommandResult(HelpText),
            "quit" or "exit" => new CommandResult(string.Empty, Quit: true),
            _ => new CommandResult($"Unknown command '{words[0]}'. Type 'help' for the list of commands."),
        };
    }

    /// <summary>Sends the current state to the renderers.</summary>
    public Task PublishAsync(CancellationToken cancellationToken = default) =>
        publisher.PublishAsync(new SimulationSnapshot(simulation.Tick), cancellationToken);

    private async Task<CommandResult> StepAsync(string[] arguments, CancellationToken cancellationToken)
    {
        long steps = 1;
        if (arguments.Length > 1
            || (arguments.Length == 1
                && (!long.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out steps) || steps < 1)))
        {
            return new CommandResult("Usage: step [n], where n is a whole number of at least 1.");
        }

        for (long i = 0; i < steps; i++)
        {
            simulation.Step();
        }

        await PublishAsync(cancellationToken).ConfigureAwait(false);
        return new CommandResult(Status());
    }

    private string Status() => $"Tick {simulation.Tick}";
}
