using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class CharField : IField {
    public char Default { get; set; }

    public bool IsValid(object value) => value is char;

    public object GetDefault() => Default;

    public object? RenderGui(string fieldName, object value) {
        var b = Convert.ToChar(value).ToString();
        if (ImGui.InputText(fieldName, ref b, 1))
            return b[0];

        return null;
    }
}