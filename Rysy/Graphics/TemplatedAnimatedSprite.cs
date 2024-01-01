using Rysy.Selections;

namespace Rysy.Graphics
{
    public record struct TemplatedAnimatedSprite(AnimatedSpriteTemplate Template) : ITextureSprite {
        public int? Depth {
            get => Template.Template.Depth;
            set {
                // no-op
            }
        }
    
        public Vector2 Pos { get; set; }

        public Color Color { get; set; }

        public ISprite WithMultipliedAlpha(float alpha) => this with { Color = Color * alpha, };

        public bool IsLoaded => Template.Template.IsLoaded;
    
        public void Render(SpriteRenderCtx ctx) {
            Template.RenderAt(ctx, Pos, Color, default);
        }
        
        public Rectangle? GetRenderRect() 
            => Template.Template.GetRenderRect(Pos);

        public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);
    }
}