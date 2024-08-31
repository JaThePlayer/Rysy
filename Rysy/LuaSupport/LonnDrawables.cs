﻿using KeraLua;
using Rysy.Entities;
using Rysy.Extensions;
using Rysy.Graphics;
using System.Text;
using System.Text.RegularExpressions;

namespace Rysy.LuaSupport;

public static partial class LonnDrawables {
    private static byte[] RYSY_UNPACKSPRASCII = "RYSY_UNPACKSPR"u8.ToArray();
    private static byte[] _typeASCII = "_type"u8.ToArray();

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
            "bordered" => ISprite.OutlinedRect(rect, color,
                lua.PeekTableColorValue(top, "secondaryColor", Color.White)),
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
        var thickness = lua.PeekTableFloatValue(top, "thickness") ?? 1f;
        var points = lua.PeekTableNumberList(top, "points") ?? new();

        var pointsVec2 = new Vector2[points.Count / 2];
        for (int i = 0; i < points.Count; i += 2) {
            pointsVec2[i / 2] = new(points[i], points[i + 1]);
        }

        var sprite = new LineSprite(pointsVec2) {
            Color = color, Thickness = thickness, MagnitudeOffset = magnitudeOffset, Offset = new(offX, offY)
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
        int? depth = lua.ToIntegerX(top + 8) is { } l ? (int) l : null;
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
                (int) qx,
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

        var sprite = ISprite.NineSliceFromTexture(rect, texture) with { Color = color, };

        if (lua.PeekTableIntValue(top, "depth") is { } depth)
            sprite.Depth = depth;

        return sprite;
    }

    public static IEnumerable<ISprite> LuaToWaterfall(Room room, Lua lua, int top) {
        /*
            _type = "_RYSY_waterfall",
            x = entity.x or 0,
            y = entity.y or 0,
            fillColor = fillColor or waterfallFillColor,
            borderColor = borderColor or waterfallBorderColor,
         */
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var fillColor = lua.PeekTableColorValue(top, "fillColor", Color.White);
        var borderColor = lua.PeekTableColorValue(top, "borderColor", Color.White);
        
        return Waterfall.GetSprites(room, new(x, y), fillColor, borderColor);
    }
    
    public static IEnumerable<ISprite> LuaToBigWaterfall(Room room, Lua lua, int top) {
        /*
            _type = "_RYSY_big_waterfall",
            x = x,
            y = y,
            w = width,
            h = height,
            fillColor = fillColor,
            borderColor = borderColor,
            fg = waterfallHelper.isForeground(entity)
         */
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var w = lua.PeekTableIntValue(top, "w") ?? 0;
        var h = lua.PeekTableIntValue(top, "h") ?? 0;
        var fillColor = lua.PeekTableColorValue(top, "fillColor", Color.White);
        var borderColor = lua.PeekTableColorValue(top, "borderColor", Color.White);
        var layer = lua.PeekTableBoolValue(top, "fg") is true ? BigWaterfall.Layers.FG : BigWaterfall.Layers.BG;
        
        return BigWaterfall.GetSprites(new(x, y), w, h, fillColor, borderColor, layer);
    }
    
    public static ISprite LuaToPolygon(Lua lua, int top) {
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var secondaryColor = lua.PeekTableColorValue(top, "secondaryColor", Color.White);
        var mode = lua.PeekTableStringValue(top, "mode");
        
        var points = lua.PeekTableList(top, "points", (lua, top) => lua.ToVector2(top));
        var connectFirstWithLast = false;
        
        if (points is null) {
            if (lua.PeekTableWrapper<Entity>(top, "__RYSY_entity") is { } srcEntity) {
                points = [srcEntity.Pos, .. srcEntity.Nodes];
                connectFirstWithLast = true;
            } else {
                points = [];
            }
        }

        return mode switch {
            "line" => ISprite.Line(points, color) with {
                ConnectFirstWithLast = connectFirstWithLast,
            },
            "fill" => new PolygonSprite(points, default, color),
            "bordered" => new PolygonSprite(points, color, secondaryColor),
            _ => throw new NotImplementedException(mode)
        };
    }
    
    private static ISprite LuaToDrawableText(Lua lua, int top) {
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var w = lua.PeekTableIntValue(top, "width");
        var h = lua.PeekTableIntValue(top, "height");
        var fontSize = lua.PeekTableFloatValue(top, "fontSize") ?? 1f;
        // var font = lua.PeekTableStringValue(top, "font");
        var text = lua.PeekTableStringValue(top, "text") ?? "";
        var color = lua.PeekTableColorValue(top, "color", Color.White);

        var bounds = new Rectangle((int)x, (int)y, w ?? 0, h ?? 0);
        return new PicoTextRectSprite(text, bounds) {
            Scale = fontSize,
            Color = color,
        };
    }
    
    public static void AppendSprite(Lua lua, int top, Entity entity, List<ISprite> addTo) {
        if (!lua.TryPeekTableStringValueToSpanInSharedBuffer(top, _typeASCII, out var type)) {
            return;
        }

        switch (type) {
            case "drawableSprite":
                addTo.Add(LuaToSprite(lua, top, entity.Pos));
                break;
            case "drawableLine":
                addTo.Add(LuaToLine(lua, top));
                break;
            case "drawableRectangle":
                addTo.Add(LuaToRect(lua, top));
                break;
            case "drawableNinePatch":
                addTo.Add(LuaToNineSlice(lua, top));
                break;
            case "drawableFunction":
                break;
            case "drawableText":
                addTo.Add(LuaToDrawableText(lua, top));
                break;
            // lonn extensions
            case "drawablePolygon":
                addTo.Add(LuaToPolygon(lua, top));
                break;
            // rysy-specific
            case "_RYSY_fakeTiles":
                addTo.AddRange(LuaToFakeTiles(lua, top, entity.Room) ?? Array.Empty<ISprite>());
                break;
            case "_RYSY_waterfall":
                addTo.AddRange(LuaToWaterfall(entity.Room, lua, top));
                break;
            case "_RYSY_big_waterfall":
                addTo.AddRange(LuaToBigWaterfall(entity.Room, lua, top));
                break;
            default:
                Logger.Write("LonnEntity", LogLevel.Warning, $"Unknown Lonn sprite type: {type.ToString()}: {lua.TableToDictionary(top).ToJson()}");
                break;
        }
    }
}
