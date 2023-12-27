using ImGuiNET;
using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Gui.Windows;

namespace Rysy.Scenes;

public class PersistenceMapLoadErrorWindow : Window {
    public readonly string Text;

    public readonly Exception Exception;

    public PersistenceMapLoadErrorWindow(Exception e, string langPostfix) : base(
        langPostfix.TranslateOrHumanize("rysy.mapLoadError"), null) {
        Text = langPostfix.TranslateOrHumanize("rysy.mapLoadError");
        Exception = e;
    }

    protected override void Render() {
        ImGui.Text(Text);
        ImGui.TextColored(Color.Red.ToNumVec4(), Exception.ToString());

        var loadBackupText = "rysy.mapLoadError.loadBackup".Translate();

        if (ImGui.Button(loadBackupText)) {
            var backups = BackupHandler.GetBackups();
            BackupInfo? backup = backups.FirstOrDefault();

            if (backup is { }) {
                RysyEngine.Scene.AddWindow(new ScriptedWindow(loadBackupText, (w) => {
                    ImGui.Text("rysy.mapLoadError.selectBackup".Translate());

                    ImGuiManager.Combo("", ref backup, backups,
                        toString: (b) => $"{b.MapName} ({b.Time}) [{b.Filesize.Value / 1024.0:n2}kb]");

                    if (ImGui.Button("rysy.load".Translate())) {
                        w.RemoveSelf();
                        RysyEngine.Scene = new EditorScene(backup.BackupFilepath, fromBackup: true,
                            overrideFilepath: backup.OrigFilepath);
                    }
                }, new(500, 100)));
            } else {
                RysyEngine.Scene.AddWindow(new ScriptedWindow(loadBackupText, (w) => {
                    ImGui.Text("rysy.mapLoadError.noBackups".Translate());
                }));
            }
        }
    }
}
