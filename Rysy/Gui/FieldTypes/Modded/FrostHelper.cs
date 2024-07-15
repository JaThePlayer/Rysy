using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes.Modded;

internal sealed record FrostHelperStylegroundTag : StylegroundTagField, ILonnField {
    public static string Name => "FrostHelper.stylegroundTag";
    
    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        return new FrostHelperStylegroundTag(def?.ToString() ?? "");
    }

    public FrostHelperStylegroundTag(string def) : base(def, false)
    {
    }
}

internal sealed record FrostHelperEasing : DropdownField<string>, ILonnField {
    private static readonly Dictionary<string, bool> IsValidCache = CelesteEnums.Easings.ToDictionary(x => x, _ => true);
    
    public static string Name => "FrostHelper.easing";
    
    public static Field Create(object? def, IUntypedData fieldInfoEntry) => new FrostHelperEasing {
        Default = def?.ToString() ?? "",
        Editable = true,
        Values = _ => CelesteEnums.Easings.ToDictionary(x => x, x => x)
    };

    public override bool IsValid(object? value) {
        if (value is not string str)
            return base.IsValid(value);

        if (IsValidCache.TryGetValue(str, out var cached))
            return cached;

        var code = $"return function(p) {(str.Contains("return", StringComparison.Ordinal) ? "" : "return")} {str} end";

        return IsValidCache[str] = LuaSupport.LuaSerializer.IsValidLua(code);
    }
}

internal sealed record FrostHelperCloudTag : DropdownField<string>, ILonnField {
    public static string Name => "FrostHelper.cloudTag";

    public FrostHelperCloudTag(string def) {
        Default = def ?? "";
        Editable = true;
        Values = _ => EditorState.CurrentRoom is { } room
            ? room.Entities.Concat(room.Triggers)
                .SelectWhereNotNull(e => 
                    e.Attr("cloudTag", null!)
                    ?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .SelectMany(x => x)
                .SafeToDictionary(e => (e, e))
            : [];
    }
    
    public static Field Create(object? def, IUntypedData fieldInfoEntry) => new FrostHelperCloudTag(def?.ToString() ?? "");
}

