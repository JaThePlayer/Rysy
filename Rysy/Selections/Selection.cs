using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Selections;

public struct Selection : IEquatable<Selection> {
    public Selection() { }

    public Selection(ISelectionHandler handler) => Handler = handler;

    public ISelectionHandler Handler;

    /// <summary>
    /// Checks if <paramref name="roomPos"/> intersects this selection.
    /// </summary>
    public bool Check(Rectangle roomPos) {
        if (Handler.IsWithinRectangle(roomPos))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the point <paramref name="roomPos"/> is contained within this selection.
    /// </summary>
    public bool Check(Vector2 roomPos) {
        if (Handler.IsWithinRectangle(new((int) roomPos.X, (int) roomPos.Y, 1, 1)))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if the point (<paramref name="roomX"/>,<paramref name="roomY"/>) is contained within this selection.
    /// </summary>
    public bool Check(float roomX, float roomY) {
        if (Handler.IsWithinRectangle(new((int) roomX, (int) roomY, 1, 1)))
            return true;

        return false;
    }

    public void Render(Color c) => Handler.RenderSelection(c);
    public void RenderHollow(Color c) => Handler.RenderSelectionHollow(c);

    public bool Equals(Selection other)
    {
        return Handler.Equals(other.Handler);
    }

    public override bool Equals(object? obj)
    {
        return obj is Selection other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Handler.GetHashCode();
    }

    public static bool operator ==(Selection left, Selection right) {
        return left.Equals(right);
    }

    public static bool operator !=(Selection left, Selection right) {
        return !(left == right);
    }
}

public interface ISelectionCollider {
    public Rectangle Rect { get; }

    public bool IsWithinRectangle(Rectangle roomPos);

    public void Render(Color c);
    public void RenderHollow(Color c);

    public static ISelectionCollider FromRect(Rectangle rect) => new RectangleSelection() { Rect = rect };
    public static ISelectionCollider FromRect(int x, int y, int w, int h) => FromRect(new(x, y, w, h));
    public static ISelectionCollider FromRect(float x, float y, int w, int h) => FromRect(new((int) x, (int) y, w, h));
    public static ISelectionCollider FromRect(float x, float y, float w, float h) => FromRect(new((int) x, (int) y, (int) w, (int) h));
    public static ISelectionCollider FromRect(Vector2 pos, int w, int h) => FromRect(new((int) pos.X, (int) pos.Y, w, h));

    public static ISelectionCollider FromSprite<T>(T s) where T : ITextureSprite => new SpriteSelection<T>(s);

    public static ISelectionCollider FromSprites(IEnumerable<ISprite> s) => new MergedSpriteSelection(s);
}

/// <summary>
/// Provides methods needed by the Selection tool to be able to perform operations on this object.
/// </summary>
public interface ISelectionHandler {
    /// <summary>
    /// Returns a history action representing the action of moving this selectable by the given offset.
    /// Calling this method should not have side effects
    /// </summary>
    public IHistoryAction? MoveBy(Vector2 offset);
    public IHistoryAction DeleteSelf();
    public IHistoryAction? TryResize(Point delta);
    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos = null);

    public void RenderSelection(Color c);
    public void RenderSelectionHollow(Color c);
    
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

    public Rectangle Rect { get; }

    public virtual void OnSelected() { }

    public virtual void OnDeselected() { }

    //todo: find better name for this
    public virtual IHistoryAction? GetMoveOrResizeAction(Vector2 offset, NineSliceLocation grabbed) {
        var off = offset.ToPoint();
        return grabbed switch {
            NineSliceLocation.TopLeft => new MergedAction(MoveBy(new(off.X, off.Y)), TryResize(new(-off.X, -off.Y))),
            NineSliceLocation.TopMiddle => new MergedAction(MoveBy(new(0, off.Y)), TryResize(new(0, -off.Y))),
            NineSliceLocation.TopRight => new MergedAction(MoveBy(new(0, off.Y)), TryResize(new(off.X, -off.Y))),
            NineSliceLocation.Left => new MergedAction(MoveBy(new(off.X, 0)), TryResize(new(-off.X, 0))),
            NineSliceLocation.Right => TryResize(new(off.X, 0)),
            NineSliceLocation.BottomLeft => new MergedAction(MoveBy(new(off.X, 0)), TryResize(new(-off.X, off.Y))),
            NineSliceLocation.BottomMiddle => TryResize(new(0, off.Y)),
            NineSliceLocation.BottomRight => TryResize(new(off.X, off.Y)),
            _ => MoveBy(offset),
        };
    }

    public bool ResizableX { get; }
    public bool ResizableY { get; }
}

public interface ISelectionFlipHandler {
    public IHistoryAction? TryFlipHorizontal();
    public IHistoryAction? TryFlipVertical();
    public IHistoryAction? TryRotate(RotationDirection dir);
}

public interface ISelectionPreciseRotationHandler {
    public IHistoryAction? TryPreciseRotate(float angle, Vector2 origin);
}

[Flags]
public enum SelectionLayer {
    None = 0,
    Entities = 1 << 0,
    Triggers = 1 << 1,
    FgDecals = 1 << 2,
    BgDecals = 1 << 3,
    FgTiles = 1 << 4,
    BgTiles = 1 << 5,
    Rooms = 1 << 6,

    // intentionally omits "Rooms"
    All = Entities | Triggers | FgDecals | BgDecals | FgTiles | BgTiles,
}

public static class SelectionLayerExt {
    public static string FastToString(this SelectionLayer layer) {
        return layer switch {
            SelectionLayer.None => nameof(SelectionLayer.None),
            SelectionLayer.Entities => nameof(SelectionLayer.Entities),
            SelectionLayer.Triggers => nameof(SelectionLayer.Triggers),
            SelectionLayer.FgDecals => nameof(SelectionLayer.FgDecals),
            SelectionLayer.BgDecals => nameof(SelectionLayer.BgDecals),
            SelectionLayer.FgTiles => nameof(SelectionLayer.FgTiles),
            SelectionLayer.BgTiles => nameof(SelectionLayer.BgTiles),
            SelectionLayer.Rooms => nameof(SelectionLayer.Rooms),
            SelectionLayer.All => nameof(SelectionLayer.All),
            _ => layer.ToString(),
        };
    }
}

public sealed record class RectangleSelection : ISelectionCollider {
    public Rectangle Rect;

    Rectangle ISelectionCollider.Rect => Rect;

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
    
    public void RenderHollow(Color c) {
        ISprite.OutlinedRect(Rect, Color.Transparent, c).Render();
    }
}

public sealed record SpriteSelection<T>(T Sprite) : ISelectionCollider where T : ITextureSprite {
    private Vector2 _drawOffset;
    private Point _sizeOffset;

    private Rectangle? _rect;

    public Rectangle Rect {
        get {
            if (_rect is { } r)
                return r;

            if (GetSpriteRenderRect() is { } correctRect) {
                _rect = correctRect;
                return correctRect;
            }

            // the sprite is not ready yet, let's not cache the selection size
            return new Rectangle((int) Sprite.Pos.X, (int) Sprite.Pos.Y, 2, 2);
        }
    }

    public void MoveBy(Vector2 offset) {
        _drawOffset += offset;
    }

    public void ResizeBy(Point offset) {
        _sizeOffset += offset;
    }

    public bool IsWithinRectangle(Rectangle roomPos) {
        return Rect.Intersects(roomPos);
    }

    public void Render(Color c) {
        ISprite.OutlinedRect(Rect, c * 0.4f, c).Render();
    }
    
    public void RenderHollow(Color c) {
        ISprite.OutlinedRect(Rect, Color.Transparent, c).Render();
    }

    private Rectangle? GetSpriteRenderRect() {
        return Sprite.GetRenderRect()?.MovedBy(_drawOffset).AddSize(_sizeOffset);
    }
}

public sealed class MergedSpriteSelection : ISelectionCollider {
    List<Sprite> _sprites;

    public MergedSpriteSelection(IEnumerable<ISprite> sprites) {
        _sprites = sprites.OfType<Sprite>().ToList();
    }

    private Vector2 _drawOffset;
    private Point _sizeOffset;

    public Rectangle Rect => GetRectangle();

    public void MoveBy(Vector2 offset) {
        _drawOffset += offset;
    }

    public void ResizeBy(Point offset) {
        _sizeOffset += offset;
    }

    private Rectangle GetRectangle() {
        var rects = _sprites.SelectWhereNotNull(s => s.GetRenderRect());

        return RectangleExt.Merge(rects);
    }

    public bool IsWithinRectangle(Rectangle roomPos) {
        return GetRectangle().MovedBy(_drawOffset).AddSize(_sizeOffset).Intersects(roomPos);
    }

    public void Render(Color c) {
        if (GetRectangle() is { } r)
            ISprite.OutlinedRect(r.MovedBy(_drawOffset), c * 0.4f, c).Render();
    }
    
    public void RenderHollow(Color c) {
        if (GetRectangle() is { } r)
            ISprite.OutlinedRect(r.MovedBy(_drawOffset), Color.Transparent, c).Render();
    }
}
