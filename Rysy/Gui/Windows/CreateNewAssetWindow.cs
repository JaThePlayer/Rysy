using ImGuiNET;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public abstract class CreateNewAssetWindow : Window {
    protected readonly Field PathField;
    
    protected string Path;
    
    private bool _wasInvalid;

    protected abstract string RealPath(string userPath);
    
    protected abstract string PathFieldTranslationKey { get; }

    protected abstract void Create(Map map, IWriteableModFilesystem fs, string realPath);
    
    protected CreateNewAssetWindow(string name, string defaultPath, NumVector2? size = null) : base(name.Translate(), size ??= new(400, 300)) {
        Path = defaultPath;
        PathField = Fields.NewPath("", RealPath).Translated(PathFieldTranslationKey);
    }

    protected override void Render() {
        _wasInvalid = false;
        
        if (PathField.RenderGuiWithValidation(Path, out var isValid) is { } newVal) {
            Path = newVal.ToString() ?? "";
        }
        
        ImGuiManager.RenderFileStructure(FileStructureInfo.FromPath(RealPath(Path)));

        _wasInvalid |= !isValid.IsOk;
    }
    
    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        if (EditorState.Map is not { Mod.Filesystem: IWriteableModFilesystem })
            _wasInvalid = true;
        
        ImGui.BeginDisabled(_wasInvalid);

        if (ImGuiManager.TranslatedButton("rysy.ok") && !_wasInvalid) {
            var fs = (IWriteableModFilesystem) EditorState.Map!.Mod!.Filesystem;
            var path = RealPath(Path);
            Create(EditorState.Map, fs, path);
            RemoveSelf();
        }
        
        ImGui.EndDisabled();
    }
}