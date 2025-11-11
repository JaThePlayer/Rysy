using Hexa.NET.ImGui;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Platforms;

namespace Rysy.Scenes;

public class CrashScene : Scene {
    private Scene _prevScene;
    private Exception _exception;

    private DateTime? _lastBackupDate;

    public CrashScene(Scene prev, Exception e) {
        _prevScene = prev;
        _exception = e;

        if (prev is EditorScene)
            _lastBackupDate = BackupHandler.GetMostRecentBackupDate();
    }

    public override void OnBegin() {
        base.OnBegin();

        AddWindow(new CrashWindow("Caught an unknown exception:", _exception, RenderButtons));
    }

    private void RenderButtons(CrashWindow w) {
        if (_prevScene is EditorScene { Map: { } map } editor) {
            if (_lastBackupDate is { } date && ImGui.Button("Load Backup").WithTooltip($"Tries to load the most recent backup of your map.\n[{date}]\nSafest option.")) {
                var backupMap = BackupHandler.LoadMostRecentBackup();
                if (backupMap == null) {
                    return;
                }
                backupMap.Filepath = Persistence.Instance.LastEditedMap;

                RysyEngine.Scene = new EditorScene(backupMap);
            }

            ImGui.SameLine();
            if (ImGui.Button("Reload").WithTooltip("Reloads the map editor, while leaving your map in the same state as it was right before the crash.")) {
                RysyEngine.Scene = new EditorScene(map);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Close").WithTooltip("Closes the program.")) {
            RysyPlatform.Current.ExitProcess();
        }

        ImGui.SameLine();
        if (ImGui.Button("Ignore").WithTooltip("Ignores the exception, and tries to resume the editor as if nothing happened.\nCan cause further crashes!")) {
            RysyEngine.Scene = _prevScene;
        }
    }
}
