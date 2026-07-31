using Simulacity.Constants;

namespace Simulacity.Renderer.ConsoleRenderer;

public class ConsoleRenderer : IRenderer {

    // ConsoleRenderer
    public static readonly int cellSize = 5; 
    public static readonly int renderingGridWidth = 50; // in #cells
    public static readonly int renderingGridHeight = 50; // in #cells

    private ISimulator _simulator;


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
        Console.SetCursorPosition(0, 0);
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

        var state = simulator.GetState();
        var rs = state.riverFeatures;
        for (int r = 0; r < rs.Count; ++r)
        {
            for (int rSeg = 0; rSeg < rs[r].SegmentIds.Count ; ++rSeg )
            {
                var seg = state.riverSegmentFeatures.First( (seg) => seg.Id == rs[r].SegmentIds[rSeg]);

                var part = CatmullRomRasterizer.Rasterize(
                    seg.Centerline.Points.Select((p) => p.Position).ToList(),
                    seg.Centerline.Points.Select((p) => seg.attributes.AverageWidth).ToList(),
                    projectToRaster: p => new(
                        (int)MathF.Round(p.X),
                        (int)MathF.Round(p.Y)),
                    cellSize: new(cellSize, cellSize),
                    flatnessInCells: 0.5f,
                    closed: seg.Centerline.Closed);

                all.UnionWith(part);
            }
        }

        return all;
    }

}