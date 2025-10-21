using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using System.Buffers.Text;
using System.Text;

namespace Rysy.Gui.Windows;

public class HistoryWindow : Window {
    private IHistoryAction? _selectedAction;
    private string? _selectedActionJson;
    private string? _selectedActionByteCount;
    
    public HistoryWindow() : base("History", new(480, 480)) {
        NoSaveData = false;
    }

    protected override bool Visible => Persistence.Instance.HistoryWindowOpen;

    protected override void Render() {
        var history = EditorState.History;
        if (history is null)
            return;

        if (ImGui.BeginListBox("Actions")) {
            int i = 0;
            foreach (var action in history.Actions) {
                if (ImGui.Selectable(Interpolator.Shared.InterpolateU8($"{action.GetType().Name}##{i++}"), _selectedAction == action)) {
                    _selectedAction = action;
                    var packed = HistorySerializer.SerializeAnyToElement(action);
                    _selectedActionJson = packed.ToJson();

                    using var memstream = new MemoryStream();
                    BinaryPacker.SaveToStream(new() {Data = packed}, memstream);

                    var bytes = memstream.ToArray();

                    _selectedActionByteCount = bytes.Length.ToString(CultureInfo.InvariantCulture);
                }
            }

            ImGui.EndListBox();
        }

        if (ImGui.Button("From Clipboard") && Input.Clipboard.TryGetFromJson<BinaryPacker.Element>() is {} el) {
            if (HistorySerializer.TryDeserialize<IHistoryAction>(el, Room.DummyRoom, out var action)) {
                history.ApplyNewAction(action);
            }
        }
        
        if (_selectedActionJson is {} json)
            ImGui.InputTextMultiline("##", ref json, (uint)json.Length, new NumVector2(), ImGuiInputTextFlags.ReadOnly);
        
        if (_selectedActionByteCount is {})
            ImGui.Text($"Encoded bytes: {_selectedActionByteCount}");
    }

    public override void RemoveSelf() {
        base.RemoveSelf();
        Persistence.Instance.HistoryWindowOpen = false;
    }
}
