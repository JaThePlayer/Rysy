using System.Diagnostics;

namespace Rysy.Gui.FieldTypes;

/// <summary>
/// Used by Decal Registry `animation` and `scared` properties
/// </summary>
public sealed record AnimationFrameField : ComplexTypeField<AnimationFrameEntry> {
    public override AnimationFrameEntry Parse(string data) {
        if (AnimationFrameEntry.TryParse(data, out var parsed))
            return parsed;
        return default;
    }

    public override string ConvertToString(AnimationFrameEntry data) => data.ToString();

    public override bool RenderDetailedWindow(ref AnimationFrameEntry data) {
        var ret = false;

        ret |= ImGuiManager.EnumComboTranslated("rysy.animationFrameEntryKind", ref data.Type);

        switch (data.Type) {
            case AnimationFrameEntry.Kind.SingleFrame:
                ret |= ImGuiManager.TranslatedInputInt("rysy.animationFrame.singleFrame", ref data.Id);
                break;
            case AnimationFrameEntry.Kind.Repeat:
                ret |= ImGuiManager.TranslatedInputInt("rysy.animationFrame.singleFrame", ref data.Id);
                ret |= ImGuiManager.TranslatedInputInt("rysy.animationFrame.repeatAmt", ref data.SecondNum);
                break;
            case AnimationFrameEntry.Kind.Range:
                ret |= ImGuiManager.TranslatedInputInt("rysy.animationFrame.firstFrame", ref data.Id);
                ret |= ImGuiManager.TranslatedInputInt("rysy.animationFrame.lastFrame", ref data.SecondNum);
                break;
        }
        
        return ret;
    }
}

public struct AnimationFrameEntry {
    public Kind Type;
    public int Id;
    public int SecondNum;

    public AnimationFrameEntry() {
        
    }

    public override string ToString() => Type switch {
        Kind.SingleFrame => Id.ToString(CultureInfo.InvariantCulture),
        Kind.Range => $"{Id}-{SecondNum}",
        Kind.Repeat => $"{Id}*{SecondNum}",
        _ => throw new UnreachableException()
    };

    public static bool TryParse(ReadOnlySpan<char> str, out AnimationFrameEntry ret) {
        ret = new();
        
        var starId = str.IndexOf('*');
        if (starId != -1) {
            ret.Type = Kind.Repeat;
            if (!int.TryParse(str[..starId], CultureInfo.InvariantCulture, out ret.Id))
                return false;
            if (!int.TryParse(str[(starId+1)..], CultureInfo.InvariantCulture, out ret.SecondNum))
                return false;
            
            return true;
        }
        
        var dashId = str.IndexOf('-');
        if (dashId != -1) {
            ret.Type = Kind.Range;
            
            if (!int.TryParse(str[..dashId], CultureInfo.InvariantCulture, out ret.Id))
                return false;
            if (!int.TryParse(str[(dashId+1)..], CultureInfo.InvariantCulture, out ret.SecondNum))
                return false;
            
            return true;
        }

        ret.Type = Kind.SingleFrame;
        return int.TryParse(str, CultureInfo.InvariantCulture, out ret.Id);
    }

    public enum Kind {
        SingleFrame,
        Range,
        Repeat,
    }
}