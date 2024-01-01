using Rysy.Extensions;
using Rysy.Selections;
using System.Collections;

namespace Rysy.Graphics;

public record struct LineSprite : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; }
    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
        };
    }

    public bool IsLoaded => true;

    public List<Vector2> Positions { get; set; }

    public float Thickness { get; set; } = 1;
    public float MagnitudeOffset { get; set; } = 0f;

    public Vector2 Offset { get; set; } = default;

    public bool ConnectFirstWithLast { get; set; } = false;

    private Rectangle? Bounds;

    public LineSprite(IEnumerable<Vector2> positions) {
        Positions = positions.ToListIfNotList();
    }

    public LineSprite MovedBy(Vector2 by) => MovedBy(by.X, by.Y);

    public LineSprite MovedBy(float x, float y) {
        var positions = Positions;
        var newPositions = new List<Vector2>(positions.Count);
        for (int i = 0; i < newPositions.Count; i++) {
            newPositions[i] = positions[i].Add(x, y);
        }

        return this with {
            Positions = newPositions,
        };
    }

    public void Render(SpriteRenderCtx ctx) {
        DoRender(ctx.Camera, ctx.CameraOffset, Positions, Color, ref Bounds, Thickness, Offset, MagnitudeOffset, ConnectFirstWithLast);
    }

    internal static void DoRender(Camera? cam, Vector2 offset, IList<Vector2> positions, Color c, ref Rectangle? bounds, 
        float thickness = 1f, Vector2 renderOffset = default, float magnitudeOffset = 0f,
        bool connectFirstWithLast = false) {
        if (cam is { }) {
            bounds ??= RectangleExt.FromPoints(positions);
            
            var realBounds = bounds.Value.MovedBy(offset);
            if (!cam.IsRectVisible(realBounds))
                return;

            if (cam.IsRectContained(realBounds)) {
                // all lines are visible, no point doing cull checks on each line
                cam = null;
            }
        }

        var b = GFX.Batch;
        for (int i = 0; i < positions.Count - 1; i++) {
            var start = positions[i];
            var end = positions[i + 1];
            if (cam?.IsRectVisible(RectangleExt.FromPoints(start + offset, end + offset)) ?? true)
                b.DrawLine(start, end, c, thickness, renderOffset, magnitudeOffset);
        }

        
        if (connectFirstWithLast && positions is [var first, .., var last]) {
            if (cam?.IsRectVisible(RectangleExt.FromPoints(first + offset, last + offset)) ?? true)
                b.DrawLine(last, first, c, thickness, renderOffset, magnitudeOffset);
        }
    }

    public ISelectionCollider GetCollider() {
        return ISelectionCollider.FromRect(Bounds ??= RectangleExt.FromPoints(Positions));
    }
}
