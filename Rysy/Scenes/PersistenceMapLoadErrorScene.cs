using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using System;

namespace Rysy.Scenes;

public class PersistenceMapLoadErrorScene : Scene {
    public const string Text = "Failed to load last edited map.";

    public Exception Exception;

    public PersistenceMapLoadErrorScene(Exception e) {
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

        if (ImGui.Button("Load Last Backup")) {
            var map = BackupHandler.LoadMostRecentBackup();
            if (map == null) {
                return;
            }
            map.Filepath = Persistence.Instance.LastEditedMap;

            RysyEngine.Scene = new EditorScene(map!);
        }
    }
}
