using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using System.Text;
using System.Text.RegularExpressions;

namespace Rysy.LuaSupport;

public static partial class LonnDrawables {
    private static byte[] RYSY_UNPACKSPRASCII = Encoding.ASCII.GetBytes("RYSY_UNPACKSPR");

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

        if (lua.PeekTableIntValue(top, "depth") is { } depth)
            sprite.Depth = depth;

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
            "tilesFg" => room.FG.Autotiler?.GetFilledRectSprites(new(x, y), material[0], w / 8, h / 8, Color.White),
            "tilesBg" => room.BG.Autotiler?.GetFilledRectSprites(new(x, y), material[0], w / 8, h / 8, Color.White),
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
            pointsVec2[i / 2] = new(points[i], points[i + 1]);
        }

        var sprite = new LineSprite(pointsVec2) with {
            Color = color,
            Thickness = thickness,
            MagnitudeOffset = magnitudeOffset,
            Offset = new(offX, offY)
        };

        if (lua.PeekTableIntValue(top, "depth") is { } depth)
            sprite.Depth = depth;

        return sprite;
    }

    public static Sprite LuaToSprite(Lua lua, int top, Vector2 defaultPos) {
        /*
        var texture = lua.PeekTableStringValue(top, _RYSY_INTERNAL_textureASCII) ?? throw new Exception("DrawableSprite doesn't have the '_RYSY_INTERNAL_texture' field set!");
        var x = lua.PeekTableFloatValue(top, xASCII);
        var y = lua.PeekTableFloatValue(top, yASCII);
        var scaleX = lua.PeekTableFloatValue(top, scaleXASCII);
        var scaleY = lua.PeekTableFloatValue(top, scaleYASCII);
        var originX = lua.PeekTableFloatValue(top, justificationXASCII);
        var originY = lua.PeekTableFloatValue(top, justificationYASCII);
        var color = lua.PeekTableColorValue(top, colorASCII, Color.White);
        var rotation = lua.PeekTableFloatValue(top, rotationASCII);
        var depth = lua.PeekTableIntValue(top, depthASCII);*/
        lua.GetGlobalASCII(RYSY_UNPACKSPRASCII);
        lua.PushCopy(top);
        lua.Call(1, 11);

        var x = lua.ToFloat(top + 1);
        var y = lua.ToFloat(top + 2);
        var originX = lua.ToFloat(top + 3);
        var originY = lua.ToFloat(top + 4);
        var scaleX = lua.ToFloat(top + 5);
        var scaleY = lua.ToFloat(top + 6);
        var rotation = lua.ToFloat(top + 7);
        int? depth = lua.ToIntegerX(top + 8) is { } l ? (int)l : null;
        var color = lua.ToColor(top + 9, Color.White);
        var texture = lua.FastToString(top + 10, callMetamethod: false);
        var quadX = lua.ToIntegerX(top + 11);

        lua.Pop(11);
        
        var sprite = ISprite.FromTexture(new Vector2(x, y), texture) with {
            Scale = new(scaleX, scaleY),
            Origin = new(originX, originY),
            Color = color,
            Rotation = rotation,
            Depth = depth
        };

        if (quadX is { } qx) {
            sprite = sprite.CreateSubtexture(
                (int)qx,
                lua.PeekTableIntValue(top, "_RYSYqY") ?? 0,
                lua.PeekTableIntValue(top, "_RYSYqW") ?? 0,
                lua.PeekTableIntValue(top, "_RYSYqH") ?? 0
            );

            sprite.Origin = default; // lonn ignores origin when using a subtexture...
        }

        return sprite;
    }

    /// <summary>
    /// Fixes issues like `"collectables/summitgems/" .. entity.index .. "/gem00"` returning `collectables/summitgems/0.0/gem00`
    /// </summary>
    public static string SanitizeLonnTexturePath(string? pathFromLonn) {
        if (pathFromLonn is null)
            return "";
        
        var fix = MessedUpDigitsRegex().Replace(pathFromLonn, match => match.ValueSpan[..^".0".Length].ToString());
        return fix;
    }

    [GeneratedRegex(@"\d\.0")]
    private static partial Regex MessedUpDigitsRegex();

    public static NineSliceSprite LuaToNineSlice(Lua lua, int top) {
        var texture = lua.PeekTableStringValue(top, "texture") ?? "";

        var x = lua.PeekTableFloatValue(top, "drawX") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "drawY") ?? 0f;
        var w = lua.PeekTableIntValue(top, "drawWidth") ?? 8;
        var h = lua.PeekTableIntValue(top, "drawHeight") ?? 8;
        var rect = new Rectangle((int) x, (int) y, w, h);

        var color = lua.PeekTableColorValue(top, "color", Color.White);

        /*
    ninePatch.hideOverflow = options.hideOverflow or true
    ninePatch.mode = options.mode or "fill"
    ninePatch.borderMode = options.borderMode or "repeat"
    ninePatch.fillMode = options.fillMode or "repeat"
         */

        var sprite = ISprite.NineSliceFromTexture(rect, texture) with {
            Color = color,
        };

        if (lua.PeekTableIntValue(top, "depth") is { } depth)
            sprite.Depth = depth;

        return sprite;
    }
}
