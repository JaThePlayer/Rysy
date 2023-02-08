using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class BoolField : IField {
    public bool Default { get; set; }

    public object GetDefault() => Default;

    public object? RenderGui(string fieldName, object value) {
        bool b = Convert.ToBoolean(value);
        if (ImGui.Checkbox(fieldName, ref b))
            return b;

        return null;
    }
}
