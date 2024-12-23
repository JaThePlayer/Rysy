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

    public Tooltip(Tooltip inner, ITooltip? tooltip) {
        _text = inner._text;
        
        if (tooltip != null) {
            _tooltip = inner._tooltip is {} innerTooltip ? new MergedTooltip(innerTooltip, tooltip) : tooltip;
        } else {
            _tooltip = inner._tooltip;
        }
    }
    
    public bool IsNull => _text == null && _tooltip is null;
    
    public bool IsEmpty => _text == null && (_tooltip is null || _tooltip.IsEmpty);

    public void RenderImGui() {
        if (_text is {} text)
            ImGui.Text(text);
        if (_tooltip is {} tooltip)
            tooltip.RenderImGui();
    }

    public Tooltip WrapWithValidation(ValidationResult result) {
        return new Tooltip(this, result);
    }
    
    public static implicit operator Tooltip(string? text) => new(text);

    public static Tooltip CreateTranslatedOrNull(string id, string? fallbackId = null)
        => new Tooltip(new TranslatedOrNullTooltip(id, fallbackId));
    
    public static Tooltip CreateTranslatedFormatted(string id, params object[] args)
        => new Tooltip(new TranslatedFormattedTooltip(id, args));
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

public sealed class TranslatedFormattedTooltip(string id, object[] args) : ITooltip {
    public void RenderImGui() {
        var text = id.TranslateFormatted(args);

        if (text is { }) {
            ImGui.Text(text);
        }
    }

    public bool IsEmpty => id.TranslateOrNull() is null;
}

sealed class MergedTooltip(ITooltip first, ITooltip second) : ITooltip {
    public void RenderImGui() {
        first.RenderImGui();
        second.RenderImGui();
    }

    public bool IsEmpty => first.IsEmpty && second.IsEmpty;
}