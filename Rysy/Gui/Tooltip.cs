using ImGuiNET;

namespace Rysy.Gui;

public readonly struct Tooltip : ITooltip {
    private readonly string? _text;
    private readonly ITooltip? _tooltip;

    public Tooltip(string? text) {
        _text = text;
        _tooltip = null;
    }

    public Tooltip(ITooltip tooltip) {
        _text = null;
        _tooltip = tooltip;
    }
    
    public bool IsNull => _text == null && _tooltip is null;
    
    public bool IsEmpty => _text == null && (_tooltip is null || _tooltip.IsEmpty);

    public void RenderImGui() {
        if (_text is {} text)
            ImGui.Text(text);
        if (_tooltip is {} tooltip)
            tooltip.RenderImGui();
    }
    
    public static implicit operator Tooltip(string? text) => new(text);

    public static Tooltip CreateTranslatedOrNull(string id, string? fallbackId = null)
        => new Tooltip(new TranslatedOrNullTooltip(id, fallbackId));
}

public interface ITooltip {
    public void RenderImGui();
    
    public bool IsEmpty { get; }
}

public sealed class TranslatedOrNullTooltip(string id, string? fallbackId) : ITooltip {
    public void RenderImGui() {
        var text = id.TranslateOrNull() ?? fallbackId?.TranslateOrNull();

        if (text is { }) {
            ImGui.Text(text);
        }
    }

    public bool IsEmpty => (id.TranslateOrNull() ?? fallbackId?.TranslateOrNull()) is null;
}