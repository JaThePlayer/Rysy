using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Selections;
using System.Collections.Concurrent;

namespace Rysy;

public class Trigger : Entity {
    public Color Color {
        get {
            var stored = EntityData.Attr("_editorColor");
            if (!string.IsNullOrWhiteSpace(stored) && ColorHelper.TryGet(stored, ColorFormat.RGBA, out var storedColor))
                return storedColor;
            
            if (Themes.Current.TriggerCategoryColors.TryGetValue(Category, out var categoryColor))
                return categoryColor;
            
            return Color.LightSkyBlue;
        }
        set {
            EntityData["_editorColor"] = value;
            ClearRoomRenderCache();
        }
    }

    public Color FillColor => Color * 0.15f;
    
    /// <summary>
    /// In-editor category of this trigger.
    /// Refer to the <see cref="TriggerCategories"/> class for a list of pre-defined category names,
    /// though custom ones are allowed as well.
    /// </summary>
    public virtual string Category => TriggerCategories.Default;

    private static readonly ConcurrentDictionary<string, IReadOnlyList<string>> DefaultTagCache = [];

    public override IReadOnlyList<string> Tags => DefaultTagCache.GetOrAdd(Category, x => [ x ]);

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
    
    internal PicoTextRectSprite GetTextSprite(Color color, Color outlineColor, float? fontScale = null) => new PicoTextRectSprite {
        Text = Text,
        Pos = new Rectangle(X, Y, Width, Height),
        Color = color,
        OutlineColor = outlineColor,
        Scale = fontScale ?? Settings.Instance.TriggerFontScale,
        Depth = Depth - 1
    };

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.OutlinedRect(new Rectangle(X, Y, Width, Height), FillColor, Color);
        yield return GetTextSprite(Color.White, default);
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

    public override IEnumerable<ISprite> GetPreviewSprites() {
        yield return ISprite.OutlinedRect(new Rectangle(X, Y, Width, Height), FillColor, Color) with {
            Depth = Depth
        };
        yield return GetTextSprite(Themes.Current.ImGuiStyle.TextColor, default, 1f) with {
            Depth = Depth - 1
        };
    }
}

/// <summary>
/// Contains names of predefined trigger categories.
/// </summary>
public static class TriggerCategories {
    public static string Camera => "camera";
    
    public static string Audio => "audio";
    
    public static string Visual => "visual";
    
    public static string Default => "default";
}