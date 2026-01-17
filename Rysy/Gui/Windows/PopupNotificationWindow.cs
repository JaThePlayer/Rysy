using Hexa.NET.ImGui;
using Rysy.Helpers;

namespace Rysy.Gui.Windows;

public class PopupNotificationWindow : Window {
    public LangKey MessageId { get; }
    private readonly Exception? _exception;
    private readonly string? _exceptionString;

    public Color MessageColor { get; set; } = Themes.Current.ImGuiStyle.TextColor;

    public override bool PersistBetweenScenes => true;

    public PopupNotificationWindow(LangKey titleId, LangKey? messageId = null, Exception? exception = null) 
        : base(titleId.ToString()) {
        MessageId = messageId ?? new LangKey($"{titleId.Key}.message", titleId.Args);
        
        _exception = exception;
        _exceptionString = _exception?.ToString().Censor();

        var size = GuiSize.From(MessageId.ToString());
        if (_exceptionString != null) {
            size = size.Append(_exceptionString);
        }
        Size = size.CalculateWindowSize(true);
    }

    protected override void Render() {
        base.Render();

        ImGui.TextColored(MessageColor.ToNumVec4(), MessageId.ToString());

        if (_exceptionString != null) {
            ImGuiManager.ReadOnlyInputTextMultiline("Exception", _exceptionString, ImGui.GetContentRegionAvail());
        }
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        base.RenderBottomBar();

        if (ImGuiManager.TranslatedButton("rysy.ok")) {
            RemoveSelf();
        }
    }

    public static PopupNotificationWindow Show(LangKey titleId) {
        var popup = new PopupNotificationWindow(titleId);
        RysyEngine.Scene.AddWindow(popup);
        return popup;
    }

    /// <summary>
    /// Shows a popup if there's an exception while executing the given action.
    /// </summary>
    /// <returns>Whether an exception was thrown</returns>
    public static bool ShowOnException(LangKey titleId, Action action) {
        try {
            action();
            return false;
        } catch (Exception ex) {
            var popup = new PopupNotificationWindow(titleId, exception: ex) {
                MessageColor = Themes.Current.ImGuiStyle.FormInvalidColor
            };
            Logger.Error(ex, popup.MessageId.ToString());
            RysyEngine.Scene.AddWindow(popup);
            return true;
        }
    }
}