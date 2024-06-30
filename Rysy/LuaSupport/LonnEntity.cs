using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;
using System.Text;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public class LonnEntity : Entity {
    internal ListenableDictionaryRef<string, RegisteredEntity> PluginRef;

    internal List<ISprite>? CachedSprites;
    internal Dictionary<Node, List<ISprite>>? CachedNodeSprites;
    
    private void ClearInternalCache() {
        CachedSprites = null;
        CachedNodeSprites = null;
    }
    
    [JsonIgnore]
    public LonnEntityPlugin? Plugin {
        get {
            var exists = PluginRef.TryGetValue(out var info, out var changed);

            if (changed) {
                ClearInternalCache();
                ClearRoomRenderCache();
            }
            
            return exists ? info!.LonnPlugin : null;
        }
    }

    [JsonIgnore]
    public override int Depth => Plugin?.GetDepth(Room, this) ?? 0;

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        CachedNodeSprites ??= new();

        var node = Nodes[nodeIndex];
        if (CachedNodeSprites.TryGetValue(node, out var cached)) {
            return cached;
        }
        
        var oldPos = Pos;
        SilentSetPos(node);
        List<ISprite>? sprites = null;
        
        try {
            sprites = GetSpritesUncached(out var canCache).Select(s => s.WithMultipliedAlpha(NodeSpriteAlpha)).ToList();

            if (canCache) {
                CachedNodeSprites[node] = sprites;
            }
        } finally {
            SilentSetPos(oldPos);
            sprites ??= [];
        }
        
        return sprites;
    }

    public override IEnumerable<ISprite> GetAllNodeSprites() {
        if (Plugin is null)
            return [];

        return Plugin.PushToStack((pl) => {
            var visibility = pl.GetNodeVisibility(this);

            var visible = visibility switch {
                "always" => true,
                "selected" => true, //pl.HasGetNodeSprite ? Selected : true,
                var other => false,
            };

            if (!visible) {
                return [];
            }

            if (!pl.HasGetNodeSprite) {
                return base.GetAllNodeSprites();
            }

            var roomWrapper = new RoomLuaWrapper(Room);

            var lua = pl.LuaCtx.Lua;

            var type = lua.GetTable(pl.StackLoc, "nodeSprite");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return [];
            }

            var spriteFuncLoc = lua.GetTop();

            var sprites = new List<ISprite>();
            for (int i = 0; i < Nodes.Count; i++) {
                var node = Nodes[i];

                lua.PushCopy(spriteFuncLoc);
                try {
                    sprites.AddRange(lua.PCallFunction(roomWrapper, this, node, i + 1, SpritesFromLonn) ?? []);
                } catch {
                    lua.Pop(1); // pop the "nodeSprite" func
                    throw;
                }
            }

            lua.Pop(1); // pop the "nodeSprite" func

            sprites.AddRange(GetNodePathSprites() ?? []);

            return sprites!;
        });
    }

    public override IEnumerable<ISprite> GetSprites() {
        if (CachedSprites is { } cached)
            return cached;

        var sprites = GetSpritesUncached(out var canCache);
        if (canCache)
            CachedSprites = sprites;

        return sprites;
    }

    private List<ISprite> GetSpritesUncached(out bool canCache) {
        canCache = false;
        if (Plugin is null)
            return [];

        // can't capture out vars into lambdas
        bool innerCanCache = false;
        
        var sprites = Plugin.PushToStack((pl) => {
            // push a RoomWrapper instead of the room, so that we can see whether the room ever got accessed or not
            // if the room never got accessed, we can cache, if it did, we cannot
            var roomWrapper = new RoomLuaWrapper(Room);

            var spr = _GetSprites(roomWrapper);

            if (!roomWrapper.Used)
                innerCanCache = true; // CachedSprites = spr

            return spr;
        });

        canCache = innerCanCache;
        return sprites;
    }

    private bool CallSelectionFunc<T>(Func<Lua, int, T> valueRetriever, out T? value) {
        if (Plugin is null || !Plugin.HasSelectionFunction) {
            value = default;
            return false;
        }
        var lua = Plugin.LuaCtx.Lua;

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
            if (Plugin is null)
                return base.GetMainSelection();
            
            //selection(room, entity):rectangle, table of rectangles
            if (CallSelectionFunc((lua, top) => lua.ToRectangle(top - 1), out var selectionRect)) {
                return ISelectionCollider.FromRect(selectionRect);
            }

            if (Plugin?.GetRectangle is { } rectFunc) {
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
            if (Plugin is null)
                return base.GetNodeSelection(nodeIndex);
            
            if (CallSelectionFunc((lua, top) => {
                if (lua.Type(top) != LuaType.Table) {
                    return default;
                }


                if (lua.RawGetInteger(top, nodeIndex + 1) == LuaType.Table) {
                    var rect = lua.ToRectangle(lua.GetTop());
                    
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
                
                SilentSetPos(Nodes![nodeIndex]);

                try {
                    var rectangle = rectFunc(Room, this);
                    return ISelectionCollider.FromRect(rectangle);
                } finally {
                    SilentSetPos(oldPos);
                }
            }
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to get node {nodeIndex} selection for: {ToJson()}");
        }

        return base.GetNodeSelection(nodeIndex);
    }

    public override Range NodeLimits => Plugin?.GetNodeLimits(Room, this) ?? base.NodeLimits;

    public override Point MinimumSize => Plugin?.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);
        
        ClearInternalCache();
    }

    public override Entity? TryFlipVertical() {
        return FlipImpl(false, true);
    }

    public override Entity? TryFlipHorizontal() {
        return FlipImpl(true, false);
    }

    public override Entity? TryRotate(RotationDirection dir) {
        if (Plugin?.Rotate is null) {
            return base.TryRotate(dir);
        }

        var selfWrapper = new CloneEntityWrapper(this);

        return Plugin.Rotate(Room, selfWrapper, (int)dir)
            ? selfWrapper.CreateMutatedCloneIfChanged()
            : null;
    }

    public override Entity? MoveBy(Vector2 offset, int nodeIndex, out bool shouldDoNormalMove) {
        shouldDoNormalMove = true;
        if (Plugin is null)
            return null;
        
        if (Plugin?.Move is {} move) {
            shouldDoNormalMove = false;
            var selfWrapper = new CloneEntityWrapper(this);

            move(Room, selfWrapper, nodeIndex + 1, offset.X, offset.Y);

            return selfWrapper.CreateMutatedCloneIfChanged();
        }

        return null;
    }

    private Entity? FlipImpl(bool horizontal, bool vertical) {
        if (Plugin?.Flip is null) {
            return horizontal ? base.TryFlipHorizontal() : base.TryFlipVertical();
        }

        var selfWrapper = new CloneEntityWrapper(this);

        return Plugin.Flip(Room, selfWrapper, horizontal, vertical) 
            ? selfWrapper.CreateMutatedCloneIfChanged() 
            : null;
    }

    public override List<string>? AssociatedMods => Plugin?.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    public override void ClearInnerCaches() {
        base.ClearInnerCaches();

        CachedSprites = null;
        CachedNodeSprites?.Clear();
        CachedNodeSprites = null;
    }
    
    #region Sprites
    private List<ISprite> SpritesFromLonn(Lua lua, int top) {
        var list = new List<ISprite>();

        if (lua.Type(top) != LuaType.Table)
            return list;

        switch (lua.PeekTableType(top, "_type")) {
            case LuaType.String:
                var prevTop = lua.GetTop();
                // name is provided, so there's 1 sprite
                LonnDrawables.AppendSprite(lua, top, this, list);
                //lua.Pop(1);
                if (lua.GetTop() != prevTop) {
                    Logger.Write("[LuaSupport]", LogLevel.Warning, $"Calling sprite function on {Name} changed Lua stack");
                    lua.PrintStack();
                }
                break;
            default:
                lua.IPairs((lua, i, loc) => {
                    LonnDrawables.AppendSprite(lua, loc, this, list);
                });
                break;
        }

        return list;
    }

    private List<ISprite> _GetSprites(RoomLuaWrapper roomWrapper) {
        if (Plugin is null)
            return [];
        
        if (Plugin.HasGetSprite) {
            var lua = Plugin.LuaCtx.Lua;

            var type = lua.GetTable(Plugin.StackLoc, "sprite");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return [];
            }

            var sprites = lua.PCallFunction(roomWrapper, this, SpritesFromLonn);

            return sprites!;
        }

        if (Plugin.GetTexture is { } getTexture && getTexture(roomWrapper, this) is { } texturePath) {
            var offset = Plugin.GetOffset?.Invoke(roomWrapper, this);
            return [
                ISprite.FromTexture(Pos + (offset ?? Vector2.Zero), LonnDrawables.SanitizeLonnTexturePath(texturePath)) with {
                    Origin = offset is {} ? Vector2.Zero : Plugin.GetJustification(roomWrapper, this),
                    Color = Plugin.GetColor(roomWrapper, this),
                    Scale = Plugin.GetScale(roomWrapper, this),
                    Rotation = Plugin.GetRotation(roomWrapper, this),
                }
            ];
        } else {
            Rectangle rectangle;
            if (Plugin.GetRectangle is { } rectFunc) {
                rectangle = rectFunc(roomWrapper, this);
            } else {
                rectangle = Rectangle;
            }

            return [
                ISprite.OutlinedRect(rectangle, Plugin.GetFillColor(roomWrapper, this),
                    Plugin.GetBorderColor(roomWrapper, this))
            ];
        }
    }

    #endregion
}
