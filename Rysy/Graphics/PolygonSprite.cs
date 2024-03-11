using Rysy.Selections;
using Triangulator;

namespace Rysy.Graphics;

public record struct PolygonSprite : ISprite {
    private Vector2[] Nodes;

    private VertexPositionColor[]? VertexPositionColors;

    private Rectangle? _bounds;

    public PolygonSprite(IEnumerable<Vector2> nodes, WindingOrder? windingOrder = null) {
        Nodes = nodes.ToArray();
        Order = windingOrder;
    }

    public PolygonSprite(IEnumerable<Vector2> nodes, Color color, Color outlineColor = default, WindingOrder? windingOrder = null) : this(nodes, windingOrder) {
        Color = color;
        OutlineColor = outlineColor;
    }

    public PolygonSprite(VertexPositionColor[] vertexes) {
        VertexPositionColors = vertexes;
        Nodes = [];
    }

    public int? Depth { get; set; }
    public Color Color { get; set; } = Color.White;

    public Color OutlineColor { get; set; } = default;

    public bool IsLoaded => true;

    public WindingOrder? Order { get; set; }

    public ISelectionCollider GetCollider() 
        => ISelectionCollider.FromRect(0, 0, 0, 0);
    
    public void Render(SpriteRenderCtx ctx) {
        if (Nodes.Length < 3 && VertexPositionColors is null)
            return;
        
        VertexPositionColors ??= GetFillVertsFromNodes(Nodes, Color, Order);
        if (VertexPositionColors.Length < 3)
            return;

        var cam = ctx.Camera;
        var prevSettings = GFX.EndBatch();
        var matrix = prevSettings?.TransformMatrix;
        if (matrix is null && cam is { }) {
            matrix = cam.Matrix * (Matrix.CreateTranslation(ctx.CameraOffset.X * cam.Scale, ctx.CameraOffset.Y * cam.Scale, 0f));
        }

        matrix ??= Matrix.Identity;

        if (matrix is { } m) {
            GFX.DrawVertices(m, VertexPositionColors, VertexPositionColors.Length);
        }
        GFX.BeginBatch(prevSettings);
        
        if (OutlineColor != default) {
            LineSprite.DoRender(cam, ctx.CameraOffset, Nodes, OutlineColor, ref _bounds, connectFirstWithLast: true);
        }
    }

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
            VertexPositionColors = VertexPositionColors?
                .Select(vpc => vpc with { Color = vpc.Color * alpha })
                .ToArray(),
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
