using ImGuiNET;
using Rysy.History;
using Rysy.Scenes;

namespace Rysy.Gui.Elements;
public record class DebugInfoWindow : Window {
    public static DebugInfoWindow Instance = new();

    public static bool Enabled { get; set; } = false;

    public DebugInfoWindow() : base("Debug", Render, new(480, 480)) {
        SetRemoveAction((w) => Enabled = false);
    }

    private static string HistoryFromText = "";

    public static new void Render(Window w) {
        ImGui.Text($"FPS: {RysyEngine.Framerate}");

        if (ImGui.CollapsingHeader("Metrics")) {
            var metrics = RysyEngine.GDM.GraphicsDevice.Metrics;
            ImGui.Text(metrics.ToJson());
        }
        if (RysyEngine.Scene is EditorScene editor && ImGui.CollapsingHeader("History")) {
            ImGui.Text($"Count: {editor.HistoryHandler.Actions.Count}");
            if (ImGui.BeginListBox("")) {
                ImGui.TextWrapped(string.Join('\n', editor.HistoryHandler.Actions.Select(act => act.ToString())));
                //ImGui.TextWrapped(editor.HistoryHandler.Serialize());
                ImGui.EndListBox();
            }

            if (ImGui.InputText("From Text", ref HistoryFromText, 10_000, ImGuiInputTextFlags.EnterReturnsTrue)) {
                foreach (var item in HistoryHandler.Deserialize(HistoryFromText)) {
                    editor.HistoryHandler.ApplyNewAction(item);
                }
            }
        }

        if (ImGui.CollapsingHeader("GC")) {
            var metrics = RysyEngine.GDM.GraphicsDevice.Metrics;
            ImGui.Text($"Pinned: {GC.GetGCMemoryInfo().PinnedObjectsCount}");
        }
    }
}
