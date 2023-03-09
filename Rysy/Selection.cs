using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

public class Selection {
    public Selection() { }

    public ISelectionCollider Collider;

    public ISelectionHandler Handler;

    public static Selection FromRect(ISelectionHandler handler, Rectangle r) {
        return new Selection() {
            Collider = new RectangleSelection { Rect = r },
            Handler = handler,
        };
    }

    public static Selection FromSprite(ISelectionHandler handler, Sprite s) {
        return new Selection() {
            Collider = new SpriteSelection(s),
            Handler = handler,
        };
    }

    /// <summary>
    /// Checks if <paramref name="roomPos"/> intersects this selection.
    /// </summary>
    /// <param name="roomPos"></param>
    /// <returns></returns>
    public bool Check(Rectangle roomPos) {
        if (Collider?.Overlaps(roomPos) ?? false) {
            return true;
        }

        return false;
    }

    public void Render(Color c) => Collider.Render(c);
}

public interface ISelectionCollider {
    public bool Overlaps(Rectangle roomPos);

    public void Render(Color c);

    public void MoveBy(Vector2 offset);

    public void ResizeBy(Point offset);
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

    /// <summary>
    /// The parent object which this handler manipulates.
    /// </summary>
    public object Parent { get; }
}

public enum SelectionLayer {
    None = 0,
    Entities = 1 << 0,
    Triggers = 1 << 1,
    FGDecals = 1 << 2,
    BGDecals = 1 << 3,
    FGTiles = 1 << 4,
    BGTiles = 1 << 5,

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

    public bool Overlaps(Rectangle roomPos) {
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

    public bool Overlaps(Rectangle roomPos) {
        return Sprite.GetRenderRect()?.MovedBy(DrawOffset).AddSize(SizeOffset).Intersects(roomPos) ?? false;
    }

    public void Render(Color c) {
        if (Sprite.GetRenderRect() is { } r) {
            ISprite.OutlinedRect(r.MovedBy(DrawOffset), c * 0.4f, c).Render();
        }
            
    }
}
