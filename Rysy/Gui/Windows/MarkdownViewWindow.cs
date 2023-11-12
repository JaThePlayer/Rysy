using ImGuiNET;
using Markdig;
using Markdig.Syntax;
using Rysy.Extensions;

namespace Rysy.Gui.Windows; 

public sealed class MarkdownViewWindow : Window {
    private readonly MarkdownDocument _document;

    private static float GetSizeX(string md) => ImGui
        .CalcTextSize(md.Contains('\n') ? md.Split('\n').MaxBy(l => l.Length) : md).X
        .AtMost(RysyEngine.Instance.Window.ClientBounds.Width * 0.75f);
        
    
    public MarkdownViewWindow(string name, string markdown) : base(name, new(GetSizeX(markdown), 0))
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(markdown);
        
        _document = Markdown.Parse(markdown, ImGuiMarkdown.MarkdownPipeline);
    }

    protected override void Render() {
        base.Render();
        
        ImGuiMarkdown.RenderMarkdown(_document);
    }
}