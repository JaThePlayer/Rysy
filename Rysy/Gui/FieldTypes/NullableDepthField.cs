using ImGuiNET;
using Rysy.Helpers;

using DepthValue = (int Value, string Name);

namespace Rysy.Gui.FieldTypes;

public sealed record NullableDepthField : Field {
    private object? Default;

    public override object GetDefault() => Default!;

    public override void SetDefault(object newDefault) => Default = newDefault;

    private string _search = "";
    private ComboCache<DepthValue> _comboCache = new();
    
    public override object? RenderGui(string fieldName, object value) {
        int? val = value is int j ? j : null;

        object? returnValue = null;

        bool changed = false;
        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        var valueToString = value?.ToString() ?? "";
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText($"##text{fieldName}", ref valueToString, 128).WithTooltip(Tooltip)) {
            if (valueToString.IsNullOrWhitespace()) {
                returnValue = null;
            }
            if (int.TryParse(valueToString, CultureInfo.InvariantCulture, out var newVal)) {
                returnValue = newVal;
            }
            changed = true;
        }


        ImGui.SameLine(0f, xPadding);
        
        var size = _comboCache.Size ??= ImGuiManager.CalcListSize(Values.Select(v => v.Name));
        ImGui.SetNextWindowSize(new(size.X.AtLeast(320f), ImGui.GetTextLineHeightWithSpacing() * 16.AtMost(Values.Length + 1) + ImGui.GetFrameHeight()));
        if (ImGui.BeginCombo($"##combo{fieldName}", valueToString, ImGuiComboFlags.NoPreview).WithTooltip(Tooltip)) {
            var oldStyles = ImGuiManager.PopAllStyles();

            ImGui.InputText("Search", ref _search, 512);

            var filtered = _comboCache.GetValue(Values, x => x.Name, _search);
            
            ImGui.BeginChild($"comboInner{fieldName}");

            foreach (var item in filtered.OrderBy(x => x.Value)) {
                if (ImGui.MenuItem(item.Name)) {
                    returnValue = item.Value;
                    changed = true;
                }
            }
            ImGui.EndChild();
            ImGui.EndCombo();
            
            ImGuiManager.PushAllStyles(oldStyles);
        }
        ImGui.SameLine(0f, xPadding);
        ImGui.Text(fieldName);
        true.WithTooltip(Tooltip);

        return returnValue ?? (changed ? new FieldNullReturn() : null);
    }

    public override Field CreateClone() => this with { };
    
    private static readonly DepthValue[] Values = [
        (Depths.FormationSequences, "FormationSequences (-2000000)"),
        (Depths.Top, "Top (-1000000)"),
        (Depths.FGParticles, "FGParticles (-50000)"),
        (Depths.FakeWalls, "FakeWalls (-13000)"),
        (Depths.Enemy, "Enemy (-12500)"),
        (Depths.PlayerDreamDashing, "PlayerDreamDashing (-12000)"),
        (Depths.CrystalSpinners, "CrystalSpinners (-11500)"),
        (Depths.DreamBlocks, "DreamBlocks (-11000)"),
        (Depths.FGDecals, "FGDecals (-10500)"),
        (Depths.FGTerrain, "FGTerrain (-10000)"),
        (Depths.Solids, "Solids (-9000)"),
        (Depths.Above, "Above (-8500)"),
        (Depths.Particles, "Particles (-8000)"),
        (Depths.Seeker, "Seeker (-200)"),
        (Depths.Pickups, "Pickups (-100)"),
        (Depths.Dust, "Dust (-50)"),
        (Depths.Player, "Player (0)"),
        (Depths.TheoCrystal, "TheoCrystal (100)"),
        (Depths.NPCs, "NPCs (1000)"),
        (Depths.Below, "Below (2000)"),
        (Depths.SolidsBelow, "SolidsBelow (5000)"),
        (Depths.BGParticles, "BGParticles (8000)"),
        (Depths.BGDecals, "BGDecals (9000)"),
        (Depths.BGMirrors, "BGMirrors (9500)"),
        (Depths.BGTerrain, "BGTerrain (10000)"),
    ];
}