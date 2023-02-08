using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class IntField : IField {
    public int Default { get; set; }

    public object GetDefault() => Default;

    public object? RenderGui(string fieldName, object value) {
        int b = Convert.ToInt32(value);
        if (ImGui.InputInt(fieldName, ref b))
            return b;

        return null;
    }
}