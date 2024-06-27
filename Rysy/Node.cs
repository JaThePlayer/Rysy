using KeraLua;
using LuaSharpener;
using Rysy.History;
using Rysy.LuaSupport;
using Rysy.Selections;

namespace Rysy;

public sealed class Node : IDepth, ILuaWrapper, ILuaTable {
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

#if LuaSharpener
    private float GetTable_x() => X;
    private float GetTable_y() => Y;
    
    object? ILuaTable.this[object? key] {
        get => key switch {
            "x" => X,
            "y" => Y,
            _ => null,
        };
        set =>
            Pos = key switch {
                "x" => new(value.TryAsFloat() ?? X, Y),
                "y" => new(X, value.TryAsFloat() ?? Y),
                _ => Pos
            };
    }
#endif
    
    object? ILuaTable.this[object? key] {
        get => null;
        set { }
    }
}

sealed record class NodeSelectionHandler : ISelectionHandler, ISelectionPreciseRotationHandler {
    private EntitySelectionHandler Handler;
    private int PrevNodeId;
    private Entity LastEntity;

    public Entity Entity => Handler.Entity;

    public Node Node;

    public NodeSelectionHandler(EntitySelectionHandler entity, Node node) {
        Handler = entity;
        Node = node;

        PrevNodeId = NodeIdx;
        LastEntity = Entity;
    }

    internal NodeSelectionHandler(EntitySelectionHandler entity, Node node, int nodeId) : this(entity, node) {
        PrevNodeId = nodeId;
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

            return _Collider ??= Entity.GetNodeSelection(PrevNodeId);
        }
    }

    internal void RecalculateId() {
        PrevNodeId = Entity.Nodes.IndexOf(Node);
        ClearCollideCache();
    }

    public Rectangle Rect => Collider.Rect;

    public IHistoryAction? MoveBy(Vector2 offset) {
        var newEntity = Entity.MoveBy(offset, PrevNodeId, out var shouldDoNormalMove);
        if (shouldDoNormalMove) {
            return new MoveNodeAction(Node, Entity, offset);
        }

        return newEntity is { } ? Handler.FlipImpl(newEntity, nameof(Entity.MoveBy)) : null;
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
            Entity.CreateNodeSelection(NodeIdx + 1, new NodeSelectionHandler(Handler, node, NodeIdx + 1)).Handler
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
            Node = Entity.Nodes[PrevNodeId];
            PrevNodeId = NodeIdx;
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

    public IHistoryAction? TryPreciseRotate(float angle, Vector2 origin) {
        if (!Entity.CreateNodeSelection(PrevNodeId).Check(origin)) {
            return null;
        }
        if (Entity.RotatePreciseBy(angle, origin) is not { } rotated) {
            return null;
        }

        return Handler.FlipImpl(rotated, nameof(Entity.RotatePreciseBy));
    }
}
