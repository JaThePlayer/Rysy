using ImGuiNET;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public class FilesystemExplorerWindow : Window {
    //Loenn/entities/flowerField.lua

    private string Filename = "";

    private string FileText = "";

    public FilesystemExplorerWindow() : base("Filesystem Explorer", new(800, 800)) {
        Resizable = true;
    }

    protected override void Render() {
        base.Render();

        if (ImGui.InputText("Filename", ref Filename, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
            if (ModRegistry.Filesystem.TryReadAllText(Filename) is { } file) {
                FileText = file;
            }
        }

        ImGui.InputTextMultiline("", ref FileText, (uint) FileText.Length, new(800 - ImGui.GetStyle().WindowPadding.X, 700), ImGuiInputTextFlags.ReadOnly);
    }
}
