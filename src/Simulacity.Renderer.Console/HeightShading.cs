using System.Globalization;
using System.Text;
using Simulacity.Contracts;

namespace Simulacity.Renderer.Console;

/// <summary>
/// Colours the ground by height, in bands from the lowest to the highest ground on the map
/// (decision 0015), from green lowland to grey peaks. Water stays blue. The colours are ANSI
/// escape codes for 256-colour terminals.
/// </summary>
public sealed class HeightShading
{
    /// <summary>How many bands the map's height range is split into.</summary>
    public const int MaxBands = 6;

    /// <summary>Below this range, in metres, the map counts as flat and has a single band.</summary>
    private const double FlatRange = 1;

    // Background colours from low to high: dark green, green, olive, gold, brown, light grey.
    private static readonly int[] BandBackgrounds = [22, 28, 64, 136, 94, 250];
    private const int WaterBackground = 18;
    private const int WaterForeground = 45;
    private const int LightText = 15;
    private const int DarkText = 16;
    private const string Reset = "\u001b[0m";

    private HeightShading(double lowest, double highest, int bands)
    {
        Lowest = lowest;
        Highest = highest;
        Bands = bands;
    }

    /// <summary>The lowest ground elevation on the map, in metres.</summary>
    public double Lowest { get; }

    /// <summary>The highest ground elevation on the map, in metres.</summary>
    public double Highest { get; }

    /// <summary>How many bands there are: one for a flat map, otherwise <see cref="MaxBands"/>.</summary>
    public int Bands { get; }

    /// <summary>The bands for a map, from its lowest corner to its highest.</summary>
    public static HeightShading For(LandSnapshot land)
    {
        ArgumentNullException.ThrowIfNull(land);

        double lowest = land.CornerElevations.Count == 0 ? 0 : land.CornerElevations.Min();
        double highest = land.CornerElevations.Count == 0 ? 0 : land.CornerElevations.Max();
        return new HeightShading(lowest, highest, highest - lowest < FlatRange ? 1 : MaxBands);
    }

    /// <summary>Which band an elevation is in, from 0 for the lowest.</summary>
    public int BandOf(double elevation)
    {
        if (Bands == 1)
        {
            return 0;
        }

        double fraction = (elevation - Lowest) / (Highest - Lowest);
        return Math.Clamp((int)(fraction * Bands), 0, Bands - 1);
    }

    /// <summary>The elevation where a band starts, in metres.</summary>
    public double BandStart(int band) => Lowest + ((Highest - Lowest) * band / Bands);

    /// <summary>A drawn row with colour codes: each character on the ground gets its band's background.</summary>
    public string Colour(MapPicture picture, int row)
    {
        ArgumentNullException.ThrowIfNull(picture);

        string text = picture.Rows[row];
        var result = new StringBuilder();
        (int Background, int Foreground)? current = null;
        for (int column = 0; column < text.Length; column++)
        {
            (int Background, int Foreground)? colours = picture.LandUnder[row, column] switch
            {
                null => null,
                LandKind.Water => (WaterBackground, text[column] == MapView.Water ? WaterForeground : LightText),
                _ => GroundColours(picture.Elevations[row, column]),
            };

            if (colours != current)
            {
                result.Append(colours is { } c ? Code(c.Background, c.Foreground) : Reset);
                current = colours;
            }

            result.Append(text[column]);
        }

        return result.Append(Reset).ToString();
    }

    /// <summary>The key to the bands: a coloured swatch for each, with the elevation it starts at.</summary>
    /// <returns>The plain text, and the same with colour codes.</returns>
    public (string Text, string Coloured) Legend()
    {
        var text = new StringBuilder("Height: ");
        var coloured = new StringBuilder("Height: ");
        for (int band = 0; band < Bands; band++)
        {
            string start = $"{BandStart(band).ToString("0.#", CultureInfo.InvariantCulture)} ";
            (int background, int foreground) = GroundColours(BandStart(band));
            text.Append("  ").Append(start);
            coloured.Append(Code(background, foreground)).Append("  ").Append(Reset).Append(start);
        }

        string top = Bands == 1 ? "m (flat)" : $"to {Highest.ToString("0.#", CultureInfo.InvariantCulture)} m";
        text.Append(top);
        coloured.Append(top);
        return (text.ToString(), coloured.ToString());
    }

    private (int Background, int Foreground) GroundColours(double elevation)
    {
        // A flat map is drawn in the lowland colour.
        int band = BandOf(elevation);
        int background = Bands == 1 ? BandBackgrounds[1] : BandBackgrounds[band];
        return (background, background == BandBackgrounds[^1] ? DarkText : LightText);
    }

    private static string Code(int background, int foreground) => $"\u001b[48;5;{background};38;5;{foreground}m";
}
