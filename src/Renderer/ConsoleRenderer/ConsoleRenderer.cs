using Simulacity.Constants;

namespace Simulacity.Renderer.ConsoleRenderer;

public class ConsoleRenderer : IRenderer {

    // ConsoleRenderer
    public static readonly int cellSize = 5; 
    public static readonly int renderingGridWidth = 50; // in #cells
    public static readonly int renderingGridHeight = 50; // in #cells

    private ISimulator _simulator;

    private static readonly Dictionary<GridType, char> gridSymbols = new()
    {
        { GridType.Empty,   ' ' },
        { GridType.Border,  '#' },
        { GridType.Water,   '~' },
        { GridType.Ground,  '-' },
    };

    private int width = renderingGridWidth;
    private int height = renderingGridHeight;


    public ConsoleRenderer(ISimulator simulator)
    {
        _simulator = simulator;
    }

    public void Init()
    {
        Console.CursorVisible = false;
    }

    public void Render(){
        Draw(_simulator);
    }

    void Draw(ISimulator simulator){
        var riverPixels = GetRiverPixels(simulator);

        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++){
                sb.Append(DrawPixel(x, y, riverPixels));
            }
            sb.Append('\n');
        }
/*
        foreach (var rvp in riverPixels)
        {
            Console.Write($" {rvp.X} , {rvp.Y} \n");
        }*/

        Console.Write(sb.ToString());
        
    }

    private char DrawPixel(
        int x,
        int y,
        HashSet<CatmullRomRasterizer.Int2> riverPixels
    )
    {
        if (riverPixels.Contains(new(x,y)))
        {
            return '~';
        }
        return ' ';
    }

    HashSet<CatmullRomRasterizer.Int2> GetRiverPixels(ISimulator simulator)
    {
        var all = new HashSet<CatmullRomRasterizer.Int2>();

        var rs = simulator.GetRivers();
        for (int r = 0; r < rs.Count; ++r)
        {
            var part = CatmullRomRasterizer.Rasterize(
                    rs[r].Centerline.Points.Select((p) => p.Position).ToList(),
                    rs[r].Centerline.Points.Select((p) => rs[r].attributes.AverageWidth).ToList(),
                    projectToRaster: p => new(
                        (int)MathF.Round(p.X),
                        (int)MathF.Round(p.Y)),
                    cellSize: new(cellSize, cellSize),
                    flatnessInCells: 0.5f,
                    closed: rs[r].Centerline.Closed);

                all.UnionWith(part);
        }

        return all;
    }

}