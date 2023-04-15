using Rysy.Graphics;
using Rysy.History;
using Rysy.Scripting;

namespace Rysy.Tools;

public class ScriptTool : Tool {
    public const string CURRENT_ROOM_LAYER = "Current Room";

    public override string Name => "Scripts";

    public override string PersistenceGroup => "Scripts";

    public override List<string> ValidLayers => new() { CURRENT_ROOM_LAYER };

    public override string GetMaterialDisplayName(string layer, object material) {
        if (material is Script s) {
            return s.Name;
        }

        return material.ToString() ?? "";
    }

    public override IEnumerable<object>? GetMaterials(string layer) 
        => ScriptRegistry.Scripts;

    public override string? GetMaterialTooltip(string layer, object material) {
        if (material is Script s) {
            return s.Tooltip;
        }

        return null;
    }

    public override void Render(Camera camera, Room room) {
    }

    public override void RenderOverlay() {
    }

    private void RunScript(Script script) {
        script.Prerun();

        switch (Layer) {
            case CURRENT_ROOM_LAYER:
                /*
                var action = script.Run(EditorState.CurrentRoom);

                if (action is { }) {
                    History.ApplyNewAction(action);
                }*/
                var old = EditorState.CurrentRoom;
                var clone = old.Clone();
                script.Run(clone);

                History.ApplyNewAction(new SwapRoomAction(old, clone));

                break;
            default:
                break;
        }
    }


    public override void Update(Camera camera, Room room) {
        if (Material is not Script script)
            return;

        if (Input.Mouse.Left.Clicked()) {
            RunScript(script);
        }
    }
}
