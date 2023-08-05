using ImGuiNET;
using Rysy.History;
using Rysy.Tools;

namespace Rysy.Selections;

using ContextWindowDraw = Action<ISelectionHandler, IEnumerable<Selection>, PopupCtx>;

public struct PopupCtx {
    public SelectionTool SelectionTool { get; internal set; }
    public Room Room { get; internal set; }
}

public static class SelectionContextWindowRegistry {
    private class PopupInfo {
        public SelectionLayer Layer;
        public string PopupID;

        public ISelectionHandler Main;
        public List<Selection> Selections;
    }

    private static string LayerToPopupID(SelectionLayer layer)
        => $"context_window_{layer}";

    private static Dictionary<SelectionLayer, ContextWindowDraw> DrawFunctions = new();

    private static List<PopupInfo> CurrentPopups = new();

    private static Queue<PopupInfo> NewPopupQueue = new();

    public static void AddHandler(SelectionLayer layer, ContextWindowDraw handler) {
        if (DrawFunctions.TryGetValue(layer, out var existing)) {
            handler = existing + handler;
        }

        DrawFunctions[layer] = handler;
    }

    public static void RemoveHandler(SelectionLayer layer, ContextWindowDraw handler) {
        if (DrawFunctions.TryGetValue(layer, out var existing)) {
            var newHandler = existing - handler;
            if (newHandler != null) {
                DrawFunctions[layer] = newHandler;
            } else {
                DrawFunctions.Remove(layer);
            }
        }
    }

    internal static void Init() {
        DrawFunctions.Clear();

        AddHandler(SelectionLayer.FGTiles, RemoveAll);
        AddHandler(SelectionLayer.BGTiles, RemoveAll);
        AddHandler(SelectionLayer.Entities, RemoveAll);
        AddHandler(SelectionLayer.Triggers, RemoveAll);
        AddHandler(SelectionLayer.BGDecals, RemoveAll);
        AddHandler(SelectionLayer.FGDecals, RemoveAll);
        AddHandler(SelectionLayer.Rooms, RemoveAll);
    }

    public static void Render(SelectionTool selectionTool, Room room) {
        while (NewPopupQueue.TryDequeue(out var incomingPopup)) {
            CurrentPopups.Add(incomingPopup);
            ImGui.OpenPopup(incomingPopup.PopupID);
        }

        for (int i = CurrentPopups.Count - 1; i >= 0; i--) {
            var popup = CurrentPopups[i];

            if (!ImGui.IsPopupOpen(popup.PopupID)) {
                CurrentPopups.Remove(popup);
                continue;
            }

            if (ImGui.BeginPopupContextWindow(popup.PopupID, ImGuiPopupFlags.MouseButtonMask)) {
                DrawFunctions[popup.Layer](popup.Main, popup.Selections, new() {
                    Room = room,
                    SelectionTool = selectionTool,
                });

                ImGui.EndPopup();
            }
        }
    }

    public static void OpenPopup(ISelectionHandler main, IEnumerable<Selection> all) {
        var layer = main.Layer;

        if (!DrawFunctions.TryGetValue(layer, out var drawFunc)) {
            return;
        }

        var id = LayerToPopupID(layer);
        NewPopupQueue.Enqueue(new PopupInfo() {
            Main = main,
            Selections = all.ToList(),
            Layer = layer,
            PopupID = id,
        });
    }

    private static void RemoveAll(ISelectionHandler main, IEnumerable<Selection> all, PopupCtx ctx) {
        if (ImGui.MenuItem("Delete", Settings.Instance.GetHotkey("delete"))) {
            ctx.SelectionTool.DeleteSelections();

            ImGui.CloseCurrentPopup();
        }
    }
}
