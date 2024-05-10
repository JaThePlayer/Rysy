using ImGuiNET;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
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
        var fs = map.Mod?.Filesystem;
        var allDependenciesFs = map.Mod?.GetAllDependenciesFilesystem();

        return AddTooltips(new(new {
            AnimatedTiles = Fields.Path(meta.AnimatedTiles, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            BackgroundTiles = Fields.Path(meta.BackgroundTiles, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            ForegroundTiles = Fields.Path(meta.ForegroundTiles, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            Sprites = Fields.Path(meta.Sprites, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            BloomBase = Fields.Float(meta.BloomBase ?? 0f),
            BloomStrength = Fields.Float(meta.BloomStrength ?? 1f),
            ColorGrade = Fields.Path(meta.ColorGrade, "Graphics/ColorGrading", "png", allDependenciesFs).AllowNull(),
            CoreMode = Fields.EnumNamesDropdown(CelesteEnums.CoreModes.None).AllowNull(),
            DarknessAlpha = Fields.Float(meta.DarknessAlpha ?? 0.05f),
            Dreaming = Fields.Bool(meta.Dreaming ?? false),
            IntroType = Fields.EnumNamesDropdown<CelesteEnums.IntroTypes>(meta.IntroType).AllowNull(),
            Portraits = Fields.Path(meta.Portraits, "Graphics", "xml", fs).AllowNull().WithConverter(p => p.Path),
            PostcardSoundID = Fields.String(meta.PostcardSoundID).AllowNull(),
            Wipe = Fields.String(meta.Wipe).AllowNull(), // todo: dropdown
        }));
    }

    public static FieldList GetModeFieldInfo(MapMetadata meta) => AddTooltips(new(new {
        OverrideASideMeta = Fields.Bool(meta.OverrideASideMeta ?? false),
        mode_HeartIsEnd = Fields.Bool(meta.Mode.HeartIsEnd ?? false),
        mode_Inventory = Fields.EnumNamesDropdown<CelesteEnums.Inventories>(meta.Mode.Inventory).AllowNull(),
        //["mode_PoemID"] = Fields.String(map.Mode.PoemID).AllowNull(),
        mode_SeekerSlowdown = Fields.Bool(meta.Mode.SeekerSlowdown ?? false),
        mode_StartLevel = Fields.String(meta.Mode.StartLevel).AllowNull(),
        mode_TheoInBubble = Fields.Bool(meta.Mode.TheoInBubble ?? false),
    }));

    public static FieldList GetCassetteFieldInfo(MapMetadata meta) => AddTooltips(new(new {
        CassetteSong = Fields.Dropdown(meta.CassetteSong, CelesteEnums.CassetteMusic).AllowNull().AllowEdits(), 
        cassettemodifier_BeatIndexOffset = Fields.Int(meta.CassetteModifier.BeatIndexOffset),
        cassettemodifier_BeatsPerTick = Fields.Int(meta.CassetteModifier.BeatsPerTick),
        cassettemodifier_BeatsMax = Fields.Int(meta.CassetteModifier.BeatsMax),
        cassettemodifier_LeadBeats = Fields.Int(meta.CassetteModifier.LeadBeats),
        cassettemodifier_TicksPerSwap = Fields.Int(meta.CassetteModifier.TicksPerSwap),
        sep = new PaddingField(Text: "rysy.metadata.field.name.cassettemodifier_OldBehavior".Translate()),
        cassettemodifier_OldBehavior = Fields.Bool(meta.CassetteModifier.OldBehavior),
        sep2 = new PaddingField(DrawSeparator: false),
        cassettemodifier_Blocks = Fields.Int(meta.CassetteModifier.Blocks),
        cassettemodifier_TempoMult = Fields.Float(meta.CassetteModifier.TempoMult),
    }));

    public static FieldList GetMusicFieldInfo(MapMetadata meta) => AddTooltips(new(new {
        mode_audiostate_Music = Fields.Dropdown(meta.Mode.AudioState.Music, CelesteEnums.Music).AllowNull().AllowEdits(),
        mode_audiostate_Ambience = Fields.Dropdown(meta.Mode.AudioState.Ambience, CelesteEnums.Ambience).AllowNull().AllowEdits(),
    }));

    public static FieldList GetOverworldFieldInfo(Map map, MapMetadata meta) {
        var fs = map.Mod?.Filesystem;
        
        return AddTooltips(new(new {
            Icon = Fields.Path(meta.Icon, "Graphics/Atlases/Gui", "png", fs).AllowNull(), //Fields.String(meta.Icon).AllowNull(),
            Interlude = Fields.Bool(meta.Interlude ?? false),
            TitleAccentColor = Fields.RGB((meta.TitleAccentColor ?? "2f344b").FromRGB()),
            TitleBaseColor = Fields.RGB((meta.TitleBaseColor ?? "6c7c81").FromRGB()),
            TitleTextColor = Fields.RGB((meta.TitleTextColor ?? "ffffff").FromRGB()),
            preview = new TitleCardPreviewField(),
        }));
    }

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
        Tabs.Add(("Overworld", new(GetOverworldFieldInfo(map, map.Meta), "##overworld") {
            OnChanged = ApplyChanges
        }));

        NoSaveData = false;
    }

    private void ApplyChanges(Dictionary<string, object> edited) {
        var map = Map;

        var oldMetaPacked = map.Meta.Pack();
        foreach (var (name, val) in edited) {
            var nameSplit = name.Split('_');
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

        History.ApplyNewAction(new MapMetaEditAction(newMeta));
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

internal record TitleCardPreviewField : Field {
    public override object? RenderGui(string fieldName, object value) {
        ImGui.Columns();
        
        ImGui.SeparatorText("rysy.metadata.titleCardPreview".Translate());

        var height = (int)(ImGui.GetWindowHeight() - ImGui.GetCursorPosY());
        var width = (int) ImGui.GetWindowWidth();
        
        ImGuiManager.XnaWidget("tile_card_preview", width, height, () => {
            var renderCtx = SpriteRenderCtx.Default();
            
            var baseColor = Context.RGBA("TitleBaseColor", "6c7c81");
            var accentColor = Context.RGBA("TitleAccentColor", "2f344b");
            var textColor = Context.RGBA("TitleTextColor", "ffffff");
                
            var accentWidth = 30.AtMost(width / 3);
            
            ISprite.Rect(new(0, 0, width, height), baseColor).Render(renderCtx);
            ISprite.Rect(new(0, 0, accentWidth, height), accentColor).Render(renderCtx);
            ISprite.TextRect("rysy.metadata.titleCardPreview.text".Translate(),
                new(accentWidth, 0, width - accentWidth, height), textColor, scale: 12f).Render(renderCtx);
        });
        
        return null;
    }
    
    public override object GetDefault() => new();

    public override void SetDefault(object newDefault) {
    }

    public override Field CreateClone() => this with { };
}