using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.MapAnalyzers;
using Rysy.Tools;

namespace Rysy.Scenes;

public sealed class EditorScene : Scene {
    public ToolHandler ToolHandler;

    public HistoryHandler HistoryHandler {
        get => EditorState.History!;
        set => EditorState.History = value;
    }

    public Map? Map {
        get => EditorState.Map;
        set => EditorState.Map = value;
    }

    private void OnMapChanged() {
        var map = EditorState.Map;

        SwapMapPreserveState(map);

        Camera = new();

        if (map?.Rooms.Count > 0) {
            CurrentRoom = map.Rooms.First();
            CenterCameraOnRoom(CurrentRoom);
        }

        if (map is { })
            Persistence.Instance.PushRecentMap(map);

        RysyEngine.OnEndOfThisFrame += GCHelper.VeryAggressiveGC;
    }

    private void SwapMapPreserveState(Map? map) {
        EditorState.Map = map;

        // history has to be cleared, as it might contain references to specific entity instances
        HistoryHandler.Clear();
    }

    public Room? CurrentRoom {
        get => EditorState.CurrentRoom;
        set => EditorState.CurrentRoom = value;
    }

    public Camera Camera {
        get => EditorState.Camera!;
        set => EditorState.Camera = value;
    } // will be set in Map.set

    public EditorScene(bool loadFromPersistence = true) {
        HistoryHandler = new();
        ToolHandler = new ToolHandler(HistoryHandler, Input.Global).UsePersistence(true);

        // Try to load the last edited map.
        if (loadFromPersistence && !string.IsNullOrWhiteSpace(Persistence.Instance?.LastEditedMap))
            LoadMapFromBin(Persistence.Instance.LastEditedMap, fromPersistence: true);
        else {
            Map = null;
        }

        EditorState.OnMapChanged += OnMapChanged;
    }

    public EditorScene(Map map) : this(false) {
        Map = map;
    }

    public EditorScene(string mapFilepath, bool fromPersistence = false, bool fromBackup = false, string? overrideFilepath = null) : this(false) {
        LoadMapFromBin(mapFilepath, fromPersistence, fromBackup, overrideFilepath);
    }

    public override void SetupHotkeys() {
        base.SetupHotkeys();

        HotkeysIgnoreImGui.AddHotkeyFromSettings("saveMap", "ctrl+s", () => Save());
        Hotkeys.AddHotkeyFromSettings("openMap", "ctrl+o", Open);
        Hotkeys.AddHotkeyFromSettings("newMap", "ctrl+shift+n", () => LoadNewMap());

        HotkeysIgnoreImGui.AddHotkeyFromSettings("undo", "ctrl+z|mouse3", Undo, HotkeyModes.OnHoldSmoothInterval);
        HotkeysIgnoreImGui.AddHotkeyFromSettings("redo", "ctrl+y|mouse4", Redo, HotkeyModes.OnHoldSmoothInterval);

        Hotkeys.AddHotkeyFromSettings("newRoom", "ctrl+n", AddNewRoom);

        Hotkeys.AddHotkeyFromSettings("moveRoomDown", "alt+down", () => MoveCurrentRoom(0, 1), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("moveRoomUp", "alt+up", () => MoveCurrentRoom(0, -1), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("moveRoomLeft", "alt+left", () => MoveCurrentRoom(-1, 0), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("moveRoomRight", "alt+right", () => MoveCurrentRoom(1, 0), HotkeyModes.OnHoldSmoothInterval);

        Hotkeys.AddHotkeyFromSettings("layerUp", "pageup", () => ChangeEditorLayer(1), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("layerDown", "pagedown", () => ChangeEditorLayer(-1), HotkeyModes.OnHoldSmoothInterval);

        ToolHandler.InitHotkeys(Hotkeys);

        _ = new QuickActionHandler(Hotkeys, ToolHandler);
    }

    public void ClearMapRenderCache() {
        Map?.Rooms.ForEach(r => r.ClearRenderCache());
    }

    public void ChangeEditorLayer(int by) {
        Persistence.Instance.EditorLayer = (Persistence.Instance.EditorLayer ?? 0) + by;

        ClearMapRenderCache();
    }

    public override void OnFileDrop(FileDropEventArgs args) {
        base.OnFileDrop(args);

        var file = args.Files[0];
        if (File.Exists(file) && Path.GetExtension(file) == ".bin") {
            LoadMapFromBin(file);
        }
    }

    public void LoadMapFromBin(string file, bool fromPersistence = false, bool fromBackup = false, string? overrideFilepath = null) {
        if (!File.Exists(file))
            return;

        RysyEngine.OnEndOfThisFrame += async () => {
            try {
                if (RysyEngine.Scene is not LoadingScene loadingScreen) {
                    loadingScreen = new LoadingScene();
                    RysyEngine.Scene = loadingScreen;
                }
                LoadingScene.Text = $"Loading map {file.Censor()}";

                // Just to make this run async, so we can see the loading screen.
                await Task.Delay(1);

                var mapBin = BinaryPacker.FromBinary(file);

                var map = Map.FromBinaryPackage(mapBin);
                if (overrideFilepath is { }) {
                    map.Filepath = overrideFilepath;
                }
                Map = map;

                RysyEngine.Scene = this;

                //if (fromPersistence)
                //    throw new Exception("yo");
            } catch (Exception e) {
                Logger.Write("LoadMapFromBin", LogLevel.Error, $"Failed to load map: {e}");

                if (fromPersistence) {
                    RysyEngine.Scene = new PersistenceMapLoadErrorScene(e, "fromPersistence");
                } else if (fromBackup) {
                    RysyEngine.Scene = new PersistenceMapLoadErrorScene(e, "fromBackup");
                } else {
                    RysyEngine.Scene = this;
                    AddWindow(new CrashWindow("rysy.mapLoadError.other".TranslateFormatted(file.Censor()), e, (w) => {
                        if (ImGui.Button("rysy.ok".Translate())) {
                            w.RemoveSelf();
                        }
                    }));
                }
            }
        };
    }

    public void LoadNewMap(string? packageName = null) {
        if (packageName is null) {
            AddNewMapWindow();
            return;
        }

        Map = Map.NewMap(packageName);
    }

    private void AddNewMapWindow() {
        var wData = "";
        var window = new ScriptedWindow("New map", (w) => {
            ImGui.TextWrapped("Please enter the Package Name for your map.");
            ImGui.TextWrapped("This name is only used internally, and is not visible in-game.");
            ImGui.InputText("Package Name", ref wData, 512);

            ImGuiManager.BeginWindowBottomBar(!string.IsNullOrWhiteSpace(wData));
            if (ImGui.Button("Create Map")) {
                LoadNewMap(wData);
                w.RemoveSelf();
            }
            ImGuiManager.EndWindowBottomBar();
        }, new(350, ImGui.GetTextLineHeightWithSpacing() * 8));

        AddWindow(window);
    }

    public void Save(bool saveAs = false) {
        if (Map is not { })
            return;

        if (saveAs || string.IsNullOrWhiteSpace(Map.Filepath)) {
            if (!FileDialogHelper.TrySave("bin", out var filepath)) {
                return;
            }

            Map.Filepath = filepath;
            Persistence.Instance.PushRecentMap(Map);
        }

        BackupHandler.Backup(Map);

        var analyzerCtx = MapAnalyzerRegistry.Global.Analyze(Map);
        //analyzerCtx.Results.LogAsJson();

        if (analyzerCtx.Results.Any(r => r.Level == LogLevel.Error)) {
            AddWindowIfNeeded<MapAnalyzerWindow>();
        } else {
            using var watch = new ScopedStopwatch("Saving");
            var pack = Map.IntoBinary();
            BinaryPacker.SaveToFile(pack, pack.Filename!);
        }
    }

    public void Open() {
        if (FileDialogHelper.TryOpen("bin", out var path)) {
            LoadMapFromBin(path);
        }
    }

    public void AddNewRoom() {
        if (Map is not { })
            return;

        var current = CurrentRoom;
        var room = new Room(Map, current?.Width ?? 40 * 8, current?.Height ?? 23 * 8);
        if (current is { }) {
            room.X = current.X;
            room.Y = current.Y;
            room.Name = current.Name;
        }

        room.Entities.Add(EntityRegistry.Create(new("player") {
            Attributes = new() {
                ["x"] = 20 * 8,
                ["y"] = 12 * 8,
            },
        }, room, false));
        AddWindow(new RoomEditWindow(room, newRoom: true));
    }

    public void MoveCurrentRoom(int tilesX, int tilesY) {
        if (CurrentRoom is { } room)
            HistoryHandler.ApplyNewAction(new RoomMoveAction(room, tilesX, tilesY));
    }

    public void Undo() => HistoryHandler.Undo();
    public void Redo() => HistoryHandler.Redo();


    public void QuickReload() {
        if (Map is not { } map) {
            return;
        }

        var selectedRoomName = CurrentRoom?.Name;

        Map = Map.FromBinaryPackage(map.IntoBinary());

        if (selectedRoomName is { }) {
            CurrentRoom = Map.TryGetRoomByName(selectedRoomName)!;
            CenterCameraOnRoom(CurrentRoom!);
        }
    }

    public override void Update() {
        base.Update();

        ImGuiIOPtr io = ImGui.GetIO();
        // Intentionally don't check for capturing keyboard, works weirdly with search bar, and can result in seemingly eaten inputs.
        if (io.WantCaptureMouse /*|| io.WantCaptureKeyboard*/) {
            return;
        }

        if (Map is { } && CurrentRoom is { }) {
            Camera.HandleMouseMovement(Input.Global);

            HandleRoomSwapInputs(Input.Global);

            ToolHandler.Update(Camera, CurrentRoom);
        }
    }

    private void HandleRoomSwapInputs(Input input) {
        if (Map is { } && ToolHandler.CurrentTool.AllowSwappingRooms && input.Mouse.Left.Clicked()) {
            var mousePos = input.Mouse.Pos.ToVector2();
            foreach (var room in Map.Rooms) {
                if (room == CurrentRoom)
                    continue;

                var pos = room.WorldToRoomPos(Camera, mousePos);
                if (room.IsInBounds(pos)) {
                    CurrentRoom = room;

                    input.Mouse.ConsumeLeft();
                    break;
                }
            }
        }
    }

    public override void Render() {
        base.Render();

        if (Map is not { }) {
            var windowSize = RysyEngine.Instance.Window.ClientBounds.Size;
            var height = 4 * 6;
            var center = windowSize.Y / 2;

            GFX.BeginBatch();
            PicoFont.Print("No map loaded.", new Rectangle(0, center - 32, windowSize.X, height), Color.LightSkyBlue, 4f);
            PicoFont.Print("Please drop a .bin", new Rectangle(0, center, windowSize.X, height), Color.White, 4f);
            PicoFont.Print("file onto this window", new Rectangle(0, center + 32, windowSize.X, height), Color.White, 4f);
            GFX.EndBatch();
            return;
        }

        if (CurrentRoom is null)
            return;

        foreach (var room in Map.Rooms) {
            room.Render(Camera, room == CurrentRoom);
        }

        ToolHandler.Render(Camera, CurrentRoom);

        GFX.BeginBatch();
        PicoFont.Print(RysyEngine.CurrentFPS.ToString("FPS:0"), new Vector2(4, 68), Color.Pink, 4);
        GFX.EndBatch();

        var input = Input.Global;

        if (input.Keyboard.Ctrl()) {
            // Reload everything
            if (input.Keyboard.IsKeyClicked(Keys.F5)) {
                RysyEngine.QueueReload();
            }
        } else {
            // clear render cache
            if (input.Keyboard.IsKeyClicked(Keys.F4)) {
                foreach (var room in Map.Rooms)
                    room.ClearRenderCache();
            }

            // Reload textures
            if (input.Keyboard.IsKeyClicked(Keys.F5)) {
                GFX.Atlas.DisposeTextures();
                foreach (var room in Map.Rooms)
                    room.ClearRenderCache();
                GC.Collect(3);
            }

            // Re-register entities
            if (input.Keyboard.IsKeyClicked(Keys.F6)) {
                EntityRegistry.RegisterAsync().AsTask().Wait();
                CurrentRoom.ClearRenderCache();
                GC.Collect(3);
            }
        }
    }

    public override void RenderImGui() {
        base.RenderImGui();

        Menubar.Render(this);
        RoomList.Render(this);
        ToolHandler.RenderGui();
    }

    public void CenterCameraOnRoom(Room room) {
        Camera.CenterOnRealPos(room.Bounds.Center.ToVector2());
    }
}
