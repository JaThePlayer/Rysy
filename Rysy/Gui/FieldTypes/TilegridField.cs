using Hexa.NET.ImGui;
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
    private TileLayer _layer;

    public TilegridField(TileLayer layer = TileLayer.Fg) {
        Default = "";
        _layer = layer;

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

        var xPadding = ImGui.GetStyle().FramePadding.X;

        if (ImGui.Button($"Edit##{fieldName}").WithTooltip(Tooltip) && EditorState.Map is { } map) {
            if (_window is not { }) {
                _window = new(val, TileEntity.GetAutotiler(map, _layer) ?? new(), Context, _layer, this);
                _window.SetRemoveAction((w) => _window = null);
                RysyEngine.Scene.AddWindow(_window);
            }
        }

        ImGui.SameLine(0f, xPadding);
        ImGui.Text(fieldName);
        true.WithTooltip(Tooltip);

        if (_window is { Edited: true }) {
            _window.Edited = false;

            var (width, height) = (_window.Width, _window.Height);
            var tiles = _window.Tiles;

            var trimmedTiles = tiles.CreateTrimmed('0', out int offX, out int offY);

            Context.SetValue("width", trimmedTiles.GetLength(0) * 8);
            Context.SetValue("height", trimmedTiles.GetLength(1) * 8);

            if (_window.ShouldClose)
                _window.RemoveSelf();

            return GridToSavedString(trimmedTiles);
        }

        return null;
    }

    public TilegridField WithTilegridParser(Func<string, int, int, char[,]> parser, Func<char[,], string> gridToSavedString) => this with {
        TilegridParser = parser,
        GridToSavedString = gridToSavedString,
    };

    private EditTileDataWindow? _window;

    public static char[,] DefaultTilegridParser(string tiles, int w, int h) => Tilegrid.TileArrayFromString(w * 8, h * 8, tiles);
    public static string DefaultGridToSavedString(char[,] tiles) => Tilegrid.GetSaveString(tiles);
}

internal sealed class EditTileDataWindow : Window {
    public bool Edited;

    private readonly string _xnaBufferId;

    private readonly Autotiler _autotiler;

    private AutotiledSpriteList? _sprites;

    private readonly Camera _camera;

    private readonly Input _input;

    private readonly HistoryHandler _history;

    private readonly ToolHandler _tools;

    private readonly HotkeyHandler _hotkeys;

    private readonly Room _fakeRoom;

    private TileLayer _layer;

    internal bool ShouldClose;

    internal Tilegrid Tilegrid => TileEntity.GetTilegrid(_fakeRoom, _layer);
    internal char[,] Tiles => TileEntity.GetTilegrid(_fakeRoom, _layer).Tiles;

    private readonly FormContext _formCtx;

    internal int Width {
        get => _formCtx.Int("width");
        set => _formCtx.SetValue("width", value);
    }

    internal int Height {
        get => _formCtx.Int("height");
        set => _formCtx.SetValue("height", value);
    }

    public EditTileDataWindow(string val, Autotiler autotiler, FormContext formCtx, TileLayer layer, TilegridField field) : base("Edit Tile Data") {
        _layer = layer;
        _formCtx = formCtx;
        _autotiler = autotiler;
        _xnaBufferId = Guid.NewGuid().ToString();

        _camera = new(new Viewport(0, 0, 800, 800));
        _camera.Scale = 2;

        _input = new();

        _input.Update(Time.Delta);

        _history = new(EditorState.Map ?? throw new Exception("Not in a map?"));

        _hotkeys = new(_input, HotkeyHandler.ImGuiModes.Ignore);

        _tools = new ToolHandler(_history, _input).UsePersistence(false);
        _tools.InitHotkeys(_hotkeys);
        _tools.CurrentTool.Layer = layer == TileLayer.Fg ? EditorLayers.Fg : EditorLayers.Bg;

        _hotkeys.AddHistoryHotkeys(Undo, Redo, Save);

        _hotkeys.AddHotkeyFromSettings("selection.upsizeLeft", "a", CreateUpsizeHandler(new(8, 0), new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        _hotkeys.AddHotkeyFromSettings("selection.upsizeRight", "d", CreateUpsizeHandler(new(8, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        _hotkeys.AddHotkeyFromSettings("selection.upsizeTop", "w", CreateUpsizeHandler(new(0, 8), new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        _hotkeys.AddHotkeyFromSettings("selection.upsizeBottom", "s", CreateUpsizeHandler(new(0, 8), new()), HotkeyModes.OnHoldSmoothInterval);

        _hotkeys.AddHotkeyFromSettings("selection.downsizeRight", "shift+d", CreateUpsizeHandler(new(-8, 0), new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        _hotkeys.AddHotkeyFromSettings("selection.downsizeLeft", "shift+a", CreateUpsizeHandler(new(-8, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        _hotkeys.AddHotkeyFromSettings("selection.downsizeBottom", "shift+s", CreateUpsizeHandler(new(0, -8), new(0, 8)), HotkeyModes.OnHoldSmoothInterval);
        _hotkeys.AddHotkeyFromSettings("selection.downsizeTop", "shift+w", CreateUpsizeHandler(new(0, -8), new()), HotkeyModes.OnHoldSmoothInterval);

        _camera.CreateCameraHotkeys(_hotkeys);
        
        var tiles = field.TilegridParser(val, Width / 8, Height / 8);
        _fakeRoom = new(EditorState.Map!, tiles.GetLength(0), tiles.GetLength(1));

        if (layer == TileLayer.Fg) {
            _fakeRoom.Fg.Tiles = tiles;
        } else {
            _fakeRoom.Bg.Tiles = tiles;
        }

        _fakeRoom.Fg.RenderCacheToken!.OnInvalidate += () => {
            _sprites = null;
        };
        _fakeRoom.Bg.RenderCacheToken!.OnInvalidate += () => {
            _sprites = null;
        };

        var clientBounds = RysyState.Window.ClientBounds;
        Size = new((Width * _camera.Scale).AtMost(clientBounds.Width - 600) + 300, (Height * _camera.Scale).AtMost(clientBounds.Height - 400) + ImGui.GetTextLineHeightWithSpacing() * 4f);

        if (RysyEngine.Scene is { } scene) {
            // we need to lock global hotkeys while hovering over this window, as otherwise ctrl+z would undo changes outside the window as well
            _globalHotkeyLock = scene.HotkeysIgnoreImGui.LockManager.CreateLock();
        }

        _history.OnApply += () => Edited = true;
        _history.OnUndo += () => Edited = true;
    }

    public override void RemoveSelf() {
        _globalHotkeyLock?.Release();

        base.RemoveSelf();
    }

    private ManagedLock? _globalHotkeyLock;

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

        _camera.Move(-move);
    };

    private void Undo() => _history.Undo();
    private void Redo() => _history.Redo();

    protected override void Render() {
        base.Render();

        var pos = ImGui.GetWindowPos() + ImGui.GetCursorPos();
        _input.Mouse.Offset = new((int) -pos.X, (int) -pos.Y);
        _input.Update(Time.Delta);

        if (ImGui.IsWindowHovered()) {
            _hotkeys.Update();
            _globalHotkeyLock?.SetActive(true);

            _camera.Viewport = new Viewport((int) pos.X, (int) pos.Y, 800, 800);
            _camera.HandleMouseMovement(_input);

            _tools.Update(_camera, _fakeRoom);

            NoMove = true;
        } else {
            NoMove = false;
            _globalHotkeyLock?.SetActive(false);
        }

        var imgHeight = ImGui.GetContentRegionAvail().Y.AtLeast(1);
        var imgWidth = ((int) ImGui.GetContentRegionAvail().X - 200).AtLeast(1);

        // prevent scrolling the internal part of the window
        ImGui.BeginChild("##", new(), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoInputs);
        ImGuiManager.XnaWidget(_xnaBufferId, imgWidth, (int) imgHeight, () => {
            _sprites = Tilegrid.GetSprites();// Autotiler.GetSprites(default, Tiles, Color.White).ToList();

            ISprite.OutlinedRect(default, Tilegrid.Width * 8, Tilegrid.Height * 8, Color.White * 0.1f, Color.White, outlineWidth: (int) (1f / _camera.Scale).AtLeast(1)).Render();

            var ctx = SpriteRenderCtx.Default();
            _sprites.Render(ctx);

            _tools.CurrentTool.Render(_camera, _fakeRoom);
        }, _camera);
        ImGui.SameLine();
        _tools.CurrentTool.RenderGui(new(ImGui.GetContentRegionAvail().X, imgHeight + ImGui.GetFrameHeightWithSpacing()), id: "##fancyTileToolList");
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
        ShouldClose = true;
    }
}

