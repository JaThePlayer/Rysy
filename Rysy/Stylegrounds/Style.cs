using KeraLua;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.LuaSupport;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy.Stylegrounds;

public abstract class Style : IPackable, IName, IBindTarget, ILuaWrapper, IUntypedData {
    public static FieldList GetDefaultFields() => new FieldList(new {
        only = Fields.String("*").AllowNull().ConvertEmptyToNull(),
        exclude = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        flag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        notflag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        tag = new StylegroundTagField(null!, editable: true).AllowNull().ConvertEmptyToNull().ToList(',').WithMinElements(0),
        _indent = new PaddingField()
    });

    public string Name { get; set; }

    private EntityData _data;

    public EntityData Data {
        get => _data;
        set {
            if (_data is not null) {
                _data.OnChanged -= OnChanged;
            }
            _data = value;
            _data.OnChanged += OnChanged;

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

    private StyleFolder? _parent;
    [JsonIgnore]
    public StyleFolder? Parent {
        get => _parent;
        internal set {
            _parent = value;
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
        if (Only is null && Data.ContainsKey("only"))
            return false;
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
        _isMasked = null;
        
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
        
        if (RoomNameMatchCacheAltLookup.TryGetValue(new(predicate, roomName), out var cache)) {
            return cache;
        }
        
        foreach (var filter in predicate.EnumerateSplits(',')) {
            if (filter.SequenceEqual(roomName)) {
                return SetCache(predicate, roomName, true);
            }

            if (!filter.Contains('*'))
                continue;
            
            if (!RoomNameMatchRegexCache.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(filter, out var regex)) {
                var filterString = filter.ToString();
                string pattern = "^" + Regex.Escape(filterString).Replace("\\*", ".*", StringComparison.Ordinal) + "$";

                regex = new Regex(pattern, RegexOptions.Compiled);
                RoomNameMatchRegexCache[filterString] = regex;
            }

            if (regex.IsMatch(roomName))
                return SetCache(predicate, roomName, true);
        }

        return SetCache(predicate, roomName, false);

        static bool SetCache(ReadOnlySpan<char> predicate, ReadOnlySpan<char> roomName, bool value) {
            RoomNameMatchCache[(predicate.ToString(), roomName.ToString())] = value;
            return value;
        }
    }

    private static readonly Dictionary<string, Regex> RoomNameMatchRegexCache = new();
    private static readonly Dictionary<(string, string), bool> RoomNameMatchCache = new(new StringPairComparer());
    private static readonly Dictionary<(string, string), bool>.AlternateLookup<SpanPair> RoomNameMatchCacheAltLookup = RoomNameMatchCache.GetAlternateLookup<SpanPair>();

    public static Style FromName(string name) {
        return FromElement(new(name) {
            Children = [],
            Attributes = new()
        });
    }

    public static Style FromPlacement(Placement pl) {
        var data = EntityRegistry.GetDataFromPlacement(pl);

        return FromElement(new() {
            Attributes = data,
            Name = pl.Sid
        });
    }

    public static Style FromElement(BinaryPacker.Element from) {
        if (from.Name is null)
            throw new Exception("Style with null name???");

        if (EntityRegistry.GetTypeForSid(from.Name, RegisteredEntityType.Style) is { } t && t.IsSubclassOf(typeof(Style))) {
            var style = (Style) Activator.CreateInstance(t)!;
            style.Unpack(from);

            return style;
        }

        Logger.Write("Stylegrounds", LogLevel.Warning, $"Unknown style type: {from.Name}");

        var unk = new UnknownStyle();
        unk.Unpack(from);

        return unk;
    }

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

    private bool? _isMasked;
    
    /// <summary>
    /// Checks whether this styleground is masked by some styleground masks.
    /// </summary>
    /// <returns></returns>
    internal bool IsMasked() {
        if (_isMasked is { } cached)
            return cached;
        
        foreach (var tag in Tags) {
            if (tag is null)
                continue;
            
            if (tag.StartsWith("mask_", StringComparison.Ordinal) || tag.StartsWith("sjstylemask_", StringComparison.Ordinal)) {
                _isMasked = true;
                return true;
            }
        }

        _isMasked = false;
        return false;
    }

    #region IBindTarget

    FieldList IBindTarget.GetFields() {
        var defaults = GetDefaultFields();
        var fields = EntityRegistry.GetFields(Name, RegisteredEntityType.Style);

        foreach (var (k, v) in defaults) {
            fields[k] = v;
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
        switch (key) {
            case "_type":
                lua.PushString(this switch {
                    Apply => "apply",
                    Parallax => "parallax",
                    _ => "effect",
                });
                return 1;
            case "_name":
                if (this is not Parallax and not Apply)
                    lua.PushString(Name);
                else
                    lua.PushNil();
                return 1;
            case "children" when this is Apply apply:
                lua.PushWrapper(new WrapperListWrapper<Style>(apply.Styles));
                return 1;
            default:
                if (Data.TryGetValue(key.ToString(), out var value)) {
                    lua.Push(value);
                } else {
                    lua.PushNil();
                }
                return 1;
        }
    }
    #endregion

    bool IUntypedData.TryGetValue(string key, [NotNullWhen(true)] out object? value) {
        if (GetDataContaining(key) is not { } data) {
            value = null;
            return false;
        }

        return data.TryGetValue(key, out value);
    }
}
