using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

public class EntityPropertyWindow : FormWindow {
    private static readonly HashSet<string> BlacklistedKeys = new() { "x", "y", "id", "originX", "originY", "width", "height", "_editorLayer", "_editorColor" };

    public readonly Entity Main;
    public readonly List<Entity> All;

    private HistoryHandler History;
    private Action HistoryHook;

    public static FieldList GetFields(Entity main) {
        var fieldInfo = EntityRegistry.SIDToFields.GetValueOrDefault(main.Name) ?? new();

        var fields = new FieldList();

        //if (false && all.Count == 1) {
        //    fields["x"] = Fields.Int(main.X);
        //    fields["y"] = Fields.Int(main.Y);
        //}

        var minSize = main.MinimumSize;
        if (main.Width != 0) {
            fields["width"] = Fields.Int(main.Width).WithStep(8).WithMin(minSize.X);
        }

        if (main.Height != 0) {
            fields["height"] = Fields.Int(main.Height).WithStep(8).WithMin(minSize.Y);
        }

        fields["_editorLayer"] = Fields.Int(main.EditorLayer);

        if (main is Trigger tr) {
            fields["_editorColor"] = Fields.RGBA(tr.EditorColor.ToColor(ColorFormat.RGBA));
        }

        foreach (var (k, f) in fieldInfo) {
            if (!BlacklistedKeys.Contains(k))
                fields[k] = f.CreateClone();
        }

        // Take into account properties defined on this entity, even if not present in FieldInfo
        foreach (var (k, v) in main.EntityData.Inner) {
            if (!BlacklistedKeys.Contains(k)) {
                if (fields.TryGetValue(k, out var knownFieldType)) {
                    fields[k].SetDefault(v);
                } else {
                    fields[k] = Fields.GuessFromValue(v)!;
                }
            }
        }

        return fields;
    }

    public EntityPropertyWindow(HistoryHandler history, Entity main, List<Entity> all) : base(GetFields(main), $"Edit Entity - {main.EntityData.SID}:{string.Join(',', all.Select(e => e.ID))}") {
        Main = main;
        All = all;
        History = history;

        OnChanged = (edited) => {
            History.ApplyNewAction(new EntityEditAction(All, edited));
        };

        HistoryHook = ReevaluateEditedValues;
        history.OnApply += HistoryHook;
        history.OnUndo += HistoryHook;
        SetRemoveAction((w) => {
            History.OnApply -= HistoryHook;
            History.OnUndo -= HistoryHook;
        });
    }

    private void ReevaluateEditedValues() {
        EditedValues.Clear();

        foreach (var prop in FieldList) {
            var name = prop.Name;
            var inMain = Main.EntityData.TryGetValue(name, out var current);

            if (inMain && (name is "x" or "y" ? Convert.ToInt32(current) != Convert.ToInt32(prop.Value) :
                current switch {
                    string currStr => currStr != (string) prop.Value,
                    _ => !current!.Equals(prop.Value)
                }
            )) {
                EditedValues[name] = prop.Value;
            }
        }
    }
}
