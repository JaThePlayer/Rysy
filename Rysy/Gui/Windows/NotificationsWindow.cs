using ImGuiNET;

namespace Rysy.Gui.Windows;

public sealed class NotificationsWindow : Window {
    private static readonly List<NotificationInfo> Notifications = [];
    private static Field? _minLevelField;

    public const string TitleId = "rysy.notifications.window.name";
    
    public NotificationsWindow() : base(TitleId.Translate(), new(500, 260)) {
        NoSaveData = false;
        SetIsOpenedPersistently(true);
    }

    public override void RemoveSelf() {
        base.RemoveSelf();
        SetIsOpenedPersistently(false);
    }

    private void SetIsOpenedPersistently(bool value) {
        var settings = Settings.Instance;
        if (settings.NotificationWindowOpen != value) {
            settings.NotificationWindowOpen = value;
            settings.Save();
        }
    }

    protected override void Render() {
        base.Render();

        _minLevelField ??= Fields.EnumNamesDropdown(Settings.Instance.MinimumNotificationLevel);
        _minLevelField.Translated("rysy.notifications.window.minLevel");
        
        if (_minLevelField.RenderGui(Settings.Instance.MinimumNotificationLevel.FastToString()) is string newLevelStr) {
            Settings.Instance.MinimumNotificationLevel = Enum.Parse<LogLevel>(newLevelStr);
            Settings.Instance.Save();
        }
        
        ImGui.Separator();

        var displayedNotifs = 0;

        for (int i = Notifications.Count - 1; i >= 0; i--) {
            var notifInfo = Notifications[i];
            var notif = notifInfo.Notif;

            if (notif.Level < Settings.Instance.MinimumNotificationLevel)
                continue;
            
            displayedNotifs++;
            
            ImGui.PushID(i);
            
            if (ImGui.Button(((char)ImGuiIcons.EyeSlash).ToString())) {
                Notifications.RemoveAt(i);
            }
            ImGui.SameLine();
            ImGui.TextDisabled(notifInfo.Time.ToShortTimeString());
            ImGui.SameLine();

            var color = notif.Level.ToColorNumVec();
            ImGuiManager.TranslatedText(notif.Message, color);
            ImGui.SameLine();
            
            notif.RenderGui();
            ImGui.SameLine();
            ImGui.NewLine();
            
            ImGui.PopID();
        }
        
        if (displayedNotifs == 0) {
            ImGui.BeginDisabled();
            ImGuiManager.TranslatedText("rysy.notifications.window.nothingToShow");
            ImGui.EndDisabled();
        }
    }

    public static void AddNotification(INotification notification) {
        Notifications.Add(new(notification, DateTime.Now));

        if (notification.Level >= Settings.Instance.MinimumNotificationLevel) {
            RysyState.Scene.AddWindowIfNeeded<NotificationsWindow>();
        }
    }
    
    sealed record NotificationInfo(INotification Notif, DateTime Time);
}

public interface INotification {
    public LogLevel Level { get; }
    
    public string Message { get; }

    public void RenderGui();
}

public sealed class MapAnalyzerErrorsNotification : INotification {
    public LogLevel Level => LogLevel.Error;
    public string Message => "rysy.notifications.mapAnalyzerErrors";
    
    public void RenderGui() {
        if (ImGuiManager.TranslatedButton("rysy.notifications.mapAnalyzerErrors.open")) {
            RysyState.Scene.AddWindowIfNeeded<MapAnalyzerWindow>();
        }
    }
}

public sealed class MapSavedNotification : INotification {
    public LogLevel Level => LogLevel.Info;
    public string Message => "rysy.notifications.mapSaved";
    
    public void RenderGui() {
        
    }
}