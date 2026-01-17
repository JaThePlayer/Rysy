using Hexa.NET.ImGui;
using Markdig;
using Markdig.Syntax;
using Rysy.Helpers;

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
        
        if (tooltip is { IsEmpty: false }) {
            _tooltip = inner._tooltip is {} innerTooltip ? new MergedTooltip(innerTooltip, tooltip) : tooltip;
        } else {
            _tooltip = inner._tooltip;
        }
    }
    
    public bool IsNull => _text == null && _tooltip is null;
    
    public bool IsEmpty => _text == null && (_tooltip is null || _tooltip.IsEmpty);
    
    public string? GetRawText() => _text ?? _tooltip?.GetRawText();

    public void RenderImGui() {
        if (_text is {} text)
            ImGui.Text(text);
        if (_tooltip is {} tooltip)
            tooltip.RenderImGui();
    }

    public Tooltip WrapWithValidation(ValidationResult result) {
        return new Tooltip(this, result);
    }
    
    public void RenderIfHovered() {
        true.WithTooltip(new Tooltip(this));
    }

    public static Tooltip CreateTranslatedOrNull(string id, string? fallbackId = null)
        => new Tooltip(new TranslatedOrNullTooltip(id, fallbackId));
    
    public static Tooltip CreateTranslatedFormatted(string id, params object[] args)
        => new Tooltip(new TranslatedFormattedTooltip(id, args));

    public static Tooltip CreateTranslatedMarkdown(string id, string? fallbackId = null) {
        string? prevContents = null;
        MarkdownDocument? md = null;
        GuiSize guiSize = default;
        return new Tooltip(new MarkdownTooltip(() => {
            var txt = LangRegistry.TranslateOrNull(id) ?? LangRegistry.TranslateOrNull(fallbackId);
            txt ??= "";
            if (txt != prevContents || md is null) {
                prevContents = txt;
                md = Markdown.Parse(txt, ImGuiMarkdown.MarkdownPipeline);

                guiSize = GuiSize.From(txt);
            }

            return (txt, md, guiSize);
        }));
    }
}

public interface ITooltip {
    public void RenderImGui();
    
    public bool IsEmpty { get; }

    public string? GetRawText();

    public void RenderIfHovered() {
        true.WithTooltip(new Tooltip(this));
    }
}

public sealed class CustomTooltip(Action render) : ITooltip {
    public void RenderImGui() {
        render();
    }

    public bool IsEmpty => false;

    public string? GetRawText() => null;
}

public sealed class TranslatedOrNullTooltip(string id, string? fallbackId) : ITooltip {
    public void RenderImGui() {
        var text = GetRawText();

        if (text is { }) {
            ImGui.Text(text);
        }
    }

    public bool IsEmpty => (id.TranslateOrNull() ?? fallbackId?.TranslateOrNull()) is null;
    
    public string? GetRawText() => id.TranslateOrNull() ?? fallbackId?.TranslateOrNull();
}

public sealed class TranslatedFormattedTooltip(string id, object[] args) : ITooltip {
    public void RenderImGui() {
        var text = GetRawText();

        if (text is { }) {
            ImGui.Text(text);
        }
    }

    public bool IsEmpty => id.TranslateOrNull() is null;
    
    public string? GetRawText() => id.TranslateFormatted(args);
}

sealed class MergedTooltip(ITooltip first, ITooltip second) : ITooltip {
    public void RenderImGui() {
        first.RenderImGui();
        second.RenderImGui();
    }

    public bool IsEmpty => first.IsEmpty && second.IsEmpty;

    public string GetRawText() => $"{first.GetRawText() ?? ""}\n{second.GetRawText() ?? ""}";
}

sealed class MarkdownTooltip(Func<(string raw, MarkdownDocument doc, GuiSize size)> dataGetter) : ITooltip {
    public void RenderImGui() {
        var data = dataGetter();
        if (data.doc.Count > 0) {
            ImGui.SetNextWindowSize(data.size.Calculate());
            ImGui.BeginChild(Interpolator.TempU8($"##md-tooltip-{data.doc.GetHashCode()}"));
            ImGuiMarkdown.RenderMarkdown(data.doc);
            ImGui.EndChild();
        }
    }

    public bool IsEmpty => dataGetter().doc.Count == 0;

    public string? GetRawText() => dataGetter().raw;
}