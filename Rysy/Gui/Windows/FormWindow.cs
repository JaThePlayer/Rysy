using ImGuiNET;
using Rysy.Extensions;
using Rysy.History;
using System.Reflection;

namespace Rysy.Gui.Windows;

public class FormWindow : Window {
    public delegate void FormWindowChanged(Dictionary<string, object> edited);

    internal List<Prop> FieldList = new();
    public Dictionary<string, object> EditedValues = new();
    private int ITEM_WIDTH = 175;

    public FormWindowChanged OnChanged { get; set; }

    internal record class Prop(string Name) {
        public Field Field;

        /// <summary>
        /// The value of this property, which is only set once the user changes the value.
        /// </summary>
        public object? Value;

        public object ValueOrDefault() => Value ?? Field.GetDefault();
    }

    public string SaveChangesButtonName = "Save Changes";

    public Func<string, bool> Exists;


    // used for deciding whether the form should be displayed in columns or not.
    float LongestFieldSize;

    /// <summary>
    /// Creates a dictionary containing all values in all fields, regardless of whether they've been edited or not.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, object> GetAllValues() {
        return FieldList.ToDictionary(p => p.Name, p => p.ValueOrDefault());
    }

    public FormWindow(string name) : base(name, null) {
    }

    public FormWindow(FieldList fields, string name, Func<string, bool>? exists = null) : base(name, null) {
        Init(fields, exists);
    }

    public void Init(FieldList fields, Func<string, bool>? exists = null) {
        Exists = exists ?? ((_) => true);

        foreach (var f in fields) {
            var fieldName = f.Key;
            var field = f.Value;

            FieldList.Add(new(fieldName) {
                Field = field,
                //Value = field.GetDefault()
            });
        }

        LongestFieldSize = FieldList.Select(p => ImGui.CalcTextSize(p.Field.NameOverride ?? p.Name).X).Chunk(2).Max(pair => pair.Sum());
        Size = new(
            LongestFieldSize + ITEM_WIDTH * 2.5f,
            ImGui.GetFrameHeightWithSpacing() * (FieldList.Count / 2 + 2) + ImGui.GetFrameHeightWithSpacing() * 2
        );

        Resizable = true;
    }

    protected override void Render() {
        var hasColumns = FieldList.Count > 1 && ImGui.GetWindowSize().X >= (LongestFieldSize + ITEM_WIDTH * 2.3f);

        if (hasColumns)
            ImGui.Columns(2);

        bool valid = true;

        FormContext ctx = new(this);

        foreach (var prop in FieldList) {
            if (!HandleProp(prop, ctx)) {
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

    public void RenderBody() => Render();

    private bool HandleProp(Prop prop, FormContext ctx) {
        var name = prop.Name;
        var val = prop.ValueOrDefault();

        // determine color
        var valid = prop.Field.IsValid(val);
        if (!valid)
            ImGuiManager.PushInvalidStyle();
        else if (EditedValues.ContainsKey(name))
            ImGuiManager.PushEditedStyle();
        else if (!Exists(prop.Name))
            ImGuiManager.PushNullStyle();

        object? newVal = null;
        ImGui.SetNextItemWidth(ITEM_WIDTH);
        try {
            var field = prop.Field;
            field.Context = ctx;
            newVal = field.RenderGui(field.NameOverride ??= name.Humanize(), val);
        } catch (Exception) {
            throw;
        } finally {
            ImGuiManager.PopInvalidStyle();
            ImGuiManager.PopEditedStyle();
            ImGuiManager.PopNullStyle();
        }

        if (newVal != null) {
            Set(prop, newVal);
        }

        return valid;
    }

    internal void Set(Prop prop, object newVal) {
        EditedValues[prop.Name] = newVal;
        prop.Value = newVal;
    }
}

public class FormContext {
    private FormWindow Window;

    public FormContext(FormWindow window) => Window = window;

    /// <summary>
    /// Gets the value of the field of name <paramref name="fieldName"/>, or null if the field does not exist
    /// </summary>
    public object? GetValue(string fieldName)
        => Window.FieldList.FirstOrDefault(f => f.Name == fieldName)?.ValueOrDefault();

    /// <summary>
    /// Sets the value of the field of name <paramref name="fieldName"/>
    /// </summary>
    public void SetValue(string fieldName, object newValue) {
        if (Window.FieldList.FirstOrDefault(f => f.Name == fieldName) is { } prop)
            Window.Set(prop, newValue);
    }
}