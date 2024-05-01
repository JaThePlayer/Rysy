using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Scenes;
using Rysy.Tools;

namespace Rysy.Gui.FieldTypes;

public record class TilegridField : Field {
    private TileLayer Layer;

    public TilegridField(TileLayer layer = TileLayer.FG) {
        Default = "";
        Layer = layer;

        TilegridParser ??= DefaultTilegridParser;
        GridToSavedString ??= DefaultGridToSavedString;
    }

    public string Default { get; set; }

    public Func<string, int, int, char[,]> TilegridParser { get; set; }

    public Func<char[,], string> GridToSavedString { get; set; }

    public override Field CreateClone() => this with { };

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault) => Default = newDefault.ToString() ?? "";

    public override object? RenderGui(string fieldName, object value) {
        string val = value.ToString() ?? "g";
        if (string.IsNullOrWhiteSpace(val)) {
            val = Default;
        }

        bool edited = false;

        var xPadding = ImGui.GetStyle().FramePadding.X;

        if (ImGui.Button($"Edit##{fieldName}").WithTooltip(Tooltip) && EditorState.Map is { } map) {
            if (Window is not { }) {
                Window = new(val, TileEntity.GetAutotiler(map, Layer) ?? new(), Context, Layer, this);
                Window.SetRemoveAction((w) => Window = null);
                RysyEngine.Scene.AddWindow(Window);
            }
        }

        ImGui.SameLine(0f, xPadding);
        ImGui.Text(fieldName);
        true.WithTooltip(Tooltip);

        if (Window is { Edited: true }) {
            Window.Edited = false;

            var (width, height) = (Window.Width, Window.Height);
            var tiles = Window.Tiles;

            var trimmedTiles = tiles.CreateTrimmed('0', out int offX, out int offY);

            Context.SetValue("width", trimmedTiles.GetLength(0) * 8);
            Context.SetValue("height", trimmedTiles.GetLength(1) * 8);

            Window.RemoveSelf();

            return GridToSavedString(trimmedTiles);
        }

        return null;
    }

    public TilegridField WithTilegridParser(Func<string, int, int, char[,]> parser, Func<char[,], string> gridToSavedString) => this with {
        TilegridParser = parser,
        GridToSavedString = gridToSavedString,
    };

    private EditTileDataWindow? Window;

    public static char[,] DefaultTilegridParser(string tiles, int w, int h) => Tilegrid.TileArrayFromString(w * 8, h * 8, tiles);
    public static string DefaultGridToSavedString(char[,] tiles) => Tilegrid.GetSaveString(tiles);
}

class EditTileDataWindow : Window {
    public bool Edited;

    private readonly string XnaBufferID;

    private readonly Autotiler Autotiler;

    private List<ISprite>? Sprites;

    private readonly Camera Camera;

    private readonly Input Input;

    private readonly HistoryHandler History;

    private readonly ToolHandler Tools;

    private readonly HotkeyHandler Hotkeys;

    private readonly Room FakeRoom;

    private TileLayer Layer;

    internal Tilegrid Tilegrid => TileEntity.GetTilegrid(FakeRoom, Layer);
    internal char[,] Tiles => TileEntity.GetTilegrid(FakeRoom, Layer).Tiles;

    private readonly FormContext FormCtx;

    internal int Width {
        get => (int) FormCtx.GetValue("width")!;
        set => FormCtx.SetValue("width", value);
    }

    internal int Height {
        get => (int) FormCtx.GetValue("height")!;
        set => FormCtx.SetValue("height", value);
    }

    public EditTileDataWindow(string val, Autotiler autotiler, FormContext formCtx, TileLayer layer, TilegridField field) : base("Edit Tile Data") {
        Layer = layer;
        FormCtx = formCtx;
        Autotiler = autotiler;
        XnaBufferID = Guid.NewGuid().ToString();

        Camera = new(new Viewport(0, 0, 800, 800));
        Camera.Scale = 2;

        Input = new();

        Input.Update(Time.Delta);

        History = new(EditorState.Map ?? throw new Exception("Not in a map?"));

        Hotkeys = new(Input, updateInImgui: true);

        Tools = new ToolHandler(History, Input).UsePersistence(false);
        Tools.InitHotkeys(Hotkeys);
        Tools.CurrentTool.Layer = layer == TileLayer.FG ? EditorLayers.Fg : EditorLayers.Bg;

        Hotkeys.AddHistoryHotkeys(Undo, Redo, Save);

        Hotkeys.AddHotkeyFromSettings("selection.upsizeLeft", "a", CreateUpsizeHandler(new(8, 0), new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("selection.upsizeRight", "d", CreateUpsizeHandler(new(8, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("selection.upsizeTop", "w", CreateUpsizeHandler(new(0, 8), new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("selection.upsizeBottom", "s", CreateUpsizeHandler(new(0, 8), new()), HotkeyModes.OnHoldSmoothInterval);

        Hotkeys.AddHotkeyFromSettings("selection.downsizeRight", "shift+d", CreateUpsizeHandler(new(-8, 0), new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("selection.downsizeLeft", "shift+a", CreateUpsizeHandler(new(-8, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("selection.downsizeBottom", "shift+s", CreateUpsizeHandler(new(0, -8), new(0, 8)), HotkeyModes.OnHoldSmoothInterval);
        Hotkeys.AddHotkeyFromSettings("selection.downsizeTop", "shift+w", CreateUpsizeHandler(new(0, -8), new()), HotkeyModes.OnHoldSmoothInterval);

        Camera.CreateCameraHotkeys(Hotkeys);
        
        var tiles = field.TilegridParser(val, Width / 8, Height / 8);
        FakeRoom = new(EditorState.Map!, tiles.GetLength(0), tiles.GetLength(1));

        if (layer == TileLayer.FG) {
            FakeRoom.FG.Tiles = tiles;
        } else {
            FakeRoom.BG.Tiles = tiles;
        }

        FakeRoom.FG.RenderCacheToken!.OnInvalidate += () => {
            Sprites = null;
        };
        FakeRoom.BG.RenderCacheToken!.OnInvalidate += () => {
            Sprites = null;
        };

        var clientBounds = RysyEngine.Instance.Window.ClientBounds;
        Size = new((Width * Camera.Scale).AtMost(clientBounds.Width - 600) + 300, (Height * Camera.Scale).AtMost(clientBounds.Height - 400) + ImGui.GetTextLineHeightWithSpacing() * 4f);

        if (RysyEngine.Scene is { } scene) {
            // we need to lock global hotkeys while hovering over this window, as otherwise ctrl+z would undo changes outside the window as well
            GlobalHotkeyLock = scene.HotkeysIgnoreImGui.LockManager.CreateLock();
        }
    }

    public override void RemoveSelf() {
        GlobalHotkeyLock?.Release();

        base.RemoveSelf();
    }

    Lock? GlobalHotkeyLock;

    private Action CreateUpsizeHandler(Point resize, Vector2 move) => () => {
        Width = (Width + resize.X).AtLeast(8);
        Height = (Height + resize.Y).AtLeast(8);

        var clone = (char[,]) Tiles.Clone();
        Tilegrid.Resize(Width, Height);

        var (mx, my) = ((int) move.X / 8, (int) move.Y / 8);

        if (mx == 0 && my == 0)
            return;

        var grid = Tilegrid;
        grid.Tiles.Fill('0');
        for (int x = 0; x < Width / 8; x++)
            for (int y = 0; y < Height / 8; y++) {
                var c = clone.GetOrDefault(x + mx, y + my, '0');

                grid.SafeReplaceTile(c, x, y, out char orig);
            }

        Camera.Move(-move);
    };

    private void Undo() => History.Undo();
    private void Redo() => History.Redo();

    protected override void Render() {
        base.Render();

        var size = ImGui.GetWindowSize();
        var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
        Input.Mouse.Offset = new((int) -pos.X, (int) -pos.Y);
        Input.Update(Time.Delta);

        if (ImGui.IsWindowHovered()) {
            Hotkeys.Update();
            GlobalHotkeyLock?.SetActive(true);

            Camera.Viewport = new Viewport((int) pos.X, (int) pos.Y, 800, 800);
            Camera.HandleMouseMovement(Input);

            Tools.Update(Camera, FakeRoom);

            NoMove = true;
        } else {
            NoMove = false;
            GlobalHotkeyLock?.SetActive(false);
        }

        var imgHeight = ImGui.GetWindowSize().Y.AtLeast(1);
        var imgWidth = ((int) ImGui.GetWindowSize().X - 200).AtLeast(1);

        // prevent scrolling the internal part of the window
        ImGui.BeginChild("", new(), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);
        ImGuiManager.XnaWidget(XnaBufferID, imgWidth, (int) imgHeight, () => {
            Sprites = Autotiler.GetSprites(default, Tiles, Color.White).ToList();

            ISprite.OutlinedRect(default, Tilegrid.Width * 8, Tilegrid.Height * 8, Color.White * 0.1f, Color.White, outlineWidth: (int) (1f / Camera.Scale).AtLeast(1)).Render();

            var ctx = SpriteRenderCtx.Default();
            foreach (var item in Sprites) {
                item.Render(ctx);
            }

            Tools.CurrentTool.Render(Camera, FakeRoom);
        }, Camera);
        ImGui.SameLine();
        Tools.CurrentTool.RenderGui(new(ImGui.GetWindowSize().X, imgHeight + ImGui.GetFrameHeightWithSpacing()), id: "##fancyTileToolList");
        ImGui.EndChild();

    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        base.RenderBottomBar();

        if (ImGui.Button("Save##asd")) {
            Save();
        }
    }

    private void Save() {
        Edited = true;
    }
}

