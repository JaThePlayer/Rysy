namespace Rysy.Graphics;

public record struct CircleSprite : ISprite
{
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public float Alpha
    {
        get => Color.A / 255f;
        set
        {
            Color = new Color(Color, value);
        }
    }

    public bool IsLoaded => true;


    private int _Resolution;
    /// <summary>
    /// Determines the quality of the circle, higher numbers are more laggy.
    /// This is 1/4 of the amount of lines used to render the circle
    /// </summary>
    public int Resulution
    {
        get => _Resolution / 4;
        set => _Resolution = value * 4;
    }

    public float Radius;
    public Vector2 Pos;

    public void Render()
    {
        GFX.Batch.DrawCircle(Pos, Radius, _Resolution, Color);
    }
}
