using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;

namespace Rysy.Gui.Windows;

public sealed class RoomEditWindow : Window {
    public const int WIDTH = 800;
    private Room Room;
    private RoomAttributes Attrs;

    private bool GeneralTabInvalid;

    private bool NewRoom;

    public RoomEditWindow(Room room, bool newRoom) : base($"Room Edit - {room.Name}", new(WIDTH, /*250*/ ImGuiManager.CalcListHeight(12))) {
        Room = room;

        Attrs = room.Attributes.Copy();
        NewRoom = newRoom;
    }

    protected override void Render() {
        ImGui.BeginTabBar("TabBar");

        GeneralTab();
        MusicTab();

        ImGui.EndTabBar();

        /*
        var valid = Valid(Room.Map) && Attrs != Room.Attributes;
        ImGuiManager.BeginWindowBottomBar(valid);

        if (ImGui.Button("Apply Changes")) {
            var newRoom = NewRoom;
            EditorState.History?.ApplyNewAction(new RoomAttributeChangeAction(Room, Attrs));
            if (newRoom) {
                EditorState.CurrentRoom = Room;
            }
        }

        ImGuiManager.EndWindowBottomBar();*/
    }

    protected override bool HasBottomBar => true;

    protected override void RenderBottomBar() {
        var valid = Valid(Room.Map) && Attrs != Room.Attributes;

        ImGui.BeginDisabled(!valid);
        if (ImGui.Button("Apply Changes")) {
            var newRoom = NewRoom;
            EditorState.History?.ApplyNewAction(new RoomAttributeChangeAction(Room, Attrs));
            if (newRoom) {
                EditorState.CurrentRoom = Room;
            }
        }
        ImGui.EndDisabled();
    }

    private string MusicDropdownSearch = "";

    private void MusicTab() {
        if (!ImGui.BeginTabItem("Music")) {
            return;
        }

        ImGui.Columns(2, "Music", false);

        //StringInput("Music", ref Attrs.Music);
        ImGui.SetNextItemWidth(ImGui.GetColumnWidth() / 2);
        ImGuiManager.EditableCombo("Music", ref Attrs.Music, CelesteEnums.Music, s => s, ref MusicDropdownSearch);
        ImGui.NextColumn();

        StringInput("Alt Music", ref Attrs.AltMusic);
        StringInput("Music Progress", ref Attrs.MusicProgress);
        StringInput("Ambience Progress", ref Attrs.AmbienceProgress);

        ImGui.Separator();

        ImGui.Checkbox("Layer 1", ref Attrs.MusicLayer1);
        ImGui.Checkbox("Layer 2", ref Attrs.MusicLayer2);
        ImGui.Checkbox("Layer 3", ref Attrs.MusicLayer3);
        ImGui.Checkbox("Layer 4", ref Attrs.MusicLayer4);

        ImGui.NextColumn();

        ImGui.Checkbox("Whisper", ref Attrs.Whisper);
        ImGui.Checkbox("Delay Alt Music Fade", ref Attrs.DelayAltMusicFade);
        
        ImGui.Columns();
        ImGui.EndTabItem();
    }

    private void GeneralTab() {
        var name = Attrs.Name;

        ImGuiManager.PushInvalidStyleIf(GeneralTabInvalid);
        bool begin = ImGui.BeginTabItem("General");
        ImGuiManager.PopInvalidStyle();
        if (!begin) {
            return;
        }

        GeneralTabInvalid = false;

        ImGui.Columns(2, "General", false);

        GeneralTabInvalid |= ImGuiManager.PushInvalidStyleIf(!RoomNameValid(Room.Map));
        if (StringInput("Name", ref name)) {
            Attrs.Name = name;
        }
        ImGuiManager.PopInvalidStyle();

        #region Debug color picker
        var colors = CelesteEnums.RoomColors;
        var colorNames = CelesteEnums.RoomColorNames;
        ImGui.ColorButton(colorNames[Attrs.C], colors[Attrs.C].ToNumVec4());
        ImGui.SameLine();
        if (ImGui.BeginCombo("Color", Attrs.C.ToString(CultureInfo.InvariantCulture), ImGuiComboFlags.NoPreview)) {
            for (int i = 0; i < colors.Length; i++) {
                if (ImGui.ColorButton(colorNames[i], colors[i].ToNumVec4())) {
                    Attrs.C = i;
                }
                ImGui.SameLine();
            }
            ImGui.EndCombo();
        }

        ImGui.NextColumn();
        #endregion

        DivBy8Input("X", ref Attrs.X);
        DivBy8Input("Y", ref Attrs.Y);
        DivBy8Input("Width", ref Attrs.Width);
        DivBy8Input("Height", ref Attrs.Height);

        ImGui.InputInt("Camera Offset X", ref Attrs.CameraOffsetX);
        ImGui.NextColumn();
        ImGui.InputInt("Camera Offset Y", ref Attrs.CameraOffsetY);
        ImGui.NextColumn();

        ImGuiManager.Combo("Wind Pattern", ref Attrs.WindPattern);
        ImGui.NextColumn();

        ImGui.Checkbox("Dark", ref Attrs.Dark);
        ImGui.NextColumn();

        ImGui.Checkbox("Disable Down Transition", ref Attrs.DisableDownTransition);
        ImGui.NextColumn();

        ImGui.Checkbox("Underwater", ref Attrs.Underwater);
        ImGui.NextColumn();

        ImGui.Checkbox("Checkpoint", ref Attrs.Checkpoint);
        ImGui.NextColumn();

        ImGui.Checkbox("Space", ref Attrs.Space);
        ImGui.NextColumn();



        ImGui.Columns();
        ImGui.EndTabItem();
    }

    private bool DivBy8Input(string name, ref int undivided) {
        var divided = undivided / 8;
        bool ret;
        if (ret = ImGui.InputInt(name, ref divided))
            undivided = divided * 8;

        ImGui.NextColumn();

        return ret;
    }

    private bool StringInput(string name, ref string val) {
        val ??= "";
        ImGui.SetNextItemWidth(ImGui.GetColumnWidth() / 2);
        var ret = ImGui.InputText(name, ref val, 128);
        ImGui.NextColumn();

        return ret;
    }

    private bool RoomNameValid(Map map) =>
        !string.IsNullOrWhiteSpace(Attrs.Name) && (
            (!NewRoom && Attrs.Name == Room.Name) // if we haven't changed the name, then it must be correct
            || !map.Rooms.Any(r => r.Name == Attrs.Name)
        );

    private bool Valid(Map map) {
        return RoomNameValid(map);
    }
}
