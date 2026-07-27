using Simulacity.Constants;

namespace Simulacity.Renderer.ConsoleRenderer;

public class ConsoleRenderer : IRenderer {

    // ConsoleRenderer
    public static readonly int cellSize = 5; 
    public static readonly int renderingGridWidth = 50; // in #cells
    public static readonly int renderingGridHeight = 50; // in #cells

    private static readonly Dictionary<GridType, char> gridSymbols = new()
    {
        { GridType.Empty,   ' ' },
        { GridType.Border,  '#' },
        { GridType.Water,   '~' },
        { GridType.Ground,  '-' },
    };

    private int width = renderingGridWidth;
    private int height = renderingGridHeight;


    public ConsoleRenderer()
    {
    }

    public void Init()
    {
        Console.CursorVisible = false;
    }

    public void Render(ISimulator simulator){
        Draw(simulator);
    }

    void Draw(ISimulator simulator){
        Console.SetCursorPosition(0, 0);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++){
                //sb.Append(gridSymbols[simulator.GetCell(j,i)]);
            }
            sb.Append('\n');
        }
        Console.Write(sb.ToString());
    }

}