using Simulacity.Contracts;
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
        int water = land.Cells.Count(cell => cell == LandKind.Water);
        Draw(
            $"""
            Tick {snapshot.Tick}
            Map: {land.Width} × {land.Height} cells of {land.CellSize} m, {water} of them water
            Road nodes: {snapshot.Roads.Nodes.Count}, road segments: {snapshot.Roads.Segments.Count}
            """);
    }
}
catch (OperationCanceledException) when (exit.IsCancellationRequested)
{
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
