using Rysy.Selections;

namespace Rysy.Graphics;

public record struct CircleSprite : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
        };
    }

    public bool IsLoaded => true;


    private int _Resolution;
    /// <summary>
    /// Determines the quality of the circle, higher numbers are more laggy.
    /// This is 1/4 of the amount of lines used to render the circle
    /// </summary>
    public int Resulution {
        get => _Resolution / 4;
        set => _Resolution = value * 4;
    }

    public float Radius;
    public Vector2 Pos;

    public void Render() {
        GFX.Batch.DrawCircle(Pos, Radius, _Resolution, Color);
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (cam is { }) {
            if (!cam.IsRectVisible(Pos + offset - new Vector2(Radius), (int) Radius * 2, (int) Radius * 2))
                return;
        }
        Render();
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromRect(Pos - new Vector2(Radius), (int) Radius * 2, (int) Radius * 2);

    /// <summary>
    /// Gets a point on the circle at the specified angle, with angle 0 being at the intersection of the x-axis and the unit circle.
    /// </summary>
    public Vector2 PointAtAngle(float angle) => Pos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * Radius;
}
