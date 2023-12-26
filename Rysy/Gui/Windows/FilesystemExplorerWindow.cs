using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public class FilesystemExplorerWindow : Window {
    private string Filename = "";

    record FileRef(string Path, ModMeta Mod);

    private ComboCache<FileRef> _cache = new();
    private FileRef? OpenedFile;
    private string FileText = "";
    private string Search = "";

    private List<FileRef>? FoundFiles;
    private Dictionary<FileRef, string>? FoundFilesDict;

    public FilesystemExplorerWindow() : base("Filesystem Explorer", new(800, 800)) {
        Resizable = true;
        NoSaveData = false;
    }

    protected override void Render() {
        base.Render();

        if (ImGui.InputText("Filename", ref Filename, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
            if (ModRegistry.Filesystem.TryReadAllText(Filename) is { } file) {
                FoundFiles = null;
                FileText = file;
                OpenedFile = new(Filename, ModRegistry.Filesystem.FindFirstModContaining(Filename)!);
            }
        }

        ScanForFilesButton();

        if (FoundFiles is { }) {
            OpenedFile ??= new("", ModRegistry.VanillaMod);
            
            FoundFilesDict ??= FoundFiles.ToDictionary(f => f, f => $"{f.Path} [{f.Mod.Name}]");

            if (ImGuiManager.Combo("Files", ref OpenedFile, FoundFilesDict, ref Search, null, _cache)) {
                if (OpenedFile!.Mod.Filesystem.TryReadAllText(OpenedFile.Path) is { } text) {
                    FileText = text;
                    //OpenedFile = file;
                }
            }
            /*
            ImGuiManager.DropdownMenu("Files", FoundFiles, x => $"{x.Path} [{x.Mod.Name}]", (file) => {
                if (file.Mod.Filesystem.TryReadAllText(file.Path) is { } text) {
                    FileText = text;
                    OpenedFile = file;
                }
            });*/
        }

        if (OpenedFile is { } opened) {
            if (opened.Path.FileExtension() == ".bin" && ImGui.Button("Open Map")) {
                opened.Mod.Filesystem.TryOpenFile(opened.Path, (stream) => {
                    EditorState.Map = Map.FromBinaryPackage(BinaryPacker.FromBinary(stream, null!));
                });
               
            }

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

                FoundFiles = fs.FindFilesInDirectoryRecursive(dir, extension).Select(s => new FileRef(s, mod)).ToList();
            } else {
                FoundFiles = ModRegistry.Filesystem.FindFilesInDirectoryRecursiveWithMod(dir, extension).Select(p => new FileRef(p.Item1, p.Item2)).ToList();
            }
        }
    }
}
