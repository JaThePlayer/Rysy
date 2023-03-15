using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class StringField : IField {
    public string Default { get; set; }

    public object GetDefault() => Default;

    public bool IsValid(object value) => value is string;

    public object? RenderGui(string fieldName, object value) {
        var b = value.ToString();
        if (ImGui.InputText(fieldName, ref b, 256))
            return b;

        return null;
    }
}