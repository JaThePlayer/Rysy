using Hexa.NET.ImGui;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public sealed class FilesystemExplorerWindow : Window {
    sealed record FileRef(string Path, ModMeta Mod) {
        public override string ToString() => Path ?? "";
    }

    private ComboCache<FileRef> _cache = new();
    private FileRef? _openedFile;
    private string _fileText = "";
    private string _search = "";

    private List<FileRef>? _foundFiles;
    private Dictionary<FileRef, string>? _foundFilesDict;

    public FilesystemExplorerWindow() : base("Filesystem Explorer", new(800, 800)) {
        Resizable = true;
        NoSaveData = false;
    }

    protected override void Render() {
        base.Render();
        
        _foundFiles ??= ModRegistry.Filesystem.FindFilesInDirectoryRecursiveWithMod("", "")
            .Where(p => !p.Item1.StartsWith('.') && !p.Item1.StartsWith("__MACOSX", StringComparison.Ordinal))
            .Select(p => new FileRef(p.Item1, p.Item2))
            .ToList();

        if (_foundFiles is { }) {
            _openedFile ??= new("", ModRegistry.VanillaMod);
            
            _foundFilesDict ??= _foundFiles.ToDictionary(f => f, f => $"{f.Path} [{f.Mod.DisplayName}]");

            if (ImGuiManager.Combo("Files", ref _openedFile, _foundFilesDict, ref _search, default, _cache)) {
                if (_openedFile!.Mod.Filesystem.TryReadAllText(_openedFile.Path) is { } text) {
                    _fileText = text;
                }
            }
        }

        var shouldDisplayText = true;
        
        if (_openedFile is { } opened) {
            var ext = opened.Path.FileExtension();
            
            if (ext == ".bin" && ImGui.Button("Open Map")) {
                opened.Mod.Filesystem.TryOpenFile(opened.Path, (stream) => {
                    EditorState.Map = Map.FromBinaryPackage(BinaryPacker.FromBinary(stream, null!));
                });

                shouldDisplayText = false;
            }
            else if (ext == ".png") {
                shouldDisplayText = false;
                var w = (int) ImGui.GetContentRegionAvail().X;
                var h = (int)(ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 4f);
                // TODO: zoom in/out???
                ImGuiManager.XnaWidget("fsExplorerPreview", w, h,
                    () => {
                        if (Gfx.Atlas.TryGet(opened.Path.TrimPrefix("Graphics/Atlases/Gameplay/")
                                    .TrimPostfix(".png"),
                                out var t)) {
                            var spr = ISprite.FromTexture(t).Centered();
                            spr.Scale = new(6f);
                            spr.Pos = new Vector2(w, h) / 2f;
                            spr.Render(SpriteRenderCtx.Default());
                        }
                    });
            }

            if (ImGui.Button("Save as...") && FileDialogHelper.TrySave(opened.Path.FileExtensionNoDot() ?? "", out var chosenFile)) {
                opened.Mod.Filesystem.TryOpenFile(opened.Path, (stream) => {
                    using var filestream = File.OpenWrite(chosenFile);

                    stream.CopyTo(filestream);
                    filestream.Flush();
                });
            }
        }

        if (shouldDisplayText)
            ImGuiManager.ReadOnlyInputTextMultiline("##", _fileText, new(800 - ImGui.GetStyle().WindowPadding.X, 700));
    }
}
