using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

internal sealed record FadeRegionField : ComplexTypeField<Fade.Region> {
    public override Fade.Region Parse(string data)
        => Fade.Region.Parse(data);

    public override string ConvertToString(Fade.Region data)
        => data.ToString();

    public override bool RenderDetailedWindow(ref Fade.Region region) {
        var edited = false;
        
        var pos = region.PositionRange();
        if (ImGuiManager.TranslatedInputFloat2("rysy.fields.fade.pos", ref pos)) {
            region = region with {
                FromPos = pos.X,
                ToPos = pos.Y,
            };

            edited = true;
        }

        pos = region.AlphaRange();
        if (ImGuiManager.TranslatedDragFloat2("rysy.fields.fade.alpha", ref pos, 0.01f, 0f, 1f)) {
            region = region with {
                FromAlpha = pos.X,
                ToAlpha = pos.Y,
            };

            edited = true;
        }

        return edited;
    }
}
