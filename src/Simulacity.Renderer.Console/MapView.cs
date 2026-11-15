using Simulacity.Contracts;

namespace Simulacity.Renderer.Console;

/// <summary>
/// Draws a snapshot's map as text, seen from above with north at the top (decisions 0009 and 0010).
/// </summary>
/// <remarks>
/// <para>The map is scaled to fit the space it's given. Console characters are about twice as tall as
/// they are wide, so each character covers half as many metres across as it does down, which keeps
/// distances in proportion.</para>
/// <para>Heights can't be shown top-down. The simulator says which stretches of road are bridges and
/// tunnels (decision 0012), and where roads overlap the one on top wins.</para>
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

    /// <summary>The key to the characters, to print under the map.</summary>
    public const string Legend = ". ground   ~ water   # road   = bridge   : tunnel   o node";

    // Lowest to highest priority: what's drawn later only replaces what's drawn earlier if it ranks higher.
    private const int LandLayer = 0;
    private const int TunnelLayer = 1;
    private const int RoadLayer = 2;
    private const int BridgeLayer = 3;
    private const int NodeLayer = 4;

    /// <summary>Draws the map in at most <paramref name="maxColumns"/> by <paramref name="maxRows"/> characters.</summary>
    /// <returns>The map's rows, from north to south, all the same length.</returns>
    public static IReadOnlyList<string> Draw(SimulationSnapshot snapshot, int maxColumns, int maxRows)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxColumns, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);

        var canvas = new Canvas(snapshot.Land, maxColumns, maxRows);
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

        return canvas.Rows();
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    private sealed class Canvas
    {
        private readonly LandSnapshot _land;
        private readonly double _metresPerRow;
        private readonly double _metresPerColumn;
        private readonly int _columns;
        private readonly int _rows;
        private readonly char[,] _chars;
        private readonly int[,] _layers;

        public Canvas(LandSnapshot land, int maxColumns, int maxRows)
        {
            _land = land;
            double mapWidth = land.Width * land.CellSize;
            double mapHeight = land.Height * land.CellSize;

            // A row is never finer than a cell, and the whole map has to fit.
            _metresPerRow = Math.Max(land.CellSize, Math.Max(mapHeight / maxRows, 2 * mapWidth / maxColumns));
            _metresPerColumn = _metresPerRow / 2;
            _columns = Math.Clamp((int)Math.Ceiling(mapWidth / _metresPerColumn), 1, maxColumns);
            _rows = Math.Clamp((int)Math.Ceiling(mapHeight / _metresPerRow), 1, maxRows);
            _chars = new char[_rows, _columns];
            _layers = new int[_rows, _columns];
        }

        public void DrawLand()
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    // Sample the middle of the character. Row 0 is the north edge.
                    double x = (column + 0.5) * _metresPerColumn;
                    double y = _land.Height * _land.CellSize - ((row + 0.5) * _metresPerRow);
                    int cellX = Math.Clamp((int)(x / _land.CellSize), 0, _land.Width - 1);
                    int cellY = Math.Clamp((int)(y / _land.CellSize), 0, _land.Height - 1);
                    bool water = _land.Cells[(cellY * _land.Width) + cellX] == LandKind.Water;
                    _chars[row, column] = water ? Water : Ground;
                    _layers[row, column] = LandLayer;
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
            double columnsAcross = Math.Abs(to.X - from.X) / _metresPerColumn;
            double rowsAcross = Math.Abs(to.Y - from.Y) / _metresPerRow;
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(columnsAcross, rowsAcross) * 2));
            for (int step = 0; step <= steps; step++)
            {
                double t = (double)step / steps;
                Plot(Lerp(from.X, to.X, t), Lerp(from.Y, to.Y, t), symbol, layer);
            }
        }

        public void Plot(double x, double y, char symbol, int layer)
        {
            double mapWidth = _land.Width * _land.CellSize;
            double mapHeight = _land.Height * _land.CellSize;
            if (!(x >= 0 && x <= mapWidth && y >= 0 && y <= mapHeight))
            {
                return; // Off the map.
            }

            // Points on the east or south edge belong to the last column or row.
            int column = Math.Min((int)(x / _metresPerColumn), _columns - 1);
            int row = Math.Min((int)((mapHeight - y) / _metresPerRow), _rows - 1);
            if (layer < _layers[row, column])
            {
                return; // Under something already drawn.
            }

            _chars[row, column] = symbol;
            _layers[row, column] = layer;
        }

        public IReadOnlyList<string> Rows()
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
