using ImGuiNET;
using Rysy.Extensions;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

internal class ModSettingsWindow : Window {
    public ModMeta Mod { get; set; }

    public static (FieldList? Main, FieldList? OtherValues) GetFields(ModMeta mod) {
        var settings = mod.Settings;
        if (settings is null) {
            return (null, null);
        }
        var type = settings.GetType();


        FieldList fields = new();
        foreach (var prop in type.GetProperties()) {
            var propVal = prop.GetValue(settings);
            var field = Fields.GuessFromValue(propVal, fromMapData: false);
            field?.WithNameTranslated($"rysy.modSettings.{mod.Name}.{prop.Name}.name");
            field?.WithTooltipTranslated($"rysy.modSettings.{mod.Name}.{prop.Name}.tooltip");

            if (field is { })
                fields.Add(prop.Name, field);
        }

        FieldList otherValueFields = new();
        // todo: determine whether this is useful
        foreach (var (key, val) in settings.OtherValues) {
            var field = Fields.GuessFromValue(val, fromMapData: false);

            if (field is { })
                otherValueFields.Add(key, field);
        }

        return (fields.Count > 0 ? fields : null, otherValueFields.Count > 0 ? otherValueFields : null);
    }

    public FormWindow? MainForm, OtherForm;

    public ModSettingsWindow(ModMeta mod) : base("rysy.modSettings.windowName".TranslateFormatted(mod.Name), new(500, 200)) {
        Mod = mod;

        var (mainFields, otherFields) = GetFields(Mod);

        if (mainFields is { }) {
            MainForm = new(mainFields, "Main");
            MainForm.OnChanged += (changed) => {
                var settings = mod.Settings;
                if (settings is null) {
                    return;
                }

                var type = settings.GetType();
                foreach (var (key, value) in changed) {
                    type.GetProperty(key)!.SetValue(settings, value);
                }
            };
        }

        if (otherFields is { }) {
            OtherForm = new(otherFields, "Other");
            OtherForm.OnChanged += (changed) => {
                var settings = mod.Settings;
                if (settings is null) {
                    return;
                }

                foreach (var (key, value) in changed) {
                    settings.OtherValues[key] = value;
                }
            };
        }
    }

    protected override void Render() {
        base.Render();

        if (OtherForm is { } && MainForm is { }) {
            if (!ImGui.BeginTabBar(""))
                return;

            if (ImGui.BeginTabItem("rysy.modSettings.mainTab".Translate())) {
                MainForm?.RenderBody();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("rysy.modSettings.unknownTab".Translate())) {
                OtherForm?.RenderBody();

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
            return;
        }

        // only one of these are non-null here
        MainForm?.RenderBody();
        OtherForm?.RenderBody();
    }
}
