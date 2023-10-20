using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;
using System.Reflection.Emit;

namespace Rysy.Gui.FieldTypes;

internal record class FadeField : Field {
    public Fade Default { get; set; }

    public override Field CreateClone() => this with { };

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault) => Default = GetFadeOrNull(newDefault) ?? new("");

    private Fade GetFade(object obj) => obj switch {
        string str => new Fade(str),
        Fade f => f,
        null => null!,
        var other => new Fade(other.ToString() ?? ""),
    };

    private Fade? GetFadeOrNull(object obj) {
        try {
            return GetFade(obj);
        } catch {
            return null;
        }
    }

    public override object? RenderGui(string fieldName, object value) {
        var str = value.ToString();
        var region = GetFadeOrNull(value)?.Regions.FirstOrDefault();
        bool edited = false;

        /*
        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - buttonWidth - xPadding);
        if (ImGui.InputText($"##text{fieldName}", ref str, 128).WithTooltip(Tooltip)) {
            edited = true;
        }

        ImGui.SameLine(0f, xPadding);*/

        if (ImGui.BeginCombo(fieldName, str)) {
            var pos = region?.PositionRange() ?? default;
            if (ImGuiManager.TranslatedInputFloat2("rysy.fields.fade.pos", ref pos)) {
                if (region is null) {
                    region = new(pos.X, pos.Y, 1f, 1f);
                } else {
                    region = region with {
                        FromPos = pos.X,
                        ToPos = pos.Y,
                    };
                }

                edited = true;
            }

            pos = region?.AlphaRange() ?? default;
            if (ImGuiManager.TranslatedDragFloat2("rysy.fields.fade.alpha", ref pos, 0.01f, 0f, 1f)) {
                if (region is null) {
                    region = new(0f, 0f, pos.X, pos.Y);
                } else {
                    region = region with {
                        FromAlpha = pos.X,
                        ToAlpha = pos.Y,
                    };
                }

                edited = true;
            }

            ImGui.EndCombo();
        }

        if (edited)
            return region?.ToString() ?? str;

        return null;
    }

    public override bool IsValid(object? value) {
        if (GetFadeOrNull(value ?? "") is null) 
            return false;

        return base.IsValid(value);
    }
}
