using Rysy.Extensions;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Selections;

public class EntitySelectionHandler : ISelectionHandler, ISelectionFlipHandler, ISelectionPreciseRotationHandler {
    public EntitySelectionHandler(Entity entity) {
        Entity = entity;
        entity.Selected = true;
        Entity.ClearRoomRenderCache();
    }

    public void OnDeselected() {
        Entity.ClearRoomRenderCache();
        Entity.Selected = false;
    }

    public Entity Entity { get; set; }

    private Entity? PreSimulationEntity { get; set; }

    public object Parent => Entity;

    public SelectionLayer Layer => Entity.GetSelectionLayer();

    private ISelectionCollider? _Collider;
    private ISelectionCollider Collider => _Collider ??= Entity.GetMainSelection();

    public Rectangle Rect => Collider.Rect;

    public IHistoryAction DeleteSelf() {
        return new RemoveEntityAction(Entity, Entity.Room);
    }

    public bool IsWithinRectangle(Rectangle roomPos) => Collider.IsWithinRectangle(roomPos);

    public IHistoryAction MoveBy(Vector2 offset) {
        return new MoveEntityAction(Entity, offset);
    }

    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
        var nodeLimitsEnd = Entity.NodeLimits.End;
        if (!nodeLimitsEnd.IsFromEnd && (Entity.Nodes?.Count ?? 0) == nodeLimitsEnd.Value)
            return null;

        var node = new Node(pos ?? (Entity.Pos + new Vector2(16f, 0)));

        return (
            new AddNodeAction(Entity, node, 0),
            new NodeSelectionHandler(Entity, node)
        );
    }

    public void RenderSelection(Color c) {
        Collider.Render(c);

        if (!Entity.Selected) {

            Entity.Selected = true;
            Entity.ClearRoomRenderCache();
        }
    }

    public IHistoryAction? TryResize(Point delta) {
        var resizableX = Entity.ResizableX;
        var resizableY = Entity.ResizableY;

        if ((resizableX && delta.X != 0) || (resizableY && delta.Y != 0)) {
            return new EntityResizeAction(Entity, delta);
        }

        return null;
    }

    public void ClearCollideCache() {
        _Collider = null;
    }

    private IHistoryAction? FlipImpl(Entity? flipped, string funcName) {
        var orig = Entity;

        if (flipped is null)
            return null;

        if (orig == flipped)
            throw new Exception($"When implementing Entity.{funcName}, don't return or manipulate 'this'!");

        Entity = flipped;
        return new SwapEntityAction(orig, flipped);
    }

    IHistoryAction? ISelectionFlipHandler.TryFlipHorizontal() {
        var flipped = Entity.TryFlipHorizontal();

        return FlipImpl(flipped, "TryFlipHorizontal");
    }

    IHistoryAction? ISelectionFlipHandler.TryFlipVertical() {
        var flipped = Entity.TryFlipVertical();

        return FlipImpl(flipped, "TryFlipVertical");
    }

    public IHistoryAction? TryRotate(RotationDirection dir) {
        var rotated = Entity.TryRotate(dir);

        return FlipImpl(rotated, nameof(TryRotate));
    }

    public void OnRightClicked(IEnumerable<Selection> selections) {
        CreateEntityPropertyWindow(Entity, selections);
    }

    public static void CreateEntityPropertyWindow(Entity main, IEnumerable<Selection> selections) {
        var history = EditorState.History;

        if (history is { }) {
            var allEntities = selections.SelectWhereNotNull(s => {
                switch (s.Handler) {
                    case EntitySelectionHandler entitySelect:
                        if (entitySelect.Entity.GetType() == main.GetType())
                            return entitySelect.Entity;
                        break;
                    case NodeSelectionHandler entitySelect:
                        if (entitySelect.Entity.GetType() == main.GetType())
                            return entitySelect.Entity;
                        break;
                    default:
                        break;
                }

                return null;
            }).Distinct().ToList();
            RysyEngine.Scene.AddWindow(new EntityPropertyWindow(history, main, allEntities));
        }
    }

    public BinaryPacker.Element? PackParent() {
        return Entity.Pack();
    }

    public IHistoryAction PlaceClone(Room room) {
        return new AddEntityAction(Entity.Clone(), room);
    }

    public IHistoryAction? TryPreciseRotate(float angle, bool isSimulation) {
        if (Entity is not IPreciseRotatable rotatable) {
            return null;
        }

        var rotated = rotatable.RotatePreciseBy(angle);
        if (isSimulation) {
            PreSimulationEntity ??= Entity;
        } else {
            new SwapEntityAction(Entity, PreSimulationEntity!).Apply();
            Entity = PreSimulationEntity!;
            PreSimulationEntity = null;
        }

        return FlipImpl(rotated, nameof(IPreciseRotatable.RotatePreciseBy));
    }
}