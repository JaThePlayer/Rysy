namespace Rysy.Graphics;

/// <summary>
/// Represents a sprite that renders a texture
/// </summary>
public interface ITextureSprite : ISprite {
    public Vector2 Pos { get; set; }

    public Rectangle? GetRenderRect();
}