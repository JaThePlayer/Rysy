using Rysy.Graphics;

namespace Rysy;

public class Selection {
    public Selection() { }

    public ISelectionProvider Main;

    public List<ISelectionProvider> Nodes;

    public static Selection FromRect(Rectangle r) {
        return new Selection() {
            Main = new RectangleSelection(r),
        };
    }

    public static Selection FromSprite(Sprite s) {
        return new Selection() {
            Main = new SpriteSelection(s),
        };
    }

    /// <summary>
    /// Checks if <paramref name="roomPos"/> intersects this selection. <paramref name="nodeIdx"/> is set to -1 if the main selection is selected.
    /// </summary>
    /// <param name="roomPos"></param>
    /// <param name="nodeIdx"></param>
    /// <returns></returns>
    public bool Check(Vector2 roomPos, out int nodeIdx) {
#warning TODO: Nodes

        nodeIdx = -1;
        if (Main?.Overlaps(roomPos) ?? false) {
            return true;
        }
            


        return false;
    }
}

public interface ISelectionProvider {
    public bool Overlaps(Vector2 roomPos);

    public void Render(Color c);
}

public record class RectangleSelection(Rectangle Rect) : ISelectionProvider {
    public bool Overlaps(Vector2 roomPos) {
        return Rect.Contains(roomPos);
    }

    public void Render(Color c) {
        ISprite.OutlinedRect(Rect, c * 0.2f, c).Render();
    }
}

public record class SpriteSelection(Sprite Sprite) : ISelectionProvider {
    public bool Overlaps(Vector2 roomPos) {
        return Sprite.GetRenderRect()?.Contains(roomPos) ?? false;
    }

    public void Render(Color c) {
        if (Sprite.GetRenderRect() is { } r) {
            ISprite.OutlinedRect(r, c * 0.2f, c).Render();
        }
            
    }
}
