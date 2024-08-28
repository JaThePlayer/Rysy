using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy;

public class Trigger : Entity {
    private static readonly string LightSkyBlueHex = Color.LightSkyBlue.ToRGBAString();
    
    public string EditorColor {
        get => EntityData.Attr("_editorColor", LightSkyBlueHex);
        set {
            EntityData["_editorColor"] = value;
            ClearRoomRenderCache();
        }
    }

    public Color Color {
        get {
            var color = EditorColor;
            if (color.IsNullOrWhitespace()) {
                return Color.LightSkyBlue;
            }

            return color.FromRGBA();
        }
    }

    public Color FillColor => Color * 0.15f;

    public static string GetDefaultTextForSid(string sid) => TriggerHelpers.Humanize(sid);
    
    public virtual string Text => GetDefaultTextForSid(EntityData.SID);

    public override int Depth => Depths.Top;

    public override bool ResizableX => true;
    public override bool ResizableY => true;
    public override Point RecommendedMinimumSize => new(8, 8);

    public override IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.Line(this, (self, nodeIndex) => GetNodeRect(Nodes[nodeIndex]).Center.ToVector2());

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var rect = GetNodeRect(Nodes[nodeIndex]);
        yield return ISprite.OutlinedRect(rect, FillColor, Color);
    }

    public override IEnumerable<ISprite> GetSprites() {
        var rect = new Rectangle(X, Y, Width, Height);
        yield return ISprite.OutlinedRect(rect, FillColor, Color);
        yield return new PicoTextRectSprite {
            Text = Text,
            Pos = rect,
            Color = Color.White,
            Scale = Settings.Instance.TriggerFontScale,
            Depth = Depth - 1
        };
    }

    public override ISelectionCollider GetNodeSelection(int nodeIndex) {
        return ISelectionCollider.FromRect(GetNodeRect(Nodes[nodeIndex]));
    }

    protected static Rectangle GetNodeRect(Node node) {
        var rect = new Rectangle((int) node.X - 2, (int) node.Y - 2, 5, 5);

        return rect;
    }

    public override void ClearRoomRenderCache() {
        if (Room is { } r) {
            r.ClearTriggerRenderCache();
        }
    }
}
