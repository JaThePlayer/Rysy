using KeraLua;
using Rysy.History;
using Rysy.LuaSupport;
using Rysy.Selections;

namespace Rysy;

public sealed class Node : IDepth, ILuaWrapper {
    public Node(Vector2 pos) { Pos = pos; }
    public Node(float x, float y) { Pos = new(x, y); }

    public Vector2 Pos;

    public float X => Pos.X;
    public float Y => Pos.Y;

    public int Depth => int.MinValue; // TODO: grab from entity

    public static implicit operator Vector2(Node node) => node.Pos;
    public static implicit operator Node(Vector2 node) => new(node);

    public int Lua__index(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "x":
                lua.PushNumber(X);
                return 1;
            case "y":
                lua.PushNumber(Y);
                return 1;
            default:
                lua.PushNil();
                return 1;
        }
    }
}

sealed record class NodeSelectionHandler : ISelectionHandler {
    private EntitySelectionHandler Handler;
    private int LastNodeId;
    private Entity LastEntity;

    public Entity Entity => Handler.Entity;

    public Node Node;

    public NodeSelectionHandler(EntitySelectionHandler entity, Node node) {
        Handler = entity;
        Node = node;

        LastNodeId = NodeIdx;
        LastEntity = Entity;
    }

    public void OnDeselected() {
    }

    public int NodeIdx => Entity.Nodes!.IndexOf(Node);

    public object Parent => Node;

    public SelectionLayer Layer => Entity.GetSelectionLayer(); 

    private ISelectionCollider? _Collider;
    private ISelectionCollider Collider {
        get {
            EnsureValid();

            return _Collider ??= Entity.GetNodeSelection(LastNodeId);
        }
    }

    public Rectangle Rect => Collider.Rect;

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
            new NodeSelectionHandler(Handler, node)
        );
    }

    public void RenderSelection(Color c) {
        Collider.Render(c);
    }

    public bool IsWithinRectangle(Rectangle roomPos) => Collider.IsWithinRectangle(roomPos);

    public void ClearCollideCache() {
        _Collider = null;

        if (LastEntity != Entity) {
            // transfer over to the node instance on the new entity
            Node = Entity.Nodes[LastNodeId];
            LastNodeId = NodeIdx;
            LastEntity = Entity;
        }
    }

    private void EnsureValid() {
        if (LastEntity != Entity)
            ClearCollideCache();
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
