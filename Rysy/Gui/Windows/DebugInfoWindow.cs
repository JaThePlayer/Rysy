using ImGuiNET;
using Rysy.Extensions;
using Rysy.History;
using Rysy.LuaSupport;
using Rysy.Scenes;
using System.Diagnostics;
using System.Linq;

namespace Rysy.Gui.Windows;

public class DebugInfoWindow : Window {
    public static DebugInfoWindow Instance { get; } = new();

    public static bool Enabled { get; set; } = false;

    public DebugInfoWindow() : base("Debug", new(480, 480)) {
        SetRemoveAction((w) => Enabled = false);
    }

    private static string HistoryFromText = "";

    protected override void Render() {
        ImGui.Text($"FPS: {RysyEngine.CurrentFPS}");

        if (ImGui.CollapsingHeader("Memory")) {
            ImGui.Text($"RAM: {Process.GetCurrentProcess().WorkingSet64 / 1024m}KB");
        }

#if !FNA
        if (ImGui.CollapsingHeader("Metrics")) {
            var metrics = RysyEngine.GDM.GraphicsDevice.Metrics;
            ImGui.Text(metrics.ToJson());
        }
#endif

        HistoryTab();

        if (ImGui.CollapsingHeader("GC")) {
            ImGui.Text($"Pinned: {GC.GetGCMemoryInfo().PinnedObjectsCount}");
        }

        if (RysyEngine.Scene is EditorScene editor) {
            if (ImGui.CollapsingHeader("Lonn Entity stats:")) {
                var lonnEntities = editor.CurrentRoom?.Entities.OfType<LonnEntity>().ToList() ?? new();
                var cachedAmt = lonnEntities.Count(e => e.CachedSprites is { });
                var uncachedSids = lonnEntities.Where(e => e.CachedSprites is not { }).Select(e => e.Name).Distinct();

                ImGui.TextUnformatted($"Cached: {cachedAmt}/{lonnEntities.Count} [{cachedAmt / (float)lonnEntities.Count * 100}%]");
                
                if (ImGui.BeginListBox("Uncached SID's", new(Size!.Value.X, 300))) {
                    foreach (var sid in uncachedSids) {
                        ImGui.TextUnformatted(sid);
                    }

                    ImGui.EndListBox();
                }
                
            }

            if (EditorState.CurrentRoom is { } room && ImGui.Button("Benchmark current room")) {
                var watch = Stopwatch.StartNew();
                const int times = 100;
                for (int i = 0; i < times; i++) {
                    room.ClearEntityRenderCache();
                    room.CacheSpritesIfNeeded();
                }

                watch.Stop();
                Console.WriteLine($"Benchmark: {room.Name}: {(watch.Elapsed / times).TotalMilliseconds}ms");
            }
        }
    }

    private static void HistoryTab() {
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
    }
}
