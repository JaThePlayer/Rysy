using Hexa.NET.ImGui;
using Rysy.Entities.Modded;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Tools;

namespace Rysy.Selections;

using ContextWindowDraw = Action<ISelectionHandler, IEnumerable<Selection>, PopupCtx>;

public struct PopupCtx {
    public SelectionTool SelectionTool { get; internal set; }
    public Room Room { get; internal set; }
}

public static class SelectionContextWindowRegistry {
    private sealed class PopupInfo {
        public IEditorLayer Layer;
        public string PopupId;

        public ISelectionHandler Main;
        public List<Selection> Selections;
    }

    private static string LayerToPopupId(IEditorLayer layer)
        => $"context_window_{layer.Name}";

    private static Dictionary<IEditorLayer, ContextWindowDraw> DrawFunctions = new();

    private static List<PopupInfo> CurrentPopups = new();

    private static Queue<PopupInfo> NewPopupQueue = new();

    public static void AddHandler(IEditorLayer layer, ContextWindowDraw handler) {
        if (DrawFunctions.TryGetValue(layer, out var existing)) {
            handler = existing + handler;
        }

        DrawFunctions[layer] = handler;
    }

    public static void RemoveHandler(IEditorLayer layer, ContextWindowDraw handler) {
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

        AddHandler(EditorLayers.Fg, RemoveAll);
        AddHandler(EditorLayers.Bg, RemoveAll);
        AddHandler(EditorLayers.Entities, RemoveAll);
        AddHandler(EditorLayers.Triggers, RemoveAll);
        AddHandler(EditorLayers.BgDecals, RemoveAll);
        AddHandler(EditorLayers.FgDecals, RemoveAll);
        AddHandler(EditorLayers.Room, RemoveAll);

        AddHandler(EditorLayers.Fg, ConvertTilesToEntity(TileLayer.Fg));
        AddHandler(EditorLayers.Bg, ConvertTilesToEntity(TileLayer.Bg));
    }

    private static Cache<List<(string, Type, TileLayer)>>? TilegridEntityTypes;

    public static void Render(SelectionTool selectionTool, Room room) {
        while (NewPopupQueue.TryDequeue(out var incomingPopup)) {
            CurrentPopups.Add(incomingPopup);
            ImGui.OpenPopup(incomingPopup.PopupId);
        }

        for (int i = CurrentPopups.Count - 1; i >= 0; i--) {
            var popup = CurrentPopups[i];

            if (!ImGui.IsPopupOpen(popup.PopupId)) {
                CurrentPopups.Remove(popup);
                continue;
            }

            if (ImGui.BeginPopupContextWindow(popup.PopupId, ImGuiPopupFlags.MouseButtonRight)) {
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

        var id = LayerToPopupId(layer);
        NewPopupQueue.Enqueue(new PopupInfo() {
            Main = main,
            Selections = all.ToList(),
            Layer = layer,
            PopupId = id,
        });
    }

    private static void RemoveAll(ISelectionHandler main, IEnumerable<Selection> all, PopupCtx ctx) {
        if (ImGui.MenuItem("Delete", Settings.Instance.GetHotkey("delete"))) {
            ctx.SelectionTool.DeleteSelections();

            ImGui.CloseCurrentPopup();
        }
    }

    private static ContextWindowDraw ConvertTilesToEntity(TileLayer targetLayer) {
        return (ISelectionHandler main, IEnumerable<Selection> all, PopupCtx ctx) => {

            TilegridEntityTypes ??= EntityRegistry.Registered.CreateCache(sidToType => sidToType
                .Where(kv => kv.Value.CSharpType?.IsSubclassOf(typeof(TilegridEntity)) ?? false)
                .Select(kv => (kv.Key, Type: kv.Value.CSharpType!, ((TilegridEntity)EntityRegistry.CreateFromMainPlacement(kv.Key, default, Room.DummyRoom)).Layer))
                .ToList()
            );
            if (main is not TileSelectionHandler tileHandler) {
                return;
            }

            if (TilegridEntityTypes.Value.Count > 0 && ImGui.BeginMenu("Convert To Entity")) {
                foreach (var (sid, type, layer) in TilegridEntityTypes.Value) {
                    if (layer == targetLayer && ImGui.MenuItem($"{sid}")) {
                        List<IHistoryAction> actions = new();

                        char[,] tiles = (char[,]) tileHandler.GetSelectedTiles().Clone();
                        actions.Add(tileHandler.DeleteSelf());

                        var pos = tileHandler.Rect.Location.ToVector2();
                        var fancyTile = ((TilegridEntity)EntityRegistry.CreateFromMainPlacement(sid, pos, ctx.Room, isTrigger: false)).ChangeTilesTo(tiles);

                        actions.Add(new AddEntityAction(fancyTile, ctx.Room));

                        ctx.SelectionTool.History.ApplyNewAction(actions);
                        ctx.SelectionTool.Deselect(tileHandler);
                        ctx.SelectionTool.AddSelection(fancyTile.CreateSelection());

                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndMenu();
            }
        };
    }
}
