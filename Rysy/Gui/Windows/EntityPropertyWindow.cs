using Rysy.Extensions;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

public class EntityPropertyWindow : FormWindow {
    private static readonly HashSet<string> BlacklistedKeys = new() { "x", "y", "id", "originX", "originY", "width", "height", "_editorLayer", "_editorColor" };

    public readonly Entity Main;
    public readonly List<Entity> All;

    private HistoryHandler History;
    private Action HistoryHook;

    public static (FieldList, Func<string, bool> exists) GetFields(Entity main) {
        var fieldInfo = EntityRegistry.GetFields(main);

        var fields = new FieldList();
        var order = new List<string>();

        var minSize = main.MinimumSize;
        if (main.Width != 0) {
            fields["width"] = Fields.Int(main.Width).WithStep(8).WithMin(minSize.X);
            order.Add("width");
        }

        if (main.Height != 0) {
            fields["height"] = Fields.Int(main.Height).WithStep(8).WithMin(minSize.Y);
            order.Add("height");
        }

        fields["_editorLayer"] = Fields.Int(main.EditorLayer);
        order.Add("_editorLayer");

        if (main is Trigger tr) {
            fields["_editorColor"] = Fields.RGBA(tr.EditorColor.ToColor(ColorFormat.RGBA));
            order.Add("_editorColor");
        }

        fields["__padding"] = new PaddingField();
        order.Add("__padding");

        foreach (var (k, f) in fieldInfo.OrderedEnumerable(main)) {
            if (!BlacklistedKeys.Contains(k)) {
                fields[k] = f.CreateClone();
                order.Add(k);
            }
        }

        // Take into account properties defined on this entity, even if not present in FieldInfo
        foreach (var (k, v) in main.EntityData.Inner) {
            if (!BlacklistedKeys.Contains(k)) {
                if (fields.TryGetValue(k, out var knownFieldType)) {
                    fields[k].SetDefault(v);
                } else {
                    fields[k] = Fields.GuessFromValue(v, fromMapData: true)!;
                    order.Add(k);
                }
            }
        }

        var startPrefix = main is Trigger ? "triggers" : "entities";

        var tooltipKeyPrefix = $"{startPrefix}.{main.Name}.attributes.description";
        var nameKeyPrefix = $"{startPrefix}.{main.Name}.attributes.name";
        var defaultTooltipKeyPrefix = $"entities.default.attributes.description";
        var defaultNameKeyPrefix = $"entities.default.attributes.name";

        foreach (var (name, f) in fields) {
            f.Tooltip ??= name.TranslateOrNull(tooltipKeyPrefix) ?? name.TranslateOrNull(defaultTooltipKeyPrefix);
            f.NameOverride ??= name.TranslateOrNull(nameKeyPrefix) ?? name.TranslateOrNull(defaultNameKeyPrefix);
        }

        return (fields.Ordered(order), main.EntityData.Has);
    }

    public EntityPropertyWindow(HistoryHandler history, Entity main, List<Entity> all) : base($"Edit: {main.EntityData.SID}:{string.Join(',', all.Select(e => e.ID))}") {
        Main = main;
        All = all;
        History = history;

        var (fields, exists) = GetFields(main);
        Init(fields, exists);

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
            var exists = Main.EntityData.TryGetValue(name, out var current);
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
            if (!(current?.Equals(propValue) ?? current == propValue)) {

                EditedValues[name] = propValue;
                //Console.WriteLine((current ?? "NULL", propValue ?? "NULL"));
            }
        }
    }
}
