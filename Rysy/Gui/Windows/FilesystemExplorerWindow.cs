using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public class FilesystemExplorerWindow : Window {
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
        
        FoundFiles ??= ModRegistry.Filesystem.FindFilesInDirectoryRecursiveWithMod("", "")
            .Select(p => new FileRef(p.Item1, p.Item2))
            .ToList();

        if (FoundFiles is { }) {
            OpenedFile ??= new("", ModRegistry.VanillaMod);
            
            FoundFilesDict ??= FoundFiles.ToDictionary(f => f, f => $"{f.Path} [{f.Mod.Name}]");

            if (ImGuiManager.Combo("Files", ref OpenedFile, FoundFilesDict, ref Search, null, _cache)) {
                if (OpenedFile!.Mod.Filesystem.TryReadAllText(OpenedFile.Path) is { } text) {
                    FileText = text;
                }
            }
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
}
