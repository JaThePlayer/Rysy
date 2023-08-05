using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;

namespace Rysy.Scenes;

public class PersistenceMapLoadErrorScene : Scene {
    public string Text;

    public Exception Exception;

    public PersistenceMapLoadErrorScene(Exception e, string langPostfix) {
        Text = langPostfix.TranslateOrHumanize("rysy.mapLoadError");

        AddWindow(new ScriptedWindow(Text, RenderImgui));

        Exception = e;
    }

    public override void Render() {
        base.Render();

        GFX.BeginBatch();
        PicoFont.Print(Text, new Rectangle(new(0, 0), RysyEngine.Instance.Window.ClientBounds.Size), Color.LightSkyBlue, 4f);
        GFX.EndBatch();
    }

    private void RenderImgui(Window window) {
        ImGui.Text(Text);
        ImGui.TextColored(Color.Red.ToNumVec4(), Exception.ToString());

        var loadBackupText = "rysy.mapLoadError.loadBackup".Translate();

        if (ImGui.Button(loadBackupText)) {
            var backups = BackupHandler.GetBackups();
            BackupInfo? backup = backups.FirstOrDefault();

            if (backup is { }) {
                AddWindow(new ScriptedWindow(loadBackupText, (w) => {
                    ImGui.Text("rysy.mapLoadError.selectBackup".Translate());

                    ImGuiManager.Combo("", ref backup, backups, toString: (b) => $"{b.MapName} ({b.Time}) [{b.Filesize.Value / 1024.0:n2}kb]");

                    if (ImGui.Button("rysy.load".Translate())) {
                        w.RemoveSelf();
                        RysyEngine.Scene = new EditorScene(backup.BackupFilepath, fromBackup: true, overrideFilepath: backup.OrigFilepath);
                    }
                }, new(500, 100)));
            } else {
                AddWindow(new ScriptedWindow(loadBackupText, (w) => {
                    ImGui.Text("rysy.mapLoadError.noBackups".Translate());
                }));
            }
        }
    }
}
