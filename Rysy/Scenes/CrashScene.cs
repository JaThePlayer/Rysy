using ImGuiNET;
using Rysy.Gui;
using Rysy.Gui.Windows;

namespace Rysy.Scenes;

public class CrashScene : Scene {
    private Scene PrevScene;
    private Exception Exception;

    private DateTime? LastBackupDate;

    public CrashScene(Scene prev, Exception e) {
        PrevScene = prev;
        Exception = e;

        if (prev is EditorScene)
            LastBackupDate = BackupHandler.GetMostRecentBackupDate();
    }

    public override void OnBegin() {
        base.OnBegin();

        AddWindow(new CrashWindow("Caught an unknown exception:", Exception, RenderButtons));
    }

    private void RenderButtons() {
        if (PrevScene is EditorScene { Map: { } map } editor) {
            if (LastBackupDate is { } date && ImGui.Button("Load Backup").WithTooltip($"Tries to load the most recent backup of your map.\n[{date}]\nSafest option.")) {
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
            Environment.Exit(0);
        }

        ImGui.SameLine();
        if (ImGui.Button("Ignore").WithTooltip("Ignores the exception, and tries to resume the editor as if nothing happened.\nCan cause further crashes!")) {
            RysyEngine.Scene = PrevScene;
        }
    }
}
