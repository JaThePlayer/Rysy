using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes.Modded;

internal sealed record FrostHelperStylegroundTag : StylegroundTagField, ILonnField {
    public static string Name => "FrostHelper.stylegroundTag";
    
    public static Field Create(object? def, Dictionary<string, object> fieldInfoEntry) {
        return new FrostHelperStylegroundTag(def?.ToString() ?? "");
    }

    public FrostHelperStylegroundTag(string def) : base(def, false)
    {
    }
}

internal sealed record FrostHelperEasing : DropdownField<string>, ILonnField {
    private static readonly Dictionary<string, bool> IsValidCache = CelesteEnums.Easings.ToDictionary(x => x, _ => true);
    
    public static string Name => "FrostHelper.easing";
    
    public static Field Create(object? def, Dictionary<string, object> fieldInfoEntry) => new FrostHelperEasing {
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
