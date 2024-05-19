using Rysy.Platforms;

namespace Rysy.Graphics;

/// <summary>
/// Allows rendering text using a modified version of the PICO-8-leste font.
/// </summary>
public static class PicoFont {
    // font size:
    public const int W = 4;
    public const int H = 6;

    private static Dictionary<char, Point> CharacterLocations = new();

    public static VirtTexture? Texture { get; private set; }

    internal static VirtTexture Init() {
        if (Texture is { })
            return Texture;

        // The font is slightly edited in Rysy to include ,/\<>
        var fontMap = @"abcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+-=?:.,/\<> ";
        for (int i = 0; i < fontMap.Length; i++) {
            CharacterLocations[fontMap[i]] = new Point((i % 30) * W, (i / 30) * H);
        }
        
        if (RysyPlatform.Current.GetRysyFilesystem().TryOpenFile("font.png",
                s => VirtTexture.FromTexture(Texture2D.FromStream(RysyState.GraphicsDevice, s)), out var texture)) {
            return Texture = texture!;
        }

        return Texture = GFX.UnknownTexture;
    }

    private static Sprite _GetSprite(char c, Vector2 pos, Color color, float scale) {
        var t = Texture ?? Init();

        if (!CharacterLocations.TryGetValue(char.ToLowerInvariant(c), out var loc)) {
            loc = CharacterLocations['?'];
        }

        return ISprite.FromTexture(pos, t).CreateSubtexture(loc.X, loc.Y, W, H) with {
            Scale = new(scale),
            Color = color,
        };
    }

    public static ISprite GetSprite(char c, Vector2 pos, Color color, float scale = 1f) => _GetSprite(c, pos, color, scale);

    public static void Print(char c, Vector2 pos, Color color, float scale = 1f) {
        _GetSprite(c, pos, color, scale).Render(SpriteRenderCtx.Default(false));
    }

    /// <summary>
    /// Prints a line of text.
    /// </summary>
    public static void Print(ReadOnlySpan<char> txt, Vector2 pos, Color color, float scale = 1f) {
        var startX = pos.X;
        foreach (var c in txt) {
            if (c is '\n') {
                pos.Y += H * scale;
                pos.X = startX;
            } else {
                Print(c, pos, color, scale);
                pos.X += W * scale;
            }
        }
    }

    /// <summary>
    /// Checks if all words in the span are of equal length.
    /// </summary>
    private static bool EqualWordSizes(ReadOnlySpan<char> txt) {
        int idx;
        int? len = null;
        while ((idx = txt.IndexOf(' ')) != -1) {
            len ??= idx;
            if (idx != len)
                return false;
            txt = txt[(idx + 1)..];
        }

        // check the last word
        return txt.Length == len;
    }

    /// <summary>
    /// Prints text inside of a rectangle. Will try to make the text fit inside of the rectangle as best as possible.
    /// Doesn't support newlines.
    /// </summary>
    public static void Print(ReadOnlySpan<char> txt, Rectangle bounds, Color color, float scale = 1f) {
        Init();

        if (scale == 0f)
            return;

        if (bounds.Width == 0 || bounds.Height == 0) {
            Print(txt, bounds.Location.ToVector2(), color, scale);
            return;
        }

        var rw = W * scale;
        var boundWidth = bounds.Width - 2;
        var maxPerLine = (int) (boundWidth / rw).AtLeast(1);

        // Split the text into as few vertical lines as we can fit.
        var lines = (int) Math.Floor((txt.Length / (float) maxPerLine));
        // Check for a non-full line
        if (txt.Length % maxPerLine > 0) {
            lines++;
        }

        if (lines > 0 && (txt.Length % lines == 0 || EqualWordSizes(txt)) && txt.Length / lines * rw < boundWidth) {
            // If we can fit an equal amount of chars on each line, do so.
            maxPerLine = txt.Length / lines;
        }

        var pos = bounds.Location.ToVector2();
        // center the text
        pos += new Vector2((bounds.Width / 2) - (maxPerLine * rw / 2), bounds.Height / 2 - (H * scale * lines / 2));

        // The text cannot fit in our box.
        if (pos.Y < bounds.Top) {
            // first, put the start of the text at the top of the box (minus the outline)
            pos.Y = bounds.Top + 1;

            // then, decrease the amount of lines to the maximum amount we can fit in the box.
            var maxLines = (int) (bounds.Height / (H * scale));
            lines = Math.Min(lines, maxLines);
        }

        int i = 0;
        for (int line = 0; line < lines && i < txt.Length; line++) {
            // Ignore whitespace at the start of a line
            while (char.IsWhiteSpace(txt[i]))
                i++;

            var subStr = txt[i..(Math.Min(i + maxPerLine, txt.Length))];
            Print(subStr, pos, color, scale);

            i += maxPerLine;
            pos.Y += H * scale;
        }
    }
}
