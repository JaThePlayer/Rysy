using Rysy.Graphics;
using Rysy.Gui.Windows;
using Rysy.History;
using Rysy.Scripting;
using ImGuiNET;
using Rysy.Extensions;
using Rysy.Layers;

namespace Rysy.Tools;

public class ScriptTool : Tool {
    public static EditorLayer CurrentRoomLayer { get; } = new FakeLayer("Current Room");
    public static EditorLayer AllRoomsLayer { get; } = new FakeLayer("All Rooms");

    public override string Name => "script";

    public override string PersistenceGroup => "Scripts";

    public override List<EditorLayer> ValidLayers => [CurrentRoomLayer, AllRoomsLayer];

    public override string GetMaterialDisplayName(EditorLayer layer, object material) {
        if (material is Script s) {
            return s.Name;
        }

        return material.ToString() ?? "";
    }

    public override object? MaterialToPersistenceObj(object? material) {
        if (material is Script scr) {
            return scr.Name;
        }

        return material;
    }

    public override object? PersistenceObjToMaterial(object? material) {
        if (material is string str) {
            return ScriptRegistry.Scripts.FirstOrDefault(s => s.Name == str);
        }

        return null;
    }

    public override IEnumerable<object>? GetMaterials(EditorLayer layer) 
        => ScriptRegistry.Scripts;

    public override string? GetMaterialTooltip(EditorLayer layer, object material) {
        if (material is Script s) {
            return s.Tooltip;
        }

        return null;
    }

    public override void Render(Camera camera, Room room) {
    }

    public override void RenderOverlay() {
    }

    private void RunScript(Script script, Dictionary<string, object>? fieldValues, Vector2 roomPos) {
        var args = new ScriptArgs();

        args.Args = fieldValues ?? new();
        args.RoomPos = roomPos;

        var layer = Layer;
        List<Room>? rooms;
        if (layer.Name == CurrentRoomLayer.Name)
            rooms = EditorState.CurrentRoom is { } ? new List<Room>() { EditorState.CurrentRoom } : null;
        else if (layer.Name == AllRoomsLayer.Name) {
            rooms = EditorState.Map?.Rooms;
        } else {
            throw new NotImplementedException(layer.Name);
        }

        if (rooms is null) {
            return;
        }

        if (script.CallRun) {
            var actions = new List<IHistoryAction>();

            args.Rooms = rooms;

            if (!CallPrerun(script, args, out var prerunAction))
                return; // if prerun crashed, don't call Run, just in case

            if (prerunAction is { }) {
                actions.Add(prerunAction);
            }

            // if we're calling Run, we need to clone rooms, so that we can create a history action
            var clonedRooms = rooms.Select(r => r.Clone()).ToList();
            args = args with {
                Rooms = clonedRooms,
            };

            for (int i = 0; i < rooms.Count; i++) {
                var room = rooms[i];
                var clone = clonedRooms[i];

                try {
                    if (script.Run(clone, args))
                        actions.Add(new SwapRoomAction(room, clone));
                } catch (Exception e) {
                    Logger.Error(e, $"Failed running script's Run method in room {room.Name}");
                    CreateCrashWindow(e);
                }
            }

            History.ApplyNewAction(actions.MergeActions());
        } else {
            // no need to clone the rooms, prerun should handle ctrl+z on its own.
            args.Rooms = rooms;
            CallPrerun(script, args, out var action);
            if (action is { }) {
                History.ApplyNewAction(action);
            }
        }
    }

    private bool CallPrerun(Script script, ScriptArgs args, out IHistoryAction? returnedAction) {
        returnedAction = null;

        try {
            if (script.Prerun(args) is { } action) {
                returnedAction = action;
            }
            return true;
        } catch (Exception e) {
            Logger.Error(e, $"Failed running script's Prerun method");
            CreateCrashWindow(e);
            return false;
        }
    }

    private static void CreateCrashWindow(Exception e) {
        RysyEngine.Scene.AddWindow(new CrashWindow("Failed to run script", e, (w) => {
            if (ImGui.Button("OK")) {
                w.RemoveSelf();
            }
        }));
    }

    public override void Update(Camera camera, Room? room) {
        if (room is null)
            return;
        
        if (Material is not Script script)
            return;

        if (Input.Mouse.Left.Clicked()) {
            var fields = script.Parameters;
            var roomPos = room.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2()).Snap(8);

            if (fields is { }) {
                var form = new FormWindow(fields, $"Script Parameters: {script.Name}");
                form.SaveChangesButtonName = "Run Script";

                form.OnChanged = (edited) => {
                    RunScript(script, form.GetAllValues(), roomPos);
                };

                RysyEngine.Scene.AddWindow(form);
            } else {
                RunScript(script, fieldValues: null, roomPos);
            }
        }
    }

    public override void Init() {
        base.Init();

        ScriptRegistry.OnScriptReloaded += ClearMaterialListCache;
    }
}
