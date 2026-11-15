using Simulacity.Core;
using Simulacity.Core.Land;
using Simulacity.Simulator;
using Simulacity.Transport.NamedPipes;

// The simulator: reads commands, owns and runs the simulation, and sends its state to renderers
// (decisions 0004 and 0005). Start the renderer by hand in another terminal to watch it.
//
// It starts with the starting map (decision 0013). Pass --map <file> to load another map script,
// or --empty to start with an empty 64 × 32 map.

const string usage = "Usage: Simulacity.Simulator [--map <file> | --empty]";
string? mapPath = Path.Combine(AppContext.BaseDirectory, "maps", "starter.txt");
switch (args)
{
    case []:
        break;
    case ["--empty"]:
        mapPath = null;
        break;
    case ["--map", string path]:
        mapPath = path;
        break;
    default:
        Console.Error.WriteLine(usage);
        return 2;
}

await using var publisher = new NamedPipeStatePublisher();
var interpreter = new CommandInterpreter(new Simulation(new World(new LandGrid(64, 32, 10))), publisher);

Console.WriteLine("Simulacity simulator. Type 'help' for the list of commands.");
Console.WriteLine("Run the renderer in another terminal to watch the simulation.");

if (mapPath is not null)
{
    string[] lines;
    try
    {
        lines = await File.ReadAllLinesAsync(mapPath);
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"Can't read the map '{mapPath}': {exception.Message}");
        return 1;
    }

    ScriptResult result = await interpreter.RunScriptAsync(lines, Path.GetFileName(mapPath));
    Console.WriteLine(result.Succeeded ? $"Loaded the map: {result.Message}" : $"The map didn't load completely. {result.Message}");
}

await interpreter.PublishAsync();

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

return 0;
