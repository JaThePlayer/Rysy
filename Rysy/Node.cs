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

sealed record class NodeSelectionHandler : ISelectionHandler {
    //public Node Node => Entity.Nodes![NodeIdx];
    public Entity Entity;

    public Node Node;

    public NodeSelectionHandler(Entity entity, Node node) {
        Entity = entity;
        Node = node;
    }

    public int NodeIdx => Entity.Nodes!.IndexOf(Node);

    public object Parent => Node;

    public SelectionLayer Layer => Entity.GetSelectionLayer(); 

    private ISelectionCollider? _Collider;
    private ISelectionCollider Collider => _Collider ??= Entity.GetNodeSelection(NodeIdx);

    public IHistoryAction MoveBy(Vector2 offset) {
        Entity.OnChanged();
        return new MoveNodeAction(Node, Entity, offset);
    }

    public IHistoryAction DeleteSelf() {
        var nodesLeft = Entity.Nodes!.Count - 1;
        if (nodesLeft < Entity.NodeLimits.Start.Value) {
            return new RemoveEntityAction(Entity, Entity.Room);
        }

        return new RemoveNodeAction(Node, Entity);
    }

    public IHistoryAction? TryResize(Point delta) {
        return null;
    }

    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
        var maxNodes = Entity.NodeLimits.End;
        if (!maxNodes.IsFromEnd && (Entity.Nodes?.Count ?? 0) == maxNodes.Value)
            return null;

        var node = new Node(pos ?? (Node.Pos + new Vector2(16f, 0)));

        Entity.OnChanged();

        return (
            new AddNodeAction(Entity, node, NodeIdx + 1),
            new NodeSelectionHandler(Entity, node)
        );
    }

    public void RenderSelection(Color c) => Collider.Render(c);

    public bool IsWithinRectangle(Rectangle roomPos) => Collider.IsWithinRectangle(roomPos);

    public void ClearCollideCache() {
        _Collider = null;
    }

    public void OnRightClicked(IEnumerable<Selection> selections) {
        EntitySelectionHandler.CreateEntityPropertyWindow(Entity, selections);
    }

    public BinaryPacker.Element? PackParent() {
        return Entity.Pack();
    }

    public IHistoryAction PlaceClone(Room room) {
        return IHistoryAction.Empty;
    }
}
