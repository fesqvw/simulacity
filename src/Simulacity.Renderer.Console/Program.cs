using Simulacity.Contracts;
using Simulacity.Renderer.Console;
using Simulacity.Transport.NamedPipes;

// The console renderer: only draws the state the simulator sends it. It never runs the simulation
// or changes it. Its only state is which part of the map is on screen, which the keys below move
// (decision 0014). Start it by hand, before or after the simulator (decision 0005).

using var exit = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    exit.Cancel();
};

var screen = new Screen();
await using var receiver = new NamedPipeStateReceiver();
receiver.ConnectionChanged += connected =>
{
    if (!connected)
    {
        screen.ShowMessage("Simulator disconnected. Waiting for it to start again...");
    }
};

screen.ShowMessage("Waiting for the simulator to start...");
Task keys = Task.Run(() => screen.ReadKeysAsync(exit), exit.Token);

try
{
    await foreach (SimulationSnapshot snapshot in receiver.ReceiveAsync(exit.Token))
    {
        screen.Show(snapshot);
    }
}
catch (OperationCanceledException) when (exit.IsCancellationRequested)
{
}

exit.Cancel();
try
{
    await keys;
}
catch (OperationCanceledException)
{
}

/// <summary>
/// What's on screen: the latest snapshot and the viewport onto it. Snapshots and key presses
/// arrive on different threads, so every change to the screen takes the lock.
/// </summary>
internal sealed class Screen
{
    private const string Title = "Simulacity renderer";
    private const string Controls = "WASD or arrows: move   + -: zoom   F: fit the map   Q or Ctrl+C: exit";

    // The title, the info line, the legend, the controls and the blank lines between them.
    private const int LinesAroundMap = 8;

    private readonly Lock _lock = new();
    private SimulationSnapshot? _snapshot;
    private MapViewport? _view;
    private (int Columns, int Rows) _space;
    private (int Width, int Height) _window;

    public void ShowMessage(string message)
    {
        lock (_lock)
        {
            _snapshot = null;
            Write([message]);
        }
    }

    public void Show(SimulationSnapshot snapshot)
    {
        lock (_lock)
        {
            // A different map, such as after the simulator's 'new' command, starts with a fresh view.
            if (_view is null || _snapshot is null || !SameMap(_snapshot.Land, snapshot.Land))
            {
                _space = MapSpace();
                _view = MapViewport.Initial(snapshot.Land, _space.Columns, _space.Rows);
            }

            _snapshot = snapshot;
            Redraw();
        }
    }

    /// <summary>Moves the view on key presses, and redraws when the window is resized, until cancelled.</summary>
    public async Task ReadKeysAsync(CancellationTokenSource exit)
    {
        bool interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;
        while (!exit.IsCancellationRequested)
        {
            if (interactive && Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
                {
                    exit.Cancel();
                    return;
                }

                lock (_lock)
                {
                    Handle(key);
                }

                continue;
            }

            if (interactive)
            {
                lock (_lock)
                {
                    if (_snapshot is not null && WindowSize() != _window)
                    {
                        Redraw();
                    }
                }
            }

            await Task.Delay(30, exit.Token).ConfigureAwait(false);
        }
    }

    private void Handle(ConsoleKeyInfo key)
    {
        if (_snapshot is null || _view is null)
        {
            return;
        }

        LandSnapshot land = _snapshot.Land;
        (int columns, int rows) = _space;
        MapViewport? moved = (key.Key, key.KeyChar) switch
        {
            (ConsoleKey.W or ConsoleKey.UpArrow, _) => _view.Pan(0, 1, land, columns, rows),
            (ConsoleKey.S or ConsoleKey.DownArrow, _) => _view.Pan(0, -1, land, columns, rows),
            (ConsoleKey.A or ConsoleKey.LeftArrow, _) => _view.Pan(-1, 0, land, columns, rows),
            (ConsoleKey.D or ConsoleKey.RightArrow, _) => _view.Pan(1, 0, land, columns, rows),
            (ConsoleKey.OemPlus or ConsoleKey.Add, _) or (_, '+' or '=') => _view.Zoom(1, land, columns, rows),
            (ConsoleKey.OemMinus or ConsoleKey.Subtract, _) or (_, '-' or '_') => _view.Zoom(-1, land, columns, rows),
            (ConsoleKey.F, _) => MapViewport.Fit(land, columns, rows),
            _ => null,
        };

        if (moved is not null)
        {
            _view = moved;
            Redraw();
        }
    }

    private void Redraw()
    {
        if (_snapshot is null || _view is null)
        {
            return;
        }

        // After a resize, keep the same place in the middle of the view.
        (int Columns, int Rows) space = MapSpace();
        if (space != _space)
        {
            (double x, double y) = _view.Centre(_space.Columns, _space.Rows);
            _view = _view.CentredOn(x, y, space.Columns, space.Rows);
            _space = space;
        }

        LandSnapshot land = _snapshot.Land;
        (double centreX, double centreY) = _view.Centre(_space.Columns, _space.Rows);
        string info =
            $"Tick {_snapshot.Tick}   Map {land.Width} × {land.Height} cells of {land.CellSize} m   "
            + $"Roads: {_snapshot.Roads.Nodes.Count} nodes, {_snapshot.Roads.Segments.Count} segments   "
            + $"View centre ({centreX:0}, {centreY:0}) m, {_view.MetresPerRow:0.##} m per row";
        IReadOnlyList<string> map = MapView.Draw(_snapshot, _view, _space.Columns, _space.Rows);
        Write([info, string.Empty, .. map, string.Empty, MapView.Legend, Controls]);
    }

    /// <summary>Draws over the whole window from the top, so a redraw doesn't flicker.</summary>
    private void Write(IReadOnlyList<string> lines)
    {
        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(Title);
            Console.WriteLine();
            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }

            return;
        }

        (int width, int height) = WindowSize();
        if ((width, height) != _window)
        {
            Console.Clear();
            _window = (width, height);
        }

        Console.CursorVisible = false;
        Console.SetCursorPosition(0, 0);
        var text = new System.Text.StringBuilder();
        List<string> all = [Title, string.Empty, .. lines];
        for (int row = 0; row < Math.Min(all.Count, height - 1); row++)
        {
            string line = all[row].Length >= width ? all[row][..(width - 1)] : all[row];
            text.Append(line.PadRight(width - 1)).Append('\n');
        }

        for (int row = all.Count; row < height - 1; row++)
        {
            text.Append(new string(' ', width - 1)).Append('\n');
        }

        Console.Write(text.ToString());
    }

    private static bool SameMap(LandSnapshot a, LandSnapshot b) =>
        a.Width == b.Width && a.Height == b.Height && a.CellSize == b.CellSize;

    private static (int Width, int Height) WindowSize() =>
        Console.IsOutputRedirected ? (121, 48) : (Math.Max(Console.WindowWidth, 3), Math.Max(Console.WindowHeight, LinesAroundMap + 2));

    /// <summary>The space left for the map once everything around it is drawn.</summary>
    private static (int Columns, int Rows) MapSpace()
    {
        (int width, int height) = WindowSize();
        return (Math.Max(width - 1, 2), Math.Max(height - LinesAroundMap, 1));
    }
}
