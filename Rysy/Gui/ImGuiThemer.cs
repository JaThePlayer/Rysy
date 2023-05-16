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
    }

    public static unsafe void SetFontSize(float fontSize) {
        var io = ImGui.GetIO();
        io.Fonts.Clear();

        string? fontFile = Settings.Instance?.FontFile;
        if (fontFile is null || (Settings.Instance is { } settings && !File.Exists(settings.FontFile))) {
            if (File.Exists("C:/Windows/Fonts/consola.ttf")) {
                //fontFile = "C:/Windows/Fonts/consola.ttf";
            }
        }
        fontFile ??= "Assets/RobotoMono-Bold.ttf";

        io.Fonts.AddFontFromFileTTF(fontFile, fontSize);
        io.Fonts.Build();
        ImGuiManager.GuiRenderer.BuildFontAtlas();
    }
}
