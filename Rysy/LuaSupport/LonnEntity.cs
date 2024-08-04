﻿using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public class LonnEntity : Entity, IHasLonnPlugin {
    private ListenableDictionaryRef<string, RegisteredEntity> _lonnPluginRef;
    
    ListenableDictionaryRef<string, RegisteredEntity> IHasLonnPlugin.LonnPluginRef { 
        get => _lonnPluginRef;
        set => _lonnPluginRef = value;
    }

    internal List<ISprite>? CachedSprites;
    internal Dictionary<Node, List<ISprite>>? CachedNodeSprites;
    
    private void ClearInternalCache() {
        CachedSprites = null;
        CachedNodeSprites = null;
    }
    
    [JsonIgnore]
    public LonnEntityPlugin? Plugin {
        get {
            var exists = _lonnPluginRef.TryGetValue(out var info, out var changed);

            if (changed) {
                ClearInternalCache();
                ClearRoomRenderCache();
            }
            
            return exists ? info!.LonnPlugin : null;
        }
    }

    [JsonIgnore]
    public override int Depth => Plugin?.GetDepth(Room, this) ?? 0;

    public override IEnumerable<ISprite> GetNodePathSprites() {
        var pl = Plugin;
        if (pl is null)
            return base.GetNodePathSprites();
        
        var lineType = pl.NodeLineRenderType(this);

        if (pl.NodeLineRenderOffset is { } offset) {
            var f = (Entity e, int i) => e.Nodes[i] + offset(e, e.Nodes[i], i + 1);
            return lineType switch {
                "line" => NodePathTypes.Line(this, f),
                "fan" => NodePathTypes.Fan(this, f),
                "circle" => NodePathTypes.Circle(this, f, nodeIsCenter: true),
                "none" => NodePathTypes.None,
                _ => NodePathTypes.Line(this, f),
            };
        }
        
        return lineType switch {
            "line" => NodePathTypes.Line(this),
            "fan" => NodePathTypes.Fan(this),
            "circle" => NodePathTypes.Circle(this, nodeIsCenter: true),
            "none" => NodePathTypes.None,
            _ => NodePathTypes.Line(this),
        };
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        if (Plugin is null)
            return base.GetNodeSprites(nodeIndex);
        
        var node = Nodes[nodeIndex];
        var i = nodeIndex + 1;
        
        CachedNodeSprites ??= new();

        if (CachedNodeSprites.TryGetValue(node, out var cached)) {
            return cached;
        }
        
        var roomWrapper = new RoomLuaWrapper(Room);
        
        var sprites = Plugin.PushToStack(pl => {
            var lua = pl.LuaCtx.Lua;

            if (pl.HasGetNodeSprite) {
                lua.GetTable(pl.StackLoc, "nodeSprite");

                return lua.PCallFunction(roomWrapper, this, node, i, SpritesFromLonn) ?? [];
            }
            
            if (Plugin.GetNodeTexture?.Invoke(roomWrapper, this, node, i) is { } texturePath) {
                var offset = Plugin.NodeOffset?.Invoke(roomWrapper, this, node, i);
                return [
                    ISprite.FromTexture(Pos + (offset ?? Vector2.Zero), LonnDrawables.SanitizeLonnTexturePath(texturePath)) with {
                        Origin = offset is {} ? Vector2.Zero : Plugin.NodeJustification(roomWrapper, this, node, i),
                        Color = Plugin.NodeColor(roomWrapper, this, node, i),
                        Scale = Plugin.NodeScale(roomWrapper, this, node, i),
                        Rotation = Plugin.NodeRotation(roomWrapper, this, node, i),
                    }
                ];
            }
            
            if (pl.NodeRectangle?.Invoke(roomWrapper, this, node, i) is { } nodeRect) {
                var borderColor = pl.BothNodeColors
                    ? pl.NodeBorderColor(roomWrapper, this, node, i)
                    : pl.NodeColor(roomWrapper, this, node, i);
                var fillColor = pl.BothNodeColors
                    ? pl.NodeFillColor(roomWrapper, this, node, i)
                    : pl.NodeColor(roomWrapper, this, node, i);
                
                return [
                    ISprite.OutlinedRect(nodeRect, fillColor, borderColor)
                ];
            }

            var oldPos = Pos;
            SilentSetPos(node);
            List<ISprite>? sprites;
        
            try {
                sprites = GetSpritesUncached(roomWrapper).Select(s => s.WithMultipliedAlpha(NodeSpriteAlpha)).ToList();
            } finally {
                SilentSetPos(oldPos);
            }
            
            return sprites;
        });

        if (Plugin.NodeDepth?.Invoke(roomWrapper, this, node, i) is { } nodeDepth) {
            foreach (var s in sprites) {
                s.Depth ??= nodeDepth;
            }
        }

        if (!roomWrapper.Used)
            CachedNodeSprites[Nodes[nodeIndex]] = sprites;
        
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

            var sprites = base.GetAllNodeSprites();

            return sprites;
        });
    }

    public override IEnumerable<ISprite> GetSprites() {
        if (CachedSprites is { } cached)
            return cached;

        var roomWrapper = new RoomLuaWrapper(Room);
        var sprites = GetSpritesUncached(roomWrapper);
        if (!roomWrapper.Used)
            CachedSprites = sprites;

        return sprites;
    }

    private List<ISprite> GetSpritesUncached(RoomLuaWrapper roomWrapper) {
        if (Plugin is null)
            return [];
        
        var sprites = Plugin.PushToStack((pl) => {
            var spr = _GetSprites(roomWrapper);

            return spr;
        });

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

            if (Plugin.NodeRectangle?.Invoke(Room, this, Nodes[nodeIndex], nodeIndex + 1) is { } nodeRect) {
                return ISelectionCollider.FromRect(nodeRect);
            }

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

    public override Range NodeLimits
    {
        get {
            try {
                return Plugin?.GetNodeLimits(Room, this) ?? base.NodeLimits;
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to get nodeLimits for: {ToJson()}");
                return base.NodeLimits;
            }
        }
    }

    public override Point MinimumSize {
        get {
            try {
                return Plugin?.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;
            } catch (Exception ex) {
                Logger.Error(ex, $"Failed to get minimumSize for: {ToJson()}");
                return base.MinimumSize;
            }
        }
    }

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
