using Rysy.Selections;
using Triangulator;

namespace Rysy.Graphics;

public record struct PolygonSprite : ISprite {
    private Vector2[] _nodes;

    private VertexPositionColor[]? _vertexPositionColors;

    private Rectangle? _bounds;

    public PolygonSprite(IEnumerable<Vector2> nodes, WindingOrder? windingOrder = null) {
        _nodes = nodes is Vector2[] arr ? arr : nodes.ToArray();
        Order = windingOrder;
    }

    public PolygonSprite(IEnumerable<Vector2> nodes, Color color, Color outlineColor = default, WindingOrder? windingOrder = null) : this(nodes, windingOrder) {
        Color = color;
        OutlineColor = outlineColor;
    }

    public PolygonSprite(VertexPositionColor[] vertexes) {
        _vertexPositionColors = vertexes;
        _nodes = [];
    }

    public int? Depth { get; set; }
    public Color Color { get; set; } = Color.White;

    public Color OutlineColor { get; set; } = default;

    public bool IsLoaded => true;

    public WindingOrder? Order { get; set; }

    public ISelectionCollider GetCollider() 
        => ISelectionCollider.FromRect(0, 0, 0, 0);
    
    public void Render(SpriteRenderCtx ctx) {
        if (_nodes.Length < 3 && _vertexPositionColors is null)
            return;
        
        _vertexPositionColors ??= GetFillVertsFromNodes(_nodes, Color, Order);
        if (_vertexPositionColors.Length < 3)
            return;

        var cam = ctx.Camera;
        var prevSettings = Gfx.EndBatch();
        var matrix = prevSettings?.TransformMatrix;
        if (matrix is null && cam is { }) {
            matrix = cam.Matrix * (Matrix.CreateTranslation(ctx.CameraOffset.X * cam.Scale, ctx.CameraOffset.Y * cam.Scale, 0f));
        }

        matrix ??= Matrix.Identity;

        if (matrix is { } m) {
            if (prevSettings is { ScissorRect: { } scissorRect }) {
                Gfx.Batch.GraphicsDevice.ScissorRectangle = scissorRect;
            }
            Gfx.DrawVertices(m, _vertexPositionColors, _vertexPositionColors.Length, rasterizerState: prevSettings?.RasterizerState);
        }
        Gfx.BeginBatch(prevSettings);
        
        if (OutlineColor != default) {
            LineSprite.DoRender(cam, ctx.CameraOffset, _nodes, OutlineColor, ref _bounds, connectFirstWithLast: true);
        }
    }

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
            _vertexPositionColors = _vertexPositionColors?
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
