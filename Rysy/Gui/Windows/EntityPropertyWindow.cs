using ImGuiNET;
using Markdig;
using Rysy.Extensions;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

public class EntityPropertyWindow : FormWindow {
    private static readonly HashSet<string> BlacklistedKeys = new() { "x", "y", "id", "originX", "originY", "width", "height", "_editorColor" };

    public Entity Main { get; }
    public List<Entity> All { get; }

    private HistoryHandler History;
    private Action HistoryHook;

    public static (FieldList, Func<string, bool> exists) GetFields(Entity main) {
        ArgumentNullException.ThrowIfNull(main);

        var fieldInfo = EntityRegistry.GetFields(main);

        var fields = new FieldList();
        fields.SetHiddenFields(fieldInfo.GetDynamicallyHiddenFields);
        
        var order = new List<string>();

        var minSize = main.MinimumSize;
        var maxSize = main.MaximumSize;
        
        if (main.Width != 0) {
            fields["width"] = Fields.Int(main.Width).WithStep(8).WithMin(minSize.X).WithMax(maxSize.X);
            order.Add("width");
        }

        if (main.Height != 0) {
            fields["height"] = Fields.Int(main.Height).WithStep(8).WithMin(minSize.Y).WithMax(maxSize.Y);
            order.Add("height");
        }

        fields[Entity.EditorGroupEntityDataKey] = Fields.EditorGroup(main.Room.Map.EditorGroups, main.EditorGroups);
        order.Add(Entity.EditorGroupEntityDataKey);

        if (main is Trigger tr) {
            fields["_editorColor"] = Fields.RGBA(tr.EditorColor.ToColor(ColorFormat.RGBA));
            order.Add("_editorColor");
        }

        fields["__padding"] = new PaddingField();
        order.Add("__padding");

        // Make sure fields present in fieldOrder but not in fieldInformation gets ordered.
        foreach (var orderKey in fieldInfo.Order?.Invoke(main) ?? []) {
            order.Add(orderKey);
        }

        foreach (var (k, f) in fieldInfo.OrderedEnumerable(main)) {
            if (!IsValidKey(k))
                continue;
            
            fields[k] = f.CreateClone();
        }

        // Take into account properties defined on this entity, even if not present in FieldInfo
        foreach (var (k, v) in main.EntityData.Inner) {
            if (!IsValidKey(k))
                continue;
            
            if (fields.TryGetValue(k, out var knownFieldType)) {
                fields[k].SetDefault(v);
            } else {
                fields[k] = Fields.GuessFromValue(v, fromMapData: true)!;
                order.Add(k);
            }
        }

        var startPrefix = main is Trigger ? "triggers" : "entities";

        var tooltipKeyPrefix = $"{startPrefix}.{main.Name}.attributes.description";
        var nameKeyPrefix = $"{startPrefix}.{main.Name}.attributes.name";
        var defaultTooltipKeyPrefix = $"entities.default.attributes.description";
        var defaultNameKeyPrefix = $"entities.default.attributes.name";

        fields.AddTranslations(tooltipKeyPrefix, nameKeyPrefix, defaultTooltipKeyPrefix, defaultNameKeyPrefix);

        return (fields.Ordered(order), main.EntityData.Has);

        bool IsValidKey(string key) => !BlacklistedKeys.Contains(key);
    }

    public EntityPropertyWindow(HistoryHandler history, Entity main, List<Entity> all) 
        : base($"Edit: {main.EntityData.SID}:{string.Join(',', all.Select(e => e.Id))}") {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(all);

        Main = main;
        All = all;
        History = history;

        var (fields, exists) = GetFields(main);
        Init(fields, exists);

        OnChanged = (edited) => {
            History.ApplyNewAction(new EntityEditAction(All, edited));
        };
        OnLiveUpdate = (edited) => {
            foreach (var e in All) {
                e.EntityData.SetOverlay(edited);
            }
        };
        
        HistoryHook = ReevaluateEditedValues;
        history.OnApply += HistoryHook;
        history.OnUndo += HistoryHook;
        SetRemoveAction((w) => {
            History.OnApply -= HistoryHook;
            History.OnUndo -= HistoryHook;
        });
    }

    public override void RenderBottomBar() {
        base.RenderBottomBar();

        //Console.WriteLine(DocumentationString);
        if (Main.Documentation is { } docString) {
            ImGui.SameLine();
            if (ImGuiManager.TranslatedButton("rysy.entityEdit.documentation")) {
                OpenDocs(docString);
            }
        }
    }

    private void OpenDocs(string docString) {
        if (LinkOpenHelper.OpenLinkIfValid(docString)) {
            return;
        }

        try {
            var windowTitle = "rysy.entityEdit.documentation.mdViewWindowName".TranslateFormatted(Main.Name);
            RysyEngine.Scene.AddWindow(new MarkdownViewWindow(windowTitle, docString));
        } catch {
            
        }
    }

    public override void RemoveSelf() {
        base.RemoveSelf();

        foreach (var e in All) {
            e.EntityData.SetOverlay(null);
        }
    }

    private void ReevaluateEditedValues() {
        EditedValues.Clear();

        foreach (var prop in FieldList) {
            var name = prop.Name;
            var exists = Main.EntityData.TryGetValue(name, out var current);
            var propValue = exists ? prop.ValueOrDefault() : prop.Value;
            
            var equal = (current, propValue) switch {
                (int c, float val) => val == c,
                (float c, int val) => val == c,
                _ => (current?.Equals(propValue) ?? current == propValue)
            };

            if (!equal) {
                EditedValues[name] = propValue;
                Console.WriteLine((current ?? "NULL", propValue ?? "NULL"));
            }
        }
        
        UpdateDynamicallyHiddenFields();
    }
}
