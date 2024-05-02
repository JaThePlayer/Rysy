using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.LuaSupport;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy.Stylegrounds;

public abstract class Style : IPackable, IName, IBindTarget, ILuaWrapper {
    public static FieldList GetDefaultFields() => new FieldList(new {
        only = Fields.String("*").AllowNull().ConvertEmptyToNull(),
        exclude = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        flag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        notflag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        tag = new StylegroundTagField(null!, editable: true).AllowNull().ConvertEmptyToNull().ToList(',').WithMinElements(0),
        _indent = new PaddingField()
    });

    public string Name { get; set; }

    private EntityData _Data;

    public EntityData Data {
        get => _Data;
        set {
            if (_Data is not null) {
                _Data.OnChanged -= OnChanged;
            }
            _Data = value;
            _Data.OnChanged += OnChanged;

            OnChanged(new() {
                AllChanged = true
            });
        }
    }

    protected BinaryPacker.Element[]? UnhandledChildren;

    [Bind("only")]
    public string? Only;

    [Bind("exclude")]
    public string? Exclude;

    [Bind("flag")]
    public string? Flag;

    [Bind("notflag")]
    public string? NotFlag;

    [Bind("tag")]
    public ReadOnlyArray<string> Tags;

    public bool HasTag(string tag) {
        foreach (var selfTag in Tags) {
            if (selfTag == tag)
                return true;
        }

        return false;
    }

    private StyleFolder? _Parent;
    [JsonIgnore]
    public StyleFolder? Parent {
        get => _Parent;
        internal set {
            _Parent = value;
            OnChanged(new() {
                AllChanged = true
            });
        }
    }

    [JsonIgnore]
    public virtual string DisplayName => $"style.effects.{Name}.name".TranslateOrNull() ?? Name[(Name.LastIndexOf('/') + 1)..].Humanize();

    [JsonIgnore]
    public virtual bool CanBeInBackground => true;

    [JsonIgnore]
    public virtual bool CanBeInForeground => true;

    [JsonIgnore]
    public virtual List<string>? AssociatedMods => null;

    /// <summary>
    /// Gets the sprites needed to render this style in the styleground window.
    /// <see cref="PreviewRectangle"/> returns a rectangle with the bounds of this window.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<ISprite> GetPreviewSprites() {
        yield return new PicoTextRectSprite("No preview") {
            Pos = new(0, 0, 100, 150),
        };
    }

    /// <summary>
    /// Gets the sprites needed to render this style in-editor (outside of the styleground window)
    /// </summary>
    public virtual IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        yield break;
    }

    public virtual BinaryPacker.Element Pack() {
        return new(Name) {
            Attributes = new(Data.Inner),
            Children = UnhandledChildren!,
        };
    }

    public virtual void Unpack(BinaryPacker.Element from) {
        Name = from.Name ?? throw new Exception("Style with no name???");
        Data = new(Name, from.Attributes);
        UnhandledChildren = from.Children;
    }

    public virtual bool Visible(StylegroundRenderCtx ctx) {
        if (Parent is { } && !Parent.Visible(ctx))
            return false;
        if (!Flag.IsNullOrWhitespace() || !NotFlag.IsNullOrWhitespace())
            return false;

        var roomName = ctx.Room.Name;
        if (Only is { } only && !MatchRoomName(only, roomName))
            return false;

        if (Exclude is { } exclude && MatchRoomName(exclude, roomName))
            return false;

        return true;
    }

    /// <summary>
    /// Returns the state the sprite batch needs to be in for this styleground to render correctly.
    /// Null should be returned if the state should be kept default.
    /// </summary>
    public virtual SpriteBatchState? GetSpriteBatchState() => null;

    public virtual void OnChanged(EntityDataChangeCtx ctx) {
        BindAttribute.GetBindContext<Style>(this).UpdateBoundFields(this, ctx);
    }

    /// <summary>
    /// The rectangle to be used in <see cref="GetPreviewSprites"/>
    /// </summary>
    public Rectangle PreviewRectangle() => new(0, 0, 320, 180);
    
    public static bool MatchRoomName(ReadOnlySpan<char> predicate, ReadOnlySpan<char> roomName) {
        if (predicate is ['*'])
            return true;
        
        if (roomName.StartsWith("lvl_")) {
            roomName = roomName["lvl_".Length..];
        }

        foreach (var filter in predicate.EnumerateSplits(',')) {
            if (filter.SequenceEqual(roomName))
                return true;

            if (!filter.Contains('*'))
                continue;
            
            if (!RoomNameMatchRegexCache.TryGetValue(StringRef.FromSpanIntoShared(filter), out var regex)) {
                var filterString = filter.ToString();
                string pattern = "^" + Regex.Escape(filterString).Replace("\\*", ".*") + "$";

                regex = new Regex(pattern, RegexOptions.Compiled);
                RoomNameMatchRegexCache[StringRef.FromString(filterString)] = regex;
            }

            if (regex.IsMatch(roomName))
                return true;
        }

        return false;
    }

    private static readonly Dictionary<StringRef, Regex> RoomNameMatchRegexCache = new();

    public static Style FromName(string name) {
        return FromElement(new(name) {
            Children = Array.Empty<BinaryPacker.Element>(),
            Attributes = new()
        });
    }

    public static Style FromPlacement(Placement pl) {
        var data = EntityRegistry.GetDataFromPlacement(pl);

        return FromElement(new() {
            Attributes = data,
            Name = pl.SID
        });
    }

    public static Style FromElement(BinaryPacker.Element from) {
        if (from.Name is null)
            throw new Exception("Style with null name???");

        if (EntityRegistry.GetTypeForSID(from.Name) is { } t && t.IsSubclassOf(typeof(Style))) {
            var style = (Style) Activator.CreateInstance(t)!;
            style.Unpack(from);

            return style;
        }

        Logger.Write("Stylegrounds", LogLevel.Warning, $"Unknown style type: {from.Name}");

        var unk = new UnknownStyle();
        unk.Unpack(from);

        return unk;
    }

    #region EntityData wrappers

    /// <summary>
    /// Gets the <see cref="EntityData"/> from either this style or any of its parents recursively, which contains the given key. If no such data exists, null is returned
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private EntityData? GetDataContaining(string key) {
        Style? style = this;
        while (style is { }) {
            if (style.Data.ContainsKey(key))
                return style.Data;

            style = style.Parent;
        }

        return null;
    }

    /// <summary>
    /// Wrapper for <see cref="EntityData.Bool"/>, which calls it on <see cref="Parent"/> if the key is not defined on this style.
    /// </summary>
    public bool Bool(string key, bool def) => GetDataContaining(key)?.Bool(key, def) ?? def;

    /// <summary>
    /// Wrapper for <see cref="EntityData.Attr"/>, which calls it on <see cref="Parent"/> if the key is not defined on this style.
    /// </summary>
    public string Attr(string key, string def = "") => GetDataContaining(key)?.Attr(key, def) ?? def;

    /// <summary>
    /// Wrapper for <see cref="EntityData.Float"/>, which recursively calls it on <see cref="Parent"/> if the key is not defined on this style.
    /// </summary>
    public float Float(string key, float def) => GetDataContaining(key)?.Float(key, def) ?? def;

    /// <summary>
    /// Wrapper for <see cref="EntityData.GetColor(string, Color, ColorFormat)"/>, which recursively calls it on <see cref="Parent"/> if the key is not defined on this style.
    /// </summary>
    public Color GetColor(string key, Color def, ColorFormat format) => GetDataContaining(key)?.GetColor(key, def, format) ?? def;
    #endregion

    #region IBindTarget

    FieldList IBindTarget.GetFields() {
        var defaults = GetDefaultFields();
        var fields = EntityRegistry.GetFields(Name);

        foreach (var (k, v) in defaults) {
            fields.TryAdd(k, v);
        }

        return fields;
    }

    object IBindTarget.GetValueForField(Field field, string key) {
        Style? style = this;
        while (style is { }) {
            if (style.Data.TryGetValue(key, out var value))
                return value;

            style = style.Parent;
        }

        return field.GetDefault();
    }
    #endregion

    #region ILuaWrapper
    public int LuaIndex(Lua lua, long key) {
        lua.PushNil();
        return 1;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        lua.PushNil();
        return 1;
    }
    #endregion
}
