using ImGuiNET;
using Rysy.Extensions;
using Rysy.Gui.FieldTypes;

namespace Rysy.Gui.Windows;

public class FormWindow : Window {
    public delegate void FormWindowChanged(Dictionary<string, object> edited);

    internal List<Prop> FieldList = new();
    public Dictionary<string, object> EditedValues = new();
    internal const int ITEM_WIDTH = 175;

    public FormWindowChanged OnChanged { get; set; }
    public FormWindowChanged OnLiveUpdate { get; set; }

    internal sealed record class Prop(string Name) {
        public Field Field;

        /// <summary>
        /// The value of this property, which is only set once the user changes the value.
        /// </summary>
        public object? Value;

        public object ValueOrDefault() => Value ?? Field.GetDefault();
    }

    protected override ImGuiWindowFlags EditWindowFlags(ImGuiWindowFlags prev) {
        if (EditedValues.Count > 0)
            prev |= ImGuiWindowFlags.UnsavedDocument;

        return base.EditWindowFlags(prev);
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
        return FieldList.Where(f => f.Field is not PaddingField).ToDictionary(p => p.Name, p => p.ValueOrDefault());
    }

    public FormWindow(string name) : base(name, null) {
    }

    public FormWindow(FieldList fields, string name, Func<string, bool>? exists = null) : base(name, null) {
        Init(fields, exists);
    }

    public void Init(FieldList fields, Func<string, bool>? exists = null) {
        Exists = exists ?? ((_) => true);
        
        FormContext ctx = new(this);

        foreach (var f in fields.OrderedEnumerable(null!)) {
            var fieldName = f.Key;
            var field = f.Value;
            field.Context = ctx;

            FieldList.Add(new(fieldName) {
                Field = field,
                //Value = field.GetDefault()
            });
        }

        LongestFieldSize = fields.Count > 0 ? FieldList.Select(p => ImGui.CalcTextSize(p.Field.NameOverride ?? p.Name).X).Chunk(2).Max(pair => pair.Sum()) : 50;
        Size = new(
            LongestFieldSize + ITEM_WIDTH * 2.5f,
            ImGui.GetFrameHeightWithSpacing() * (FieldList.Count / 2 + 2) + ImGui.GetFrameHeightWithSpacing() * 2 + ImGui.GetStyle().WindowPadding.Y
        );

        Resizable = true;
    }

    public void ReevaluateChanged(Dictionary<string, object> newDefaults) {
        EditedValues.Clear();

        foreach (var prop in FieldList) {
            var name = prop.Name;
            var exists = newDefaults.TryGetValue(name, out var current);
            var propValue = exists ? prop.ValueOrDefault() : prop.Value;

            /*
            if (inMain && (name is "x" or "y" ? Convert.ToInt32(current) != Convert.ToInt32(prop.Value) :
                current switch {
                    string currStr => currStr != (string?) prop.Value,
                    null => prop.Value != current,
                    _ => !current.Equals(prop.Value)
                }
            )) {
                EditedValues[name] = prop.Value;
            }*/
            if (!(current?.Equals(propValue) ?? current == propValue)
                && (current, propValue) switch {
                    (float f, int i) => f != i,
                    (int i, float f) => f != i,
                    _ => true,
                }) {

                EditedValues[name] = propValue;
                //Console.WriteLine((current ?? "NULL", propValue ?? "NULL"));
            }
        }
    }

    private bool AllFieldsValid;

    protected override void Render() {
        var hasColumns = FieldList.Count > 1 && ImGui.GetWindowSize().X >= (LongestFieldSize + ITEM_WIDTH * 2.3f);

        if (hasColumns)
            ImGui.Columns(2);

        bool valid = true;

        foreach (var prop in FieldList) {
            if (prop.Field is PaddingField pad) {
                if (pad.Text is { } text) {
                    if (hasColumns) {
                        ImGui.Columns();
                    }
                    ImGui.SeparatorText(text);
                    if (hasColumns) {
                        ImGui.Columns(2);
                    }
                } else {
                    if (ImGui.GetColumnIndex() != 0)
                        ImGui.NextColumn();

                    ImGui.Separator();
                }

                continue;
            }

            if (!HandleProp(prop)) {
                valid = false;
            }

            if (hasColumns)
                ImGui.NextColumn();
        }

        if (hasColumns)
            ImGui.Columns();

        AllFieldsValid = valid;
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        ImGui.BeginDisabled(!AllFieldsValid);

        if (ImGui.Button(SaveChangesButtonName)) {
            OnChanged?.Invoke(EditedValues);

            EditedValues = new();
        }

        ImGui.EndDisabled();
    }

    public void RenderBody() {
        ImGuiManager.WithBottomBar(
            renderMain: Render,
            renderBottomBar: RenderBottomBar,
            id: (uint)GetHashCode()
        );
    }

    private bool HandleProp(Prop prop) {
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
        var val = newVal is FieldNullReturn ? null : newVal;

        EditedValues[prop.Name] = val;
        prop.Value = val;

        OnLiveUpdate?.Invoke(EditedValues);
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