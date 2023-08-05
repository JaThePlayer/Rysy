using Rysy.Selections;
using Triangulator;

namespace Rysy.Graphics;

public record struct PolygonSprite : ISprite {
    private Vector2[] Nodes;

    private VertexPositionColor[]? VertexPositionColors;

    public PolygonSprite(IEnumerable<Vector2> nodes) {
        Nodes = nodes.ToArray();
    }

    public int? Depth { get; set; }
    public Color Color { get; set; }

    public bool IsLoaded => true;

    public ISelectionCollider GetCollider() 
        => ISelectionCollider.FromRect(0, 0, 0, 0);
    public void Render() {
        if (Nodes.Length < 3)
            return;
        
        VertexPositionColors ??= GetFillVertsFromNodes(Nodes, Color);

        if (VertexPositionColors.Length < 3)
            return;

        GFX.DrawVertices(Matrix.Identity, VertexPositionColors, VertexPositionColors.Length);
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (Nodes.Length < 3)
            return;

        if (cam is { }) {
            VertexPositionColors ??= GetFillVertsFromNodes(Nodes, Color);

            if (VertexPositionColors.Length < 3)
                return;
            GFX.EndBatch();
            GFX.DrawVertices(cam.Matrix * (Matrix.CreateTranslation(offset.X * cam.Scale, offset.Y * cam.Scale, 0f)), VertexPositionColors, VertexPositionColors.Length);

            GFX.BeginBatchWithPreviousSettings();
        }
    }

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
            VertexPositionColors = null,
        };
    }

    public static VertexPositionColor[] GetFillVertsFromNodes(Vector2[] nodes, Color color) {
        Triangulator.Triangulator.Triangulate(nodes, WindingOrder.CounterClockwise, out var verts, out var indices);

        var fill = new VertexPositionColor[indices.Length];
        for (int i = 0; i < indices.Length; i++) {
            ref var f = ref fill[i];

            f.Position = new(verts[indices[i]], 0f);
            f.Color = color;
        }

        return fill;
    }
}
