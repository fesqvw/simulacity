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
          step [n]                     advance the simulation by n steps (default 1)
          status                       show the current state
          node <x> <y> [z]             add a road node at a world position, in metres
                                       (z defaults to the ground height there)
          road <from> <to> [x,y[,z]]…  add a road between two nodes, optionally through points.
                                       It joins roads it crosses at the same height
          remove <segment>             remove a road segment (its nodes stay)
          roads                        list the road nodes and segments
          land <type> <x> <y> [<x2> <y2>]
                                       set a land cell, or a rectangle of cells, to ground or water
          height <i> <j> <z>           set the ground height at a cell corner, in metres
          help                         show this list
          quit                         exit the simulator

        Water can only be crossed on a bridge or in a tunnel: a road more than 1 m above
        or below the ground or water. Roads become bridges and tunnels by their height.
        Nodes are written n1, n2…, and segments s1, s2… (the letter is optional).
        Land cells and corners are counted from 0 at the south-west corner of the map.
        """;

    private World World => simulation.World;

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

        (string output, bool changed) = command switch
        {
            "step" => Step(arguments),
            "status" => (Status(), false),
            "node" => AddNode(arguments),
            "road" => AddRoad(arguments),
            "remove" => RemoveRoad(arguments),
            "roads" => (ListRoads(), false),
            "land" => PaintLand(arguments),
            "height" => SetHeight(arguments),
            "help" or "?" => (HelpText, false),
            "quit" or "exit" => (string.Empty, false),
            _ => ($"Unknown command '{words[0]}'. Type 'help' for the list of commands.", false),
        };

        if (changed)
        {
            await PublishAsync(cancellationToken).ConfigureAwait(false);
        }

        return new CommandResult(output, Quit: command is "quit" or "exit");
    }

    /// <summary>Sends the current state to the renderers.</summary>
    public Task PublishAsync(CancellationToken cancellationToken = default) =>
        publisher.PublishAsync(SnapshotBuilder.From(simulation), cancellationToken);

    private (string Output, bool Changed) Step(string[] arguments)
    {
        long steps = 1;
        if (arguments.Length > 1
            || (arguments.Length == 1
                && (!long.TryParse(arguments[0], NumberStyles.None, CultureInfo.InvariantCulture, out steps) || steps < 1)))
        {
            return ("Usage: step [n], where n is a whole number of at least 1.", false);
        }

        for (long i = 0; i < steps; i++)
        {
            simulation.Step();
        }

        return (Status(), true);
    }

    private string Status() => $"Tick {simulation.Tick}";

    private (string Output, bool Changed) AddNode(string[] arguments)
    {
        const string usage = "Usage: node <x> <y> [z], in metres.";
        if (arguments.Length is < 2 or > 3
            || !TryParseNumber(arguments[0], out double x)
            || !TryParseNumber(arguments[1], out double y))
        {
            return (usage, false);
        }

        double z;
        if (arguments.Length == 3)
        {
            if (!TryParseNumber(arguments[2], out z))
            {
                return (usage, false);
            }
        }
        else if (!World.Land.TryGetGroundElevation(new Point3(x, y, 0), out z))
        {
            return ($"({Format(x)}, {Format(y)}) is off the map, so there is no ground height there. Give a z.", false);
        }

        RoadNode node = World.Roads.AddNode(new Point3(x, y, z));
        return ($"Added node {node.Id} at {FormatPoint(node.Position)}.", true);
    }

    private (string Output, bool Changed) AddRoad(string[] arguments)
    {
        const string usage = "Usage: road <from> <to> [x,y[,z]]…, where <from> and <to> are nodes such as n1.";
        if (arguments.Length < 2
            || !TryParseId(arguments[0], 'n', out int from)
            || !TryParseId(arguments[1], 'n', out int to))
        {
            return (usage, false);
        }

        var start = new NodeId(from);
        var end = new NodeId(to);
        foreach (NodeId id in (ReadOnlySpan<NodeId>)[start, end])
        {
            if (!World.Roads.ContainsNode(id))
            {
                return ($"There is no node {id}. Type 'roads' to list them.", false);
            }
        }

        if (start == end)
        {
            return ("A road must join two different nodes.", false);
        }

        var points = new List<Point3>();
        foreach (string argument in arguments[2..])
        {
            string[] parts = argument.Split(',');
            if (parts.Length is < 2 or > 3
                || !TryParseNumber(parts[0], out double x)
                || !TryParseNumber(parts[1], out double y))
            {
                return ($"'{argument}' isn't a point. Write points as x,y or x,y,z with no spaces.", false);
            }

            double z;
            if (parts.Length == 3)
            {
                if (!TryParseNumber(parts[2], out z))
                {
                    return ($"'{argument}' isn't a point. Write points as x,y or x,y,z with no spaces.", false);
                }
            }
            else if (!World.Land.TryGetGroundElevation(new Point3(x, y, 0), out z))
            {
                return ($"'{argument}' is off the map, so there is no ground height there. Write it as x,y,z.", false);
            }

            points.Add(new Point3(x, y, z));
        }

        RoadAddition addition;
        try
        {
            addition = World.AddRoad(start, end, new PolylineShape(points));
        }
        catch (ArgumentException exception)
        {
            return (exception.Message, false);
        }

        return (DescribeAddition(start, end, addition), true);
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

    private (string Output, bool Changed) RemoveRoad(string[] arguments)
    {
        if (arguments.Length != 1 || !TryParseId(arguments[0], 's', out int value))
        {
            return ("Usage: remove <segment>, such as remove s1.", false);
        }

        var id = new SegmentId(value);
        return World.Roads.RemoveSegment(id)
            ? ($"Removed road {id}.", true)
            : ($"There is no road {id}. Type 'roads' to list them.", false);
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

    private (string Output, bool Changed) PaintLand(string[] arguments)
    {
        const string usage = "Usage: land <ground|water> <x> <y> [<x2> <y2>], where x and y are cell numbers.";
        if (arguments.Length is not (3 or 5)
            || !Enum.TryParse(arguments[0], ignoreCase: true, out LandType type)
            || !Enum.IsDefined(type)
            || int.TryParse(arguments[0], out _)
            || !TryParseCells(arguments[1..], out int x1, out int y1, out int x2, out int y2))
        {
            return (usage, false);
        }

        LandGrid land = World.Land;
        if (!land.Contains(x1, y1) || !land.Contains(x2, y2))
        {
            return ($"The map's cells go from (0, 0) to ({land.Width - 1}, {land.Height - 1}).", false);
        }

        try
        {
            World.SetLand(x1, y1, x2, y2, type);
        }
        catch (InvalidOperationException exception)
        {
            return (exception.Message, false);
        }

        (int west, int east) = (Math.Min(x1, x2), Math.Max(x1, x2));
        (int south, int north) = (Math.Min(y1, y2), Math.Max(y1, y2));

        int count = (east - west + 1) * (north - south + 1);
        string name = type.ToString().ToLowerInvariant();
        return (count == 1
            ? $"Set cell ({west}, {south}) to {name}."
            : $"Set {count} cells from ({west}, {south}) to ({east}, {north}) to {name}.", true);
    }

    private (string Output, bool Changed) SetHeight(string[] arguments)
    {
        if (arguments.Length != 3
            || !TryParseInt(arguments[0], out int i)
            || !TryParseInt(arguments[1], out int j)
            || !TryParseNumber(arguments[2], out double z))
        {
            return ("Usage: height <i> <j> <z>, where i and j are corner numbers and z is in metres.", false);
        }

        LandGrid land = World.Land;
        if (i < 0 || i > land.Width || j < 0 || j > land.Height)
        {
            return ($"The map's corners go from (0, 0) to ({land.Width}, {land.Height}).", false);
        }

        try
        {
            World.SetCornerElevation(i, j, z);
        }
        catch (InvalidOperationException exception)
        {
            return (exception.Message, false);
        }

        return ($"Set the ground height at corner ({i}, {j}) to {Format(z)} m.", true);
    }

    private static bool TryParseCells(string[] arguments, out int x1, out int y1, out int x2, out int y2)
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

    private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatPoint(Point3 point) => $"({Format(point.X)}, {Format(point.Y)}, {Format(point.Z)})";
}
