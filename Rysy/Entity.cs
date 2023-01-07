using Rysy.Graphics;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Rysy;

public abstract class Entity {
    public abstract int Depth { get; }

    // set by EntityRegistry:
    public EntityData EntityData = null!;
    public Room Room { get; set; } = null!;

    public int ID;

    public Vector2 Pos;

    public Vector2[]? Nodes => EntityData.Nodes;

    public int Width {
        get => EntityData.Int("width");
        set => EntityData["width"] = value;
    }

    public int Height {
        get => EntityData.Int("height");
        set => EntityData["height"] = value;
    }

    public int EditorLayer {
        get => EntityData.Int("_editorLayer");
        set => EntityData["_editorLayer"] = value;
    }

    /// <summary>
    /// Gets the center of this entity. Used for centering node paths, for example, but can be used in your own plugins as well.
    /// </summary>
    public virtual Vector2 Center {
        get {
            var x = Pos.X;
            var y = Pos.Y;

            x += Width / 2;
            y += Height / 2;

            return new(x, y);
        }
    }


    /// <summary>
    /// Gets the rectangle that this entity occupies. This makes use of the <see cref="Width"/> and <see cref="Height"/> properties, defaulting them to 8 if they're equal to 0.
    /// </summary>
    public Rectangle Rectangle {
        get {
            var bw = Width;
            var bh = Height;
            Rectangle bRect = new((int) Pos.X, (int) Pos.Y, bw == 0 ? 8 : bw, bh == 0 ? 8 : bh);
            return bRect;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetNodeCentered(int index) {
        return Nodes![index] + new Vector2(Width / 2, Height / 2);
    }

    public virtual IEnumerable<ISprite> GetSprites() {
        yield break;
    }

    public override string ToString() {
        return $"{GetType().FullName}{{Room:{Room.Name}, Pos:{Pos}}}";
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

    public BinaryPacker.Element Pack() {
        var el = new BinaryPacker.Element(EntityData.Name);
        el.Attributes = new Dictionary<string, object>(EntityData.Inner);
        el.Attributes["x"] = Pos.X;
        el.Attributes["y"] = Pos.Y;
        el.Attributes["id"] = ID;

        el.Children = Nodes is { } nodes ? nodes.Select(n => new BinaryPacker.Element("node") { 
            Attributes = new() {
                ["x"] = n.X,
                ["y"] = n.Y,
            }
        }).ToArray() : null!;
        return el;
    }
}

public class EntityData : IDictionary<string, object> {
    public string Name;

    public Vector2[]? Nodes;

    public EntityData(string sid, BinaryPacker.Element e) {
        Name = sid;

        var dict = e.Attributes;
        Inner = new(dict.Count - 3);

        foreach (var item in dict) {
            if (item.Key is not "x" and not "y" and not "id") {
                Inner[item.Key] = item.Value;
            }
        }

        if (e.Children is { Length: > 0 }) {
            var nodes = Nodes = new Vector2[e.Children.Length];

            for (int i = 0; i < e.Children.Length; i++) {
                var child = e.Children[i];
                nodes[i] = new(child.Float("x"), child.Float("y"));
            }
        }
    }

    internal Dictionary<string, object> Inner = new();

    public object this[string key] { get => Inner[key]; set => Inner[key] = value; }

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
            return Convert.ToInt32(obj);
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