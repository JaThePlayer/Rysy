﻿using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Selections;

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
        if (Handler.IsWithinRectangle(roomPos))
            return true;

        return false;
    }

    public void Render(Color c) => Handler.RenderSelection(c);
}

public interface ISelectionCollider {
    public Rectangle Rect { get; }

    public bool IsWithinRectangle(Rectangle roomPos);

    public void Render(Color c);

    public static ISelectionCollider FromRect(Rectangle rect) => new RectangleSelection() { Rect = rect };
    public static ISelectionCollider FromRect(int x, int y, int w, int h) => FromRect(new(x, y, w, h));
    public static ISelectionCollider FromRect(float x, float y, int w, int h) => FromRect(new((int) x, (int) y, w, h));
    public static ISelectionCollider FromRect(float x, float y, float w, float h) => FromRect(new((int) x, (int) y, (int) w, (int) h));
    public static ISelectionCollider FromRect(Vector2 pos, int w, int h) => FromRect(new((int) pos.X, (int) pos.Y, w, h));
    public static ISelectionCollider FromSprite(Sprite s) => new SpriteSelection(s);
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

    public Rectangle Rect { get; }

    public virtual void OnDeselected() { }
}

public enum RotationDirection {
    Left = -1,
    Right = 1
}

public interface ISelectionFlipHandler {
    public IHistoryAction? TryFlipHorizontal();
    public IHistoryAction? TryFlipVertical();
    public IHistoryAction? TryRotate(RotationDirection dir);
}

public interface ISelectionPreciseRotationHandler {
    public IHistoryAction? TryPreciseRotate(float angle, bool isSimulation);
}

[Flags]
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

public static class SelectionLayerExt {
    public static string FastToString(this SelectionLayer layer) {
        return layer switch {
            SelectionLayer.None => nameof(SelectionLayer.None),
            SelectionLayer.Entities => nameof(SelectionLayer.Entities),
            SelectionLayer.Triggers => nameof(SelectionLayer.Triggers),
            SelectionLayer.FGDecals => nameof(SelectionLayer.FGDecals),
            SelectionLayer.BGDecals => nameof(SelectionLayer.BGDecals),
            SelectionLayer.FGTiles => nameof(SelectionLayer.FGTiles),
            SelectionLayer.BGTiles => nameof(SelectionLayer.BGTiles),
            SelectionLayer.Rooms => nameof(SelectionLayer.Rooms),
            SelectionLayer.All => nameof(SelectionLayer.All),
            _ => layer.ToString(),
        };
    }
}

public record class RectangleSelection : ISelectionCollider {
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
}

public record class SpriteSelection(Sprite Sprite) : ISelectionCollider {
    private Vector2 DrawOffset;
    private Point SizeOffset;

    public Rectangle Rect => GetRect() ?? new Rectangle((int) Sprite.Pos.X, (int) Sprite.Pos.Y, 2, 2);

    public void MoveBy(Vector2 offset) {
        DrawOffset += offset;
    }

    public void ResizeBy(Point offset) {
        SizeOffset += offset;
    }

    private Rectangle? GetRect() {
        return Sprite.GetRenderRect()?.MovedBy(DrawOffset).AddSize(SizeOffset);
    }

    public bool IsWithinRectangle(Rectangle roomPos) {
        return (GetRect() ?? new Rectangle((int) Sprite.Pos.X, (int) Sprite.Pos.Y, 2, 2)).Intersects(roomPos);
    }

    public void Render(Color c) {
        if (GetRect() is { } r)
            ISprite.OutlinedRect(r, c * 0.4f, c).Render();

    }
}

public class MergedSpriteSelection : ISelectionCollider {
    List<Sprite> Sprites;

    public MergedSpriteSelection(IEnumerable<ISprite> sprites) {
        Sprites = sprites.OfType<Sprite>().ToList();
    }

    private Vector2 DrawOffset;
    private Point SizeOffset;

    public Rectangle Rect => GetRectangle();

    public void MoveBy(Vector2 offset) {
        DrawOffset += offset;
    }

    public void ResizeBy(Point offset) {
        SizeOffset += offset;
    }

    private Rectangle GetRectangle() {
        var rects = Sprites.SelectWhereNotNull(s => s.GetRenderRect());

        return RectangleExt.Merge(rects);
    }

    public bool IsWithinRectangle(Rectangle roomPos) {
        return GetRectangle().MovedBy(DrawOffset).AddSize(SizeOffset).Intersects(roomPos);
    }

    public void Render(Color c) {
        if (GetRectangle() is { } r)
            ISprite.OutlinedRect(r.MovedBy(DrawOffset), c * 0.4f, c).Render();
    }
}
