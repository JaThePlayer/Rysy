using Rysy.Layers;

namespace Rysy.Scripting.Builtin; 

internal sealed class MigrateLayersToGroups : Script {
    public override string Name => "Migrate Layers";
    public override string? Tooltip => "Migrates Lönn Extended Editor Layers into Groups.";
    
    public override FieldList? Parameters => new(new {
        editorLayer = Fields.Int(1).WithTooltip("The Editor Layer to migrate into Groups"),
        intoGroup = Fields.EditorGroup(EditorState.Map?.EditorGroups ?? new())
            .WithMinElements(1)
            .WithTooltip("Which group the entities should be moved into."),
        removeLayers = Fields.Bool(false).WithTooltip("Whether the _editorLayer field should be removed from entities.\nThis will break compatibility with Lönn Extended."),
    });

    public override bool Run(Room room, ScriptArgs args) {
        var groups = EditorGroupList.FromString(room.Map.EditorGroups, args.Get<string>("intoGroup"));
        var targetLayer = args.Get<int>("editorLayer");
        var removeLayers = args.Get<bool>("removeLayers");
        
        if (groups is null)
            return false;
        
        var changed = false;
        
        foreach (var e in room.GetAllEntitylikes().ToList()) {
            if (!e.EntityData.TryGetValue("_editorLayer", out var layerObj))
                continue;
            
            try {
                var layer = Convert.ToInt32(layerObj);
                if (layer == targetLayer) {
                    Console.WriteLine(e.ToJson());
                    e.EditorGroups.AddIfUnique(groups);
                    
                    if (removeLayers)
                        e.EntityData.Remove("_editorLayer");
                    changed = true;
                }
            } catch {
                // ignored
            }
        }

        return changed;
    }
}