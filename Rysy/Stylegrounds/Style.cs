using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy.Stylegrounds;

public abstract class Style : IPackable, IName {
    public static FieldList GetDefaultFields() => new FieldList(new {
        only = Fields.String("*").AllowNull().ConvertEmptyToNull(),
        exclude = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        flag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        notflag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        tag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        _indent = new PaddingField()
    });

    public string Name { get; set; }

    public EntityData Data { get; set; }

    /// <summary>
    /// Gets set by the styleground window for safely allowing for live-preview of changed values.
    /// TODO: refactor to make this apply to entities as well without many code changes...
    /// </summary>
    internal EntityData? FakePreviewData { get; set; }

    protected BinaryPacker.Element[]? UnhandledChildren;

    internal EntityData FakeOrRealData() => FakePreviewData ?? Data;

    [JsonIgnore]
    public string? Only => FakeOrRealData().Attr("only", null!);

    [JsonIgnore]
    public string? Exclude => FakeOrRealData().Attr("exclude", null!);

    [JsonIgnore]
    public string? Flag => FakeOrRealData().Attr("flag", null!);

    [JsonIgnore]
    public string? NotFlag => FakeOrRealData().Attr("notflag", null!);

    [JsonIgnore]
    public StyleFolder? Parent { get; internal set; } = null;

    [JsonIgnore]
    public virtual string DisplayName => $"style.effects.{Name}.name".TranslateOrNull() ?? Name[(Name.LastIndexOf('/') + 1)..].Humanize();

    [JsonIgnore]
    public virtual bool CanBeInBackground => true;

    [JsonIgnore]
    public virtual bool CanBeInForeground => true;

    [JsonIgnore]
    public virtual List<string>? AssociatedMods => null;

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

    public static bool MatchRoomName(string predicate, string roomName) {
        // currently copied from celeste,
        // TODO: optimise with caching and spans
        if (roomName.StartsWith("lvl_", StringComparison.Ordinal))
            roomName = roomName[4..];

        string[] array = predicate.Split(',');
        foreach (string text in array)
            if (text.Equals(roomName))
                return true;
            else if (text.Contains("*")) {
                string pattern = "^" + Regex.Escape(text).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(roomName, pattern))
                    return true;
            }

        return false;
    }

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
}
