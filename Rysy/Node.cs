using Rysy.History;

namespace Rysy;

public sealed class Node  {
    public Node(Vector2 pos) { Pos = pos; }
    public Node(float x, float y) { Pos = new(x, y); }

    public Vector2 Pos;

    public float X => Pos.X;
    public float Y => Pos.Y;

    public static implicit operator Vector2(Node node) => node.Pos;
    public static implicit operator Node(Vector2 node) => new(node);

    public ISelectionHandler ToSelectionHandler(Entity entity) => new NodeSelectionHandler(entity, this);
}

sealed record class NodeSelectionHandler(Entity Entity, Node Node) : ISelectionHandler {
    public IHistoryAction MoveBy(Vector2 offset) {
        return new MoveNodeAction(Node, Entity, offset);
    }

    public IHistoryAction DeleteSelf() {
        return new RemoveNodeAction(Node, Entity);
    }
}
