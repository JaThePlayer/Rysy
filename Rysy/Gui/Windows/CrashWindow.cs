using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Gui.Windows;

public class CrashWindow : Window {
    public Exception Exception;
    public Action<CrashWindow> ButtonGenerator;
    public string Message;

    private string _exceptionString;

    public CrashWindow(string message, Exception e, Action<CrashWindow> buttonGenerator) : base("Crash Handler", new(800, 500)) {
        Exception = e;
        ButtonGenerator = buttonGenerator;
        Message = message;

        _exceptionString = Exception.ToString().Censor();

        try {
            // avoid future crashes
            Gfx.EndBatch();
        } catch {

        }
    }

    protected override void Render() {
        base.Render();

        ImGui.TextColored(Themes.Current.ImGuiStyle.FormInvalidColor.ToNumVec4(), Message);

        ImGuiManager.ReadOnlyInputTextMultiline("Exception", _exceptionString, ImGui.GetContentRegionAvail());

        ImGui.NewLine();

        ButtonGenerator(this);
    }
}
