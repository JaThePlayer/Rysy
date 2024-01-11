using ImGuiNET;
using System.Text.Json;

namespace Rysy.Gui;
public static class ImGuiThemer {
    static ImGuiThemer() {
        Settings.OnLoaded += (s) => {
            //SetFontSize(s.FontSize);
        };
    }

    public static unsafe void LoadTheme(string filename) {
        if (!File.Exists(filename)) {
            var internalPath = $"Assets/themes/{filename}.json";
            if (File.Exists(internalPath)) {
                filename = internalPath;
            } else {
                Logger.Write("ImGuiThemer", LogLevel.Warning, $"Theme doesn't exist: {filename}.");
                return;
            }
        }

        ImGuiStylePtr ptr = ImGui.GetStyle();
        ImGuiStyle s = JsonSerializer.Deserialize<ImGuiStyle>(File.ReadAllText(filename), new JsonSerializerOptions() {
            IncludeFields = true,
        });
        var nptr = ptr.NativePtr;
        *nptr = s;

        nptr->DockingSeparatorSize = 1;
        nptr->SeparatorTextAlign = new(.5f, .5f);
        nptr->SeparatorTextBorderSize = 1;
    }

    public static unsafe void SetFontSize(float fontSize) {
        var io = ImGui.GetIO();
        io.Fonts.Clear();
        /*
        string? fontFile = Settings.Instance?.FontFile;
        if (fontFile is null || (Settings.Instance is { } settings && !File.Exists(settings.FontFile))) {
            if (File.Exists("C:/Windows/Fonts/consola.ttf")) {
                fontFile = "C:/Windows/Fonts/consola.ttf";
            }
        }*/

        BoldFont = io.Fonts.AddFontFromFileTTF("Assets/RobotoMono-Bold.ttf", fontSize);
        DefaultFont = io.Fonts.AddFontFromFileTTF("Assets/RobotoMono-Regular.ttf", fontSize);
        ItalicFont = io.Fonts.AddFontFromFileTTF("Assets/RobotoMono-Italic.ttf", fontSize);
        ItalicBoldFont = io.Fonts.AddFontFromFileTTF("Assets/RobotoMono-BoldItalic.ttf", fontSize);
        
        HeaderFont = io.Fonts.AddFontFromFileTTF("Assets/RobotoMono-Bold.ttf", fontSize * 2f);
        Header2Font = io.Fonts.AddFontFromFileTTF("Assets/RobotoMono-Bold.ttf", fontSize * 1.5f);

        io.Fonts.Build();
        ImGuiManager.GuiRenderer.BuildFontAtlas();
    }

    public static ImFontPtr DefaultFont { get; private set; }
    public static ImFontPtr BoldFont { get; private set; }
    public static ImFontPtr ItalicFont { get; private set; }
    public static ImFontPtr ItalicBoldFont { get; private set; }
    public static ImFontPtr HeaderFont { get; private set; }
    public static ImFontPtr Header2Font { get; private set; }
}
