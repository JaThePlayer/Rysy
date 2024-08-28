using ImGuiNET;
using Rysy.Mods;
using Rysy.Platforms;
using System.Text.Json;

namespace Rysy.Gui;
public static class ImGuiThemer {
    static ImGuiThemer() {
        Settings.OnLoaded += (s) => {
            //SetFontSize(s.FontSize);
        };
    }

    public static unsafe void LoadTheme(string filename) {
        var fs = RysyPlatform.Current.GetRysyFilesystem();
        if (!fs.FileExists(filename)) {
            var internalPath = $"themes/{filename}.json";
            if (fs.FileExists(internalPath)) {
                filename = internalPath;
            } else {
                Logger.Write("ImGuiThemer", LogLevel.Warning, $"Theme doesn't exist: {filename}.");
                return;
            }
        }

        if (fs.TryReadAllText(filename) is {} themeJson) {
            ImGuiStylePtr ptr = ImGui.GetStyle();
            ImGuiStyle s = JsonSerializer.Deserialize<ImGuiStyle>(themeJson, new JsonSerializerOptions() {
                IncludeFields = true,
            });
            var nptr = ptr.NativePtr;
            *nptr = s;

            nptr->DockingSeparatorSize = 1;
            nptr->SeparatorTextAlign = new(.5f, .5f);
            nptr->SeparatorTextBorderSize = 1;
        }
    }

    public static unsafe void SetFontSize(float fontSize) {
        var io = ImGui.GetIO();
        io.Fonts.Clear();
        
        BoldFont = AddFont("RobotoMono-Bold.ttf", fontSize);
        DefaultFont = AddFont("RobotoMono-Regular.ttf", fontSize);
        ItalicFont = AddFont("RobotoMono-Italic.ttf", fontSize);
        ItalicBoldFont = AddFont("RobotoMono-BoldItalic.ttf", fontSize);
        
        HeaderFont = AddFont("RobotoMono-Bold.ttf", fontSize * 2f);
        Header2Font = AddFont("RobotoMono-Bold.ttf", fontSize * 1.5f);

        io.Fonts.Build();
        ImGuiManager.GuiResourceManager.BuildFontAtlas();
        
        ImFontPtr AddFont(string name, float size) {
            var fs = RysyPlatform.Current.GetRysyFilesystem();

            if (File.Exists($"{fs.Root}/{name}")) {
                return io.Fonts.AddFontFromFileTTF($"{fs.Root}/{name}", size);
            }

            Console.WriteLine("using slow fallback for font loading...");
            // TODO: fix this to load from memory
            if (RysyPlatform.Current.GetRysyFilesystem().TryReadAllBytes(name) is { } bytes) {
                var temp = Path.GetTempFileName();
                File.WriteAllBytes(temp, bytes);
                var ret = io.Fonts.AddFontFromFileTTF(temp, size);
                File.Delete(temp);
                return ret;

                //var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                //fixed (byte* bPtr = &bytes[0])
                //    return io.Fonts.AddFontFromMemoryTTF((nint)bPtr, bytes.Length, size);
            }

            Console.WriteLine($"FAILED: {name}");
            return default;
        }
    }

    public static ImFontPtr DefaultFont { get; private set; }
    public static ImFontPtr BoldFont { get; private set; }
    public static ImFontPtr ItalicFont { get; private set; }
    public static ImFontPtr ItalicBoldFont { get; private set; }
    public static ImFontPtr HeaderFont { get; private set; }
    public static ImFontPtr Header2Font { get; private set; }
}
