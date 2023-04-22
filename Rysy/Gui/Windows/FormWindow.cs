using ImGuiNET;
using Rysy.Extensions;
using Rysy.History;

namespace Rysy.Gui.Windows;

public class FormWindow : Window {
    public delegate void FormWindowChanged(Dictionary<string, object> edited);

    protected List<Prop> FieldList;
    public Dictionary<string, object> EditedValues = new();
    private int ITEM_WIDTH = 150;

    public FormWindowChanged OnChanged { get; set; }

    protected record class Prop(string Name) {
        public IField Field;
        public object Value;
    }

    public string SaveChangesButtonName = "Save Changes";

    /// <summary>
    /// Creates a dictionary containing all values in all fields, regardless of whether they've been edited or not.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, object> GetAllValues() {
        return FieldList.ToDictionary(p => p.Name, p => p.Value);
    }

    public FormWindow(FieldList fields, string name) : base(name, null) {
        FieldList = new();

        foreach (var f in fields) {
            var fieldName = f.Key;
            var field = f.Value;

            FieldList.Add(new(fieldName) { 
                Field = field,
                Value = field.GetDefault()
            });
        }

        Size = new(
            FieldList.Select(p => p.Name.Length).Chunk(2).Max(pair => pair.Sum() + 2) * ImGui.GetFontSize() + ITEM_WIDTH * 2f,
            ImGui.GetFrameHeightWithSpacing() * (FieldList.Count / 2 + 2) + ImGui.GetFrameHeightWithSpacing() * 2
        );

        Resizable = true;
    }

    protected override void Render() {
        var hasColumns = FieldList.Count > 1;

        if (hasColumns)
            ImGui.Columns(2);

        bool valid = true;

        foreach (var prop in FieldList) {
            if (!HandleProp(prop)) {
                valid = false;
            }

            if (hasColumns)
                ImGui.NextColumn();
        }

        if (hasColumns)
            ImGui.Columns();

        ImGuiManager.BeginWindowBottomBar(valid);
        if (ImGui.Button(SaveChangesButtonName)) {
            OnChanged?.Invoke(EditedValues);

            EditedValues = new();
        }
        ImGuiManager.EndWindowBottomBar();
    }

    private bool HandleProp(Prop prop) {
        var name = prop.Name;
        var val = prop.Value;

        // determine color
        var valid = prop.Field.IsValid(val);
        if (!valid)
            ImGuiManager.PushInvalidStyle();
        else if (EditedValues.ContainsKey(name))
            ImGuiManager.PushEditedStyle();

        ImGui.SetNextItemWidth(ITEM_WIDTH);
        var newVal = prop.Field.RenderGui(name.Humanize(), val);

        ImGuiManager.PopInvalidStyle();
        ImGuiManager.PopEditedStyle();

        if (newVal != null) {
            EditedValues[name] = newVal;
            prop.Value = newVal;
        }

        return valid;
    }
}
