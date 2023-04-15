using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

public class Selection {
    public Selection() { }

    public Selection(ISelectionHandler handler) => Handler = handler;

    public ISelectionHandler Handler;

    /// <summary>
    /// Checks if <paramref name="roomPos"/> intersects this selection.
    /// </summary>
    /// <param name="roomPos"></param>
    /// <returns></returns>
    public bool Check(Rectangle roomPos) {
        if (Handler.IsWithinRectangle(roomPos)) {
            return true;
        }

        return false;
    }

    public void Render(Color c) => Handler.RenderSelection(c);
}

public interface ISelectionCollider {
    public bool IsWithinRectangle(Rectangle roomPos);

    public void Render(Color c);

    public static ISelectionCollider FromRect(Rectangle rect) => new RectangleSelection() { Rect = rect };
    public static ISelectionCollider FromRect(int x, int y, int w, int h) => FromRect(new(x, y, w, h));
    public static ISelectionCollider FromRect(float x, float y, int w, int h) => FromRect(new((int) x, (int) y, w, h));
    public static ISelectionCollider FromRect(float x, float y, float w, float h) => FromRect(new((int) x, (int) y, (int) w, (int) h));
    public static ISelectionCollider FromRect(Vector2 pos, int w, int h) => FromRect(new((int) pos.X, (int) pos.Y, w, h));
    public static ISelectionCollider FromSprite(Sprite s) => new SpriteSelection(s);
}

/// <summary>
/// Provides methods needed by the Selection tool to be able to perform operations on this object.
/// </summary>
public interface ISelectionHandler {
    /// <summary>
    /// Returns a history action representing the action of moving this selectable by the given offset.
    /// Calling this method should not have side effects
    /// </summary>
    public IHistoryAction MoveBy(Vector2 offset);
    public IHistoryAction DeleteSelf();
    public IHistoryAction? TryResize(Point delta);
    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos = null);

    public void RenderSelection(Color c);
    public bool IsWithinRectangle(Rectangle roomPos);
    public void ClearCollideCache();
    public void OnRightClicked(IEnumerable<Selection> selections);

    public BinaryPacker.Element? PackParent();

    /// <summary>
    /// The parent object which this handler manipulates.
    /// </summary>
    public object Parent { get; }

    public SelectionLayer Layer { get; }

    /// <summary>
    /// Places the entity 
    /// </summary>
    public IHistoryAction PlaceClone(Room room);
}

public interface ISelectionFlipHandler {
    public IHistoryAction? TryFlipHorizontal();
    public IHistoryAction? TryFlipVertical();
}

public enum SelectionLayer {
    None = 0,
    Entities = 1 << 0,
    Triggers = 1 << 1,
    FGDecals = 1 << 2,
    BGDecals = 1 << 3,
    FGTiles = 1 << 4,
    BGTiles = 1 << 5,
    Rooms = 1 << 6,

    // intentionally omits "Rooms"
    All = Entities | Triggers | FGDecals | BGDecals | FGTiles | BGTiles,
}

public record class RectangleSelection : ISelectionCollider {
    public Rectangle Rect;

    public void MoveBy(Vector2 offset) {
        Rect.X += (int) offset.X;
        Rect.Y += (int) offset.Y;
    }

    public void ResizeBy(Point offset) {
        Rect.Width += offset.X;
        Rect.Height += offset.Y;
    }

    public bool IsWithinRectangle(Rectangle roomPos) {
        return Rect.Intersects(roomPos);
    }

    public void Render(Color c) {
        ISprite.OutlinedRect(Rect, c * 0.4f, c).Render();
    }
}

public record class SpriteSelection(Sprite Sprite) : ISelectionCollider {
    private Vector2 DrawOffset;
    private Point SizeOffset;

    public void MoveBy(Vector2 offset) {
        DrawOffset += offset;
    }

    public void ResizeBy(Point offset) {
        SizeOffset += offset;
    }

    public bool IsWithinRectangle(Rectangle roomPos) {
        return Sprite.GetRenderRect()?.MovedBy(DrawOffset).AddSize(SizeOffset).Intersects(roomPos) ?? false;
    }

    public void Render(Color c) {
        if (Sprite.GetRenderRect() is { } r) {
            ISprite.OutlinedRect(r.MovedBy(DrawOffset), c * 0.4f, c).Render();
        }
            
    }
}
