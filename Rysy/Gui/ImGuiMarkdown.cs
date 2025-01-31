using ImGuiNET;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Runtime.InteropServices;
using System.Text;

namespace Rysy.Gui;

internal static partial class ImGuiMarkdown {
    public static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .UseBootstrap()
        .UseAutoLinks()
        .UseGridTables()
        .UsePipeTables()
        .EnableTrackTrivia()
        .Build();

    public static void RenderMarkdown(MarkdownObject doc) {
        ImGui.PushFont(ImGuiThemer.DefaultFont);
        var startX = ImGui.GetCursorPosX();
        
        if (doc is ContainerBlock container) {
            foreach (var contained in container) {
                ImGui.SetCursorPosX(startX);

                switch (contained) {
                    case HeadingBlock headingBlock: {
                        if (headingBlock.Inline is null)
                            continue;

                        var first = true;
                        foreach (var inline in headingBlock.Inline) {
                            RenderInline(inline, headingBlock.Level, first, startX);
                            first = false;
                        }
                    }
                        break;
                    case ParagraphBlock paragraphBlock: {
                        if (paragraphBlock.Inline is null)
                            continue;
                        var first = true;
                        foreach (var inline in paragraphBlock.Inline) {
                            RenderInline(inline, 0, first, startX);
                            first = false;
                        }
                    }
                        break;
                    case ListBlock list:
                        var bullet = list.BulletType;
                        foreach (var innerBlock in list) {
                            ImGui.Bullet();
                            ImGui.SameLine();
                            RenderMarkdown(innerBlock);
                            ImGui.SetCursorPosX(startX);
                        }
                        break;
                    case Table table:
                        var columns = table.ColumnDefinitions.Count - 1;
                        ImGui.BeginTable($"##", columns, ImGuiManager.TableFlags);
                    
                        foreach (var row in table.Descendants<TableRow>()) {
                            if (row.IsHeader) {
                                foreach (var inner in row) {
                                    ImGui.TableSetupColumn(inner.Descendants<LiteralInline>()?.FirstOrDefault()?.ToString() ?? "?");
                                }

                                ImGui.TableHeadersRow();
                            } else { 
                                ImGui.TableNextRow();
                                foreach (var innerBlock in row) {
                                    if (ImGui.TableNextColumn())
                                        RenderMarkdown(innerBlock);
                                }
                                
                            }
                        }
                        ImGui.EndTable();
                        break;
                    case FencedCodeBlock fencedCodeBlock: {
                        ImGui.BeginDisabled();
                        if (fencedCodeBlock.Lines is {})
                            foreach (var line in fencedCodeBlock.Lines.Lines.AsSpan()
                                         .SkipWhileFromEnd(l => l.ToString().IsNullOrWhitespace())) {
                                ImGui.Text(line.Slice.AsSpan());
                            }
                        ImGui.EndDisabled();
                        break;
                    }
                    case LeafBlock codeBlock: {
                        var first = true;

                        if (codeBlock.Inline is null) {
                            if (codeBlock.Lines is {})
                                foreach (var line in codeBlock.Lines.Lines) {
                                    ImGui.Text(line.Slice.AsSpan());
                                    first = false;
                                }
                            
                            continue;
                        }

                        foreach (var inline in codeBlock.Inline) {
                            RenderInline(inline, 0, first, startX);
                            first = false;
                        }
                        
                        break;
                    }
                    default:
                        Logger.Write("Markdown", LogLevel.Warning, $"Unknown block: {contained}");
                        break;
                }
                
                ImGui.NewLine();
            }
        }
        
        ImGui.PopFont();
    }

    private static void RenderInline(Inline inline, int headerLevel, bool firstInThisLine, float startX) {
        switch (inline) {
            case EmphasisInline emphasisInline:

                break;
            case CodeInline codeInline:
                ImGui.BeginDisabled();
                RenderTextWrapped(codeInline.Content, startX);
                ImGui.EndDisabled();
                ImGui.SameLine(0f, 0f);
                break;
            case LiteralInline literalInline:
                var emphasis = new TextEmphasis(literalInline, headerLevel);
                var renderText = true;

                var content = literalInline.Content.AsSpan();
                var contentTrimmed = firstInThisLine ? content.TrimStart() : content;
                    
                ImGuiManager.PushEmphasis(emphasis);
                    
                if (emphasis is { Link: {  } link, LinkIsImage: true }) {
                    if (GFX.Atlas.TryGet(link, out var texture)) {
                        DrawTexture(texture, contentTrimmed);
                    } else if (LinkOpenHelper.IsValidLink(link, out var uri)) {
                        if (GFX.GetTextureFromWebIfReady(uri) is { } webTexture) {
                            DrawTexture(webTexture, contentTrimmed);
                        }
                    }
                }

                if (renderText) {
                    RenderTextWrapped(contentTrimmed, startX);
                }
                    
                ImGuiManager.PopEmphasis();
                break;

                void DrawTexture(VirtTexture texture, ReadOnlySpan<char> linkTooltip) {
                    ImGuiManager.XnaWidget($"md.img.{link}", texture.Width, texture.Height, () => {
                        ISprite.FromTexture(texture).Render(SpriteRenderCtx.Default(true));
                    });
                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.Text(linkTooltip.ToString());
                        ImGui.EndTooltip();
                    }
                    renderText = false;
                }
            case LineBreakInline:
                ImGui.NewLine();
                break;
            case LinkInline:
                break;
            default:
                Logger.Write("Markdown", LogLevel.Warning, $"Unknown inline: {inline}");
                break;
        }

        if (inline is ContainerInline container) {
            foreach (var inner in container) {
                RenderInline(inner, headerLevel, false, startX);
            }
        }
    }


    // ImGui::TextWrapped will wrap at the starting position
    // so to work around this we render using our own wrapping for the first line
    // furthermore, we need to pop/push the Emphasis to render underline/strikethrough text correctly
    // based on https://gist.github.com/dougbinks/65d125e0c11fba81c5e78c546dcfe7af
    private static unsafe void RenderTextWrapped(ReadOnlySpan<char> textUtf16, float wrapStart) {
        if (textUtf16 is "") {
            ImGui.NewLine();
            return;
        }
        const float scale = 1.0f;
        
        var pFont = ImGui.GetFont();
        float widthLeft = ImGui.GetColumnWidth();
        
        var utf8ByteCount = Encoding.UTF8.GetByteCount(textUtf16);
        Span<byte> utf8 = utf8ByteCount <= 2048 ? stackalloc byte[utf8ByteCount + 1] : new byte[utf8ByteCount + 1];
        var utf8Written = Encoding.UTF8.GetBytes(textUtf16, utf8);
        utf8[utf8Written] = 0;
        utf8 = utf8[..utf8Written];
        
        fixed (byte* utf8Ptr = &utf8[0]) {
            var textEnd = &utf8Ptr[utf8.Length];
            var endPrevLine = CalcWordWrapPosition(pFont.NativePtr, scale, utf8Ptr, textEnd, widthLeft);

            if (endPrevLine == textEnd) {
                //ImGuiNative.igText(utf8ptr);
                ImGui.Text(textUtf16);
                return;
            }
            ImGuiNative.igTextUnformatted(utf8Ptr, endPrevLine);
            
            while (endPrevLine < textEnd)
            {
                widthLeft = ImGui.GetContentRegionAvail().X;
                
                var text = endPrevLine;
                if( *text == ' ' ) { ++text; } // skip a space at start of line
                endPrevLine = CalcWordWrapPosition(pFont.NativePtr, scale, text, textEnd, widthLeft );
                
                var popped = ImGuiManager.PopEmphasis();
                if (popped is { }) {
                    ImGui.NewLine();
                    ImGui.SetCursorPosX(wrapStart);
                    ImGuiManager.PushEmphasis(popped.Value);
                }
                
                ImGui.SetCursorPosX(wrapStart);
                ImGuiNative.igTextUnformatted(text, endPrevLine );
            }
            ImGuiManager.PopEmphasis();
        }
    }

    [LibraryImport("cimgui", EntryPoint = "ImFont_CalcWordWrapPositionA")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial byte* CalcWordWrapPosition(ImFont* font, float scale, byte* text, byte* textEnd, float wrapWidth);
}
