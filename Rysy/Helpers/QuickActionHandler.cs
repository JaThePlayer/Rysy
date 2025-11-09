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
            action.Apply(ToolHandler);
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

            var info = QuickActionInfo.CreateFrom(tool);

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
    public string Hotkey { get; set; }
    public string ToolName { get; private init; }
    public string Layer { get; private init; }

    public string? MaterialString { get; private set; }
    
    public bool IsFavourite { get; set; }
    
    private object? SourceMaterial { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public object? GetMaterial(ToolHandler handler) {
        if (SourceMaterial is { } src) {
            return src;
        }
        
        if (handler.SetToolByName(ToolName) is { } tool && MaterialString is not null) {
            return SourceMaterial = tool.DeserializeMaterial(
                EditorLayers.EditorLayerFromName(Layer), 
                MaterialString
            );
        }

        return null;
    }

    public static QuickActionInfo CreateFrom(Tool tool) {
        var info = new QuickActionInfo {
            ToolName = tool.Name,
            Layer = tool.Layer.Name,
            SourceMaterial = tool.Material,
            MaterialString = tool.SerializeMaterial(tool.Layer, tool.Material),
        };

        return info;
    }

    public void Apply(ToolHandler handler) {
        if (handler.SetToolByName(ToolName) is { } tool) {
            tool.Layer = EditorLayers.EditorLayerFromName(Layer);
            tool.Material = GetMaterial(handler);
        }
    }
}
