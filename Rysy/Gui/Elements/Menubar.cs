using ImGuiNET;
using Rysy.Gui.Elements;
using Rysy.Scenes;
using System.Linq;

namespace Rysy.Gui;
public static class Menubar {
    private static string NewMapPackageName = "";

    public static void Render(EditorScene editor) {
        ImGuiManager.PushWindowStyle();
        if (!ImGui.BeginMainMenuBar())
            return;
        ImGuiManager.PopWindowStyle();
        ImGuiManager.MenubarHeight = ImGui.GetWindowHeight();

        FileMenu(editor);
        EditMenu(editor);

        ImGui.EndMainMenuBar();
    }

    private static void EditMenu(EditorScene editor) {
        if (ImGui.BeginMenu("Edit")) {
            if (ImGui.MenuItem("Settings")) {
                SettingsWindow.Add(editor);
            }

            ImGui.EndMenu();
        }
    }

    private static void FileMenu(EditorScene editor) {
        if (ImGui.BeginMenu("File")) {
            if (ImGui.MenuItem("New")) {
                NewMapPackageName = "";
                editor.AddWindow(new("New map", (w) => {
                    ImGui.TextWrapped("Please enter the Package Name for your map.");
                    ImGui.TextWrapped("This name is only used internally, and is not visible in-game.");
                    if (ImGui.InputText("Package Name", ref NewMapPackageName, 512, ImGuiInputTextFlags.EnterReturnsTrue)) {
                        editor.LoadNewMap(NewMapPackageName);
                        w.RemoveSelf();
                    }
                }, new(350, ImGui.GetTextLineHeightWithSpacing() * 8)));
            }

            if (ImGui.MenuItem("Open").WithTooltip("UNIMPLEMENTED"))
                ShowUnimplemented(editor);

            ImGuiManager.DropdownMenu("Recent", Persistence.Instance.RecentMaps,
                p => Persistence.Instance.RecentMaps.Count(p2 => p2.Package == p.Package) > 1
                    ? $"{p.Package} [{p.Filename.TryCensor().CorrectSlashes()}]"
                    : p.Package,
                p => editor.LoadMapFromBin(p.Filename));

            if (ImGui.MenuItem("Save", "CTRL + s")) {
#warning Extract + Implement hotkey
                var pack = editor.Map.PackFully();
                BinaryPacker.SaveToFile(pack, pack.Filename + ".rysy.bin");
            }
            if (ImGui.MenuItem("Save as").WithTooltip("UNIMPLEMENTED"))
                ShowUnimplemented(editor);
            if (ImGui.MenuItem("Exit"))
                RysyEngine.Instance.Exit();

            ImGui.EndMenu();
        }
    }

    private static void ShowUnimplemented(EditorScene editor) {
        editor.AddWindow(new("Unimplemented", (w) => {
            ImGui.Text("AAAA");
        }));
    }
}
