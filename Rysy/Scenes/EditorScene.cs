using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.History;
using Rysy.Tools;

namespace Rysy.Scenes;

public sealed class EditorScene : Scene {
    public ToolHandler ToolHandler;

    public HistoryHandler HistoryHandler;

    private Map _map = null!;
    public Map Map {
        get => _map;
        set {
            _map = value;
            Camera = new();

            if (_map.Rooms.Count > 0) {
                CurrentRoom = _map.Rooms.First().Value;
                CenterCameraOnRoom(CurrentRoom);
            }

            Persistence.Instance.PushRecentMap(value);

            GC.Collect(3);
        }
    }

    private Room _currentRoom = null!; // will be set in Map.set
    public Room CurrentRoom {
        get => _currentRoom;
        set {
            _currentRoom = value;
            RysyEngine.ForceActiveTimer = 0.25f;
        }
    }

    public Camera Camera { get; set; } = null!; // will be set in Map.set

    public EditorScene() {
        HistoryHandler = new();
        ToolHandler = new(HistoryHandler);

        // Try to load the last edited map.
        if (!string.IsNullOrWhiteSpace(Persistence.Instance?.LastEditedMap))
            LoadMapFromBin(Persistence.Instance.LastEditedMap);
    }

    public EditorScene(Map map) : this() {
        Map = map;
    }

    public string CurrentRoomName {
        get => CurrentRoom.Name;
        set => CurrentRoom = Map.Rooms[value];
    }

    public override void OnFileDrop(FileDropEventArgs args) {
        base.OnFileDrop(args);

        var file = args.Files[0];
        if (File.Exists(file) && Path.GetExtension(file) == ".bin") {
            LoadMapFromBin(file);
        }
    }

    public void LoadMapFromBin(string file) {
        if (!File.Exists(file))
            return;

        RysyEngine.OnFrameEnd += async () => {
            //try {
                if (RysyEngine.Scene is not LoadingScene loadingScreen) {
                    loadingScreen = new LoadingScene();
                    RysyEngine.Scene = loadingScreen;
                }
                loadingScreen.SetText($"Loading map {file.TryCensor()}");
                
                // Just to make this run async, so we can see the loading screen.
                await Task.Delay(1);

                var mapBin = BinaryPacker.FromBinary(file);

                var map = Map.FromBinaryPackage(mapBin);
                Map = map;

                RysyEngine.Scene = this;
            //} catch {
            //
            //}
        };
    }

    public void LoadNewMap(string packageName) {
        Map = new() {
            Package = packageName,
        };
    }

    public override void Update() {
        base.Update();

        ImGuiIOPtr io = ImGui.GetIO();
        // Intentionally don't check for capturing keyboard, works weirdly with search bar, and can result in seemingly eaten inputs.
        if (io.WantCaptureMouse /*|| io.WantCaptureKeyboard*/) {
            return;
        }

        if (Map is { } && CurrentRoom is { }) {
            Camera.HandleMouseMovement();

            HandleRoomSwapInputs();

            HandleHistoryInput();

            ToolHandler.Update(Camera, CurrentRoom);
        }
    }

    private void HandleRoomSwapInputs() {
        if (Input.Mouse.Left.Clicked()) {
            var mousePos = Input.Mouse.Pos.ToVector2();
            foreach (var (_, room) in Map.Rooms) {
                if (room == CurrentRoom)
                    continue;

                var pos = room.WorldToRoomPos(Camera, mousePos);
                if (room.IsInBounds(pos)) {
                    CurrentRoom = room;

                    Input.Mouse.ConsumeLeft();
                    break;
                }
            }
        }
    }

    private double _smoothUndoNextInterval;
    private void HandleHistoryInput() {
        if (Input.Mouse.X1HoldTime > 0.2f && OnInterval(_smoothUndoNextInterval)
            || Input.Mouse.MouseX1 is MouseInputState.Clicked) {
            HistoryHandler.Undo();
            _smoothUndoNextInterval = NextInterval(Input.Mouse.X1HoldTime);
        } else if (Input.Mouse.X2HoldTime > 0.2f && OnInterval(_smoothUndoNextInterval) ||
              Input.Mouse.MouseX2 is MouseInputState.Clicked) {
            HistoryHandler.Redo();
            _smoothUndoNextInterval = NextInterval(Input.Mouse.X2HoldTime);
        } else {
            _smoothUndoNextInterval = 0;
        }

        double NextInterval(float holdTime) => 0.50 - (holdTime / 2.5f);
    }

    //TODO REMOVE
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

        foreach (var (_, room) in Map.Rooms) {
            room.Render(Camera, room == CurrentRoom);
        }

        ToolHandler.Render(Camera, CurrentRoom);

        GFX.BeginBatch();
        PicoFont.Print(RysyEngine.Framerate.ToString("FPS:0"), new Vector2(4, 68), Color.Pink, 4);
        GFX.EndBatch();

        if (Input.Keyboard.IsKeyClicked(Keys.Up)) {
            CurrentRoom = Map.Rooms.ElementAt(Map.Rooms.Values.ToList().IndexOf(CurrentRoom) + 1).Value;
            Logger.Write("DebugHotkey", LogLevel.Debug, $"Switching to room {CurrentRoom.Name}");
        }
        if (Input.Keyboard.IsKeyClicked(Keys.Down)) {
            CurrentRoom = Map.Rooms.ElementAt(Map.Rooms.Values.ToList().IndexOf(CurrentRoom) - 1).Value;
            Logger.Write("DebugHotkey", LogLevel.Debug, $"Switching to room {CurrentRoom.Name}");
        }

        if (Input.Keyboard.Ctrl()) {
            // Reload everything
            if (Input.Keyboard.IsKeyClicked(Keys.F5)) {
                Task.Run(async () => {
                    await RysyEngine.Instance.ReloadAsync();
                    GC.Collect(3);
                });
            }
        } else {
            // clear render cache
            if (Input.Keyboard.IsKeyClicked(Keys.F4)) {
                foreach (var item in Map.Rooms)
                    item.Value.ClearRenderCache();
            }

            // Reload textures
            if (Input.Keyboard.IsKeyClicked(Keys.F5)) {
                GFX.Atlas.DisposeTextures();
                foreach (var item in Map.Rooms)
                    item.Value.ClearRenderCache();
                GC.Collect(3);
            }

            // Re-register entities
            if (Input.Keyboard.IsKeyClicked(Keys.F6)) {
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
        ToolHandler.RenderGui(this);
    }

    public void CenterCameraOnRoom(Room room) {
        Camera.CenterOnRealPos(room.Bounds.Center.ToVector2());
    }
}
