using Simulacity.Contracts;
using Simulacity.Renderer.Console;
using Simulacity.Transport.NamedPipes;

// The console renderer: only draws the state the simulator sends it. It never runs the simulation,
// keeps its own state, or accepts commands (decision 0004). Start it by hand, before or after the
// simulator (decision 0005). Press Ctrl+C to exit.

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    exit.Cancel();
};

await using var receiver = new NamedPipeStateReceiver();
receiver.ConnectionChanged += connected =>
{
    if (!connected)
    {
        Draw("Simulator disconnected. Waiting for it to start again...");
    }
};

Draw("Waiting for the simulator to start...");

try
{
    await foreach (SimulationSnapshot snapshot in receiver.ReceiveAsync(exit.Token))
    {
        LandSnapshot land = snapshot.Land;
        (int columns, int rows) = MapSpace();
        string info =
            $"Tick {snapshot.Tick}   Map {land.Width} × {land.Height} cells of {land.CellSize} m   "
            + $"Roads: {snapshot.Roads.Nodes.Count} nodes, {snapshot.Roads.Segments.Count} segments";
        IReadOnlyList<string> map = MapView.Draw(snapshot, columns, rows);
        Draw(string.Join(Environment.NewLine, [info, string.Empty, .. map, string.Empty, MapView.Legend]));
    }
}
catch (OperationCanceledException) when (exit.IsCancellationRequested)
{
}

// The space left for the map once the title, the info line, the legend and the blank lines between them are drawn.
static (int Columns, int Rows) MapSpace()
{
    const int linesAroundMap = 7;
    if (Console.IsOutputRedirected)
    {
        return (120, 40);
    }

    return (Math.Max(Console.WindowWidth - 1, 2), Math.Max(Console.WindowHeight - linesAroundMap, 1));
}

static void Draw(string text)
{
    if (!Console.IsOutputRedirected)
    {
        Console.Clear();
    }

    Console.WriteLine("Simulacity renderer (Ctrl+C to exit)");
    Console.WriteLine();
    Console.WriteLine(text);
}
