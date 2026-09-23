using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Signals;

namespace Rysy.Gui.Windows;

public class EntityPropertyWindow : FormWindow, ISignalListener<HistoryChanged> {
    private static readonly HashSet<string> BlacklistedKeys = new() { "x", "y", "id", "originX", "originY", "width", "height", "_editorColor" };

    public Entity Main { get; }
    public List<Entity> All { get; }

    public static (FieldList, Func<string, bool> exists) GetFields(Entity main) {
        ArgumentNullException.ThrowIfNull(main);

        var fieldInfo = EntityRegistry.GetFields(main);

        var fields = new FieldList();
        fields.SetHiddenFields(fieldInfo.GetDynamicallyHiddenFields);
        
        var order = new List<string>();

        var minSize = main.MinimumSize;
        var maxSize = main.MaximumSize;
        var minRecSize = main.RecommendedMinimumSize;
        var maxRecSize = main.RecommendedMaximumSize;
        
        if (Settings.Instance.PositionInProperties) {
            fields["x"] = Fields.Int(main.X);
            order.Add("x");

            fields["y"] = Fields.Int(main.Y);
            order.Add("y");
        }
        
        if (main.Width != 0) {
            fields["width"] = Fields.Int(main.Width)
                .WithRecommendedStep(8)
                .WithMin(minSize.X).WithMax(maxSize.X)
                .WithRecommendedMin(minRecSize.X).WithRecommendedMax(maxRecSize.X);
            order.Add("width");
        }

        if (main.Height != 0) {
            fields["height"] = Fields.Int(main.Height)
                .WithRecommendedStep(8)
                .WithMin(minSize.Y).WithMax(maxSize.Y)
                .WithRecommendedMin(minRecSize.Y).WithRecommendedMax(maxRecSize.Y);
            order.Add("height");
        }

        fields[Entity.EditorGroupEntityDataKey] = Fields.EditorGroup(main.Room.Map.EditorGroups, main.EditorGroups);
        order.Add(Entity.EditorGroupEntityDataKey);

        if (main is Trigger tr) {
            fields["_editorColor"] = Fields.Rgba(tr.Color).AllowNull().TreatEmptyAsNull();
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
                if (knownFieldType.IsValidType(v)) {
                    fields[k].SetDefault(v);
                } else {
                    // Mapdata stored an invalid type for this field, replace it with a default.
                    // This is needed for Frost Helper Spinner's Dash Through, which changed types from bool to string enum.
                    fields[k] = knownFieldType.GetAlternativeForInvalidFieldDefaultType(v);
                }
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

    public EntityPropertyWindow(IHistoryHandler history, Entity main, List<Entity> all) 
        : base(CreateWindowTitle(main, all)) {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(main);
        ArgumentNullException.ThrowIfNull(all);

        Main = main;
        All = all;

        var (fields, exists) = GetFields(main);
        Init(fields, exists);

        OnChanged = (edited) => {
            history.ApplyNewAction(new EntityEditAction(All, edited));
        };
        OnLiveUpdate = (edited) => {
            foreach (var e in All) {
                e.EntityData.SetOverlay(edited);
            }
        };
    }

    private static string CreateWindowTitle(Entity main, List<Entity> all)
    {
        return $"Edit: {main.EntityData.Sid}:{string.Join(',', all.Select(e => e.Id))}";
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
            OnEntityRemovedFromPropertyWindow(e);
        }
    }

    public void OnSignal(HistoryChanged signal) {
        ReevaluateChanged(Main.EntityData);

        // If the entity we're editing is removed from the room, remove the window.
        if (Main.Room.TryGetEntityById(Main.Id) != Main)
            RemoveSelf();

        for (int i = All.Count - 1; i >= 0; i--) {
            var e = All[i];
            if (e.Room.TryGetEntityById(e.Id) != e) {
                OnEntityRemovedFromPropertyWindow(e);
                All.RemoveAt(i);
            }
        }

        Name = CreateWindowTitle(Main, All);
    }

    private void OnEntityRemovedFromPropertyWindow(Entity e) {
        e.EntityData.SetOverlay(null);
    }
}
