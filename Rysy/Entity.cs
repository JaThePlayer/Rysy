using KeraLua;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.LuaSupport;
using Rysy.Selections;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using LuaException = Rysy.LuaSupport.LuaException;

namespace Rysy;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Entity : ILuaWrapper, IConvertibleToPlacement, IDepth, IName, IBindTarget, IUntypedData, ISimilar<Entity> {
    [JsonPropertyName("Room")]
    public string RoomName => Room.Name;

    // set by EntityRegistry:
    public EntityData EntityData = null!;

    [JsonIgnore]
    public Room Room { get; set; } = null!;

    #region EntityData Wrappers
    public string Name => EntityData.Sid;

    private int _id;
    
    public int Id {
        get => _id;
        set => EntityData["id"] = value;
    }

    public int X {
        get => (int) _pos.X;
        set {
            EntityData["x"] = value;
            //ClearRoomRenderCache();
        }
    }

    public int Y {
        get => (int) _pos.Y;
        set {
            EntityData["y"] = value;
            //ClearRoomRenderCache();
        }
    }

    private Vector2 _pos;
    public Vector2 Pos {
        get => _pos;
        set {
            X = (int) value.X;
            Y = (int) value.Y;

            //ClearRoomRenderCache();
        }
    }

    internal void SilentSetPos(Vector2 newPos) {
        _pos = newPos;
    }

    [JsonIgnore]
    public IList<Node> Nodes => EntityData.Nodes;

    [JsonIgnore]
    public int Width {
        get => EntityData.Int("width");
        set {
            EntityData["width"] = value;
            ClearRoomRenderCache();
        }
    }

    [JsonIgnore]
    public int Height {
        get => EntityData.Int("height");
        set {
            EntityData["height"] = value;
            ClearRoomRenderCache();
        }
    }

    /*
    [JsonIgnore]
    [Obsolete("Use EditorGroup instead")]
    public int EditorLayer {
        get => EntityData.Int("_editorLayer");
        set => EntityData["_editorLayer"] = value;
    }*/

    public const string EditorGroupEntityDataKey = "_editorGroup";

    private EditorGroupList? _editorGroupList;
    public EditorGroupList EditorGroups {
        get => _editorGroupList!;
        internal set {
            if (_editorGroupList is { } prev) {
                prev.OnChanged -= EditorGroupsListChanged;
            }
            _editorGroupList = value;
            _editorGroupList.OnChanged += EditorGroupsListChanged;
        }
    }

    private void EditorGroupsListChanged() {
        var p = string.Join(",", _editorGroupList!);
        EntityData[EditorGroupEntityDataKey] = string.Join(",", _editorGroupList!);
    }

    private void AssureAutoAssignedGroupsExist() {
        EditorGroups ??= new();
        
        foreach (var gr in Room.Map.EditorGroups) {
            var shouldAssign = false;
            if (gr.AutoAssignTo is { Count: > 0 } autoAssignTo) {
                if (autoAssignTo.Contains(Name)) {
                    shouldAssign = true;
                }
            }

            if (!shouldAssign && this is Decal d && gr.AutoAssignToDecals is { Count: > 0 } autoAssignToDecals) {
                foreach (var p in autoAssignToDecals) {
                    if (p.AffectsDecalPath(d.Texture)) {
                        shouldAssign = true;
                        break;
                    }
                }
            }
            
            if (shouldAssign) {
                if (!_editorGroupList!.Contains(gr))
                    _editorGroupList.Add(gr);
            } else {
                _editorGroupList!.Remove(gr);
            }
        }
    }

    #endregion

    [JsonIgnore]
    public abstract int Depth { get; }

    /// <summary>
    /// Gets the documentation for this entity.
    /// This can be a lang file entry, path to a .md file asset, a url, or plain markdown.
    /// </summary>
    public virtual string? Documentation => null;

    /// <summary>
    /// Gets the center of this entity. Used for centering node paths, for example, but can be used in your own plugins as well.
    /// </summary>
    [JsonIgnore]
    public virtual Vector2 Center {
        get {
            var x = X;
            var y = Y;

            x += Width / 2;
            y += Height / 2;

            return new(x, y);
        }
    }

    /// <summary>
    /// Gets the rectangle that this entity occupies. This makes use of the <see cref="Width"/> and <see cref="Height"/> properties, defaulting them to 8 if they're equal to 0.
    /// </summary>
    [JsonIgnore]
    public Rectangle Rectangle {
        get {
            var bw = Width;
            var bh = Height;
            Rectangle bRect = new(X, Y, bw == 0 ? 8 : bw, bh == 0 ? 8 : bh);
            return bRect;
        }
    }

    /// <summary>
    /// Returns the selection collider used for your entity.
    /// For nodes, call <see cref="GetNodeSelection"/> instead
    /// </summary>
    /// <returns></returns>
    public virtual ISelectionCollider GetMainSelection() {
        if (Width > 0 || Height > 0) {
            var rect = Rectangle;

            return ISelectionCollider.FromRect(rect);
        }

        try {
            if (GetSprites().FirstOrDefault() is { } firstSprite)
                return firstSprite.GetCollider();
        } catch { }

        return ISelectionCollider.FromRect(Rectangle);
    }

    public virtual ISelectionCollider GetNodeSelection(int nodeIndex) {
        var node = Nodes![nodeIndex];

        if (Width > 0 || Height > 0) {
            return ISelectionCollider.FromRect(Rectangle.MovedTo(node));
        }

        try {
            if (GetNodeSprites(nodeIndex).FirstOrDefault() is { } firstSprite) {
                return firstSprite.GetCollider();
            }
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to get node {nodeIndex} selection for: {ToJson()}");
        }

        return ISelectionCollider.FromRect(Rectangle.MovedTo(node));
    }

    public virtual IEnumerable<ISprite> GetSprites() {
        var w = Width;
        var h = Height;
        if (w != 0 || h != 0) {
            yield return ISprite.OutlinedRect(Pos, w == 0 ? 8 : w, h == 0 ? 8 : h, Color.Green * 0.3f, Color.Green);
        } else {
            yield return ISprite.OutlinedRect(Pos - new Vector2(2, 2), 4, 4, Color.Green * 0.3f, Color.Green);
        }
    }


    internal const float NodeSpriteAlpha = 0.5f;
    
    /// <summary>
    /// Gets the sprites needed to render the node <paramref name="nodeIndex"/>.
    /// </summary>
    public virtual IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var node = Nodes![nodeIndex];
        var oldPos = Pos;
        SilentSetPos(node);
        try {
            var spr = GetSprites();
            foreach (var item in spr) {
                yield return item.WithMultipliedAlpha(NodeSpriteAlpha);
            }
        } finally {
            SilentSetPos(oldPos);
        }
    }

    /// <summary>
    /// Gets the sprites needed to render lines which connect the nodes to the entity.
    /// </summary>
    public virtual IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.Line(this);

    /// <summary>
    /// Gets the sprites needed to render all of the nodes of this entity.
    /// By default, this calls <see cref="GetNodePathSprites"/> and <see cref="GetNodeSprites(int)"/>
    /// </summary>
    public virtual IEnumerable<ISprite> GetAllNodeSprites() {
        if (Nodes is null or []) {
            yield break;
        }

        foreach (var item in GetNodePathSprites()) {
            yield return item;
        }

        for (int i = 0; i < Nodes.Count; i++) {
            foreach (var item in GetNodeSprites(i)) {
                yield return item;
            }
        }
    }

    public IEnumerable<ISprite> GetSpritesWithNodes() {
        try {
            if (Nodes is { }) {
                return GetSprites().Concat(GetAllNodeSprites()).WithErrorCatch(LogError).SetDepth(Depth);
            }

            return GetSprites().WithErrorCatch(LogError).SetDepth(Depth);
        } catch (Exception ex) {
            return LogError(ex);
        }
    }

    /// <summary>
    /// Gets sprites to be rendered as a preview in the GUI.
    /// </summary>
    public virtual IEnumerable<ISprite> GetPreviewSprites()
        => GetSpritesWithNodes();

    /// <summary>
    /// Whether rendering errors should be logged to the console.
    /// </summary>
    public static bool LogErrors { get; set; } = true;

    private IEnumerable<ISprite> LogError(Exception ex) {
        if (!LogErrors)
            return Array.Empty<ISprite>();

        Logger.Error(ex, $"Erroring entity definition for {Name}: {ToJson()}");

        var w = Width;
        var h = Height;
        if (w != 0 || h != 0) {
            return new List<ISprite>(1) { 
                ISprite.OutlinedRect(Pos, w == 0 ? 8 : w, h == 0 ? 8 : h, Color.Red * 0.3f, Color.Red) with { 
                    Depth = Depths.Top,
                }
            };
        } else {
            return new List<ISprite>(1) { 
                ISprite.OutlinedRect(Pos - new Vector2(2, 2), 4, 4, Color.Red * 0.3f, Color.Red) with {
                    Depth = Depths.Top,
                } 
            };
        }
    }

    [JsonIgnore]
    public virtual bool ResizableX => Width > 0;

    [JsonIgnore]
    public virtual bool ResizableY => Height > 0;

    /// <summary>
    /// Minimum size for this entity, where lower values would cause crashes in-game.
    /// Values below this will not be accepted by the entity edit window.
    /// </summary>
    [JsonIgnore]
    public virtual Point MinimumSize => new(ResizableX ? 8 : 0, ResizableY ? 8 : 0);
    
    /// <summary>
    /// Recommended minimum size for this entity, where lower values would cause visual issues, but still work.
    /// Placement/Selection tool wont resize below this size, but manual editing still allows lower values.
    /// </summary>
    [JsonIgnore]
    public virtual Point RecommendedMinimumSize => MinimumSize;
    
    /// <summary>
    /// Maximum size for this entity, where higher values would cause crashes in-game.
    /// Values above this will not be accepted by the entity edit window.
    /// </summary>
    [JsonIgnore]
    public virtual Point MaximumSize => new(int.MaxValue, int.MaxValue);
    
    /// <summary>
    /// Recommended maximum size for this entity, where higher values would cause visual issues, but still work.
    /// Placement/Selection tool wont resize above this size, but manual editing still allows higher values.
    /// </summary>
    [JsonIgnore]
    public virtual Point RecommendedMaximumSize => MaximumSize;

    [JsonIgnore]
    public virtual Range NodeLimits => 0..0;

    [JsonIgnore]
    public virtual List<string>? AssociatedMods => null;
    
    [JsonIgnore]
    public virtual IReadOnlyList<string>? Tags => null;
    
    /// <summary>
    /// Checks whether this is similar to the given entity, used for the select-similar hotkey.
    /// </summary>
    public virtual bool IsSimilarTo(Entity entity) {
        if (entity.Name != Name)
            return false;
        
        var otherData = entity.EntityData;
        if (otherData.Count != EntityData.Count)
            return false;
        
        foreach (var (k, v) in EntityData) {
            if (k is "x" or "y" or "width" or "height" or "id")
                continue;

            if (!otherData.TryGetValue(k, out var otherVal) || !otherVal.Equals(v))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Whether this entity is currently selected.
    /// </summary>
    [JsonIgnore]
    public bool Selected { get; internal set; }

    public override string ToString() {
        return (Room, EntityData) switch {
            ( { } r, { } data) => $"{GetType().FullName}{{Room:{Room.Name}, Pos:{Pos}}}",
            ( { } r, null) => $"{GetType().FullName}{{Room:{Room.Name}}}",
            (null, { } data) => $"{GetType().FullName}{{Pos:{Pos}}}",
            (null, null) => $"{GetType().FullName}",
        };
    }

    bool IUntypedData.TryGetValue(string key, [NotNullWhen(true)] out object? value)
        => EntityData.TryGetValue(key, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetNodeCentered(int index) {
        return Nodes![index] + new Vector2(Width / 2, Height / 2);
    }


#pragma warning disable CA1720 // Identifier contains type name
    public int Int(string attrName, int def = 0) => EntityData.Int(attrName, def);
    public string Attr(string attrName, string def = "") => EntityData.Attr(attrName, def);
    public float Float(string attrName, float def = 0f) => EntityData.Float(attrName, def);
    public bool Bool(string attrName, bool def = false) => EntityData.Bool(attrName, def);
    public char Char(string attrName, char def = '0') => EntityData.Char(attrName, def);
#pragma warning restore CA1720 // Identifier contains type name

    public Color Rgb(string attrName, Color def) => EntityData.Rgb(attrName, def);
    public Color Rgb(string attrName, string def = "ffffff") => EntityData.Rgb(attrName, def);
    public Color Rgba(string attrName, Color def) => EntityData.Rgba(attrName, def);
    public Color Rgba(string attrName, string def = "ffffff") => EntityData.Rgba(attrName, def);

    public Color Argb(string attrName, Color def) => EntityData.Argb(attrName, def);
    public Color Argb(string attrName, string def = "ffffff") => EntityData.Argb(attrName, def);

    public T Enum<T>(string attrName, T def) where T : struct, Enum => EntityData.Enum(attrName, def);

    public bool Has(string attrName) => EntityData.Has(attrName);

    /// <summary>
    /// Clears the correct render cache in the parent room
    /// </summary>
    public virtual void ClearRoomRenderCache() {
        if (Room is { } r && Id >= 0) {
            //Logger.Write("INVALIDATE", LogLevel.Debug, $"{new System.Diagnostics.StackTrace().ToString()}");
            r.ClearEntityRenderCache();
        }
    }

    /// <summary>
    /// Clears any and all internal caches used by this entity, to minimize RAM usage.
    /// Does not clear the room's render cache.
    /// Should be called sparingly.
    /// </summary>
    public virtual void ClearInnerCaches() {
        _nameAsAscii = null;
        EntityData.ClearCaches();
        _cachedPackedElement = null;
    }

    public IList<Entity> GetRoomList() => this switch {
        Decal d => d.Fg ? Room.FgDecals : Room.BgDecals,
        Trigger => Room.Triggers,
        _ => Room.Entities,
    };

    /// <summary>
    /// Creates a clone of this entity by creating a placement out of this entity, then using <see cref="EntityRegistry.Create(Placement, Microsoft.Xna.Framework.Vector2, Room, bool, bool)"/>
    /// </summary>
    public Entity Clone() {
        var clone = EntityRegistry.Create(ToPlacement(), Pos, Room, false, this is Trigger);
        clone.Id = Id;

        return clone;
    }

    /// <summary>
    /// Creates a clone of this entity by creating a placement out of this entity, then creating an entity out of it.
    /// The generated placement gets passed to <paramref name="manipulator"/> before being used for entity creation, allowing to, for example, change the SID.
    /// </summary>
    public Entity CloneWith(Action<Placement> manipulator) {
        var placement = ToPlacement();
        manipulator(placement);

        var clone = EntityRegistry.Create(placement, Pos, Room, false, this is Trigger);
        clone.Id = Id;
        
        if (placement.ValueOverrides.TryGetValue("x", out var x) && x is IConvertible) {
            clone.X = Convert.ToInt32(x, CultureInfo.CurrentCulture);
        }
        if (placement.ValueOverrides.TryGetValue("y", out var y) && y is IConvertible) {
            clone.Y = Convert.ToInt32(y, CultureInfo.CurrentCulture);
        }

        return clone;
    }

    /// <summary>
    /// Tries to flip the entity horizontally. Returning null means that the entity cannot be flipped.
    /// A clone of the entity should be returned, and 'this' should not be manipulated in any way here, or history will break.
    /// </summary>
    public virtual Entity? TryFlipHorizontal() {
        if (Nodes is not { Count: > 0 } nodes) {
            return null;
        }

        // flip all nodes horizontally along the entity pos
        return CloneWith(pl => {
            var pos = Pos;
            for (int i = 0; i < nodes.Count; i++) {
                pl.Nodes![i] = nodes[i].Pos.FlipHorizontalAlong(pos);
            }
        });
    }
    /// <summary>
    /// Tries to flip the entity vertically. Returning null means that the entity cannot be flipped.
    /// A clone of the entity should be returned, and 'this' should not be manipulated in any way here, or history will break.
    /// </summary>
    public virtual Entity? TryFlipVertical() {
        if (Nodes is not { Count: > 0 } nodes) {
            return null;
        }

        // flip all nodes vertically along the entity pos
        return CloneWith(pl => {
            var pos = Pos;
            for (int i = 0; i < nodes.Count; i++) {
                pl.Nodes![i] = nodes[i].Pos.FlipVerticalAlong(pos);
            }
        });
    }

    /// <summary>
    /// Tries to rotate the entity in the given direction. Returning null means that the entity cannot be flipped.
    /// A clone of the entity should be returned, and 'this' should not be manipulated in any way here, or history will break.
    /// </summary>
    public virtual Entity? TryRotate(RotationDirection dir) {
        if (Nodes is not { Count: > 0 } nodes) {
            return null;
        }

        // rotate all nodes along the entity pos
        return CloneWith(pl => {
            var pos = Pos;
            var angle = dir.ToAndleRad();
            for (int i = 0; i < nodes.Count; i++) {
                pl.Nodes![i] = nodes[i].Pos.RotateAround(pos, angle);
            }
        });
    }

    /// <summary>
    /// Rotates this entity by <paramref name="angleRad"/> degrees (in radians).
    /// A clone of the entity should be returned, and 'this' should not be manipulated in any way here, or history will break.
    /// </summary>
    public virtual Entity? RotatePreciseBy(float angleRad, Vector2 origin) {
        if (Nodes is not { Count: > 0 } nodes) {
            /*
            var snapped = angleRad.RadToDegrees() % 360f;
            switch (snapped) {
                case (> 0 and <= 45) or (> 315 and <= 360):
                    return null;
                case > 45 and <= 45+90:
                    return TryRotate(RotationDirection.Right);
                case > 135 and <= 135+90:
                    return TryRotate(RotationDirection.Right)?.TryRotate(RotationDirection.Right);
                case > 225 and <= 225+90:
                    return TryRotate(RotationDirection.Left);
            }
            */
            return null;
        }

        // find the anchor the closest to the origin
        origin = Nodes.Select(n => n.Pos).Append(Pos).MinBy(p => Vector2.DistanceSquared(p, origin));

        // rotate all nodes along the entity pos
        var clone = CloneWith(pl => {
            //var pos = Pos;
            for (int i = 0; i < nodes.Count; i++) {
                pl.Nodes![i] = nodes[i].Pos.RotateAround(origin, angleRad).Floored();
            }
        });

        clone.Pos = Pos.RotateAround(origin, angleRad).Floored();

        return clone;
    }

    /// <summary>
    /// Handles moving this entity by the given offset.
    /// NodeIndex is -1 if moving the main entity.
    /// If 'shouldDoNormalMove' is set to true, the default move behaviour is used, which is more efficient than doing this manually.
    /// Set 'shouldDoNormalMove' to false and return a new entity instance (don't edit 'this'!) to make changes to the entity.
    /// Set 'shouldDoNormalMove' to false and return null to cancel the move.
    /// </summary>
    public virtual Entity? MoveBy(Vector2 offset, int nodeIndex, out bool shouldDoNormalMove) {
        shouldDoNormalMove = true;
        return null;
    }

    /// <summary>
    /// Gets fired whenever the EntityData for this entity gets changed in any way.
    /// </summary>
    public virtual void OnChanged(EntityDataChangeCtx changed) {
        _cachedPackedElement = null;
        
        _pos = new(EntityData.X, EntityData.Y);
        _id = EntityData.Int("id");
        SelectionHandler?.ClearCollideCache();
        if (changed.NodesChanged && NodeSelectionHandlers is { } handlers) {
            foreach (var item in handlers) {
                item?.ClearCollideCache();
                item?.RecalculateId();
            }

            if (changed.NodeCountChanged) {
                NodeSelectionHandlers = new NodeSelectionHandler?[handlers.Length];
                for (int i = 0; i < NodeSelectionHandlers.Length; i++) {
                    NodeSelectionHandlers[i] = handlers.FirstOrDefault(h => h is {} && h.NodeIdx == i);
                }
            }
        }

        if (!changed.OnlyPositionChanged) {
            BindAttribute.GetBindContext<Entity>(this).UpdateBoundFields(this, changed);
        }
        
        if ((changed.AllChanged || changed.ChangedFieldName == EditorGroupEntityDataKey) && Room is {} room) {
            var groups = EditorGroupList.FromString(room.Map.EditorGroups, Attr(EditorGroupEntityDataKey, "Default"));
            EditorGroups = groups;
            groups.SuppressCallbacks();
            // the default group is only used for entities with no other group.
            // if there's no group at the end of the method, the default group will be re-added.
            groups.Remove(EditorGroup.Default);
            
            AssureAutoAssignedGroupsExist();
        
            if (groups.Count == 0) {
                groups.Add(EditorGroup.Default);
            }
        
            // silently update the inner dictionary, to avoid calling OnChanged again
            EntityData.SilentSet(EditorGroupEntityDataKey, groups.ToString() ?? "");
            groups.Unsuppress();
        }

        ClearRoomRenderCache();
    }

    /// <summary>
    /// Checks whether the given value for a EntityData key is the default value for that key, based on the main placement for this entity's SID.
    /// </summary>
    public bool IsDefault(string key, object val) {
        var values = EntityRegistry.GetMainPlacementValues(Name, RegistryType);

        if (!values.TryGetValue(key, out var defVal)) {
            return false;
        }

        return val.Equals(defVal) || NumberExt.IntFloatLooselyEqual(val, defVal);
    }
    
    /// <summary>
    /// Returns whether the given EntityData value should be trimmed from the .bin file.
    /// This should only return true if the Celeste code can handle the value being missing properly.
    /// </summary>
    public virtual bool CanTrim(string key, object val) {
        return false;
    }

    private (BinaryPacker.Element element, bool isTrimmed)? _cachedPackedElement;

    public BinaryPacker.Element Pack(bool trim = false) {
        if (_cachedPackedElement is { } cached && cached.isTrimmed == trim) {
            return cached.element;
        }

        var el = DoPack(trim);
        _cachedPackedElement = (el, trim);
        
        return el;
    }
    
    protected virtual BinaryPacker.Element DoPack(bool trim) {
        var el = new BinaryPacker.Element(EntityData.Sid);

        var outAttrs = el.Attributes = new(EntityData.Inner.Count);
        foreach (var (k, v) in EntityData.Inner) {
            var shouldTrim = k switch {
                "x" => false,
                "y" => false,
                "id" => false,
                "width" => Width == 0,
                "height" => Height == 0,
                EditorGroupEntityDataKey => EditorGroups.IsOnlyDefault,
                _ => trim && CanTrim(k, v)
            };

            if (!shouldTrim) {
                outAttrs[k] = v;
            }
        }

        el.Children = Nodes is { Count: > 0 } nodes ? nodes.Select(n => new BinaryPacker.Element("node") {
            Attributes = new() {
                ["x"] = n.X,
                ["y"] = n.Y,
            }
        }).ToArray() : null!;

        return el;
    }

    public Placement ToPlacement() {
        var overrides = new Dictionary<string, object>(
            EntityData.Inner.Where(x=> !IsDefault(x.Key, x.Value) || x.Key is "width" or "height"), 
            StringComparer.Ordinal);

        return new Placement(EntityData.Sid) {
            Sid = EntityData.Sid,
            PlacementHandler = this is Trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity,
            RegisteredEntityType = RegistryType,
            ValueOverrides = overrides,
            Nodes = Nodes?.Select(n => n.Pos).ToArray()
        };
    }

    public RegisteredEntityType RegistryType =>
        this is Trigger ? RegisteredEntityType.Trigger : RegisteredEntityType.Entity;
    
    public Decal? AsDecal() => this as Decal;
    public Trigger? AsTrigger() => this as Trigger;

    public SelectionLayer GetSelectionLayer() => this switch {
        Trigger => SelectionLayer.Triggers,
        Decal d => d.Fg ? SelectionLayer.FgDecals : SelectionLayer.BgDecals,
        _ => SelectionLayer.Entities,
    };

    internal EntitySelectionHandler? SelectionHandler;
    public Selection CreateSelection() => new() {
        Handler = SelectionHandler ??= new EntitySelectionHandler(this)
    };

    // todo: refactor node selections
    internal NodeSelectionHandler?[]? NodeSelectionHandlers;
    public Selection CreateNodeSelection(int node) {
        if (node >= Nodes.Count)
            throw new Exception($"Tried to get selection for node at index {node}, but entity has {Nodes.Count} nodes.");
        
        NodeSelectionHandlers ??= new NodeSelectionHandler[Nodes.Count];

        if (node >= NodeSelectionHandlers.Length)
            Array.Resize(ref NodeSelectionHandlers, node + 1);

        return new(NodeSelectionHandlers[node] ??= new((EntitySelectionHandler) CreateSelection().Handler, Nodes[node]));
    }
    
    internal Selection CreateNodeSelection(int node, NodeSelectionHandler handler) {
        NodeSelectionHandlers ??= new NodeSelectionHandler[Nodes.Count];

        if (node >= NodeSelectionHandlers.Length)
            Array.Resize(ref NodeSelectionHandlers, node + 1);

        return new(NodeSelectionHandlers[node] = handler);
    }

    /// <summary>
    /// Transfers the selection handler from this entity to <paramref name="newEntity"/>, used by <see cref="SwapEntityAction"/>
    /// </summary>
    internal void TransferHandlersTo(Entity newEntity) {
        if (SelectionHandler is { } handler) {
            newEntity.SelectionHandler = handler;
            SelectionHandler = null;

            handler.Entity = newEntity;
        }

        if (NodeSelectionHandlers is { } nodeHandlers) {
            newEntity.NodeSelectionHandlers = nodeHandlers;
            NodeSelectionHandlers = null;
        }
    }

    public void InitializeNodePositions() {
        if (Nodes is { } nodes) {
            var (x, y) = (X, Y);
            var xOffset = Rectangle.Width + 8;

            for (int i = 0; i < nodes.Count; i++) {
                nodes[i].Pos = new(x + (xOffset * (i + 1)), y);
            }
        }
    }

    public string ToJson() {
        return Pack().ToJson();
    }

    #region IBindTarget
    FieldList IBindTarget.GetFields() => EntityRegistry.GetFields(this);

    object IBindTarget.GetValueForField(Field field, string key) {
        if (EntityData.TryGetValue(key, out var value))
            return value;

        return field.GetDefault();
    }

    string IBindTarget.Name => Name;
    #endregion

    #region ILuaWrapper
    private byte[]? _nameAsAscii = null;
    private NodesWrapper? _nodesWrapper;

    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException($"Can't index entity via number key: {key}");
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "x":
                lua.PushNumber(X);
                return 1;
            case "y":
                lua.PushNumber(Y);
                return 1;

            case "_id":
                lua.PushNumber(Id);
                return 1;
            case "nodes":
                if (Nodes is { }) {
                    lua.PushWrapper(_nodesWrapper ??= new NodesWrapper(this));
                } else {
                    lua.PushNil();
                }
                return 1;
            case "_name":
                lua.PushUtf8String(_nameAsAscii ??= Encoding.ASCII.GetBytes(Name));
                return 1;
            default:
                EntityData.TryGetValue(key.ToString(), out var value);
                lua.Push(value);
                return 1;
        }
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<byte> keyAscii) {
        switch (keyAscii) {
            case [(byte) 'x']:
                lua.PushNumber(X);
                return 1;
            case [(byte) 'y']:
                lua.PushNumber(Y);
                return 1;
            case [(byte) '_', (byte) 'i', (byte) 'd']:
                lua.PushNumber(Id);
                return 1;
            case [(byte) '_', (byte) 'n', (byte) 'a', (byte) 'm', (byte) 'e']:
                lua.PushUtf8String(_nameAsAscii ??= Encoding.ASCII.GetBytes(Name));
                return 1;
            case [(byte) 'n', (byte) 'o', (byte) 'd', (byte) 'e', (byte) 's']:
                if (Nodes is { }) {
                    lua.PushWrapper(_nodesWrapper ??= new NodesWrapper(this));
                } else {
                    lua.PushNil();
                }
                return 1;
            default:
                EntityData.TryGetLuaValue(keyAscii, out var value);
                lua.Push(value);
                return 1;
        }
    }

    private sealed record class NodesWrapper(Entity Entity) : ILuaWrapper {
        public int LuaIndex(Lua lua, long i) {
            var node = Entity.Nodes?.ElementAtOrDefault((int) i - 1);
            if (node is { } n) {
                lua.PushWrapper(node);

                return 1;
            } else {
                return 0;
            }
        }

        public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
            throw new LuaException(lua, $"Tried to index NodeWrapper with non-number key: {key} [{typeof(ReadOnlySpan<char>)}]");
        }

        public int LuaLen(Lua lua) {
            lua.PushInteger(Entity.Nodes?.Count ?? 0);

            return 1;
        }
    }
    #endregion
}

public readonly struct EntityDataChangeCtx {
    public string? ChangedFieldName { get; init; }
    public object? NewValue { get; init; }

    public bool NodesChanged { get; init; }
    
    public bool NodeCountChanged { get; init; }

    public bool AllChanged { get; init; }

    public bool OnlyPositionChanged => !AllChanged && ChangedFieldName is "x" or "y";

    public bool IsChanged(string fieldName) => AllChanged || fieldName.Equals(ChangedFieldName, StringComparison.Ordinal);
}

public class EntityData : IDictionary<string, object>, IUntypedData {
    public string Sid { get; init; }

    public ListenableList<Node> Nodes { get; private set; }

    public Action<EntityDataChangeCtx>? OnChanged { get; set; }

    internal Dictionary<string, object> Inner { get; private set; }

    /// <summary>
    /// Data that gets overlaid on top of <see cref="Inner"/>, used to safely implement live previews
    /// </summary>
    internal Dictionary<string, object>? FakeOverlay { get; private set; }

    /// <summary>
    /// Sets the value at the given key without calling <see cref="OnChanged"/>.
    /// For internal use only.
    /// </summary>
    internal void SilentSet(string key, object value) {
        Inner[key] = value;
    }

    internal void SetOverlay(Dictionary<string, object>? overlay) {
        ClearCaches();
        FakeOverlay = overlay;

        OnChanged?.Invoke(new EntityDataChangeCtx {
            AllChanged = true,
        });
    }

    /// <summary>
    /// Used by lua to efficiently retrieve items from entity data using an ascii span, stores strings as ASCII
    /// </summary>
    private Dictionary<nint, object>? _luaValues;

    private int? _x;
    private int? _y;
    public int X => _x ??= this.Int("x");
    public int Y => _y ??= this.Int("y");

    public EntityData(string sid, BinaryPacker.Element e) {
        Sid = sid;
        Inner = new(e.Attributes, StringComparer.Ordinal);

        if (e.Children is { Length: > 0 }) {
            InitializeNodes(e.Children.Length);
            var nodes = Nodes!;

            for (int i = 0; i < e.Children.Length; i++) {
                var child = e.Children[i];
                nodes.Add(new(child.Float("x"), child.Float("y")));
            }
        } else {
            InitializeNodes(0);
        }
    }

    public EntityData(string sid, Dictionary<string, object> attributes, Vector2[]? nodes = null) {
        Sid = sid;

        Nodes = nodes?.Select(n => new Node(n)).ToListenableList() ?? new(capacity: 0);
        Nodes.OnChanged = () => OnChanged?.Invoke(new EntityDataChangeCtx {
            NodesChanged = true,
            NodeCountChanged = true,
        });

        Inner = new(attributes);
    }

    public void ReplaceNodes(IEnumerable<Vector2>? newNodes) {
        Nodes.Clear();
        if (newNodes is {})
            foreach (var node in newNodes) {
                Nodes.Add(node);
            }
    }

    public void InitializeNodes(int capacity) {
        Nodes = new(() => OnChanged?.Invoke(new EntityDataChangeCtx {
            NodesChanged = true,
            NodeCountChanged = true,
        }), capacity: capacity);
    }

    public void BulkUpdate(IReadOnlyDictionary<string, object> delta) {
        foreach (var (k, v) in delta) {
            this[k] = v;
        }
    }

    internal void ClearCaches(string? key = null) {
        _luaValues?.Clear();

        FakeOverlay = null;

        _x = null;
        _y = null;
    }

    public object this[string key] {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
        set {
            bool edited;
            if (value is null) {
                edited = Inner.Remove(key);
            } else {
                Inner[key] = value;
                edited = true;
            }

            if (edited) {
                ClearCaches(key);

                OnChanged?.Invoke(new EntityDataChangeCtx {
                    NewValue = value,
                    ChangedFieldName = key
                });
            }
        }
    }

    #region IDictionary
    public ICollection<string> Keys => FakeOverlay is { } ov ? Inner.CreateMerged(ov).Keys : Inner.Keys;

    public ICollection<object> Values => FakeOverlay is { } ov ? Inner.CreateMerged(ov).Values : Inner.Values;

    public int Count => FakeOverlay is { } ov ? Inner.CreateMerged(ov).Count : Inner.Count;

    public bool IsReadOnly => false;

    public void Add(string key, object value) {
        Inner.Add(key, value);
        ClearCaches(key);

        OnChanged?.Invoke(new() {
            ChangedFieldName = key,
            NewValue = value,
        });
    }

    public void Add(KeyValuePair<string, object> item) {
        ((ICollection<KeyValuePair<string, object>>) Inner).Add(item);
        ClearCaches(item.Key);

        OnChanged?.Invoke(new() {
            ChangedFieldName = item.Key,
            NewValue = item.Value,
        });
    }

    public void Clear() {
        Inner.Clear();
        _luaValues?.Clear();
        ClearCaches();
        OnChanged?.Invoke(new() {
            AllChanged = true,
        });
    }

    public bool Contains(KeyValuePair<string, object> item) =>
        TryGetValue(item.Key, out var value) && value == item.Value;

    public bool ContainsKey(string key)
        => TryGetValue(key, out _);

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
        ((ICollection<KeyValuePair<string, object>>) Inner).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        if (FakeOverlay is { } ov)
            return Inner.CreateMerged(ov).GetEnumerator();

        return Inner.GetEnumerator();
    }

    public bool Remove(string key) {
        if (Inner.Remove(key)) {
            ClearCaches(key);
            OnChanged?.Invoke(new() {
                ChangedFieldName = key,
            });

            return true;
        }

        return false;
    }

    public bool Remove(KeyValuePair<string, object> item) {
        return Remove(item.Key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) {
        if (FakeOverlay is { } ov && ov.TryGetValue(key, out value)) {
            return value is not null; // key = null in overlays means that the key should get removed
        }

        return Inner.TryGetValue(key, out value);
    }

    /// <summary>
    /// Retries a value from this entity data using a span. All strings returned by this are converted to ASCII byte[]
    /// </summary>
    internal unsafe bool TryGetLuaValue(ReadOnlySpan<byte> key, out object? value) {
        _luaValues ??= new();

        // as lua strings are interned, we can somewhat trust that a pointer is enough to uniquely identify the string,
        // saving the need to iterate the string to hash it.
        // var hash = new HashCode();
        // hash.AddBytes(key);
        fixed (byte* bp = key) {
            ref var valueInDict = ref CollectionsMarshal.GetValueRefOrAddDefault(_luaValues, (nint) bp, out var exists);
            if (exists) {
                value = valueInDict;
                return true;
            }

            if (TryGetValue(Encoding.ASCII.GetString(key), out var fromData)) {
                if (fromData is string str) {
                    fromData = Encoding.ASCII.GetBytes(str);
                }

                valueInDict = fromData;
                value = fromData;
                return true;
            }

            valueInDict = null;
            value = null;
            return false;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    #endregion

    public bool Has(string attrName) 
        => ContainsKey(attrName);
}