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
#pragma warning disable CA2225 // Operator overloads have named alternates - doesn't really make sense to make nodes that way
    public static implicit operator Node(Vector2 node) => new(node);
#pragma warning restore CA2225

    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
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

    public Vector2 ToVector2() => this;
}

sealed record class NodeSelectionHandler : ISelectionHandler, ISelectionPreciseRotationHandler {
    private EntitySelectionHandler _handler;
    private int _prevNodeId;
    private Entity _lastEntity;

    public Entity Entity => _handler.Entity;

    public Node Node;

    public NodeSelectionHandler(EntitySelectionHandler entity, Node node) {
        _handler = entity;
        Node = node;

        _prevNodeId = NodeIdx;
        _lastEntity = Entity;
    }

    internal NodeSelectionHandler(EntitySelectionHandler entity, Node node, int nodeId) : this(entity, node) {
        _prevNodeId = nodeId;
    }

    public void OnDeselected() {
    }

    public int NodeIdx => Entity.Nodes!.IndexOf(Node);

    public object Parent => Node;

    public SelectionLayer Layer => Entity.GetSelectionLayer(); 

    private ISelectionCollider? _collider;
    private ISelectionCollider Collider {
        get {
            EnsureValid();

            return _collider ??= Entity.GetNodeSelection(_prevNodeId);
        }
    }

    internal void RecalculateId() {
        _prevNodeId = Entity.Nodes.IndexOf(Node);
        ClearCollideCache();
    }

    public Rectangle Rect => Collider.Rect;

    public IHistoryAction? MoveBy(Vector2 offset) {
        var newEntity = Entity.MoveBy(offset, _prevNodeId, out var shouldDoNormalMove);
        if (shouldDoNormalMove) {
            return new MoveNodeAction(Node, Entity, offset);
        }

        return newEntity is { } ? _handler.FlipImpl(newEntity, nameof(Entity.MoveBy)) : null;
    }

    public IHistoryAction DeleteSelf() {
        var nodesLeft = Entity.Nodes!.Count - 1;
        if (nodesLeft < Entity.NodeLimits.Start.Value) {
            return new RemoveEntityAction(Entity);
        }

        return new RemoveNodeAction(Node, Entity);
    }

    public IHistoryAction? TryResize(Point delta) {
        return null;
    }

    public bool ResizableX => false;

    public bool ResizableY => false;

    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
        var maxNodes = Entity.NodeLimits.End;
        if (!maxNodes.IsFromEnd && (Entity.Nodes?.Count ?? 0) == maxNodes.Value)
            return null;

        var node = new Node(pos ?? (Node.Pos + new Vector2(16f, 0)));

        return (
            new AddNodeAction(Entity, node, NodeIdx + 1),
            Entity.CreateNodeSelection(NodeIdx + 1, new NodeSelectionHandler(_handler, node, NodeIdx + 1)).Handler
        );
    }

    public void RenderSelection(Color c) {
        Collider.Render(c);
    }
    
    public void RenderSelectionHollow(Color c) {
        Collider.RenderHollow(c);
    }

    public bool IsWithinRectangle(Rectangle roomPos) => Collider.IsWithinRectangle(roomPos);

    public void ClearCollideCache() {
        _collider = null;

        if (_lastEntity != Entity) {
            // transfer over to the node instance on the new entity
            Node = Entity.Nodes[_prevNodeId];
            _prevNodeId = NodeIdx;
            _lastEntity = Entity;
        }
    }

    private void EnsureValid() {
        if (_lastEntity != Entity)
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

    public IHistoryAction? TryPreciseRotate(float angle, Vector2 origin) {
        if (!Entity.CreateNodeSelection(_prevNodeId).Check(origin)) {
            return null;
        }
        if (Entity.RotatePreciseBy(angle, origin) is not { } rotated) {
            return null;
        }

        return _handler.FlipImpl(rotated, nameof(Entity.RotatePreciseBy));
    }
}
