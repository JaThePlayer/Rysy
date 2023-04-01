﻿using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.LuaSupport;
using Rysy.Scenes;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Rysy;

public abstract class Entity : ILuaWrapper, IConvertibleToPlacement, IDepth {
    [JsonIgnore]
    public abstract int Depth { get; }

    public string Name => EntityData.SID;

    [JsonPropertyName("Room")]
    public string RoomName => Room.Name;

    // set by EntityRegistry:
    public EntityData EntityData = null!;

    [JsonIgnore]
    public Room Room { get; set; } = null!;

    public int ID {
        get => EntityData.Int("id");
        set => EntityData["id"] = value;
    }

    public int X {
        get => EntityData.X;
        set {
            EntityData["x"] = value;
            ClearRoomRenderCache();
        }
    }

    public int Y {
        get => EntityData.Y;
        set {
            EntityData["y"] = value;
            ClearRoomRenderCache();
        }
    }

    public Vector2 Pos {
        get => new(EntityData.Float("x"), EntityData.Float("y"));
        set {
            EntityData["x"] = value.X;
            EntityData["y"] = value.Y;

            ClearRoomRenderCache();
        }
    }

    [JsonIgnore]
    public List<Node>? Nodes => EntityData.Nodes;

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

    [JsonIgnore]
    public int EditorLayer {
        get => EntityData.Int("_editorLayer");
        set => EntityData["_editorLayer"] = value;
    }

    [JsonIgnore]
    /// <summary>
    /// Gets the center of this entity. Used for centering node paths, for example, but can be used in your own plugins as well.
    /// </summary>
    public virtual Vector2 Center {
        get {
            var x = X;
            var y = Y;

            x += Width / 2;
            y += Height / 2;

            return new(x, y);
        }
    }

    [JsonIgnore]
    /// <summary>
    /// Gets the rectangle that this entity occupies. This makes use of the <see cref="Width"/> and <see cref="Height"/> properties, defaulting them to 8 if they're equal to 0.
    /// </summary>
    public Rectangle Rectangle {
        get {
            var bw = Width;
            var bh = Height;
            Rectangle bRect = new(X, Y, bw == 0 ? 8 : bw, bh == 0 ? 8 : bh);
            return bRect;
        }
    }

    public virtual ISelectionCollider GetMainSelection() {
        if (Width > 0 || Height > 0) {
            var rect = Rectangle;

            return ISelectionCollider.FromRect(rect);
        }

        var firstSprite = GetSprites().FirstOrDefault();

        if (firstSprite is Sprite s) {
            return ISelectionCollider.FromSprite(s);
        }

        return ISelectionCollider.FromRect(Rectangle);
    }

    public virtual ISelectionCollider GetNodeSelection(int nodeIndex) {
        var node = Nodes![nodeIndex];

        if (Width > 0 || Height > 0) {
            var rect = Rectangle;
            return ISelectionCollider.FromRect(rect.MovedTo(node));
        }

        var firstSprite = NodeHelper.GetNodeSpritesForNode(this, nodeIndex).FirstOrDefault();
        if (firstSprite is Sprite s) {
            return ISelectionCollider.FromSprite(s);
        }

        return ISelectionCollider.FromRect(Rectangle.MovedTo(node));
    }

    public virtual IEnumerable<ISprite> GetSprites() {
        yield break;
    }

    public IEnumerable<ISprite> GetSpritesWithNodes() {
        if (Nodes is { }) {
            return GetSprites().Concat(NodeHelper.GetNodeSpritesFor(this));
        }

        return GetSprites();
    }

    [JsonIgnore]
    public virtual bool ResizableX => Width > 0;

    [JsonIgnore]
    public virtual bool ResizableY => Height > 0;

    [JsonIgnore]
    public virtual Point MinimumSize => new(ResizableX ? 8 : 0, ResizableY ? 8 : 0);

    [JsonIgnore]
    public virtual Range NodeLimits => 0..0;

    public override string ToString() {
        return (Room, EntityData) switch {
            ( { } r, { } data) => $"{GetType().FullName}{{Room:{Room.Name}, Pos:{Pos}}}",
            ( { } r, null) => $"{GetType().FullName}{{Room:{Room.Name}}}",
            (null, { } data) => $"{GetType().FullName}{{Pos:{Pos}}}",
            (null, null) => $"{GetType().FullName}",
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetNodeCentered(int index) {
        return Nodes![index] + new Vector2(Width / 2, Height / 2);
    }
    public int Int(string attrName, int def = 0) => EntityData.Int(attrName, def);
    public string Attr(string attrName, string def = "") => EntityData.Attr(attrName, def);
    public float Float(string attrName, float def = 0f) => EntityData.Float(attrName, def);
    public bool Bool(string attrName, bool def = false) => EntityData.Bool(attrName, def);
    public char Char(string attrName, char def = '0') => EntityData.Char(attrName, def);

    public Color RGB(string attrName, Color def) => EntityData.RGB(attrName, def);
    public Color RGB(string attrName, string def = "ffffff") => EntityData.RGB(attrName, def);
    public Color RGBA(string attrName, Color def) => EntityData.RGBA(attrName, def);
    public Color RGBA(string attrName, string def = "ffffff") => EntityData.RGBA(attrName, def);

    public Color ARGB(string attrName, Color def) => EntityData.ARGB(attrName, def);
    public Color ARGB(string attrName, string def = "ffffff") => EntityData.ARGB(attrName, def);

    public T Enum<T>(string attrName, T def) where T : struct, Enum => EntityData.Enum(attrName, def);

    /// <summary>
    /// Clears the correct render cache in the parent room
    /// </summary>
    public void ClearRoomRenderCache() {
        if (Room is { } r) {
            switch (this) {
                case Decal d:
                    if (d.FG)
                        r.ClearFgDecalsRenderCache();
                    else
                        r.ClearBgDecalsRenderCache();
                    break;
                case Trigger:
                    r.ClearTriggerRenderCache();
                    break;
                default:
                    r.ClearEntityRenderCache();
                    break;
            }
        }
    }

    public IList<Entity> GetRoomList() => this switch {
        Decal d => d.FG ? Room.FgDecals : Room.BgDecals,
        Trigger => Room.Triggers,
        _ => Room.Entities,
    };

    /// <summary>
    /// Creates a clone of this entity by creating a placement out of this entity, then using <see cref="EntityRegistry.Create"/>
    /// </summary>
    public Entity Clone() {
        var clone = EntityRegistry.Create(ToPlacement(), Pos, Room, false, this is Trigger);
        clone.ID = ID;

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
        clone.ID = ID;

        return clone;
    }

    /// <summary>
    /// Tries to flip the entity horizontally. Returning null means that the entity cannot be flipped.
    /// A clone of the entity should be returned, and 'this' should not be manipulated in any way here, or history will break.
    /// </summary>
    public virtual Entity? TryFlipHorizontal() => null;
    /// <summary>
    /// Tries to flip the entity vertically. Returning null means that the entity cannot be flipped.
    /// A clone of the entity should be returned, and 'this' should not be manipulated in any way here, or history will break.
    /// </summary>
    public virtual Entity? TryFlipVertical() => null;

    public virtual BinaryPacker.Element Pack() {
        var el = new BinaryPacker.Element(EntityData.SID);
        el.Attributes = new Dictionary<string, object>(EntityData.Inner, StringComparer.Ordinal);

        if (ID == 0) {
            el.Attributes.Remove("id");
        }

        el.Children = Nodes is { } nodes ? nodes.Select(n => new BinaryPacker.Element("node") {
            Attributes = new() {
                ["x"] = n.X,
                ["y"] = n.Y,
            }
        }).ToArray() : null!;
        return el;
    }

    public Placement ToPlacement() {
        var overrides = new Dictionary<string, object>(EntityData.Inner, StringComparer.Ordinal);

        return new Placement(EntityData.SID) {
            SID = EntityData.SID,
            PlacementHandler = this is Trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity,
            ValueOverrides = overrides,
            Nodes = Nodes?.Select(n => n.Pos).ToArray()
        };
    }

    public Decal? AsDecal() => this as Decal;
    public Trigger? AsTrigger() => this as Trigger;

    public SelectionLayer GetSelectionLayer() => this switch {
        Trigger => SelectionLayer.Triggers,
        Decal d => d.FG ? SelectionLayer.FGDecals : SelectionLayer.BGDecals,
        _ => SelectionLayer.Entities,
    };

    public void InitializeNodePositions() {
        if (Nodes is { } nodes) {
            var (x, y) = (X, Y);
            var xOffset = Rectangle.Width + 8;

            for (int i = 0; i < nodes.Count; i++) {
                nodes[i].Pos = new(x + (xOffset * (i + 1)), y);
            }
        }
    }

    #region ILuaWrapper
    private byte[]? _NameAsASCII = null;

    public int Lua__index(Lua lua, long key) {
        throw new NotImplementedException($"Can't index entity via number key: {key}");
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "x":
                lua.PushNumber(X);
                return 1;
            case "y":
                lua.PushNumber(Y);
                return 1;

            case "_id":
                lua.PushNumber(ID);
                return 1;
            case "nodes":
                if (Nodes is { }) {
                    lua.PushWrapper(new NodesWrapper(this));
                } else {
                    lua.PushNil();
                }
                return 1;
            case "_name":
                //lua.PushString(Name);
                lua.PushASCIIString(_NameAsASCII ??= Encoding.ASCII.GetBytes(Name));
                return 1;
            default:
                EntityData.TryGetValue(key.ToString(), out var value);
                lua.Push(value);
                return 1;
        }
    }

    private record class NodesWrapper(Entity Entity) : ILuaWrapper {
        public int Lua__index(Lua lua, long i) {
            var node = Entity.Nodes?.ElementAtOrDefault((int) i - 1);
            if (node is { } n) {
                lua.CreateTable(0, 2);
                var tableLoc = lua.GetTop();

                lua.PushString("x");
                lua.PushNumber(n.X);
                lua.SetTable(tableLoc);

                lua.PushString("y");
                lua.PushNumber(n.Y);
                lua.SetTable(tableLoc);

                return 1;
            } else {
                return 0;
            }
        }

        public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
            throw new LuaException(lua, $"Tried to index NodeWrapper with non-number key: {key} [{typeof(ReadOnlySpan<char>)}]");
        }
    }
    #endregion
}

internal class EntitySelectionHandler : ISelectionHandler, ISelectionFlipHandler {
    public EntitySelectionHandler(Entity entity) => Entity = entity;

    public Entity Entity { get; set; }

    public object Parent => Entity;

    public SelectionLayer Layer => Entity.GetSelectionLayer();

    private ISelectionCollider? _Collider;
    private ISelectionCollider Collider => _Collider ??= Entity.GetMainSelection();

    public IHistoryAction DeleteSelf() {
        return new RemoveEntityAction(Entity, Entity.Room);
    }

    public bool IsWithinRectangle(Rectangle roomPos) => Collider.IsWithinRectangle(roomPos);

    public IHistoryAction MoveBy(Vector2 offset) {
        return new MoveEntityAction(Entity, offset);
    }

    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos) {
        var nodeLimitsEnd = Entity.NodeLimits.End;
        if (!nodeLimitsEnd.IsFromEnd && (Entity.Nodes?.Count ?? 0) == nodeLimitsEnd.Value)
            return null;

        var node = new Node(pos ?? (Entity.Pos + new Vector2(16f, 0)));

        return (
            new AddNodeAction(Entity, node, 0),
            new NodeSelectionHandler(Entity, node)
        );
    }

    public void RenderSelection(Color c) {
        Collider.Render(c);
    }

    public IHistoryAction? TryResize(Point delta) {
        var resizableX = Entity.ResizableX;
        var resizableY = Entity.ResizableY;

        if ((resizableX && delta.X != 0) || (resizableY && delta.Y != 0)) {
            return new EntityResizeAction(Entity, delta);
        }

        return null;
    }

    public void ClearCollideCache() {
        _Collider = null;
    }

    private IHistoryAction? FlipImpl(Entity? flipped, string funcName) {
        var orig = Entity;

        if (flipped is null)
            return null;

        if (orig == flipped)
            throw new Exception($"When implementing Entity.{funcName}, don't return or manipulate 'this'!");

        Entity = flipped;
        return new SwapEntityAction(orig, flipped);
    }

    IHistoryAction? ISelectionFlipHandler.TryFlipHorizontal() {
        var flipped = Entity.TryFlipHorizontal();

        return FlipImpl(flipped, "TryFlipHorizontal");
    }

    IHistoryAction? ISelectionFlipHandler.TryFlipVertical() {
        var flipped = Entity.TryFlipVertical();

        return FlipImpl(flipped, "TryFlipVertical");
    }

    public void OnRightClicked(IEnumerable<Selection> selections) {
        CreateEntityPropertyWindow(Entity, selections);
    }

    public static void CreateEntityPropertyWindow(Entity main, IEnumerable<Selection> selections) {
        var history = EditorState.History;

        if (history is { }) {
            var allEntities = selections.SelectWhereNotNull(s => {
                switch (s.Handler) {
                    case EntitySelectionHandler entitySelect:
                        if (entitySelect.Entity.GetType() == main.GetType())
                            return entitySelect.Entity;
                        break;
                    case NodeSelectionHandler entitySelect:
                        if (entitySelect.Entity.GetType() == main.GetType())
                            return entitySelect.Entity;
                        break;
                    default:
                        break;
                }

                return null;
            }).Distinct().ToList();
            RysyEngine.Scene.AddWindow(new EntityPropertyWindow(history, main, allEntities));
        }
    }

    public BinaryPacker.Element? PackParent() {
        return Entity.Pack();
    }

    public IHistoryAction PlaceClone(Room room) {
        return new AddEntityAction(Entity.Clone(), room);
    }
}

public class EntityData : IDictionary<string, object> {
    public string SID { get; init; }

    public List<Node>? Nodes;

    private int? _X;
    private int? _Y;
    public int X => _X ??= Int("x");
    public int Y => _Y ??= Int("y");

    public EntityData(string sid, BinaryPacker.Element e) {
        SID = sid;
        Inner = new(e.Attributes, StringComparer.Ordinal);

        if (e.Children is { Length: > 0 }) {
            var nodes = Nodes = new(e.Children.Length);

            for (int i = 0; i < e.Children.Length; i++) {
                var child = e.Children[i];
                nodes.Add(new(child.Float("x"), child.Float("y")));
            }
        }
    }

    public EntityData(string sid, Dictionary<string, object> attributes, Vector2[]? nodes = null) {
        SID = sid;
        Nodes = nodes?.Select(n => new Node(n)).ToList();
        Inner = new(attributes);
    }

    internal Dictionary<string, object> Inner { get; set; } = new();

    public object this[string key] { 
        get => Inner[key]; 
        set {
            Inner[key] = value;
            _X = null;
            _Y = null;
        }
    }

    #region IDictionary
    public ICollection<string> Keys => Inner.Keys;

    public ICollection<object> Values => Inner.Values;

    public int Count => Inner.Count;

    public bool IsReadOnly => false;

    public void Add(string key, object value) {
        Inner.Add(key, value);
    }

    public void Add(KeyValuePair<string, object> item) {
        ((ICollection<KeyValuePair<string, object>>) Inner).Add(item);
    }

    public void Clear() {
        ((ICollection<KeyValuePair<string, object>>) Inner).Clear();
    }

    public bool Contains(KeyValuePair<string, object> item) {
        return ((ICollection<KeyValuePair<string, object>>) Inner).Contains(item);
    }

    public bool ContainsKey(string key) {
        return Inner.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
        ((ICollection<KeyValuePair<string, object>>) Inner).CopyTo(array, arrayIndex);
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        return Inner.GetEnumerator();
    }

    public bool Remove(string key) {
        return Inner.Remove(key);
    }

    public bool Remove(KeyValuePair<string, object> item) {
        return ((ICollection<KeyValuePair<string, object>>) Inner).Remove(item);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) {
        return Inner.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return Inner.GetEnumerator();
    }

    #endregion


    public int Int(string attrName, int def = 0) {
        if (Inner.TryGetValue(attrName, out var obj)) {
            return obj is int i ? i : Convert.ToInt32(obj);
        }

        return def;
    }

    public string Attr(string attrName, string def = "") {
        if (Inner.TryGetValue(attrName, out var obj)) {
            return obj.ToString()!;
        }

        return def;
    }

    public float Float(string attrName, float def = 0f) {
        if (Inner.TryGetValue(attrName, out var obj))
            return Convert.ToSingle(obj);

        return def;
    }

    public bool Bool(string attrName, bool def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return Convert.ToBoolean(obj);

        return def;
    }

    public char Char(string attrName, char def) {
        if (Inner.TryGetValue(attrName, out var obj) && char.TryParse(obj.ToString(), out var result))
            return result;

        return def;
    }

    public T Enum<T>(string attrName, T def) where T : struct, Enum {
        if (Inner.TryGetValue(attrName, out var obj) && System.Enum.TryParse<T>(obj.ToString(), true, out var result))
            return result;

        return def;
    }

    public Color RGB(string attrName, Color def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return ColorHelper.RGB(obj.ToString());

        return def;
    }

    public Color RGB(string attrName, string def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return ColorHelper.RGB(obj.ToString());

        return ColorHelper.RGB(def);
    }

    public Color RGBA(string attrName, Color def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return ColorHelper.RGBA(obj.ToString());

        return def;
    }

    public Color RGBA(string attrName, string def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return ColorHelper.RGBA(obj.ToString());

        return ColorHelper.RGBA(def);
    }

    public Color ARGB(string attrName, Color def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return ColorHelper.ARGB(obj.ToString());

        return def;
    }

    public Color ARGB(string attrName, string def) {
        if (Inner.TryGetValue(attrName, out var obj))
            return ColorHelper.ARGB(obj.ToString());

        return ColorHelper.ARGB(def);
    }
}