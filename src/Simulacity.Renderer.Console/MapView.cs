using Simulacity.Contracts;

namespace Simulacity.Renderer.Console;

/// <summary>
/// Which part of the map is on screen, and at what scale (decision 0014). This is the renderer's
/// only state, and it never affects the simulation.
/// </summary>
/// <remarks>
/// Console characters are about twice as tall as they are wide, so each character covers half as
/// many metres across as it does down, which keeps distances in proportion.
/// </remarks>
/// <param name="West">The world x of the view's left edge, in metres.</param>
/// <param name="North">The world y of the view's top edge, in metres.</param>
/// <param name="MetresPerRow">How many metres each row of characters covers from north to south.</param>
public sealed record MapViewport(double West, double North, double MetresPerRow)
{
    /// <summary>The most detail: four rows per cell.</summary>
    private const double MostRowsPerCell = 4;

    /// <summary>How many metres each column of characters covers from west to east.</summary>
    public double MetresPerColumn => MetresPerRow / 2;

    /// <summary>The scale that shows the whole map in the space, but never coarser rows than one cell.</summary>
    public static MapViewport Fit(LandSnapshot land, int columns, int rows)
    {
        ArgumentNullException.ThrowIfNull(land);

        double mapWidth = land.Width * land.CellSize;
        double mapHeight = land.Height * land.CellSize;
        double metresPerRow = Math.Max(land.CellSize, Math.Max(mapHeight / rows, 2 * mapWidth / columns));
        return new MapViewport(0, mapHeight, metresPerRow);
    }

    /// <summary>
    /// The view to start with: the whole map if it fits at one row per cell, or else one row per
    /// cell centred on the map, to move around in.
    /// </summary>
    public static MapViewport Initial(LandSnapshot land, int columns, int rows)
    {
        ArgumentNullException.ThrowIfNull(land);

        MapViewport fit = Fit(land, columns, rows);
        if (fit.MetresPerRow <= land.CellSize)
        {
            return fit;
        }

        return new MapViewport(0, 0, land.CellSize).CentredOn(land.Width * land.CellSize / 2, land.Height * land.CellSize / 2, columns, rows);
    }

    /// <summary>The same scale, moved so the space's middle is at a world position.</summary>
    public MapViewport CentredOn(double x, double y, int columns, int rows) =>
        this with { West = x - (columns * MetresPerColumn / 2), North = y + (rows * MetresPerRow / 2) };

    /// <summary>
    /// Moves the view by a quarter of the space for each step: east and north for positive steps.
    /// The middle of the view stays on the map.
    /// </summary>
    public MapViewport Pan(int stepsEast, int stepsNorth, LandSnapshot land, int columns, int rows)
    {
        ArgumentNullException.ThrowIfNull(land);

        (double x, double y) = Centre(columns, rows);
        x += stepsEast * Math.Max(1, columns / 4) * MetresPerColumn;
        y += stepsNorth * Math.Max(1, rows / 4) * MetresPerRow;
        return CentredOn(
            Math.Clamp(x, 0, land.Width * land.CellSize),
            Math.Clamp(y, 0, land.Height * land.CellSize),
            columns,
            rows);
    }

    /// <summary>
    /// Zooms in (positive steps) or out (negative steps) by a factor of two each, keeping the same
    /// place in the middle. It stops at four rows per cell, and when the whole map fits.
    /// </summary>
    public MapViewport Zoom(int stepsIn, LandSnapshot land, int columns, int rows)
    {
        ArgumentNullException.ThrowIfNull(land);

        double finest = land.CellSize / MostRowsPerCell;
        double coarsest = Math.Max(Fit(land, columns, rows).MetresPerRow, finest);
        double metresPerRow = Math.Clamp(MetresPerRow / Math.Pow(2, stepsIn), finest, coarsest);
        (double x, double y) = Centre(columns, rows);
        return (this with { MetresPerRow = metresPerRow }).CentredOn(x, y, columns, rows);
    }

    /// <summary>The world position in the middle of the space.</summary>
    public (double X, double Y) Centre(int columns, int rows) =>
        (West + (columns * MetresPerColumn / 2), North - (rows * MetresPerRow / 2));
}

/// <summary>
/// Draws a snapshot's map as text, seen from above with north at the top (decisions 0009 and 0010).
/// </summary>
/// <remarks>
/// Heights can't be shown top-down. The simulator says which stretches of road are bridges and
/// tunnels (decision 0012), and where roads overlap the one on top wins.
/// </remarks>
public static class MapView
{
    /// <summary>The character for ground.</summary>
    public const char Ground = '.';

    /// <summary>The character for water.</summary>
    public const char Water = '~';

    /// <summary>The character for a road on the ground.</summary>
    public const char Road = '#';

    /// <summary>The character for a road above the ground.</summary>
    public const char Bridge = '=';

    /// <summary>The character for a road below the ground.</summary>
    public const char Tunnel = ':';

    /// <summary>The character for a road node.</summary>
    public const char Node = 'o';

    /// <summary>The character for anything off the map.</summary>
    public const char OffMap = ' ';

    /// <summary>The key to the characters, to print under the map.</summary>
    public const string Legend = ". ground   ~ water   # road   = bridge   : tunnel   o node";

    // Lowest to highest priority: what's drawn later only replaces what's drawn earlier if it ranks higher.
    private const int LandLayer = 0;
    private const int TunnelLayer = 1;
    private const int RoadLayer = 2;
    private const int BridgeLayer = 3;
    private const int NodeLayer = 4;

    /// <summary>
    /// Draws the whole map, scaled to fit in at most <paramref name="maxColumns"/> by
    /// <paramref name="maxRows"/> characters, and trimmed to the map's size.
    /// </summary>
    /// <returns>The map's rows, from north to south, all the same length.</returns>
    public static IReadOnlyList<string> Draw(SimulationSnapshot snapshot, int maxColumns, int maxRows)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxColumns, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);

        LandSnapshot land = snapshot.Land;
        MapViewport view = MapViewport.Fit(land, maxColumns, maxRows);
        int columns = Math.Clamp((int)Math.Ceiling(land.Width * land.CellSize / view.MetresPerColumn), 1, maxColumns);
        int rows = Math.Clamp((int)Math.Ceiling(land.Height * land.CellSize / view.MetresPerRow), 1, maxRows);
        return Draw(snapshot, view, columns, rows);
    }

    /// <summary>Draws the part of the map a viewport shows, in exactly <paramref name="columns"/> by <paramref name="rows"/> characters.</summary>
    /// <returns>The rows, from north to south. Anything off the map is blank.</returns>
    public static IReadOnlyList<string> Draw(SimulationSnapshot snapshot, MapViewport view, int columns, int rows) =>
        Render(snapshot, view, columns, rows).Rows;

    /// <summary>
    /// Draws the part of the map a viewport shows, and also says what land is under each character
    /// and how high it is, so the map can be coloured by height.
    /// </summary>
    public static MapPicture Render(SimulationSnapshot snapshot, MapViewport view, int columns, int rows)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);

        var canvas = new Canvas(snapshot.Land, view, columns, rows);
        canvas.DrawLand();
        foreach (RoadSegmentSnapshot segment in snapshot.Roads.Segments)
        {
            foreach (RoadStretchSnapshot stretch in segment.Stretches)
            {
                canvas.DrawStretch(stretch);
            }
        }

        foreach (RoadNodeSnapshot node in snapshot.Roads.Nodes)
        {
            canvas.Plot(node.Position.X, node.Position.Y, Node, NodeLayer);
        }

        return canvas.Picture();
    }

    /// <summary>The ground elevation under a position, interpolated bilinearly between the corners of its cell (decision 0009).</summary>
    /// <returns>The elevation, or null if the position is off the map.</returns>
    public static double? GroundElevation(LandSnapshot land, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(land);

        double u = x / land.CellSize;
        double v = y / land.CellSize;
        if (!(u >= 0 && u <= land.Width && v >= 0 && v <= land.Height))
        {
            return null;
        }

        int i = Math.Min((int)u, land.Width - 1);
        int j = Math.Min((int)v, land.Height - 1);
        double fx = u - i;
        double fy = v - j;
        double Corner(int ci, int cj) => land.CornerElevations[(cj * (land.Width + 1)) + ci];

        double south = Lerp(Corner(i, j), Corner(i + 1, j), fx);
        double north = Lerp(Corner(i, j + 1), Corner(i + 1, j + 1), fx);
        return Lerp(south, north, fy);
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private sealed class Canvas
    {
        private readonly LandSnapshot _land;
        private readonly MapViewport _view;
        private readonly double _mapWidth;
        private readonly double _mapHeight;
        private readonly int _columns;
        private readonly int _rows;
        private readonly char[,] _chars;
        private readonly int[,] _layers;
        private readonly LandKind?[,] _landUnder;
        private readonly double[,] _elevations;

        public Canvas(LandSnapshot land, MapViewport view, int columns, int rows)
        {
            _land = land;
            _view = view;
            _mapWidth = land.Width * land.CellSize;
            _mapHeight = land.Height * land.CellSize;
            _columns = columns;
            _rows = rows;
            _chars = new char[rows, columns];
            _layers = new int[rows, columns];
            _landUnder = new LandKind?[rows, columns];
            _elevations = new double[rows, columns];
        }

        public void DrawLand()
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    // Sample the middle of the character. Row 0 is the north edge.
                    double x = _view.West + ((column + 0.5) * _view.MetresPerColumn);
                    double y = _view.North - ((row + 0.5) * _view.MetresPerRow);
                    _layers[row, column] = LandLayer;
                    if (!(x >= 0 && x <= _mapWidth && y >= 0 && y <= _mapHeight))
                    {
                        _chars[row, column] = OffMap;
                        continue;
                    }

                    int cellX = Math.Clamp((int)(x / _land.CellSize), 0, _land.Width - 1);
                    int cellY = Math.Clamp((int)(y / _land.CellSize), 0, _land.Height - 1);
                    LandKind kind = _land.Cells[(cellY * _land.Width) + cellX];
                    _chars[row, column] = kind == LandKind.Water ? Water : Ground;
                    _landUnder[row, column] = kind;
                    _elevations[row, column] = GroundElevation(_land, x, y) ?? 0;
                }
            }
        }

        public void DrawStretch(RoadStretchSnapshot stretch)
        {
            (char symbol, int layer) = stretch.Level switch
            {
                RoadLevelKind.Bridge => (Bridge, BridgeLayer),
                RoadLevelKind.Tunnel => (Tunnel, TunnelLayer),
                _ => (Road, RoadLayer),
            };

            for (int i = 1; i < stretch.Points.Count; i++)
            {
                DrawLine(stretch.Points[i - 1], stretch.Points[i], symbol, layer);
            }
        }

        /// <summary>Draws a straight piece of road, one step per character it crosses.</summary>
        private void DrawLine(Position from, Position to, char symbol, int layer)
        {
            double columnsAcross = Math.Abs(to.X - from.X) / _view.MetresPerColumn;
            double rowsAcross = Math.Abs(to.Y - from.Y) / _view.MetresPerRow;
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(columnsAcross, rowsAcross) * 2));
            for (int step = 0; step <= steps; step++)
            {
                double t = (double)step / steps;
                Plot(Lerp(from.X, to.X, t), Lerp(from.Y, to.Y, t), symbol, layer);
            }
        }

        public void Plot(double x, double y, char symbol, int layer)
        {
            if (!(x >= 0 && x <= _mapWidth && y >= 0 && y <= _mapHeight))
            {
                return; // Off the map.
            }

            // Points exactly on the map's east or south edge belong to the column or row inside the map.
            int column = (int)Math.Floor((x - _view.West) / _view.MetresPerColumn);
            if (x == _mapWidth && column > 0 && _view.West + (column * _view.MetresPerColumn) >= _mapWidth)
            {
                column--;
            }

            int row = (int)Math.Floor((_view.North - y) / _view.MetresPerRow);
            if (y == 0 && row > 0 && _view.North - (row * _view.MetresPerRow) <= 0)
            {
                row--;
            }

            if (column < 0 || column >= _columns || row < 0 || row >= _rows || layer < _layers[row, column])
            {
                return; // Out of view, or under something already drawn.
            }

            _chars[row, column] = symbol;
            _layers[row, column] = layer;
        }

        public MapPicture Picture() => new(Rows(), _landUnder, _elevations);

        private string[] Rows()
        {
            var rows = new string[_rows];
            for (int row = 0; row < _rows; row++)
            {
                var line = new char[_columns];
                for (int column = 0; column < _columns; column++)
                {
                    line[column] = _chars[row, column];
                }

                rows[row] = new string(line);
            }

            return rows;
        }
    }
}

/// <summary>A drawn map, with what's under each character so it can be coloured.</summary>
/// <param name="Rows">The characters, row by row from north to south.</param>
/// <param name="LandUnder">The land under each character, by [row, column], or null where it's off the map.</param>
/// <param name="Elevations">The ground elevation in the middle of each character, by [row, column], in metres.</param>
public sealed record MapPicture(IReadOnlyList<string> Rows, LandKind?[,] LandUnder, double[,] Elevations);
