using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Runtime.InteropServices;
using YamlDotNet.Core.Tokens;

namespace Rysy.Gui;

public static class ImGuiManager {
    public static ImGuiRenderer GuiRenderer;

    public static float MenubarHeight;

    public static uint CentralDockingSpaceID;

    public static ImGuiWindowFlags WindowFlagsResizable =>
        //ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoScrollbar |
        //ImGuiWindowFlags.NoResize |
        //ImGuiWindowFlags.NoTitleBar |
        //ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.None;

    public static ImGuiWindowFlags WindowFlagsUnresizable =>
        //ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoResize |
        //ImGuiWindowFlags.NoTitleBar |
        //ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.None;

    public static ImGuiTableFlags TableFlags =>
        ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | 
        ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | 
        ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.Hideable;

    public static unsafe void Load(RysyEngine game) {
        GuiRenderer = new ImGuiRenderer(game);

        ImGuiThemer.SetFontSize(16f);

        RysyEngine.ImGuiAvailable = true;
    }

    public static void PushWindowStyle() {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
    }

    public static void PopWindowStyle() {
        ImGui.PopStyleVar(3);
    }

    /// <summary>
    /// Calls <see cref="PushInvalidStyle"/> if <paramref name="condition"/> is true.
    /// Returns <paramref name="condition"/>
    /// </summary>
    public static bool PushInvalidStyleIf(bool condition) {
        if (condition)
            PushInvalidStyle();

        return condition;
    }

    private static bool _invalidStyleEnabled;
    public static void PushInvalidStyle() {
        ImGui.PushStyleColor(ImGuiCol.Text, new NumVector4(255, 0, 0, 255));
        ImGui.PushStyleColor(ImGuiCol.Border, new NumVector4(255, 0, 0, 255));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1);
        _invalidStyleEnabled = true;
    }

    public static void PopInvalidStyle() {
        if (_invalidStyleEnabled) {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
            _invalidStyleEnabled = false;
        }
    }

    private static bool _editedStylePushed;
    public static void PushEditedStyle() {
        ImGui.PushStyleColor(ImGuiCol.Text, new NumVector4(0, 255, 0, 255));
        ImGui.PushStyleColor(ImGuiCol.Border, new NumVector4(0, 255, 0, 255));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1);
        _editedStylePushed = true;
    }

    public static void PopEditedStyle() {
        if (_editedStylePushed) {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
            _editedStylePushed = false;
        }
    }

    private static bool _nullStylePushed;
    public static void PushNullStyle() {
        var color = (Color.LightGray * 0.8f).ToNumVec4();

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushStyleColor(ImGuiCol.Border, color);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1);
        _nullStylePushed = true;
    }

    public static void PopNullStyle() {
        if (_nullStylePushed) {
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
            _nullStylePushed = false;
        }
    }

    public struct StyleHolder {
        public bool Null { get; set; }
        public bool Edited { get; set; }
        public bool Invalid { get; set; }
    }

    public static void PushAllStyles(StyleHolder holder) {
        if (holder.Null) {
            PushNullStyle();
        } else {
            PopNullStyle();
        }

        if (holder.Edited) {
            PushEditedStyle();
        } else {
            PopEditedStyle();
        }

        if (holder.Invalid) {
            PushInvalidStyle();
        } else {
            PopInvalidStyle();
        }
    }

    public static StyleHolder PopAllStyles() {
        var holder = new StyleHolder() {
            Null = _nullStylePushed,
            Edited = _editedStylePushed,
            Invalid = _invalidStyleEnabled,
        };

        PopEditedStyle();
        PopInvalidStyle();
        PopNullStyle();

        return holder;
    }

    public static void List<T>(IEnumerable<T> source, Func<T, string> itemNameGetter, ComboCache<T>? cache, Action<T> onClick, HashSet<string>? favorites = null) {
        cache ??= new();
        var search = cache.Search;

        if (ImGui.InputText("Search", ref search, 512)) {
            cache.Search = search;
        }

        var filtered = cache.GetValue(source, itemNameGetter, search, favorites);

        foreach (var item in filtered) {
            var name = itemNameGetter(item);
            if (ImGui.MenuItem(favorites?.Contains(name) ?? false ? $"* {name}" : name)) {
                onClick(item);
            }
        }
    }

    /// <summary>
    /// Creates a menu with <see cref="ImGui.BeginMenu(string)"/> using elements from the <paramref name="source"/>.
    /// </summary>
    public static void DropdownMenu<T>(string name, IEnumerable<T> source, Func<T, string> itemNameGetter, Action<T> onClick) {
        if (ImGui.BeginMenu(name)) {
            foreach (var item in source) {
                if (ImGui.MenuItem(itemNameGetter(item) ?? "[null]")) {
                    onClick(item);
                }
            }
            ImGui.EndMenu();
        }
    }

    public static void DropdownMenu<T>(string name, Action<T> onClick) where T : struct, Enum {
        var values = Enum.GetValues<T>();

        if (ImGui.BeginMenu(name)) {
            foreach (var item in values) {
                if (ImGui.MenuItem(item.ToString())) {
                    onClick(item);
                }
            }
            ImGui.EndMenu();
        }
    }

    public static void Combo<T>(string name, ref T value) where T : struct, Enum {
        var values = Enum.GetValues<T>();

        if (ImGui.BeginCombo(name, value.ToString())) {
            foreach (var item in values) {
                if (ImGui.MenuItem(item.ToString())) {
                    value = item;
                }
            }
            ImGui.EndCombo();
        }
    }

    public static bool Combo<T>(string name, ref T value, IDictionary<T, string> values, ref string search, string? tooltip = null, ComboCache<T>? cache = null) where T : notnull {
        if (!values.TryGetValue(value, out var valueName)) {
            valueName = value!.ToString();
        }

        bool changed = false;

        if (ImGui.BeginCombo(name, valueName).WithTooltip(tooltip)) {
            ImGui.InputText("Search", ref search, 512);

            cache ??= new();
            var filtered = cache.GetValue(values, search);

            foreach (var item in filtered) {
                if (ImGui.MenuItem(item.Value)) {
                    value = item.Key;
                    changed = true;
                }
            }

            ImGui.EndCombo();
        }

        return changed;
    }

    public static bool Combo<T>(string name, ref T value, IList<T> values, Func<T, string> toString, string? tooltip = null, ImGuiComboFlags flags = ImGuiComboFlags.None) where T : notnull {
        var valueName = toString(value);
        bool changed = false;

        if (ImGui.BeginCombo(name, valueName, flags).WithTooltip(tooltip)) {
            foreach (var item in values) {
                if (ImGui.MenuItem(toString(item))) {
                    value = item;
                    changed = true;
                }
            }

            ImGui.EndCombo();
        }

        return changed;
    }

    public static bool EditableCombo<T>(string name, ref T value, IDictionary<T, string> values, Func<string, T> stringToValue, ref string search, 
        string? tooltip = null, ComboCache<T>? cache = null)
        where T : notnull {

        bool changed = false;
        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        var valueToString = value.ToString() ?? "";
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText($"##text{name}", ref valueToString, 128).WithTooltip(tooltip)) {
            value = stringToValue(valueToString);
            changed = true;
        }

        cache ??= new();

        ImGui.SameLine(0f, xPadding);

        var size = cache.Size ??= CalcListSize(values.Values);
        ImGui.SetNextWindowSize(new(size.X, (ImGui.GetTextLineHeightWithSpacing() + ImGui.GetFrameHeight()) * 120.AtMost(values.Count)));
        if (ImGui.BeginCombo($"##combo{name}", valueToString, ImGuiComboFlags.NoPreview).WithTooltip(tooltip)) {

            ImGui.InputText("Search", ref search, 512);

            ImGui.BeginChild($"comboInner{name}");

            var filtered = cache.GetValue(values, search);

            foreach (var item in filtered) {
                if (ImGui.MenuItem(item.Value)) {
                    value = item.Key;
                    changed = true;
                }
            }
            ImGui.EndChild();
            ImGui.EndCombo();
        }
        ImGui.SameLine(0f, xPadding);
        ImGui.Text(name);
        true.WithTooltip(tooltip);

        return changed;
    }

    public static bool ColorEdit(string label, ref Color color, ColorFormat format, string? tooltip) {
        var colorHex = ColorHelper.ToString(color, format);
        bool edited = false;

        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText($"##text{label}", ref colorHex, 24).WithTooltip(tooltip)) {
            if (ColorHelper.TryGet(colorHex, format, out var newColor)) {
                color = newColor;
            }
            edited = true;
        }

        ImGui.SameLine(0f, xPadding);

        switch (format) {
            case ColorFormat.RGB:
                var colorN3 = color.ToNumVec3();
                if (ImGui.ColorEdit3($"##combo{label}", ref colorN3, ImGuiColorEditFlags.NoInputs).WithTooltip(tooltip)) {
                    color = new Color(colorN3);
                    edited = true;
                }
                break;
            case ColorFormat.RGBA:
            case ColorFormat.ARGB:
                var colorN4 = color.ToNumVec4();
                if (ImGui.ColorEdit4($"##combo{label}", ref colorN4, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs).WithTooltip(tooltip)) {
                    color = new Color(colorN4);
                    edited = true;
                }
                break;
            default:
                break;
        }


        ImGui.SameLine(0f, xPadding);
        ImGui.Text(label);
        true.WithTooltip(tooltip);

        return edited;
    }

    public static void BeginWindowBottomBar(bool valid) {
        var height = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().FramePadding.Y;
        var posy = ImGui.GetWindowHeight() - height - ImGui.GetStyle().FramePadding.Y * 2f;

        ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new NumVector2(ImGui.GetStyle().FramePadding.X * 2, posy));

        ImGui.BeginChild(1, new(0, height - ImGui.GetStyle().FramePadding.Y), false, ImGuiWindowFlags.Modal);
        ImGui.Separator();
        ImGui.BeginDisabled(!valid);
    }

    public static void EndWindowBottomBar() {
        ImGui.EndDisabled();
        ImGui.EndChild();
    }

    public static NumVector2 CalcListSize(IEnumerable<string> strings) {
        int i = 1;
        string longest = "";

        foreach (var str in strings) {
            if (str.Length > longest.Length)
                longest = str;

            i++;
        }

        var style = ImGui.GetStyle();

        return new(
            ImGui.CalcTextSize(longest).X + style.WindowPadding.X * 2 + style.ItemSpacing.X,
            ImGui.GetTextLineHeightWithSpacing() * i + ImGui.GetFrameHeightWithSpacing() * 2
        );
    }


    private static Dictionary<string, (RenderTarget2D Target, nint ID)> Targets = new();
    public static void XnaWidget(string id, int w, int h, Action renderFunc, Camera? camera = null) {
        if (w <= 0 || h <= 0)
            return;

        if (!Targets.TryGetValue(id, out var t) || t.Target.Width != w || t.Target.Height != h) {
            if (t.Target != null) {
                GuiRenderer.UnbindTexture(t.ID);
                t.Target.Dispose();
            }

            t.Target = new(RysyEngine.GDM.GraphicsDevice, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            t.ID = GuiRenderer.BindTexture(t.Target);
            Targets[id] = t;
        }

        ImGui.Image(t.ID, new(w, h));

        var g = RysyEngine.GDM.GraphicsDevice;
        g.SetRenderTarget(t.Target);
        g.Clear(Color.Transparent);
        GFX.BeginBatch(camera);
        renderFunc();
        GFX.EndBatch();
        g.SetRenderTarget(null);
    }

    // Mostly taken from https://github.com/woofdoggo/Starforge/blob/main/Starforge/Core/Interop/ImGuiRenderer.cs
    public class ImGuiRenderer {
        private RasterizerState RasterizerState;
        private RysyEngine Engine;
        private GraphicsDevice GraphicsDevice;
        private BasicEffect Effect;

        private byte[] VertexData;
        private VertexBuffer VertexBuffer;
        private int VertexBufferSize;

        private byte[] IndexData;
        private IndexBuffer IndexBuffer;
        private int IndexBufferSize;

        private Dictionary<IntPtr, Texture2D> Textures;
        private int TextureID;
        private IntPtr? FontTextureID;

        private int ScrollWheelValue;
        private List<int> ImGUIKeys = new List<int>();

        public ImGuiRenderer(RysyEngine engine) {
            //File.Delete("imgui.ini");

            IntPtr ctx = ImGui.CreateContext();
            ImGui.SetCurrentContext(ctx);

            EnableDocking();

            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            GraphicsDevice = RysyEngine.Instance.GraphicsDevice;
            Textures = new Dictionary<IntPtr, Texture2D>();

            RasterizerState = new RasterizerState() {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0
            };

            SetupInput();
        }

        private static void EnableDocking() {
            var io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigDockingAlwaysTabBar = true;
            io.ConfigDockingTransparentPayload = true;
        }

        public unsafe void BuildFontAtlas() {
            // Get ImGUI font texture
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bpp);

            // Copy data to managed array
            byte[] pixels = new byte[width * height * bpp];
            unsafe {
                Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);
            }

            // Create XNA texture of font
            Texture2D fontTex = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);
            fontTex.SetData(pixels);

            // Deallocate and unbind any previously built font texture
            if (FontTextureID.HasValue)
                UnbindTexture(FontTextureID.Value);

            // Bind font texture to ImGUI
            FontTextureID = BindTexture(fontTex);

            io.Fonts.SetTexID(FontTextureID.Value);
            io.Fonts.ClearTexData();
        }

        public IntPtr BindTexture(Texture2D tex) {
            IntPtr id = new IntPtr(TextureID++);
            Textures.Add(id, tex);
            return id;
        }

        public void UnbindTexture(IntPtr texPtr) {
            Textures.Remove(texPtr);
        }

        public void BeforeLayout(GameTime gt) {
            ImGui.GetIO().DeltaTime = (float) gt.ElapsedGameTime.TotalSeconds;
            if (RysyEngine.Instance.IsActive)
                UpdateInput();
            ImGui.NewFrame();

            // allow docking windows to the sides of the window
            CentralDockingSpaceID = ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingInCentralNode);
        }

        public void AfterLayout() {
            ImGui.Render();
            unsafe {
                RenderDrawData(ImGui.GetDrawData());
            }
        }

        protected void SetupInput() {
            ImGuiIOPtr io = ImGui.GetIO();

            // Bind XNA keys to ImGUI keys
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Tab] = (int) Keys.Tab);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Keys.Left);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Keys.Right);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Keys.Up);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Keys.Down);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.PageUp] = (int) Keys.PageUp);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.PageDown] = (int) Keys.PageDown);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Home] = (int) Keys.Home);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.End] = (int) Keys.End);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Delete] = (int) Keys.Delete);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Backspace] = (int) Keys.Back);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Enter] = (int) Keys.Enter);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Escape] = (int) Keys.Escape);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Space] = (int) Keys.Space);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.A] = (int) Keys.A);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.C] = (int) Keys.C);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.V] = (int) Keys.V);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.X] = (int) Keys.X);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Y] = (int) Keys.Y);
            ImGUIKeys.Add(io.KeyMap[(int) ImGuiKey.Z] = (int) Keys.Z);

            /*
            TextInputEXT.TextInput += c => {
                if (c == '\t')
                    return;
                io.AddInputCharacter(c);
            };

            unsafe {
                io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(ClipboardDelegates.SDL_Get);
            }*/

            RysyEngine.Instance.Window.TextInput += (object? sender, TextInputEventArgs e) => {
                const char VOLUME_UP = (char) 128;
                const char VOLUME_DOWN = (char) 129;

                var c = e.Character;
                if (c is '\t' or VOLUME_UP or VOLUME_DOWN)
                    return;

                io.AddInputCharacter(c);
            };

            io.Fonts.AddFontDefault();
        }

        protected Effect UpdateEffect(Texture2D texture) {
            Effect = Effect ?? new BasicEffect(GraphicsDevice);
            ImGuiIOPtr io = ImGui.GetIO();

            Effect.World = Matrix.Identity;
            Effect.View = Matrix.Identity;
            Effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
            Effect.TextureEnabled = true;
            Effect.Texture = texture;
            Effect.VertexColorEnabled = true;

            return Effect;
        }

        protected void UpdateInput() {
            // Make sure the window is focused before responding to input.
            if (!RysyEngine.Instance.IsActive)
                return;

            ImGuiIOPtr io = ImGui.GetIO();

            MouseState m = Mouse.GetState();
            KeyboardState kbd = Keyboard.GetState();

            for (int i = 0; i < ImGUIKeys.Count; i++) {
                io.KeysDown[ImGUIKeys[i]] = kbd.IsKeyDown((Keys) ImGUIKeys[i]);
            }

            io.KeyShift = kbd.IsKeyDown(Keys.LeftShift) || kbd.IsKeyDown(Keys.RightShift);
            io.KeyCtrl = kbd.IsKeyDown(Keys.LeftControl) || kbd.IsKeyDown(Keys.RightControl);
            io.KeyAlt = kbd.IsKeyDown(Keys.LeftAlt) || kbd.IsKeyDown(Keys.RightAlt);
            io.KeySuper = kbd.IsKeyDown(Keys.LeftWindows) || kbd.IsKeyDown(Keys.RightWindows);

            io.DisplaySize = new System.Numerics.Vector2(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
            io.MousePos = new System.Numerics.Vector2(m.X, m.Y);

            io.MouseDown[0] = m.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = m.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = m.MiddleButton == ButtonState.Pressed;

            int scrollDelta = m.ScrollWheelValue - ScrollWheelValue;
            io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
            ScrollWheelValue = m.ScrollWheelValue;
        }

        private void RenderDrawData(ImDrawDataPtr ptr) {
            Viewport lastViewport = GraphicsDevice.Viewport;
            Rectangle lastScissor = GraphicsDevice.ScissorRectangle;

            GraphicsDevice.BlendFactor = Color.White;
            GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            GraphicsDevice.RasterizerState = RasterizerState;
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            ptr.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
            GraphicsDevice.Viewport = new Viewport(0, 0, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            UpdateBuffers(ptr);
            RenderCommandLists(ptr);

            // Restore graphics state
            GraphicsDevice.Viewport = lastViewport;
            GraphicsDevice.ScissorRectangle = lastScissor;
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr ptr) {
            if (ptr.TotalVtxCount == 0)
                return;

            // Make vertex/index buffers larger if needed
            if (ptr.TotalVtxCount > VertexBufferSize) {
                if (VertexBuffer != null)
                    VertexBuffer.Dispose();

                VertexBufferSize = (int) (ptr.TotalVtxCount * 1.5f);
                VertexBuffer = new VertexBuffer(GraphicsDevice, DrawVertDeclaration.Declaration, VertexBufferSize, BufferUsage.None);
                VertexData = new byte[VertexBufferSize * DrawVertDeclaration.Size];
            }

            if (ptr.TotalIdxCount > IndexBufferSize) {
                if (IndexBuffer != null)
                    IndexBuffer.Dispose();

                IndexBufferSize = (int) (ptr.TotalIdxCount * 1.5f);
                IndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, IndexBufferSize, BufferUsage.None);
                IndexData = new byte[IndexBufferSize * sizeof(ushort)];
            }

            // Copy draw data to managed byte arrays
            int vtxOffset = 0;
            int idxOffset = 0;

            for (int i = 0; i < ptr.CmdListsCount; i++) {
                ImDrawListPtr cmdList = ptr.CmdListsRange[i];
                fixed (void* vtxDstPtr = &VertexData[vtxOffset * DrawVertDeclaration.Size]) {
                    fixed (void* idxDstPtr = &IndexData[idxOffset * sizeof(ushort)]) {
                        Buffer.MemoryCopy((void*) cmdList.VtxBuffer.Data, vtxDstPtr, VertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                        Buffer.MemoryCopy((void*) cmdList.IdxBuffer.Data, idxDstPtr, IndexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
                    }
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            // Copy byte arrays to GPU
            VertexBuffer.SetData(VertexData, 0, ptr.TotalVtxCount * DrawVertDeclaration.Size);
            IndexBuffer.SetData(IndexData, 0, ptr.TotalIdxCount * sizeof(ushort));
        }

        private unsafe void RenderCommandLists(ImDrawDataPtr ptr) {
            GraphicsDevice.SetVertexBuffer(VertexBuffer);
            GraphicsDevice.Indices = IndexBuffer;

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int i = 0; i < ptr.CmdListsCount; i++) {
                ImDrawListPtr cmdList = ptr.CmdListsRange[i];

                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++) {
                    ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdi];
                    if (!Textures.ContainsKey(cmd.TextureId)) {
                        throw new InvalidOperationException($"Could not find ImGUI texture with ID {cmd.TextureId}");
                    }

                    GraphicsDevice.ScissorRectangle = new Rectangle(
                        (int) cmd.ClipRect.X,
                        (int) cmd.ClipRect.Y,
                        (int) (cmd.ClipRect.Z - cmd.ClipRect.X),
                        (int) (cmd.ClipRect.W - cmd.ClipRect.Y)
                    );

                    Effect e = UpdateEffect(Textures[cmd.TextureId]);
                    for (int passIndex = 0; passIndex < e.CurrentTechnique.Passes.Count; passIndex++) {
                        EffectPass pass = e.CurrentTechnique.Passes[passIndex];
                        pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(
                            primitiveType: PrimitiveType.TriangleList,
                            baseVertex: vtxOffset,
                            startIndex: idxOffset,
                            primitiveCount: (int) cmd.ElemCount / 3
                        );
                    }

                    idxOffset += (int) cmd.ElemCount;
                }

                vtxOffset += cmdList.VtxBuffer.Size;
            }
        }
    }

    public static class DrawVertDeclaration {
        public static readonly VertexDeclaration Declaration;

        public static readonly int Size;

        static DrawVertDeclaration() {
            unsafe {
                Size = sizeof(ImDrawVert);
            }

            Declaration = new VertexDeclaration(
                Size,
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0), // Position
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), // UV
                new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );
        }
    }
}
