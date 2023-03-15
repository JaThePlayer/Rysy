namespace Rysy.Graphics;

/// <summary>
/// Allows rendering PICO-8 text, centered into a rectangle.
/// </summary>
public record struct PicoTextRectSprite : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public void MultiplyAlphaBy(float alpha) {
        Color *= alpha;
    }

    public bool IsLoaded => true;

    public string Text;
    public Rectangle Pos;
    public float Scale;

    public PicoTextRectSprite(string text) {
        Text = text;
    }

    public void Render() {
        PicoFont.Print(Text, Pos, Color, Scale);
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (cam is { }) {
            if (!cam.IsRectVisible(Pos.MovedBy(offset)))
                return;
        }

        Render();
    }
}
