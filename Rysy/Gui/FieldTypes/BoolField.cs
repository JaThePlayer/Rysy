using ImGuiNET;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record class BoolField : Field, IFieldConvertible<bool>, ILonnField {
    public bool Default { get; set; }

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault)
        => Default = ConvertMapDataValue(newDefault);

    public override ValidationResult IsValid(object? value) 
        => value is bool ? base.IsValid(value) : ValidationResult.MustBeBool;

    public override object? RenderGui(string fieldName, object value) {
        bool b = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        if (ImGui.Checkbox(fieldName, ref b).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public override Field CreateClone() => this with { };

    public bool ConvertMapDataValue(object value) => Convert.ToBoolean(value, CultureInfo.InvariantCulture);

    public static string Name => "boolean";

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        bool defB = false;
        try {
            defB = Convert.ToBoolean(def, CultureInfo.InvariantCulture);
        } catch {
            
        }
        
        return new BoolField {
            Default = Convert.ToBoolean(defB)
        };
    }
}
