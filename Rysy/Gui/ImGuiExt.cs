using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Helpers;
using System.Linq;

namespace Rysy.Gui;

public static class ImGuiExt {

    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTooltip(this bool val, string? tooltip) {
        if (tooltip is { } && ImGui.IsItemHovered()) {
            ImGui.SetTooltip(tooltip);
        }

        return val;
    }
    
    public static bool WithTooltip(this bool val, Tooltip tooltip) {
        if (!tooltip.IsEmpty && ImGui.IsItemHovered()) {
            ImGui.BeginTooltip();
            var prev = ImGuiManager.PopAllStyles();
            tooltip.RenderImGui();
            ImGuiManager.PushAllStyles(prev);
            ImGui.EndTooltip();
        }

        return val;
    }

    public static bool WithTooltip(this bool val, object? tooltip) {
        return tooltip switch {
            string s => val.WithTooltip(s),
            Tooltip s => val.WithTooltip(s),
            ITooltip s => val.WithTooltip(new Tooltip(s)),
            _ => val,
        };
    }

    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTranslatedTooltip(this bool val, string tooltipKey) {
        if (ImGui.IsItemHovered() && tooltipKey.TranslateOrNull() is { } translatedTooltip) {
            ImGui.SetTooltip(translatedTooltip);
        }

        return val;
    }
    
    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTranslatedTooltip(this bool val, LangKey tooltipKey) {
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip(tooltipKey.ToString());
        }

        return val;
    }
    
    /// <summary>
    /// Adds a tooltip to the last added element, then fluently returns the bool that was passed to this function, for further handling.
    /// </summary>
    public static bool WithTranslatedTooltip(this bool val, Interpolator.Handler tooltipKey) {
        if (ImGui.IsItemHovered() && tooltipKey.Result.TranslateOrNull() is { } translatedTooltip) {
            ImGui.SetTooltip(translatedTooltip);
        }

        return val;
    }
    
    public static bool WithHotkeyTooltip(this bool val, string hotkeyId, string defaultKeybind) {
        if (ImGui.IsItemHovered() && ImGui.BeginTooltip()) {
            var hotkey = Settings.Instance.GetOrCreateHotkey(hotkeyId, defaultKeybind);
            
            ImGui.TextUnformatted("rysy.hotkey".TranslateFormatted(hotkey));
            ImGui.EndTooltip();
        }

        return val;
    }
}
