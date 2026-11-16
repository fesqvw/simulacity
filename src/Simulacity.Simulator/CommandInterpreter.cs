using System.Globalization;
using System.Text;
using Simulacity.Contracts;
using Simulacity.Core;
using Simulacity.Core.Geometry;
using Simulacity.Core.Land;
using Simulacity.Core.Roads;

namespace Simulacity.Simulator;

/// <summary>The result of running one command line.</summary>
/// <param name="Output">Text to show the user. May be empty.</param>
/// <param name="Quit">Whether the simulator should exit.</param>
/// <param name="Succeeded">Whether the command did what it was asked. A refused or mistyped command didn't.</param>
public readonly record struct CommandResult(string Output, bool Quit = false, bool Succeeded = true);

/// <summary>The result of running a map script (decision 0013).</summary>
/// <param name="Succeeded">Whether every command in it succeeded.</param>
/// <param name="Commands">How many commands ran successfully.</param>
/// <param name="Message">A summary, or which line failed and why.</param>
public readonly record struct ScriptResult(bool Succeeded, int Commands, string Message);

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
          step [n]                     advance the simulation by n steps (default 1)
          status                       show the current state
          new <width> <height> [cell]  start an empty map of width × height cells
                                       (cell size in metres, default 10)
          load <file>                  run the commands in a map script
          node <x> <y> [z]             add a road node at a world position, in metres
                                       (z defaults to the ground height there)
          road <from> <to> [x,y[,z]]…  add a road, optionally through points. The ends are
                                       nodes such as n1, or points x,y[,z], which use the node
                                       there or add one. It joins roads it crosses at the same height
          remove <segment>             remove a road segment (its nodes stay)
          roads                        list the road nodes and segments
          land <type> <x> <y> [<x2> <y2>]
                                       set a land cell, or a rectangle of cells, to ground or water
          height <i> <j> [<i2> <j2>] <z>
                                       set the ground height at a cell corner, or a rectangle
                                       of corners, in metres
          hill <x> <y> <radius> <height>
                                       raise a smooth hill (or, with a negative height, a valley)
                                       centred on a world position, in metres
          help                         show this list
          quit                         exit the simulator

        Water can only be crossed on a bridge or in a tunnel: a road more than 1 m above
        or below the ground or water. Roads become bridges and tunnels by their height.
        Nodes are written n1, n2…, and segments s1, s2… (the letter is optional).
        Land cells and corners are counted from 0 at the south-west corner of the map.
        In a map script, each line is a command, and everything after # is a comment.
        """;

    private const int MaxScriptDepth = 8;

    private int _scriptDepth;

    /// <summary>The simulation being run. The <c>new</c> command replaces it.</summary>
    public Simulation Simulation { get; private set; } = simulation ?? throw new ArgumentNullException(nameof(simulation));

    private World World => Simulation.World;

    /// <summary>Runs one command line typed by the user, and publishes the state if it changed.</summary>
    public async Task<CommandResult> ExecuteAsync(string line, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(line);

        (Outcome outcome, bool quit) = Execute(line);
        if (outcome.Changed)
        {
            await PublishAsync(cancellationToken).ConfigureAwait(false);
        }

        return new CommandResult(outcome.Output, quit, !outcome.Failed);
    }

    /// <summary>
    /// Runs a map script: one command per line, with <c>#</c> starting a comment (decision 0013). It
    /// stops at the first command that fails, keeping what the earlier ones did. The state is
    /// published once at the end.
    /// </summary>
    /// <param name="lines">The script's lines.</param>
    /// <param name="source">What to call the script in messages, such as its file name.</param>
    /// <param name="cancellationToken">Cancels publishing.</param>
    public async Task<ScriptResult> RunScriptAsync(IEnumerable<string> lines, string source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(source);

        (ScriptResult result, bool changed) = RunScript(lines, source);
        if (changed)
        {
            await PublishAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>Sends the current state to the renderers.</summary>
    public Task PublishAsync(CancellationToken cancellationToken = default) =>
        publisher.PublishAsync(SnapshotBuilder.From(Simulation), cancellationToken);

    private (Outcome Outcome, bool Quit) Execute(string line)
    {
        int comment = line.IndexOf('#', StringComparison.Ordinal);
        string[] words = (comment >= 0 ? line[..comment] : line).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return (Ok(string.Empty), false);
        }

        string command = words[0].ToLowerInvariant();
        string[] arguments = words[1..];

        Outcome outcome = command switch
        {
            "step" => Step(arguments),
            "status" => Ok(Status()),
            "new" => NewMap(arguments),
            "load" => Load(arguments),
            "node" => AddNode(arguments),
            "road" => AddRoad(arguments),
            "remove" => RemoveRoad(arguments),
            "roads" => Ok(ListRoads()),
            "land" => PaintLand(arguments),
            "height" => SetHeight(arguments),
            "hill" => RaiseHill(arguments),
            "help" or "?" => Ok(HelpText),
            "quit" or "exit" => Ok(string.Empty),
            _ => Fail($"Unknown command '{words[0]}'. Type 'help' for the list of commands."),
        };

        return (outcome, command is "quit" or "exit");
    }

    private (ScriptResult Result, bool Changed) RunScript(IEnumerable<string> lines, string source)
    {
        if (_scriptDepth >= MaxScriptDepth)
        {
            return (new ScriptResult(false, 0, $"{source}: scripts load each other more than {MaxScriptDepth} deep."), false);
        }

        _scriptDepth++;
        try
        {
            int number = 0, commands = 0;
            bool changed = false;
            foreach (string line in lines)
            {
                number++;
                (Outcome outcome, bool quit) = Execute(line);
                changed |= outcome.Changed;
                if (outcome.Failed)
                {
                    return (new ScriptResult(false, commands, $"{source}, line {number}: {outcome.Output}"), changed);
                }

                if (quit)
                {
                    return (new ScriptResult(false, commands, $"{source}, line {number}: a map script can't quit the simulator."), changed);
                }

                if (outcome.Output.Length > 0)
                {
                    commands++;
                }
            }

            return (new ScriptResult(true, commands, $"Ran {commands} commands from {source}."), changed);
        }
        finally
        {
            _scriptDepth--;
        }
    }

    private Outcome Step(string[] arguments)
    {
        long steps = 1;
        if (arguments.Length > 1
            || (arguments.Length == 1
                && (!long.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out steps) || steps < 1)))
        {
            return Fail("Usage: step [n], where n is a whole number of at least 1.");
        }

        for (long i = 0; i < steps; i++)
        {
            Simulation.Step();
        }

        return Ok(Status(), changed: true);
    }

    private string Status() => $"Tick {Simulation.Tick}";

    private Outcome NewMap(string[] arguments)
    {
        const int maxCells = 4000;
        double cellSize = 10;
        if (arguments.Length is < 2 or > 3
            || !TryParseInt(arguments[0], out int width)
            || !TryParseInt(arguments[1], out int height)
            || (arguments.Length == 3 && !TryParseNumber(arguments[2], out cellSize))
            || width is < 1 or > maxCells
            || height is < 1 or > maxCells
            || cellSize is <= 0 or > 1000)
        {
            return Fail($"Usage: new <width> <height> [cell size], with 1 to {maxCells} cells each way and cells up to 1000 m.");
        }

        Simulation = new Simulation(new World(new LandGrid(width, height, cellSize)));
        return Ok(
            $"Started an empty map of {width} × {height} cells of {Format(cellSize)} m "
            + $"({Format(width * cellSize)} m × {Format(height * cellSize)} m).",
            changed: true);
    }

    private Outcome Load(string[] arguments)
    {
        if (arguments.Length != 1)
        {
            return Fail("Usage: load <file>, with no spaces in the path.");
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(arguments[0]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Fail($"Can't read '{arguments[0]}': {exception.Message}");
        }

        (ScriptResult result, bool changed) = RunScript(lines, arguments[0]);
        return new Outcome(result.Message, changed, !result.Succeeded);
    }

    private Outcome AddNode(string[] arguments)
    {
        const string usage = "Usage: node <x> <y> [z], in metres.";
        if (arguments.Length is < 2 or > 3
            || !TryParseNumber(arguments[0], out double x)
            || !TryParseNumber(arguments[1], out double y))
        {
            return Fail(usage);
        }

        double z;
        if (arguments.Length == 3)
        {
            if (!TryParseNumber(arguments[2], out z))
            {
                return Fail(usage);
            }
        }
        else if (!World.Land.TryGetGroundElevation(new Point3(x, y, 0), out z))
        {
            return Fail($"({Format(x)}, {Format(y)}) is off the map, so there is no ground height there. Give a z.");
        }

        RoadNode node = World.Roads.AddNode(new Point3(x, y, z));
        return Ok($"Added node {node.Id} at {FormatPoint(node.Position)}.", changed: true);
    }

    private Outcome AddRoad(string[] arguments)
    {
        const string usage = "Usage: road <from> <to> [x,y[,z]]…, where <from> and <to> are nodes such as n1, or points x,y[,z].";
        if (arguments.Length < 2)
        {
            return Fail(usage);
        }

        var points = new List<Point3>();
        foreach (string argument in arguments[2..])
        {
            if (!TryParsePoint(argument, out Point3 point, out string? error))
            {
                return Fail(error);
            }

            points.Add(point);
        }

        var addedNodes = new List<RoadNode>();
        NodeId end = default;
        Outcome? failure = ResolveEnd(arguments[0], addedNodes, out NodeId start);
        failure ??= ResolveEnd(arguments[1], addedNodes, out end);
        if (failure is null && start == end)
        {
            failure = Fail("A road must join two different nodes.");
        }

        RoadAddition? addition = null;
        if (failure is null)
        {
            try
            {
                addition = World.AddRoad(start, end, new PolylineShape(points));
            }
            catch (ArgumentException exception)
            {
                failure = Fail(exception.Message);
            }
        }

        if (failure is { } failed)
        {
            // Take back the nodes this command added for its ends, so a refused road leaves nothing behind.
            foreach (RoadNode node in addedNodes)
            {
                World.Roads.RemoveNode(node.Id);
            }

            return failed;
        }

        var text = new StringBuilder();
        foreach (RoadNode node in addedNodes)
        {
            text.Append(CultureInfo.InvariantCulture, $"Added node {node.Id} at {FormatPoint(node.Position)}.").AppendLine();
        }

        text.Append(DescribeAddition(start, end, addition!));
        return Ok(text.ToString(), changed: true);
    }

    /// <summary>Finds the node a road end names: a node ID, or a point where a node is found or added.</summary>
    /// <returns>A failure, or null if the end was found.</returns>
    private Outcome? ResolveEnd(string text, List<RoadNode> addedNodes, out NodeId id)
    {
        id = default;
        if (!text.Contains(',', StringComparison.Ordinal))
        {
            if (!TryParseId(text, 'n', out int value))
            {
                return Fail($"'{text}' isn't a node or a point. Write nodes as n1, and points as x,y or x,y,z.");
            }

            id = new NodeId(value);
            return World.Roads.ContainsNode(id) ? null : Fail($"There is no node {id}. Type 'roads' to list them.");
        }

        if (!TryParsePoint(text, out Point3 point, out string? error))
        {
            return Fail(error);
        }

        if (World.Roads.TryFindNode(point, out RoadNode? existing))
        {
            id = existing.Id;
            return null;
        }

        RoadNode added = World.Roads.AddNode(point);
        addedNodes.Add(added);
        id = added.Id;
        return null;
    }

    private string DescribeAddition(NodeId start, NodeId end, RoadAddition addition)
    {
        RoadNetwork roads = World.Roads;
        double length = addition.Segments.Sum(segment => roads.LengthOf(segment.Id));
        var text = new StringBuilder();
        text.Append(addition.Segments.Count == 1
            ? $"Added road {addition.Segments[0].Id} from {start} to {end}, {Format(length)} m long."
            : $"Added road from {start} to {end} as {JoinIds(addition.Segments)}, {Format(length)} m long.");

        foreach (RoadNode node in addition.NewNodes)
        {
            text.AppendLine().Append(CultureInfo.InvariantCulture, $"Added junction {node.Id} at {FormatPoint(node.Position)}.");
        }

        foreach (RoadSplit split in addition.Splits)
        {
            text.AppendLine().Append(CultureInfo.InvariantCulture, $"Split road {split.Removed} into {JoinIds(split.Pieces)}.");
        }

        return text.ToString();
    }

    private static string JoinIds(IReadOnlyList<RoadSegment> segments) =>
        segments.Count == 2
            ? $"{segments[0].Id} and {segments[1].Id}"
            : string.Join(", ", segments.Select(segment => segment.Id));

    private Outcome RemoveRoad(string[] arguments)
    {
        if (arguments.Length != 1 || !TryParseId(arguments[0], 's', out int value))
        {
            return Fail("Usage: remove <segment>, such as remove s1.");
        }

        var id = new SegmentId(value);
        return World.Roads.RemoveSegment(id)
            ? Ok($"Removed road {id}.", changed: true)
            : Fail($"There is no road {id}. Type 'roads' to list them.");
    }

    private string ListRoads()
    {
        RoadNetwork roads = World.Roads;
        if (roads.Nodes.Count == 0)
        {
            return "There are no roads yet. Add nodes with 'node' and join them with 'road'.";
        }

        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"Nodes ({roads.Nodes.Count}):");
        foreach (RoadNode node in roads.Nodes.OrderBy(node => node.Id.Value))
        {
            text.AppendLine().Append(CultureInfo.InvariantCulture, $"  {node.Id} at {FormatPoint(node.Position)}");
        }

        text.AppendLine().Append(CultureInfo.InvariantCulture, $"Roads ({roads.Segments.Count}):");
        foreach (RoadSegment segment in roads.Segments.OrderBy(segment => segment.Id.Value))
        {
            text.AppendLine().Append(
                CultureInfo.InvariantCulture,
                $"  {segment.Id} from {segment.Start} to {segment.End}, {Format(roads.LengthOf(segment.Id))} m");
        }

        return text.ToString();
    }

    private Outcome PaintLand(string[] arguments)
    {
        const string usage = "Usage: land <ground|water> <x> <y> [<x2> <y2>], where x and y are cell numbers.";
        if (arguments.Length is not (3 or 5)
            || !Enum.TryParse(arguments[0], ignoreCase: true, out LandType type)
            || !Enum.IsDefined(type)
            || int.TryParse(arguments[0], out _)
            || !TryParseRectangle(arguments[1..], out int x1, out int y1, out int x2, out int y2))
        {
            return Fail(usage);
        }

        LandGrid land = World.Land;
        if (!land.Contains(x1, y1) || !land.Contains(x2, y2))
        {
            return Fail($"The map's cells go from (0, 0) to ({land.Width - 1}, {land.Height - 1}).");
        }

        try
        {
            World.SetLand(x1, y1, x2, y2, type);
        }
        catch (InvalidOperationException exception)
        {
            return Fail(exception.Message);
        }

        (int west, int east) = (Math.Min(x1, x2), Math.Max(x1, x2));
        (int south, int north) = (Math.Min(y1, y2), Math.Max(y1, y2));

        int count = (east - west + 1) * (north - south + 1);
        string name = type.ToString().ToLowerInvariant();
        return Ok(
            count == 1
                ? $"Set cell ({west}, {south}) to {name}."
                : $"Set {count} cells from ({west}, {south}) to ({east}, {north}) to {name}.",
            changed: true);
    }

    private Outcome SetHeight(string[] arguments)
    {
        const string usage = "Usage: height <i> <j> [<i2> <j2>] <z>, where i and j are corner numbers and z is in metres.";
        if (arguments.Length is not (3 or 5)
            || !TryParseRectangle(arguments[..^1], out int i1, out int j1, out int i2, out int j2)
            || !TryParseNumber(arguments[^1], out double z))
        {
            return Fail(usage);
        }

        LandGrid land = World.Land;
        foreach ((int i, int j) in (ReadOnlySpan<(int, int)>)[(i1, j1), (i2, j2)])
        {
            if (i < 0 || i > land.Width || j < 0 || j > land.Height)
            {
                return Fail($"The map's corners go from (0, 0) to ({land.Width}, {land.Height}).");
            }
        }

        (int west, int east) = (Math.Min(i1, i2), Math.Max(i1, i2));
        (int south, int north) = (Math.Min(j1, j2), Math.Max(j1, j2));
        var corners = new List<(int, int, double)>();
        for (int j = south; j <= north; j++)
        {
            for (int i = west; i <= east; i++)
            {
                corners.Add((i, j, z));
            }
        }

        try
        {
            World.SetCornerElevations(corners);
        }
        catch (InvalidOperationException exception)
        {
            return Fail(exception.Message);
        }

        return Ok(
            corners.Count == 1
                ? $"Set the ground height at corner ({west}, {south}) to {Format(z)} m."
                : $"Set the ground height at {corners.Count} corners from ({west}, {south}) to ({east}, {north}) to {Format(z)} m.",
            changed: true);
    }

    private Outcome RaiseHill(string[] arguments)
    {
        if (arguments.Length != 4
            || !TryParseNumber(arguments[0], out double x)
            || !TryParseNumber(arguments[1], out double y)
            || !TryParseNumber(arguments[2], out double radius)
            || !TryParseNumber(arguments[3], out double height)
            || radius <= 0)
        {
            return Fail("Usage: hill <x> <y> <radius> <height>, in metres, with a radius above 0.");
        }

        // A smooth bump: the full height in the middle, easing to nothing at the radius.
        // It adds to the ground that's already there, so hills can overlap.
        LandGrid land = World.Land;
        var corners = new List<(int, int, double)>();
        for (int j = 0; j <= land.Height; j++)
        {
            for (int i = 0; i <= land.Width; i++)
            {
                double dx = (i * land.CellSize) - x, dy = (j * land.CellSize) - y;
                double distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance < radius)
                {
                    double rise = height * (1 + Math.Cos(Math.PI * distance / radius)) / 2;
                    corners.Add((i, j, land.GetCornerElevation(i, j) + rise));
                }
            }
        }

        if (corners.Count == 0)
        {
            return Fail("The hill doesn't reach any corner of the map. Give a larger radius or move it onto the map.");
        }

        try
        {
            World.SetCornerElevations(corners);
        }
        catch (InvalidOperationException exception)
        {
            return Fail(exception.Message);
        }

        string kind = height >= 0 ? "hill" : "valley";
        return Ok(
            $"Raised a {kind} of {Format(Math.Abs(height))} m at ({Format(x)}, {Format(y)}) with a radius of {Format(radius)} m.",
            changed: true);
    }

    /// <summary>Parses a point written x,y or x,y,z. Without a z, it's the ground height there.</summary>
    private bool TryParsePoint(string text, out Point3 point, [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? error)
    {
        point = default;
        error = null;
        string[] parts = text.Split(',');
        double z = 0;
        if (parts.Length is < 2 or > 3
            || !TryParseNumber(parts[0], out double x)
            || !TryParseNumber(parts[1], out double y)
            || (parts.Length == 3 && !TryParseNumber(parts[2], out z)))
        {
            error = $"'{text}' isn't a point. Write points as x,y or x,y,z with no spaces.";
            return false;
        }

        if (parts.Length == 2 && !World.Land.TryGetGroundElevation(new Point3(x, y, 0), out z))
        {
            error = $"'{text}' is off the map, so there is no ground height there. Write it as x,y,z.";
            return false;
        }

        point = new Point3(x, y, z);
        return true;
    }

    /// <summary>Parses "x y" as a single cell or corner, or "x1 y1 x2 y2" as a rectangle of them.</summary>
    private static bool TryParseRectangle(string[] arguments, out int x1, out int y1, out int x2, out int y2)
    {
        x2 = y2 = 0;
        bool parsed = TryParseInt(arguments[0], out x1) & TryParseInt(arguments[1], out y1);
        if (arguments.Length == 2)
        {
            (x2, y2) = (x1, y1);
            return parsed;
        }

        return parsed & TryParseInt(arguments[2], out x2) & TryParseInt(arguments[3], out y2);
    }

    private static bool TryParseInt(string text, out int value) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

    private static bool TryParseNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);

    /// <summary>Parses an ID such as "n3" or "3", where the letter is optional.</summary>
    private static bool TryParseId(string text, char prefix, out int value)
    {
        ReadOnlySpan<char> digits = text.Length > 0 && char.ToLowerInvariant(text[0]) == prefix ? text.AsSpan(1) : text;
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    private static Outcome Ok(string output, bool changed = false) => new(output, changed, Failed: false);

    private static Outcome Fail(string output) => new(output, Changed: false, Failed: true);

    private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatPoint(Point3 point) => $"({Format(point.X)}, {Format(point.Y)}, {Format(point.Z)})";

    /// <summary>What a command did: its output, whether it changed the world, and whether it failed.</summary>
    private readonly record struct Outcome(string Output, bool Changed, bool Failed);
}
