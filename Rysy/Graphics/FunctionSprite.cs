using Rysy.Selections;

namespace Rysy.Graphics;

/// <summary>
/// Allows returning an arbitrary action as a ISprite
/// </summary>
public record struct FunctionSprite<TData>(TData Data, Action<FunctionSprite<TData>, SpriteRenderCtx, TData> Action) : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; } = Color.White;

    public bool IsLoaded => true;

    public ISelectionCollider GetCollider() 
        => ISelectionCollider.FromRect(0, 0, 0, 0);

    public void Render(SpriteRenderCtx ctx) {
        Action(this, ctx, Data);
    }

    public ISprite WithMultipliedAlpha(float alpha) => this with {
        Color = Color * alpha,
    };
}
