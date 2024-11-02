using ImGuiNET;
using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Gui.Windows;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers; 

public static class LinkOpenHelper {
    public static bool IsValidLink(string potentialLink, [NotNullWhen(true)] out Uri? uri) {
        if (!Uri.TryCreate(potentialLink, UriKind.Absolute, out uri)) {
            return false;
        }

        if (uri.IsFile)
            return false;

        if (uri.Scheme is not ("https" or "http"))
            return false;

        return true;
    }
    
    public static bool OpenLinkIfValid(string link) {
        if (!IsValidLink(link, out var uri)) {
            return false;
        }
        
        RysyEngine.Scene.AddWindow(new LinkOpenConfirmationWindow(uri));

        return true;
    }
}

internal sealed class LinkOpenConfirmationWindow : Window {
    private readonly Uri _uri;

    private static float XSize(Uri uri) => 
        float.Max(
            ImGui.CalcTextSize("rysy.linkOpenConfirmWindow.desc".Translate()).X,
            ImGui.CalcTextSize(uri.ToString()).X)
        + ImGui.GetStyle().FramePadding.X * 6;

    private static float YSize => ImGui.GetTextLineHeightWithSpacing() * 4 + ImGui.GetFrameHeightWithSpacing() * 2;
    
    public LinkOpenConfirmationWindow(Uri uri) : base("rysy.linkOpenConfirmWindow".Translate(), new(XSize(uri), YSize)) {
        _uri = uri;

        NoSaveData = true;
    }

    protected override void Render() {
        base.Render();
        
        ImGuiManager.TranslatedText("rysy.linkOpenConfirmWindow.desc");
        var textColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Text];
        ImGui.TextColored(textColor * 0.75f, _uri.Scheme);
        ImGui.SameLine(0f, 0f);
        
        ImGui.TextColored(textColor * 0.75f, "://");
        ImGui.SameLine(0f, 0f);
        
        ImGui.Text(_uri.Authority);
        ImGui.SameLine(0f, 0f);
        
        ImGui.TextColored(textColor * 0.75f, _uri.PathAndQuery);
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        base.RenderBottomBar();
        
        if (ImGuiManager.TranslatedButton("rysy.ok")) {
            Process.Start(new ProcessStartInfo {
                FileName = _uri.ToString(),
                UseShellExecute = true
            });
            RemoveSelf();
        }
        
        ImGui.SameLine();

        if (ImGuiManager.TranslatedButton("rysy.cancel")) {
            RemoveSelf();
        }
    }
}