using KeraLua;
using Rysy.Graphics;

namespace Rysy.LuaSupport;

public static class LonnDrawables {
    public static RectangleSprite LuaToRect(Lua lua, int top) {
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var w = lua.PeekTableIntValue(top, "width") ?? 8;
        var h = lua.PeekTableIntValue(top, "height") ?? 8;
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var mode = lua.PeekTableStringValue(top, "mode");
        var rect = new Rectangle((int) x, (int) y, w, h);

        var sprite = mode switch {
            "fill" => ISprite.Rect(rect, color),
            "line" => ISprite.OutlinedRect(rect, Color.Transparent, color),
            "bordered" => ISprite.OutlinedRect(rect, color, lua.PeekTableColorValue(top, "secondaryColor", Color.White)),
            _ => ISprite.Rect(rect, color),
        };
        return sprite;
    }

    public static IEnumerable<ISprite>? LuaToFakeTiles(Lua lua, int top, Room room) {
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var w = lua.PeekTableIntValue(top, "w") ?? 8;
        var h = lua.PeekTableIntValue(top, "h") ?? 8;
        var material = lua.PeekTableStringValue(top, "material") ?? "3";
        var layer = lua.PeekTableStringValue(top, "layer") ?? "tilesFg";

        var sprites = layer switch {
            "tilesFg" => room.FG.Autotiler?.GetSprites(new(x, y), material[0], w / 8, h / 8),
            "tilesBg" => room.BG.Autotiler?.GetSprites(new(x, y), material[0], w / 8, h / 8),
            _ => null
        };
        return sprites;
    }

    public static LineSprite LuaToLine(Lua lua, int top) {
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var offX = lua.PeekTableFloatValue(top, "offsetX") ?? 0;
        var offY = lua.PeekTableFloatValue(top, "offsetY") ?? 0;
        var magnitudeOffset = lua.PeekTableFloatValue(top, "magnitudeOffset") ?? 0;
        var thickness = lua.PeekTableIntValue(top, "thickness") ?? 1;
        var points = lua.PeekTableNumberList(top, "points") ?? new();

        var pointsVec2 = new Vector2[points.Count / 2];
        for (int i = 0; i < points.Count; i += 2) {
            pointsVec2[i / 2] = new(points[i] + offX, points[i + 1] + offY);
        }

        return new LineSprite(pointsVec2) with {
            Color = color,
            Thickness = thickness,
        };
    }

    public static Sprite LuaToSprite(Lua lua, int top, Vector2 defaultPos) {
        var texture = lua.PeekTableStringValue(top, "_RYSY_INTERNAL_texture") ?? throw new Exception("DrawableSprite doesn't have the '_RYSY_INTERNAL_texture' field set!");
        var x = lua.PeekTableFloatValue(top, "x");
        var y = lua.PeekTableFloatValue(top, "y");
        var scaleX = lua.PeekTableFloatValue(top, "scaleX");
        var scaleY = lua.PeekTableFloatValue(top, "scaleY");
        var originX = lua.PeekTableFloatValue(top, "justificationX");
        var originY = lua.PeekTableFloatValue(top, "justificationY");
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var rotation = lua.PeekTableFloatValue(top, "rotation");
        var depth = lua.PeekTableIntValue(top, "depth");

        var sprite = ISprite.FromTexture(new Vector2(x ?? defaultPos.X, y ?? defaultPos.Y), texture) with {
            Scale = new(scaleX ?? 1f, scaleY ?? 1f),
            Origin = new(originX ?? .5f, originY ?? .5f),
            Color = color,
            Rotation = rotation ?? 0f,
            Depth = depth
        };

        if (lua.PeekTableIntValue(top, "_RYSY_quadX") is { } qx) {
            sprite = sprite.CreateSubtexture(
                qx,
                lua.PeekTableIntValue(top, "_RYSY_quadY") ?? 0,
                lua.PeekTableIntValue(top, "_RYSY_quadW") ?? 0,
                lua.PeekTableIntValue(top, "_RYSY_quadH") ?? 0
            );
        }

        return sprite;
    }
}
