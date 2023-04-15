using ImGuiNET;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public class FilesystemExplorerWindow : Window {
    //Loenn/entities/flowerField.lua

    private string Filename = "";


    private (string Path, ModMeta Mod)? OpenedFile;
    private string FileText = "";

    private List<(string Path, ModMeta Mod)>? FoundFiles;

    public FilesystemExplorerWindow() : base("Filesystem Explorer", new(800, 800)) {
        Resizable = true;
    }

    protected override void Render() {
        base.Render();

        if (ImGui.InputText("Filename", ref Filename, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
            if (ModRegistry.Filesystem.TryReadAllText(Filename) is { } file) {
                FoundFiles = null;
                FileText = file;
                OpenedFile = (Filename, ModRegistry.Filesystem.FindFirstModContaining(Filename)!);
            }
        }

        ScanForFilesButton();

        if (FoundFiles is { }) {
            ImGuiManager.DropdownMenu("Files", FoundFiles, x => $"{x.Path} [{x.Mod.Name}]", (file) => {
                if (file.Mod.Filesystem.TryReadAllText(file.Path) is { } text) {
                    FileText = text;
                    OpenedFile = file;
                }
            });
        }

        if (OpenedFile is { } opened) {
            if (ImGui.Button("Save as...") && FileDialogHelper.TrySave("", out var chosenFile)) {
                opened.Mod.Filesystem.TryOpenFile(opened.Path, (stream) => {
                    using var filestream = File.OpenWrite(chosenFile);

                    stream.CopyToAsync(filestream);
                    filestream.Flush();
                });
            }
        }

        ImGui.InputTextMultiline("", ref FileText, (uint) FileText.Length, new(800 - ImGui.GetStyle().WindowPadding.X, 700), ImGuiInputTextFlags.ReadOnly);
    }

    private void ScanForFilesButton() {
        if (ImGui.Button("Scan for files")) {
            var filenameSplit = Filename.Split(';');
            var dir = filenameSplit.ElementAtOrDefault(0) ?? "";
            var extension = filenameSplit.ElementAtOrDefault(1) ?? "";
            var modName = filenameSplit.ElementAtOrDefault(2);

            if (modName is { }) {
                var mod = ModRegistry.GetModByName(modName);
                if (mod is null)
                    return;
                var fs = mod.Filesystem;
                if (fs is null)
                    return;

                FoundFiles = fs.FindFilesInDirectoryRecursive(dir, extension).Select(s => (s, mod)).ToList();
            } else {
                FoundFiles = ModRegistry.Filesystem.FindFilesInDirectoryRecursiveWithMod(dir, extension).ToList();
            }
        }
    }
}
