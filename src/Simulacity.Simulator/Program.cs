using Simulacity.Core;
using Simulacity.Core.Land;
using Simulacity.Simulator;
using Simulacity.Transport.NamedPipes;

// The simulator: reads commands, owns and runs the simulation, and sends its state to renderers
// (decisions 0004 and 0005). Start the renderer by hand in another terminal to watch it.

await using var publisher = new NamedPipeStatePublisher();
var interpreter = new CommandInterpreter(new Simulation(new World(new LandGrid(64, 32, 10))), publisher);
await interpreter.PublishAsync();

Console.WriteLine("Simulacity simulator. Type 'help' for the list of commands.");
Console.WriteLine("Run the renderer in another terminal to watch the simulation.");

while (true)
{
    Console.Write("> ");
    string? line = Console.ReadLine();
    if (line is null)
    {
        break; // End of input.
    }

    CommandResult result = await interpreter.ExecuteAsync(line);
    if (result.Output.Length > 0)
    {
        Console.WriteLine(result.Output);
    }

    if (result.Quit)
    {
        break;
    }
}
