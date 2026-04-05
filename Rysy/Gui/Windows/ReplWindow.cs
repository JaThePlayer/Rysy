using Hexa.NET.ImGui;
using Rysy.Helpers.Repl;

namespace Rysy.Gui.Windows;

public sealed class ReplWindow : Window {
    private readonly IRepl _repl;

    private string _newText = "";

    private readonly List<HistoryEntry> _history = [];

    public ReplWindow(string name, IRepl repl) : base(name, new GuiSize(120, 23).CalculateWindowSize(true)) {
        _repl = repl;
    }

    public override bool HasBottomBar => true;

    protected override void Render() {
        base.Render();

        ImGui.InputTextMultiline("Code", ref _newText, nuint.Max((nuint)_newText.Length + 2, 8192));
        foreach (var entry in _history.AsEnumerable().Reverse()) {
            ImGui.Text(entry.Prompt);
            if (entry.Response is not null)
                ImGuiManager.TextColored(entry.ResponseColor, entry.Response);
            else
                ImGuiManager.TextColored(ThemeColors.FormNullColor, "...");
        }
    }

    private string FormatResult(object? obj) {
        return obj.ToStringInvariant();
    }

    public override void RenderBottomBar() {
        base.RenderBottomBar();

        if (ImGuiManager.TranslatedButton("rysy.windows.csharprepl.run")) {
            var entry = new HistoryEntry { Prompt = $"> {_newText}" };
            _history.Add(entry);
            
            _repl.ContinueWith(_newText).ContinueWith(x => {
                if (x.Exception != null) {
                    Exception exception = x.Exception;
                    if (x.Exception.InnerExceptions is [var only]) {
                        exception = only;
                    }
                    
                    entry.Response = exception.ToString() + "\n";
                    entry.ResponseColor = ThemeColors.FormInvalidColor;
                } else {
                    entry.Response = FormatResult(x.Result);
                }
            });
        }
    }

    class HistoryEntry {
        public required string Prompt { get; init; }
        
        public string? Response { get; set; }

        public IThemeColor ResponseColor { get; set; } = ThemeColors.TextColor;
    }
}
