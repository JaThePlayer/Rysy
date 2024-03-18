using ImGuiNET;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Gui.FieldTypes;

public record BirdTutorialInputField : ComplexTypeField<BirdTutorialInput> {
    public override BirdTutorialInput Parse(string data) {
        return BirdTutorialInput.TryParse(data, out var input) ? input : BirdTutorialInput.Default;
    }

    public override string ConvertToString(BirdTutorialInput data) => data.ToString();

    public override bool RenderDetailedWindow(ref BirdTutorialInput data) {
        var kind = data.GetKind();
        var changed = false;

        if (ImGuiManager.Combo("Type", ref kind, BirdTutorialInput.KindsAsList, x => x.ToString(), tooltip: null)) {
            changed = true;
            data.SetToDefaultForKind(kind);
        }
        
        ImGui.Separator();

        switch (kind) {
            case BirdTutorialInput.Kind.Input:
                changed |= ImGuiManager.Combo("Input", ref data.Literal, BirdTutorialInput.ValidInputs, x => x, tooltip: null);
                break;
            case BirdTutorialInput.Kind.Direction:
                changed |= ImGuiManager.Combo("Direction", ref data.Literal, BirdTutorialInput.ValidDirections, x => x, tooltip: null);
                break;
            case BirdTutorialInput.Kind.DialogKey: {
                var dialogKey = data.ExtractDialogKey().ToString();
                if (ImGui.InputText("Dialog Key", ref dialogKey, 512)) {
                    changed = true;
                    data.Literal = $"dialog:{dialogKey}";
                }
                break;
            }
            case BirdTutorialInput.Kind.TexturePath: {
                changed |= ImGui.InputText("Texture Path", ref data.Literal, 512).WithTranslatedTooltip("rysy.fields.birdTutorialInput.texturePath");
                break;
            }
        }

        return changed;
    }
}

// Down, DownRight, Right, UpRight, Up, UpLeft, Left, DownLeft
// Jump, Dash, CrouchDash, Grab, Talk, ESC, Pause, MenuLeft, MenuRight, MenuUp, MenuDown, MenuConfirm, MenuCancel, MenuJournal, QuickRestart
// dialog:dialogKey
// a path to a texture in the Gui atlas (for example "tinyarrow")
// plaintext
public class BirdTutorialInput {
    public string Literal;

    public BirdTutorialInput(string value) {
        Literal = value;
    }
    
    public Kind GetKind() => Literal.AsSpan() switch {
        "Down" or "DownRight" or "Right" or "UpRight" or "Up" or "UpLeft" or "Left" or "DownLeft" => Kind.Direction,
        "Jump" or "Dash" or "CrouchDash" or "Grab" or "Talk" or "ESC" or 
        "Pause" or "MenuLeft" or "MenuRight" or "MenuUp" or "MenuDown" or
        "MenuConfirm" or "MenuCancel" or "MenuJournal" or "QuickRestart" => Kind.Input,
        [ 'd', 'i', 'a', 'l', 'o', 'g', ':', .. ] => Kind.DialogKey,
        var other => /*Gfx.Gui.Has(other)*/ Kind.TexturePath,
    };

    public void SetToDefaultForKind(Kind kind) {
        Literal = kind switch {
            Kind.Direction => "Down",
            Kind.Input => "Jump",
            Kind.DialogKey => "dialog:",
            Kind.TexturePath => "",
            _ => "",
        };
    }

    public ReadOnlySpan<char> ExtractDialogKey() => Literal.AsSpan() is ['d', 'i', 'a', 'l', 'o', 'g', ':', .. var rest]
        ? rest
        : ReadOnlySpan<char>.Empty;
    
    public static bool TryParse(ReadOnlySpan<char> data, [NotNullWhen(true)] out BirdTutorialInput? res) {
        res = new(data.ToString());
        return true;
    }

    public override string ToString() => Literal;

    public static BirdTutorialInput Default => new("");

    public enum Kind {
        Direction,
        Input,
        DialogKey,
        TexturePath
    }

    internal static List<Kind> KindsAsList = Enum.GetValues<Kind>().ToList();

    internal static List<string> ValidDirections { get; } = ["Down", "DownRight", "Right", "UpRight", "Up", "UpLeft", "Left", "DownLeft"];
    internal static List<string> ValidInputs { get; } = [
        "Jump", "Dash", "CrouchDash", "Grab", "Talk", "ESC", 
        "Pause", "MenuLeft", "MenuRight", "MenuUp", "MenuDown",
        "MenuConfirm", "MenuCancel", "MenuJournal", "QuickRestart"
    ];
}