using KeraLua;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public sealed class LonnEntity : Entity, ICustomNodeHandler {
    [JsonIgnore]
    public LonnEntityPlugin Plugin;

    [JsonIgnore]
    public override int Depth => Plugin.GetDepth(Room, this);

    public IEnumerable<ISprite> GetNodeSprites() {
        using var stackHolder = Plugin.PushToStack();

        var visibility = Plugin.GetNodeVisibility(this);

        return visibility switch {
            "always" => NodeHelper.GetGuessedNodeSpritesFor(this),
            "selected" => Array.Empty<ISprite>(),
            var other => Array.Empty<ISprite>(),
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        try {
            using var stackHolder = Plugin.PushToStack();

            var spr = _GetSprites();
            //stackHolder?.Dispose();

            return spr;
        } catch (LuaException ex) {
            Logger.Error(ex, $"Erroring entity definition for {Plugin.Name} at {this.ToJson()}");
            return Array.Empty<ISprite>();
        }
    }

    private bool CallSelectionFunc<T>(Func<Lua, int, T> valueRetriever, out T? value) {
        var lua = Plugin.LuaCtx.Lua;

        if (!Plugin.HasSelectionFunction) {
            value = default;
            return false;
        }


        using (var stackHolder = Plugin.PushToStack()) {
            var type = lua.GetTable(Plugin.StackLoc, "selection");

            if (type != LuaType.Function) {
                lua.Pop(1);
                value = default;
                return false;
            }

            value = lua.PCallFunction(Room, this, valueRetriever, results: 2);
            return true;
        }
    }

    public override ISelectionCollider GetMainSelection() {
        //selection(room, entity):rectangle, table of rectangles
        if (CallSelectionFunc((lua, top) => lua.ToRectangle(top - 1), out var selectionRect)) {
            return ISelectionCollider.FromRect(selectionRect);
        }

        if (Plugin.GetRectangle is { } rectFunc) {
            var rectangle = rectFunc(Room, this);
            return ISelectionCollider.FromRect(rectangle);
        }

        return base.GetMainSelection();
    }

    public override ISelectionCollider GetNodeSelection(int nodeIndex) {
        if (CallSelectionFunc((lua, top) => {
            if (lua.RawGetInteger(top, nodeIndex + 1) == LuaType.Table) {
                var rect = lua.ToRectangle(lua.GetTop()); ;
                lua.Pop(1);
                return rect;
            }
            lua.Pop(1);
            return default;
        }, out var selectionRect) && selectionRect != default) {
            return ISelectionCollider.FromRect(selectionRect);
        }

        // todo: nodeRectangle

        if (Plugin.GetRectangle is { } rectFunc) {
            var oldPos = Pos;
            Pos = Nodes![nodeIndex];

            try {
                var rectangle = rectFunc(Room, this);
                return ISelectionCollider.FromRect(rectangle);
            } finally { 
                Pos = oldPos; 
            }
        }

        return base.GetNodeSelection(nodeIndex);
    }

    public override Range NodeLimits => Plugin.GetNodeLimits(Room, this);

    public override Point MinimumSize => Plugin.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;

    #region Sprites
    private List<ISprite> SpritesFromLonn(Lua lua, int top) {
        var list = new List<ISprite>();

        if (lua.Type(top) != LuaType.Table)
            return new();

        switch (lua.PeekTableType(top, "_type")) {
            case LuaType.String:
                // name is provided, so there's 1 placement
                NextSprite(top, list);
                break;
            default:
                var prevTop = lua.GetTop();
                lua.IPairs((lua, i, loc) => {
                    NextSprite(loc, list);
                });
                break;
        }

        return list;

        void NextSprite(int top, List<ISprite> addTo) {
            var type = lua.PeekTableStringValue(top, "_type");
            switch (type) {
                case "drawableSprite":
                    addTo.Add(LonnDrawables.LuaToSprite(lua, top, Pos));
                    break;
                case "drawableLine":
                    addTo.Add(LonnDrawables.LuaToLine(lua, top));
                    break;
                case "_RYSY_fakeTiles":
                    addTo.AddRange(LonnDrawables.LuaToFakeTiles(lua, top, Room) ?? Array.Empty<ISprite>());
                    break;
                case "drawableRectangle":
                    addTo.Add(LonnDrawables.LuaToRect(lua, top));
                    break;
                case "drawableNinePatch":
                    addTo.Add(LonnDrawables.LuaToNineSlice(lua, top));
                    break;
                default:
                    Logger.Write("LonnEntity", LogLevel.Warning, $"Unknown Lonn sprite type: {type}: {lua.TableToDictionary(top).ToJson()}");
                    break;
            }
        }
    }

    private List<ISprite> _GetSprites() {
        if (Plugin.HasGetSprite) {
            var lua = Plugin.LuaCtx.Lua;

            var type = lua.GetTable(Plugin.StackLoc, "sprite");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return new();
            }

            var sprites = lua.PCallFunction(Room, this, (lua, i) => SpritesFromLonn(lua, i));

            return sprites!;
        }


        if (Plugin.GetTexture is { } getTexture && getTexture(Room, this) is { } texturePath) {
            return new() { ISprite.FromTexture(Pos, texturePath) with {
                Origin = Plugin.GetJustification(Room, this),
                Color = Plugin.GetColor(Room, this),
                Scale = Plugin.GetScale(Room, this),
                Rotation = Plugin.GetRotation(Room, this),
            }};
        } else {
            Rectangle rectangle;
            if (Plugin.GetRectangle is { } rectFunc) {
                rectangle = rectFunc(Room, this);
            } else {
                rectangle = Rectangle;
            }

            return new() { ISprite.OutlinedRect(rectangle, Plugin.GetFillColor(Room, this), Plugin.GetBorderColor(Room, this)) };
        }
    }

    #endregion
}
