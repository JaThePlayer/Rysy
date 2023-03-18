using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class FloatField : IField {
    public float Default { get; set; }

    public object GetDefault() => Default;

    public bool IsValid(object value) => value is float or int;

    public object? RenderGui(string fieldName, object value) {
        float b = Convert.ToSingle(value);
        if (ImGui.InputFloat(fieldName, ref b))
            return b;

        return null;
    }
}