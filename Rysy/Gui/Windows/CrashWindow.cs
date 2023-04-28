using ImGuiNET;
using Rysy.Extensions;

namespace Rysy.Gui.Windows;

public class CrashWindow : Window {
    public Exception Exception;
    public Action<CrashWindow> ButtonGenerator;
    public string Message;

    private string ExceptionString;

    public CrashWindow(string message, Exception e, Action<CrashWindow> buttonGenerator) : base("Crash Handler", new(800, 500)) {
        Exception = e;
        ButtonGenerator = buttonGenerator;
        Message = message;

        ExceptionString = Exception.ToString().Censor();
    }

    protected override void Render() {
        base.Render();

        ImGui.TextColored(Color.Red.ToNumVec4(), Message);

        ImGui.InputTextMultiline("Exception", ref ExceptionString, (uint)ExceptionString.Length, Size!.Value - new NumVector2(0f, 6 * ImGui.GetTextLineHeightWithSpacing()), ImGuiInputTextFlags.ReadOnly);

        ImGui.NewLine();

        ButtonGenerator(this);
    }
}
