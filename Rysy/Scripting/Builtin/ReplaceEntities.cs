using Rysy.Gui.Windows;
using Rysy.History;
using Rysy.Selections;

namespace Rysy.Scripting.Builtin;

public sealed class ReplaceEntities : Script {
    public override string Name => "Replace Entities";

    public override string? Tooltip => "Replaces entities of one type with another type, allowing you to configure attributes";

    public override FieldList? Parameters => new() {
        ["from"] = Fields.Sid("spinner", RegisteredEntityType.Entity),
        ["to"] = Fields.Sid("FrostHelper/IceSpinner", RegisteredEntityType.Entity),
    };

    public override IHistoryAction? Prerun(ScriptArgs args) {
        var origSid = args.Get<string>("from");
        var newSid = args.Get<string>("to");

        var mainPlacement = EntityRegistry.EntityPlacements.FirstOrDefault(pl => pl.SID == newSid);
        if (mainPlacement is null)
        {
            throw new Exception($"Can't run the script, as there are no placements for {newSid}");
        }

        var tempEntity = EntityRegistry.Create(mainPlacement, new(), room: args.Rooms[0], assignID: false, isTrigger: false);
        var (fields, existChecker) = EntityPropertyWindow.GetFields(tempEntity);

        var formWindow = new FormWindow(fields, $"Configuring Replace Script: {origSid} -> {newSid}");
        formWindow.SaveChangesButtonName = "Run Script";

        formWindow.OnChanged = (edited) => {
            var placement = mainPlacement with {
                ValueOverrides = formWindow.GetAllValues(),
            };

            IHistoryAction? action = args.Rooms
                .SelectMany(r => r.Entities[origSid])
                .Select(e => new SwapEntityAction(e, EntityRegistry.Create(placement, e.Pos, e.Room, assignID: false, isTrigger: e is Trigger)))
                .MergeActions();

            // generally, your scripts should return a history action in Prerun instead of adding actions manually,
            // but in this case, our script actually finishes at an arbitrary time, so we have to do this ourselves
            var history = EditorState.History;
            history?.ApplyNewAction(action);
        };

        RysyEngine.Scene.AddWindow(formWindow);

        return null;
    }
}