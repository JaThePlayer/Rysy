using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;
using System.Text;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public class LonnEntity : Entity {
    [JsonIgnore]
    public LonnEntityPlugin Plugin { get; internal set; }

    [JsonIgnore]
    public override int Depth => Plugin.GetDepth(Room, this);

    public override IEnumerable<ISprite> GetAllNodeSprites() {
        //using var stackHolder = Plugin.PushToStack();

        return Plugin.PushToStack((pl) => {
            var visibility = pl.GetNodeVisibility(this);

            var visible = visibility switch {
                "always" => true,
                "selected" => Selected,
                var other => false,
            };

            if (!visible) {
                return Array.Empty<ISprite>();
            }

            if (!pl.HasGetNodeSprite) {
                return base.GetAllNodeSprites();
            }

            var roomWrapper = new RoomLuaWrapper(Room);

            var lua = pl.LuaCtx.Lua;

            var type = lua.GetTable(pl.StackLoc, "nodeSprite");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return Array.Empty<ISprite>();
            }

            var spriteFuncLoc = lua.GetTop();

            var sprites = new List<ISprite>();
            for (int i = 0; i < Nodes.Count; i++) {
                var node = Nodes[i];

                lua.PushCopy(spriteFuncLoc);
                sprites.AddRange(lua.PCallFunction(roomWrapper, this, node, i + 1, (lua, idx) => SpritesFromLonn(lua, idx)) ?? new());
            }

            lua.Pop(1); // pop the "nodeSprite" func

            sprites.AddRange(GetNodePathSprites() ?? Array.Empty<ISprite>());

            return sprites!;
        });
    }

    public override IEnumerable<ISprite> GetSprites() {
        if (CachedSprites is { } cached)
            return cached;

        return Plugin.PushToStack((pl) => {
            // TODO: push a RoomWrapper instead of the room, so that we can see whether the room ever got accessed or not
            // if the room never got accessed, we can cache, if it did, we cannot
            var roomWrapper = new RoomLuaWrapper(Room);

            var spr = _GetSprites(roomWrapper);

            if (!roomWrapper.Used)
                CachedSprites = spr;
            //else
            //    (Name, roomWrapper.Reasons).LogAsJson();

            return spr;
        });
    }

    private bool CallSelectionFunc<T>(Func<Lua, int, T> valueRetriever, out T? value) {
        var lua = Plugin.LuaCtx.Lua;

        if (!Plugin.HasSelectionFunction) {
            value = default;
            return false;
        }

        (var ret, value) = Plugin.PushToStack((pl) => {
            var type = lua.GetTable(pl.StackLoc, "selection");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return (false, default);
            }

            var value = lua.PCallFunction(Room, this, valueRetriever, results: 2);
            return (true, value);
        });

        return ret;
    }

    public override ISelectionCollider GetMainSelection() {
        try {
            //selection(room, entity):rectangle, table of rectangles
            if (CallSelectionFunc((lua, top) => lua.ToRectangle(top - 1), out var selectionRect)) {
                return ISelectionCollider.FromRect(selectionRect);
            }

            if (Plugin.GetRectangle is { } rectFunc) {
                var rectangle = rectFunc(Room, this);
                return ISelectionCollider.FromRect(rectangle);
            }
        } catch(Exception ex) {
            Logger.Error(ex, $"Failed to get selection for: {ToJson()}");
        }


        return base.GetMainSelection();
    }

    public override ISelectionCollider GetNodeSelection(int nodeIndex) {
        try {
            if (CallSelectionFunc((lua, top) => {
                if (lua.Type(top) != LuaType.Table) {
                    return default;
                }


                if (lua.RawGetInteger(top, nodeIndex + 1) == LuaType.Table) {
                    var rect = lua.ToRectangle(lua.GetTop());
                    ;
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
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to get node {nodeIndex} selection for: {ToJson()}");
        }

        return base.GetNodeSelection(nodeIndex);
    }

    public override Range NodeLimits => Plugin.GetNodeLimits(Room, this);

    public override Point MinimumSize => Plugin.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;

    public override void OnChanged() {
        base.OnChanged();

        CachedSprites = null;
    }

    public override List<string>? AssociatedMods => Plugin.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    #region Sprites
    private static byte[] _typeASCII = Encoding.ASCII.GetBytes("_type");

    internal List<ISprite>? CachedSprites;

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
            //var type = lua.PeekTableStringValue(top, _typeASCII);
            if (!lua.TryPeekTableStringValueToSpanInSharedBuffer(top, _typeASCII, out var type)) {
                return;
            }

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
                case "drawableFunction":
                    break;
                default:
                    Logger.Write("LonnEntity", LogLevel.Warning, $"Unknown Lonn sprite type: {type.ToString()}: {lua.TableToDictionary(top).ToJson()}");
                    break;
            }
        }
    }

    private List<ISprite> _GetSprites(RoomLuaWrapper roomWrapper) {
        if (Plugin.HasGetSprite) {
            var lua = Plugin.LuaCtx.Lua;

            var type = lua.GetTable(Plugin.StackLoc, "sprite");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return new();
            }

            var sprites = lua.PCallFunction(roomWrapper, this, (lua, i) => SpritesFromLonn(lua, i));

            return sprites!;
        }

        if (Plugin.GetTexture is { } getTexture && getTexture(roomWrapper, this) is { } texturePath) {
            return new() { ISprite.FromTexture(Pos, texturePath) with {
                Origin = Plugin.GetJustification(roomWrapper, this),
                Color = Plugin.GetColor(roomWrapper, this),
                Scale = Plugin.GetScale(roomWrapper, this),
                Rotation = Plugin.GetRotation(roomWrapper, this),
            }};
        } else {
            Rectangle rectangle;
            if (Plugin.GetRectangle is { } rectFunc) {
                rectangle = rectFunc(roomWrapper, this);
            } else {
                rectangle = Rectangle;
            }

            return new() { ISprite.OutlinedRect(rectangle, Plugin.GetFillColor(roomWrapper, this), Plugin.GetBorderColor(roomWrapper, this)) };
        }
    }

    #endregion
}
