using ImGuiNET;
using Rysy.History;

namespace Rysy.Gui.Elements;

public record class EntityPropertyWindow : Window {
    public readonly Entity Main;
    public readonly List<Entity> All;

    private HistoryHandler History;

    private Dictionary<string, Prop> FieldList;
    private Dictionary<string, object> EditedValues = new();

    private record class Prop() {
        public IField Field;
        public object Value;
    }

    private Action HistoryHook;

    public EntityPropertyWindow(HistoryHandler history, Entity main, List<Entity> all) : base($"Edit Entity - {main.EntityData.SID}:{string.Join(',', all.Select(e => e.ID))}", null) {
        Render = DoRender;
        Main = main;
        All = all;
        History = history;

        HashSet<string> blacklistedKeys = new() { "x", "y", "id", "originX", "originY" };

        HistoryHook = ReevaluateEditedValues;
        history.OnApply += HistoryHook;
        history.OnUndo += HistoryHook;
        SetRemoveAction((w) => {
            History.OnApply -= HistoryHook;
            History.OnUndo -= HistoryHook;
        });

        var fields = EntityRegistry.SIDToFields.GetValueOrDefault(main.EntityData.SID) ?? new();
        FieldList = new() {
            ["x"] = new() {
                Field = Fields.Int(0),
                Value = main.X,
            },
            ["y"] = new() {
                Field = Fields.Int(0),
                Value = main.Y,
            },
        };

        foreach (var (k, f) in fields) {
            if (!blacklistedKeys.Contains(k))
                FieldList[k] = new() {
                    Field = f,
                    Value = f.GetDefault()
                };
        }

        // Take into account properties defined on this entity, even if not present in FieldInfo
        foreach (var (k, v) in Main.EntityData.Inner) {
            if (!blacklistedKeys.Contains(k)) {
                if (fields.TryGetValue(k, out var knownFieldType)) {
                    FieldList[k].Value = v;
                } else {
                    FieldList[k] = new() {
                        Field = Fields.GuessFromValue(v)!,
                        Value = v,
                    };
                }
            }
        }

        Size = new(500, ImGui.GetTextLineHeightWithSpacing() * FieldList.Count + ImGui.GetFrameHeightWithSpacing() * 2);
    }

    private void ReevaluateEditedValues() {
        EditedValues.Clear();

        foreach (var (name, prop) in FieldList) {
            var inMain = Main.EntityData.TryGetValue(name, out var current);
            if (inMain && (name is "x" or "y" ? Convert.ToInt32(current) != Convert.ToInt32(prop.Value) :
                current switch {
                    string currStr => currStr != (string)prop.Value,
                    _ => !current!.Equals(prop.Value)
                }
            )) {
                EditedValues[name] = prop.Value;
            }
        }
    }

    private void DoRender(Window w) {
        ImGui.Columns(2);

        foreach (var (name, prop) in FieldList) {
            HandleProp(name, prop);

            ImGui.NextColumn();
        }

        ImGui.Columns();

        ImGuiManager.BeginWindowBottomBar(true);
        if (ImGui.Button("Save Changes")) {
            History.ApplyNewAction(new EntityEditAction(All, EditedValues));
            ReevaluateEditedValues();
        }
        ImGuiManager.EndWindowBottomBar();
    }

    private void HandleProp(string name, Prop prop) {
        var val = prop.Value;

        // determine color
        var valid = prop.Field.IsValid(val);
        if (!valid)
            ImGuiManager.PushInvalidStyle();
        else if (EditedValues.ContainsKey(name))
            ImGuiManager.PushEditedStyle();

        var newVal = prop.Field.RenderGui(name, val);

        ImGuiManager.PopInvalidStyle();
        ImGuiManager.PopEditedStyle();

        if (newVal != null) {
            EditedValues[name] = newVal;
            prop.Value = newVal;
        }
    }
}
