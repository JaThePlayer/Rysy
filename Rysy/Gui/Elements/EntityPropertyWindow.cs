using ImGuiNET;

namespace Rysy.Gui.Elements;

public record class EntityPropertyWindow : Window {
    public readonly Entity Main;

    //private FieldList FieldList;

    private Dictionary<string, Prop> FieldList;

    private record class Prop() {
        public IField Field;
        public object Value;
    }

    public EntityPropertyWindow(Entity main) : base($"Edit Entity - {main.EntityData.Name}:{main.ID}", null) {
        Render = DoRender;
        Main = main;

        var fields = EntityRegistry.SIDToFields.GetValueOrDefault(main.EntityData.Name) ?? new();
        FieldList = new() {
            ["x"] = new() {
                Field = Fields.Int(0),
                Value = (int) main.Pos.X,
            },
            ["y"] = new() {
                Field = Fields.Int(0),
                Value = (int) main.Pos.Y,
            },
        };

        foreach (var (k, f) in fields) {
            FieldList[k] = new() {
                Field = f,
                Value = f.GetDefault()
            };
        }

        // Take into account properties defined on this entity, even if not present in FieldInfo
        foreach (var (k, v) in Main.EntityData.Inner) {
            if (fields.TryGetValue(k, out var knownFieldType)) {
                FieldList[k].Value = v;
            } else {
                FieldList[k] = new() {
                    Field = Fields.GuessFromValue(v)!,
                    Value = v,
                };
            }
        }

        Size = new(500, ImGui.GetTextLineHeightWithSpacing() * FieldList.Count + ImGui.GetFrameHeightWithSpacing() * 2);
    }

    private void DoRender(Window w) {
        ImGui.Columns(2);

        foreach (var (name, prop) in FieldList) {
            HandleProp(name, prop);

            ImGui.NextColumn();
        }

        ImGui.Columns();
    }

    private void HandleProp(string name, Prop prop) {
        var val = prop.Value;

        var newVal = prop.Field.RenderGui(name, val);

        if (newVal != null) {
            Console.WriteLine($"NEW: {newVal}");
        }
    }
}
