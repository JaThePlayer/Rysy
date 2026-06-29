using Rysy.Components;
using Rysy.Helpers;
using Rysy.Platforms;

namespace Rysy.Gui.Windows;

public sealed class PlaytestButton(UnsavedChangesManager unsavedChangesManager) : IMenubarIndicator, ICommandPaletteCommand, IHotkeyProvider {
    private const string PlaytestHotkeyId = "playtest";
    private const string PlaytestHotkeyDefault = "ctrl+p";
    
    private Task? _playtestTask;
    
    public void RenderMenubarIndicator(Menubar menubar) {
        var color = ThemeColors.FormEditedColor;
        if (_playtestTask is { IsCompleted: false })
            color = ThemeColors.FormNullColor;
        
        if (ImGuiManager.SelectableIcon(ImGuiIcons.Play, color).WithTranslatedTooltip("rysy.playtest.tooltip").WithHotkeyTooltip(PlaytestHotkeyId, PlaytestHotkeyDefault)) {
            Playtest();
        }
    }

    private void SaveIfNeeded() {
        unsavedChangesManager.SaveIfNeeded();
    }
    
    private void Playtest() {
        if (RysyState.Scene.Get<IDebugRcClient>() is not { } debugRcClient
            || !(EditorState.Current?.Map?.TryGetSid(out var sid, out var side) ?? false)
            || EditorState.Current.CurrentRoom is not { IsPlayable: true } currentRoom) {
            return;
        }

        if (_playtestTask is { IsCompleted: false })
            return;
        
        SaveIfNeeded();

        _playtestTask = WrapWithExceptionHandling(debugRcClient.Tp(sid, side, currentRoom.Name, null, null, forceNewSession: true))
            .ContinueWith(_ => RysyPlatform.Current.FocusCelesteWindow());
    }

    private async Task<HttpResponseMessage?> WrapWithExceptionHandling(Task<HttpResponseMessage> task) {
        try {
            var response = await task;

            if (response.IsSuccessStatusCode)
                return response;

            var content = await response.Content.ReadAsStringAsync();
            PopupNotificationWindow.Show(new LangKey("rysy.playtest.failedCelesteSideUnsuccessfulCode", (int)response.StatusCode, response.ReasonPhrase ?? "", content));

            return response;
        } catch (HttpRequestException ex) when (ex.HttpRequestError is HttpRequestError.ConnectionError) {
            PopupNotificationWindow.ShowException(new LangKey("rysy.debugRc.gameNotOpen"), ex);
        } catch (Exception ex) {
            PopupNotificationWindow.ShowException(new LangKey("rysy.playtest.failedCelesteSide"), ex);
        }

        return null;
    }

    public Searchable Searchable => new Searchable("rysy.playtest".Translate());
    
    public XnaWidgetDef? CreatePreview() {
        return null;
    }

    public bool HasPreview => false;

    public ITooltip? Tooltip { get; } = new TranslatedOrNullTooltip("rysy.playtest.tooltip", null);
    
    public void Run() {
        Playtest();
    }

    public bool HotkeysIgnoreImGui => false;

    public void AddHotkeysTo(HotkeyHandler handler) {
        handler.AddHotkeyFromSettings(PlaytestHotkeyId, PlaytestHotkeyDefault, Run);
    }
}