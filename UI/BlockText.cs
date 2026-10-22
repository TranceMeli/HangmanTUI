using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace HangmanSpectre.UI;

/// <summary>
/// Renders text in the "ANSI Shadow" block style: solid blocks (█) in the
/// main color, box-drawing shadow characters (╔═╝) in the shadow color.
/// </summary>
public static class BlockText
{
    private const int Height = 6;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = [" █████╗ ", "██╔══██╗", "███████║", "██╔══██║", "██║  ██║", "╚═╝  ╚═╝"],
        ['E'] = ["███████╗", "██╔════╝", "█████╗  ", "██╔══╝  ", "███████╗", "╚══════╝"],
        ['G'] = [" ██████╗ ", "██╔════╝ ", "██║  ███╗", "██║   ██║", "╚██████╔╝", " ╚═════╝ "],
        ['H'] = ["██╗  ██╗", "██║  ██║", "███████║", "██╔══██║", "██║  ██║", "╚═╝  ╚═╝"],
        ['I'] = ["██╗", "██║", "██║", "██║", "██║", "╚═╝"],
        ['M'] = ["███╗   ███╗", "████╗ ████║", "██╔████╔██║", "██║╚██╔╝██║", "██║ ╚═╝ ██║", "╚═╝     ╚═╝"],
        ['N'] = ["███╗   ██╗", "████╗  ██║", "██╔██╗ ██║", "██║╚██╗██║", "██║ ╚████║", "╚═╝  ╚═══╝"],
        ['O'] = [" ██████╗ ", "██╔═══██╗", "██║   ██║", "██║   ██║", "╚██████╔╝", " ╚═════╝ "],
        ['R'] = ["██████╗ ", "██╔══██╗", "██████╔╝", "██╔══██╗", "██║  ██║", "╚═╝  ╚═╝"],
        ['U'] = ["██╗   ██╗", "██║   ██║", "██║   ██║", "██║   ██║", "╚██████╔╝", " ╚═════╝ "],
        ['V'] = ["██╗   ██╗", "██║   ██║", "██║   ██║", "╚██╗ ██╔╝", " ╚████╔╝ ", "  ╚═══╝  "],
        ['W'] = ["██╗    ██╗", "██║    ██║", "██║ █╗ ██║", "██║███╗██║", "╚███╔███╔╝", " ╚══╝╚══╝ "],
        ['Y'] = ["██╗   ██╗", "╚██╗ ██╔╝", " ╚████╔╝ ", "  ╚██╔╝  ", "   ██║   ", "   ╚═╝   "],
        ['!'] = ["██╗", "██║", "██║", "╚═╝", "██╗", "╚═╝"],
        [' '] = ["  ", "  ", "  ", "  ", "  ", "  "],
    };

    /// <summary>Width in console columns that <paramref name="text"/> needs.</summary>
    public static int Measure(string text)
    {
        return text.ToUpperInvariant().Sum(c =>
            (Glyphs.TryGetValue(c, out var glyph) ? glyph : Glyphs[' '])[0].Length);
    }

    public static IRenderable Create(string text, string mainColor, string shadowColor)
    {
        var markup = new StringBuilder();

        for (int row = 0; row < Height; row++)
        {
            string line = string.Concat(
                text.ToUpperInvariant().Select(c =>
                    Glyphs.TryGetValue(c, out var glyph) ? glyph[row] : Glyphs[' '][row]));

            AppendColored(markup, line, i => line[i] switch
            {
                ' ' => null,
                '█' => mainColor,
                _ => shadowColor
            });

            if (row < Height - 1)
            {
                markup.Append('\n');
            }
        }

        return Align.Center(new Markup(markup.ToString()));
    }

    /// <summary>
    /// Appends <paramref name="line"/> as markup, grouping runs of characters
    /// that share the same color. <paramref name="colorAt"/> maps a character
    /// index to its color; null means unstyled.
    /// </summary>
    internal static void AppendColored(
        StringBuilder markup,
        string line,
        Func<int, string?> colorAt)
    {
        int i = 0;

        while (i < line.Length)
        {
            string? color = colorAt(i);
            int start = i;

            while (i < line.Length && colorAt(i) == color)
            {
                i++;
            }

            string run = Markup.Escape(line[start..i]);

            markup.Append(color is null ? run : $"[{color}]{run}[/]");
        }
    }
}
