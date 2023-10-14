using Rysy.Selections;
using Triangulator;

namespace Rysy.Graphics;

public record struct PolygonSprite : ISprite {
    private Vector2[] Nodes;

    private VertexPositionColor[]? VertexPositionColors;

    public PolygonSprite(IEnumerable<Vector2> nodes, WindingOrder? windingOrder = null) {
        Nodes = nodes.ToArray();
        Order = windingOrder;
    }

    public int? Depth { get; set; }
    public Color Color { get; set; }

    public bool IsLoaded => true;

    public WindingOrder? Order { get; set; }

    public ISelectionCollider GetCollider() 
        => ISelectionCollider.FromRect(0, 0, 0, 0);

    public void Render() {
        if (Nodes.Length < 3)
            return;
        
        VertexPositionColors ??= GetFillVertsFromNodes(Nodes, Color, Order);

        if (VertexPositionColors.Length < 3)
            return;

        GFX.DrawVertices(Matrix.Identity, VertexPositionColors, VertexPositionColors.Length);
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (Nodes.Length < 3)
            return;

        if (cam is { }) {
            VertexPositionColors ??= GetFillVertsFromNodes(Nodes, Color, Order);

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

    public PolygonSprite WithWindingOrder(WindingOrder? order) => this with {
        Order = order,
    };

    public static VertexPositionColor[] GetFillVertsFromNodes(Vector2[] nodes, Color color, WindingOrder? inputWindingOrder) {
        Triangulator.Triangulator.Triangulate(nodes, WindingOrder.CounterClockwise, inputWindingOrder, out var verts, out var indices);

        var fill = new VertexPositionColor[indices.Length];
        for (int i = 0; i < indices.Length; i++) {
            ref var f = ref fill[i];

            f.Position = new(verts[indices[i]], 0f);
            f.Color = color;
        }

        return fill;
    }
}
