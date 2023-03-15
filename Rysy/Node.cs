using Rysy.History;

namespace Rysy;

public sealed class Node : IDepth {
    public Node(Vector2 pos) { Pos = pos; }
    public Node(float x, float y) { Pos = new(x, y); }

    public Vector2 Pos;

    public float X => Pos.X;
    public float Y => Pos.Y;

    public int Depth => int.MinValue; // TODO: grab from entity

    public static implicit operator Vector2(Node node) => node.Pos;
    public static implicit operator Node(Vector2 node) => new(node);
}

sealed record class NodeSelectionHandler(Entity Entity, int NodeIdx) : ISelectionHandler {
    public Node Node => Entity.Nodes![NodeIdx];

    public object Parent => Node;

    private ISelectionCollider? _Collider;
    private ISelectionCollider Collider => _Collider ??= Entity.GetNodeSelection(NodeIdx);

    public IHistoryAction MoveBy(Vector2 offset) {
        return new MoveNodeAction(Node, Entity, offset);
    }

    public IHistoryAction DeleteSelf() {
        return new RemoveNodeAction(Node, Entity);
    }

    public IHistoryAction? TryResize(Point delta) {
        return null;
    }

    public void RenderSelection(Color c) => Collider.Render(c);

    public bool IsWithinRectangle(Rectangle roomPos) => Collider.IsWithinRectangle(roomPos);

    public void ClearCollideCache() {
        _Collider = null;
    }

    public void OnRightClicked(IEnumerable<Selection> selections) {
    }
}
