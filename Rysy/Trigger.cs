using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy;

public class Trigger : Entity, INodeSpriteProvider, INodePathProvider {
    public string EditorColor {
        get => EntityData.Attr("_editorColor", Color.LightSkyBlue.ToRGBAString());
        set {
            EntityData["_editorColor"] = value;
            ClearRoomRenderCache();
        }
    }

    public virtual Color Color => EditorColor.FromARGB();

    public virtual Color FillColor => Color * 0.15f;

    public override int Depth => Depths.Top;

    public override bool ResizableX => true;
    public override bool ResizableY => true;
    public override Point MinimumSize => new(8, 8);

    public IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.Line(this, (self, nodeIndex) => self.GetNodeRect(nodeIndex).Center.ToVector2());

    public IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var rect = GetNodeRect(nodeIndex);
        yield return ISprite.OutlinedRect(rect, FillColor, Color);
    }

    public override IEnumerable<ISprite> GetSprites() {
        var rect = new Rectangle(X, Y, Width, Height);
        yield return ISprite.OutlinedRect(rect, FillColor, Color);
        yield return new PicoTextRectSprite() {
            Text = TriggerHelpers.Humanize(EntityData.SID),
            Pos = rect,
            Color = Color.White,
            Scale = 0.5f,
        };
    }

    public override ISelectionCollider GetNodeSelection(int nodeIndex) {
        return ISelectionCollider.FromRect(GetNodeRect(nodeIndex));
    }

    private Rectangle GetNodeRect(int nodeIndex) {
        var node = Nodes![nodeIndex];
        var rect = new Rectangle((int) node.X - 2, (int) node.Y - 2, 5, 5);

        return rect;
    }
}
