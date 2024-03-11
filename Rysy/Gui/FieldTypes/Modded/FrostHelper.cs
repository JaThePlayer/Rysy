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