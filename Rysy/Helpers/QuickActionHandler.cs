using Rysy.Extensions;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Tools;
using System.Text.Json;

namespace Rysy.Helpers;

public class QuickActionHandler {
    public readonly HotkeyHandler Hotkeys;
    public readonly ToolHandler ToolHandler;

    private void AddHotkey(QuickActionInfo action) {
        Hotkeys.AddHotkey(action.Hotkey, () => {
            if (ToolHandler.SetToolByName(action.ToolName) is { } tool) {
                tool.Layer = EditorLayers.EditorLayerFromName(action.Layer);

                if (action.Material is { } material) {
                    var t = Type.GetType(action.MaterialTypeName!)!;
                    tool.Material = material.Deserialize(t, JsonSerializerHelper.DefaultOptions);
                } else {
                    tool.Material = null;
                }
            }
        });
    }

    public QuickActionHandler(HotkeyHandler hotkeyHandler, ToolHandler toolHandler) {
        Hotkeys = hotkeyHandler;
        ToolHandler = toolHandler;

        foreach (var action in Actions.Value) {
            AddHotkey(action);
        }

        hotkeyHandler.AddHotkeyFromSettings("add_quick_action", "alt+q", () => {
            var tool = toolHandler.CurrentTool;

            var info = new QuickActionInfo() {
                ToolName = tool.Name,
                Layer = tool.Layer.Name,
            };

            switch (tool.Material) {
                case not null:
                    info.MaterialTypeName = tool.Material.GetType()!.FullName!;
                    info.Material = JsonSerializer.SerializeToElement(tool.Material, JsonSerializerHelper.DefaultOptions);
                    break;
                default:
                    break;
            }

            var fields = new FieldList(new {
                hotkey = Fields.String("").WithValidator(HotkeyHandler.IsValid)
            });
            var form = new Gui.Windows.FormWindow(fields, "rysy.quick_actions.window_name".Translate());

            form.OnChanged += (edited) => {
                info.Hotkey = edited["hotkey"]?.ToString() ?? "";

                Actions.Value.Add(info);

                var fs = SettingsHelper.GetFilesystem(perProfile: true);
                fs.TryWriteToFile(Filename, Actions.Value.ToJson());

                form.RemoveSelf();

                AddHotkey(info);
            };

            RysyEngine.Scene.AddWindow(form);
        });
    }

    private static string Filename => "quickActions.json";

    private static Lazy<List<QuickActionInfo>> Actions = new(() => LoadActions(Filename));

    private static List<QuickActionInfo> LoadActions(string file) {
        try {
            var fs = SettingsHelper.GetFilesystem(perProfile: true);
            if (fs.TryReadAllText(file) is { } txt) {
                return JsonSerializer.Deserialize<List<QuickActionInfo>>(txt, JsonSerializerHelper.DefaultOptions) ?? new();
            }

            return [];
        } catch (Exception e) {
            Logger.Error(e, $"Error loading quick actions");
            return new();
        }
    }
}

public class QuickActionInfo {
    public string Hotkey;
    public string ToolName;
    public string Layer;

    public string? MaterialTypeName;
    public JsonElement? Material;
}
