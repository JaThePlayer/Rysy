using ImGuiNET;
using Rysy.Extensions;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

public sealed class RoomEditWindow : Window {
    private Action HistoryHook;
    private readonly FormWindow _generalTab;
    private readonly Room _room;
    private readonly RoomAttributes _attrs;
    private bool _newRoom;
    
    private FieldList GeneralTabFields(RoomAttributes attrs) {
        var fields = new FieldList(new {
            name = Fields.String(attrs.Name).WithValidator(s => RoomNameValid(this, s)),
            color = new RoomDebugColorField(attrs.C),
            
            x = Fields.Int(attrs.X).WithDisplayScale(8),
            y = Fields.Int(attrs.Y).WithDisplayScale(8),
            width = Fields.Int(attrs.Width).WithMin(8).WithDisplayScale(8),
            height = Fields.Int(attrs.Height).WithMin(8).WithDisplayScale(8),
            
            cameraOffsetX = attrs.CameraOffsetX,
            cameraOffsetY = attrs.CameraOffsetY,
            
            windPattern = attrs.WindPattern,
            dark = attrs.Dark,
            disableDownTransition = attrs.DisableDownTransition,
            underwater = attrs.Underwater,
            checkpoint = attrs.Checkpoint,
            space = attrs.Space,
            
            __musicSep = new PaddingField("Music"),

            music = Fields.Dropdown(attrs.Music, CelesteEnums.Music, editable: true),
            alt_music = Fields.Dropdown(attrs.AltMusic, CelesteEnums.Music, editable: true),
            musicProgress = attrs.MusicProgress,
            ambienceProgress = attrs.AmbienceProgress,
            musicLayer1 = attrs.MusicLayer1,
            musicLayer2 = attrs.MusicLayer2,
            musicLayer3 = attrs.MusicLayer3,
            musicLayer4 = attrs.MusicLayer4,
            whisper = attrs.Whisper,
            delayAltMusicFade = attrs.DelayAltMusicFade,
        });
        
        var tooltipKeyPrefix = "room.description";
        var nameKeyPrefix = "room.attribute";
        fields.AddTranslations(tooltipKeyPrefix, nameKeyPrefix, tooltipKeyPrefix, nameKeyPrefix);

        return fields;
    }
    
    private static bool RoomNameValid(RoomEditWindow window, string? name) =>
        !string.IsNullOrWhiteSpace(name) && (
            (!window._newRoom && name == window._room.Name) // if we haven't changed the name, then it must be correct
            || !window._room.Map.Rooms.Any(r => r.Name == name)
        );
    
    public RoomEditWindow(Room room, bool newRoom) : base($"Room Edit - {room.Name}") {
        Room room1 = room;
        var attrs = room.Attributes.Copy();
        _room = room;
        _attrs = attrs;
        _newRoom = newRoom;

        _generalTab = new FormWindow(GeneralTabFields(attrs), "");
        Size = _generalTab.Size;
        
        _generalTab.OnChanged += edited => {
            _newRoom = false;
            
            foreach (var (k, v) in edited) {
                attrs.SetValueByName(k, v);
            }
            
            EditorState.History?.ApplyNewAction(new RoomAttributeChangeAction(room1, attrs));
            if (newRoom) {
                EditorState.CurrentRoom = room1;
            }
        };

        var history = EditorState.History!;
        HistoryHook = ReevaluateEditedValues;
        history.OnApply += HistoryHook;
        history.OnUndo += HistoryHook;
        SetRemoveAction((w) => {
            history.OnApply -= HistoryHook;
            history.OnUndo -= HistoryHook;
        });
    }
    
    private void ReevaluateEditedValues() {
        _generalTab.EditedValues.Clear();

        foreach (var prop in _generalTab.FieldList) {
            if (prop.Name.StartsWith("__", StringComparison.Ordinal))
                continue;
            
            var name = prop.Name;
            var current = _room.Attributes.GetValueByName(prop.Name);
            var propValue = prop.ValueOrDefault();
            
            var equal = (current, propValue) switch {
                (int c, float val) => val == c,
                (float c, int val) => val == c,
                _ => current.ToString() == propValue.ToString(),
            };

            if (!equal) {
                _generalTab.EditedValues[name] = propValue;
            }
        }
    }

    protected override void Render() {
        base.Render();
        
        _generalTab.RenderBody();
    }

    public override void RenderBottomBar() {
        base.RenderBottomBar();
        
        _generalTab.RenderBottomBar();
    }
}

internal sealed record RoomDebugColorField : Field {
    public RoomDebugColorField(int def) {
        _default = def;
    }
    
    private int _default;
    
    public override object GetDefault() => _default;

    public override void SetDefault(object newDefault) {
        _default = Convert.ToInt32(newDefault, CultureInfo.InvariantCulture);
    }

    public override object? RenderGui(string fieldName, object value) {
        var c = Convert.ToInt32(value, CultureInfo.InvariantCulture);
        object? ret = null;
        
        var colors = CelesteEnums.RoomColors;
        var colorNames = CelesteEnums.RoomColorNames;
        
        ImGui.ColorButton(colorNames[c], colors[c].ToNumVec4());
        ImGui.SameLine();
        if (ImGui.BeginCombo("Color", "", ImGuiComboFlags.NoPreview).WithTooltip(Tooltip)) {
            var oldStyles = ImGuiManager.PopAllStyles();
            
            for (int i = 0; i < colors.Length; i++) {
                if (ImGui.ColorButton(colorNames[i], colors[i].ToNumVec4())) {
                    c = i;
                    ret = c;
                }
                ImGui.SameLine();
            }
            ImGui.EndCombo();
            ImGuiManager.PushAllStyles(oldStyles);
        }

        return ret;
    }

    public override Field CreateClone() => this with { };
}
