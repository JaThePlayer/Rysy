namespace Rysy.Graphics;

public record struct LineSprite : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public void MultiplyAlphaBy(float alpha) {
        Color *= alpha;
    }

    public bool IsLoaded => true;

    public Vector2[] Positions;

    public int Thickness = 1;

    public LineSprite(Vector2[] positions) {
        Positions = positions;
    }

    public void Render() {
        var b = GFX.Batch;
        var c = Color;
        for (int i = 0; i < Positions.Length - 1; i++) {
            var start = Positions[i];
            var end = Positions[i + 1];
            b.DrawLine(start, end, c);
        }
    }

    public void Render(Camera? cam, Vector2 offset) {
        Render();
    }
}
