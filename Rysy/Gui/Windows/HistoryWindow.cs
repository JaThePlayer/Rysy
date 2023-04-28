using ImGuiNET;

namespace Rysy.Gui.Windows;

public class HistoryWindow : Window {
    public HistoryWindow() : base("History", new(480, 480)) {
        NoSaveData = false;
    }

    protected override bool Visible => Persistence.Instance.HistoryWindowOpen;

    protected override void Render() {
        var history = EditorState.History;
        if (history is null)
            return;

        if (ImGui.BeginListBox("Actions")) {
            foreach (var action in history.Actions) {
                ImGui.Button(action.GetType().Name);
            }

            ImGui.EndListBox();
        }
    }

    public override void RemoveSelf() {
        base.RemoveSelf();
        Persistence.Instance.HistoryWindowOpen = false;
    }
}
