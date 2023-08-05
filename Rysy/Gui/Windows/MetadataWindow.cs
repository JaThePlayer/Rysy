using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

public sealed class MetadataWindow : Window {
    private static FieldList AddTooltips(FieldList fields) {
        foreach (var (name, field) in fields) {
            field.Tooltip ??= name.TranslateOrNull("rysy.metadata.field.description");
            field.NameOverride ??= name.TranslateOrNull("rysy.metadata.field.name");
        }

        return fields;
    }

    public static FieldList GetMainFieldInfo(Map map, MapMetadata meta) {
        var fs = map.Mod?.GetAllDependenciesFilesystem();

        return AddTooltips(new() {
            ["AnimatedTiles"] = Fields.Path(meta.AnimatedTiles, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            ["BackgroundTiles"] = Fields.Path(meta.BackgroundTiles, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            ["BloomBase"] = Fields.Float(meta.BloomBase ?? 0f),
            ["BloomStrength"] = Fields.Float(meta.BloomStrength ?? 1f),
            ["ColorGrade"] = Fields.Path(meta.ColorGrade, "Graphics/ColorGrading", "png").AllowNull(),
            ["CoreMode"] = Fields.String(meta.CoreMode).AllowNull(), // todo: dropdown
            ["DarknessAlpha"] = Fields.Float(meta.DarknessAlpha ?? 0.05f),
            ["Dreaming"] = Fields.Bool(meta.Dreaming ?? false),
            ["ForegroundTiles"] = Fields.Path(meta.ForegroundTiles, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            ["Icon"] = Fields.String(meta.Icon).AllowNull(),
            ["Interlude"] = Fields.Bool(meta.Interlude ?? false),
            ["IntroType"] = Fields.EnumNamesDropdown<CelesteEnums.IntroTypes>(meta.IntroType).AllowNull(),
            ["Portraits"] = Fields.String(meta.Portraits).AllowNull(),
            ["PostcardSoundID"] = Fields.String(meta.PostcardSoundID).AllowNull(),
            ["Sprites"] = Fields.Path(meta.Sprites, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            ["TitleAccentColor"] = Fields.RGB((meta.TitleAccentColor ?? "2f344b").FromRGB()),
            ["TitleBaseColor"] = Fields.RGB((meta.TitleBaseColor ?? "6c7c81").FromRGB()),
            ["TitleTextColor"] = Fields.RGB((meta.TitleTextColor ?? "ffffff").FromRGB()),
            ["Wipe"] = Fields.String(meta.Wipe).AllowNull(), // todo: dropdown
        });
    }

    public static FieldList GetModeFieldInfo(MapMetadata meta) => AddTooltips(new() {
        ["OverrideASideMeta"] = Fields.Bool(meta.OverrideASideMeta ?? false),
        ["mode:HeartIsEnd"] = Fields.Bool(meta.Mode.HeartIsEnd ?? false),
        ["mode:Inventory"] = Fields.EnumNamesDropdown<CelesteEnums.Inventories>(meta.Mode.Inventory).AllowNull(),
        //["mode:PoemID"] = Fields.String(map.Mode.PoemID).AllowNull(),
        ["mode:SeekerSlowdown"] = Fields.Bool(meta.Mode.SeekerSlowdown ?? false),
        ["mode:StartLevel"] = Fields.String(meta.Mode.StartLevel).AllowNull(),
        ["mode:TheoInBubble"] = Fields.Bool(meta.Mode.TheoInBubble ?? false),
    });

    public static FieldList GetCassetteFieldInfo(MapMetadata meta) => AddTooltips(new() {
        ["CassetteSong"] = Fields.String(meta.CassetteSong).AllowNull(),
        ["cassettemodifier:BeatIndexOffset"] = Fields.Int(meta.CassetteModifier.BeatIndexOffset),
        ["cassettemodifier:BeatsMax"] = Fields.Int(meta.CassetteModifier.BeatsMax),
        ["cassettemodifier:BeatsPerTick"] = Fields.Int(meta.CassetteModifier.BeatsPerTick),
        ["cassettemodifier:Blocks"] = Fields.Int(meta.CassetteModifier.Blocks),
        ["cassettemodifier:LeadBeats"] = Fields.Int(meta.CassetteModifier.LeadBeats),
        ["cassettemodifier:OldBehavior"] = Fields.Bool(meta.CassetteModifier.OldBehavior),
        ["cassettemodifier:TempoMult"] = Fields.Float(meta.CassetteModifier.TempoMult),
        ["cassettemodifier:TicksPerSwap"] = Fields.Int(meta.CassetteModifier.TicksPerSwap),
    });

    public static FieldList GetMusicFieldInfo(MapMetadata meta) => AddTooltips(new() {
        ["mode:audiostate:Music"] = Fields.Dropdown(meta.Mode.AudioState.Music, CelesteEnums.Music).AllowNull().AllowEdits(),
        ["mode:audiostate:Ambience"] = Fields.String(meta.Mode.AudioState.Ambience).AllowNull(), // todo: dropdown
    });

    public HistoryHandler History { get; private set; }
    public Map Map { get; private set; }

    private List<(string Name, FormWindow Window)> Tabs = new();

    public MetadataWindow(HistoryHandler history, Map map) : base("Map Metadata", new(800, 400)) {
        Map = map;
        History = history;

        Tabs.Add(("Main", new(GetMainFieldInfo(map, map.Meta), "##main") {
            OnChanged = ApplyChanges
        }));
        Tabs.Add(("Mode", new(GetModeFieldInfo(map.Meta), "##mode") {
            OnChanged = ApplyChanges
        }));
        Tabs.Add(("Cassette", new(GetCassetteFieldInfo(map.Meta), "##cass") {
            OnChanged = ApplyChanges
        }));
        Tabs.Add(("Music", new(GetMusicFieldInfo(map.Meta), "##music") {
            OnChanged = ApplyChanges
        }));

        NoSaveData = false;
    }

    private void ApplyChanges(Dictionary<string, object> edited) {
        var map = Map;

        var oldMetaPacked = map.Meta.Pack();
        foreach (var (name, val) in edited) {
            var nameSplit = name.Split(':');
            var (innerchild, child, fieldName) = nameSplit.Length switch {
                1 => (null, null, nameSplit[0]),
                2 => (null, nameSplit[0], nameSplit[1]),
                3 => (nameSplit[1], nameSplit[0], nameSplit[2]),
                _ => throw new NotImplementedException()
            };

            var el = child is null ? oldMetaPacked : oldMetaPacked.Children.First(c => c.Name == child);
            if (innerchild is { })
                el = el.Children.First(c => c.Name == innerchild);

            el.Attributes[fieldName] = val;
        }

        var newMeta = new MapMetadata().Unpack(oldMetaPacked);

        History.ApplyNewAction(new MapMetaEditAction(map, newMeta));
    }

    protected override void Render() {
        base.Render();

        if (ImGui.BeginTabBar("Tabbar")) {
            foreach (var tab in Tabs) {
                if (ImGui.BeginTabItem(tab.Name)) {
                    tab.Window.RenderBody();
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }
}
