using Hexa.NET.ImGui;

namespace Rysy.Gui;

/// <summary>
/// Helper struct that handles size calculations when adding widgets to a input field.
/// </summary>
public readonly struct InputWidgetHelper {
    private readonly float _xPadding = ImGui.GetStyle().FramePadding.X;

    public readonly float ButtonWidth = ImGui.GetFrameHeight();

    public readonly ITooltip? Tooltip;

    public InputWidgetHelper(int widgetAmt, ITooltip? tooltip = null) {
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ButtonWidth + _xPadding) * widgetAmt);
        Tooltip = tooltip;
    }

    /// <summary>
    /// Sets up state to allow rendering the next widget.
    /// </summary>
    public void Next() {
        ImGui.SameLine(0f, _xPadding);
        ImGui.SetNextItemWidth(ImGui.GetFrameHeight());
    }

    /// <summary>
    /// Renders an appropriately sized button. Counts as a widget.
    /// </summary>
    public bool Button(ReadOnlySpan<byte> txt) {
        Next();
        return ImGui.Button(txt, new NumVector2(ButtonWidth, 0f)).WithTooltip(Tooltip);
    }
    
    /// <summary>
    /// Renders the final label of the field. Does not count as a widget.
    /// </summary>
    public void Label(string txt) {
        if (string.IsNullOrWhiteSpace(txt))
            return;
        
        Next();
        ImGui.TextUnformatted(txt);
        Tooltip?.RenderIfHovered();
    }
    
    /// <summary>
    /// Renders the final label of the field. Does not count as a widget.
    /// </summary>
    public void Label(ReadOnlySpan<byte> txt) {
        if (txt.IsEmpty)
            return;
        
        Next();
        ImGui.TextUnformatted(txt);
        Tooltip?.RenderIfHovered();
    }
}