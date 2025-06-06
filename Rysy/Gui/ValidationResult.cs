﻿using Rysy.Helpers;
using Rysy.Mods;
using System.Numerics;
using System.Text;

namespace Rysy.Gui;

public sealed class ValidationResult : ITooltip {
    public static ValidationResult Ok { get; } = new();
    public static ValidationResult GenericError { get; } = new(ValidationMessage.GenericError);
    public static ValidationResult MustBeBool { get; } = new(ValidationMessage.ValueMustBeBool);
    public static ValidationResult MustBeInt { get; } = new(ValidationMessage.ValueMustBeInt);
    public static ValidationResult MustBeNumber { get; } = new(ValidationMessage.ValueMustBeNumber);
    public static ValidationResult MustBeChar { get; } = new(ValidationMessage.ValueMustBeChar);
    
    public static ValidationResult MustBeRgb { get; } = new(ValidationMessage.ValueMustBeRgb);
    public static ValidationResult MustBeRgba { get; } = new(ValidationMessage.ValueMustBeRgba);
    
    public static ValidationResult MustBeFreeTilesetId { get; } = new(ValidationMessage.ValueMustBeFreeTilesetId);
    
    public static ValidationResult InvalidDropdownElement { get; } = new(ValidationMessage.InvalidDropdownElement);
    public static ValidationResult InvalidTilesetId { get; } = new(ValidationMessage.InvalidTilesetId);
    
    public static ValidationResult TilesetDisplayNameInUse { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.tilesetDisplayNameInUse")));
    public static ValidationResult TexturePathInUse(string? modName)
        => modName is {} 
            ? new(ValidationMessage.Error(Tooltip.CreateTranslatedFormatted("rysy.validate.texturePathInUseByMod", ModRegistry.GetModByName(modName)?.DisplayName ?? modName)))
            : new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.texturePathInUse")));
    
    public static ValidationResult AssetPathInUse(string? modName)
        => modName is {} 
            ? new(ValidationMessage.Error(Tooltip.CreateTranslatedFormatted("rysy.validate.assetPathInUseByMod", ModRegistry.GetModByName(modName)?.DisplayName ?? modName)))
            : new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.assetPathInUse")));

    
    public static ValidationResult DuplicateRoomName { get; } = new(ValidationMessage.DuplicateRoomName);
    
    public static ValidationResult TooLarge(object max) => new(ValidationMessage.TooLarge(max));
    public static ValidationResult TooSmall(object min) => new(ValidationMessage.TooSmall(min));
    
    public static ValidationResult TooFewElements(int min) => new(ValidationMessage.TooFewElements(min));
    public static ValidationResult TooManyElements(int max) => new(ValidationMessage.TooManyElements(max));

    public static ValidationResult MustBeColor(ColorFormat format) => format switch {
        ColorFormat.RGB => MustBeRgb,
        ColorFormat.RGBA => MustBeRgba,
        _ => GenericError,
    };
    
    public static ValidationResult CantBeNull { get; } = new(ValidationMessage.ValueCantBeNull);
    
    public static ValidationResult PathNotUnique { get; } = new(ValidationMessage.PathNotUnique);
    
    public static ValidationResult AnimTileNameNotUnique { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.pathInvalid.mustBeUniqueAnimTileName")));
    
    public static ValidationResult PathNotValidInvalidChars { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.pathInvalid.invalidChars")));
    
    public static ValidationResult PathNotValidEmptyDir { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.pathInvalid.emptyDir")));
    
    public static ValidationResult ModNameUsedAlready { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.modIdUsedAlready")));
    
    public static ValidationResult InvalidCodeName { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.invalidCodeName")));
    
    public static ValidationResult ModFolderNameUsedAlready { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.modFolderNameUsedAlready")));
    
    public static ValidationResult InvalidEnglishDialogName { get; } 
        = new(ValidationMessage.Error(Tooltip.CreateTranslatedOrNull("rysy.validate.invalidEnglishDialogName")));
    
    public static ValidationResult OverlappingRooms { get; } 
        = new(ValidationMessage.Warn(Tooltip.CreateTranslatedOrNull("rysy.validate.overlappingRoomWarning")));
    
    public bool IsOk => !HasErrors;
    
    public bool HasErrors => _errors.Count > 0;
    
    public bool HasWarnings => _warns.Count > 0;
    
    bool ITooltip.IsEmpty => !HasErrors && !HasWarnings;

    public string GetRawText() {
        var sb = new StringBuilder();
        foreach (var err in _errors) {
            sb.AppendLine(err.Tooltip.GetRawText());
        }
        foreach (var err in _warns) {
            sb.AppendLine(err.Tooltip.GetRawText());
        }

        return sb.ToString();
    }

    private readonly List<ValidationMessage> _errors = [];
    private readonly List<ValidationMessage> _warns = [];
    
    public IEnumerable<ValidationMessage> AllMessages => _errors.Concat(_warns);

    public void Add(ValidationMessage msg) {
        switch (msg.Level) {
            case LogLevel.Error:
                _errors.Add(msg);
                break;
            case LogLevel.Warning:
                _warns.Add(msg);
                break;
        }
    }
    
    public ValidationResult(params Span<ValidationMessage> messages) {
        foreach (var msg in messages) {
            Add(msg);
        }
    }

    public static ValidationResult Combine(params Span<ValidationMessage?> messages) {
        return Combine(null, messages);
    }
    
    public static ValidationResult Combine(ValidationResult? prev, params Span<ValidationMessage?> messages) {
        ValidationResult? result = null;
        if (prev is not null and not { HasErrors: false, HasWarnings: false }) {
            result ??= new();
            foreach (var msg in prev.AllMessages) {
                result.Add(msg);
            }
        }
        
        foreach (var msg in messages) {
            if (msg is null)
                continue;
            result ??= new();
            result.Add(msg);
        }

        return result ?? Ok;
    }

    
    public void RenderImGui() {
        if (_errors.Count > 0) {
            ImGuiManager.PushInvalidStyle();
            foreach (var err in _errors) {
                ImGuiManager.Icon(ImGuiIcons.AlarmExclamation);
                err.Tooltip.RenderImGui();
            }
            ImGuiManager.PopInvalidStyle();
        }
        
        if (_warns.Count > 0) {
            ImGuiManager.PushWarningStyle();
            foreach (var warn in _warns) {
                ImGuiManager.Icon(ImGuiIcons.AlarmExclamation);
                warn.Tooltip.RenderImGui();
            }
            ImGuiManager.PopWarningStyle();
        }
    }
}

public sealed record ValidationMessage {
    public LogLevel Level { get; init; }
    
    public Tooltip Tooltip { get; init; }

    public static ValidationMessage Warn(Tooltip tooltip) => new() { Level = LogLevel.Warning, Tooltip = tooltip };
    public static ValidationMessage Error(Tooltip tooltip) => new() { Level = LogLevel.Error, Tooltip = tooltip };

    public static ValidationMessage GenericError { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.genericError"));
    
    public static ValidationMessage ValueMustBeBool { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeBool"));
    
    public static ValidationMessage ValueMustBeInt { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeInt"));
    
    public static ValidationMessage ValueMustBeChar { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeChar"));
    
    public static ValidationMessage ValueMustBeNumber { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeNumber"));
    
    public static ValidationMessage ValueMustBeRgb { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeColor.rgb"));
    
    public static ValidationMessage ValueMustBeRgba { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeColor.rgba"));
    
    public static ValidationMessage ValueMustBeFreeTilesetId { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.mustBeFreeTilesetId"));
    
    public static ValidationMessage ValueCantBeNull { get; } =
        Error(Tooltip.CreateTranslatedOrNull("rysy.validate.cannotBeNull"));
    
    public static ValidationMessage TooLarge(object max) =>
        Error(Tooltip.CreateTranslatedFormatted("rysy.validate.numberTooLarge", max));
    
    public static ValidationMessage TooSmall(object min) =>
        Error(Tooltip.CreateTranslatedFormatted("rysy.validate.numberTooSmall", min));
    
    public static ValidationMessage? TooLargeRecommended<T>(T val, T max)
        where T : INumber<T> => val > max 
            ? Warn(Tooltip.CreateTranslatedFormatted("rysy.validate.numberAboveRecommended", max))
            : null;
    
    public static ValidationMessage? TooSmallRecommended<T>(T val, T min)
        where T : INumber<T> => val < min 
            ? Warn(Tooltip.CreateTranslatedFormatted("rysy.validate.numberBelowRecommended", min))
            : null;
    
    public static ValidationMessage? NotRecommendedMultiple<T>(T val, T min)
        where T : INumber<T> => val % min != T.Zero
            ? Warn(Tooltip.CreateTranslatedFormatted("rysy.validate.numberNotRecommendedMultiple", min))
            : null;
    
    public static ValidationMessage TooManyElements(object max) =>
        Error(Tooltip.CreateTranslatedFormatted("rysy.validate.tooManyElements", max));
    
    public static ValidationMessage TooFewElements(object min) =>
        Error(Tooltip.CreateTranslatedFormatted("rysy.validate.tooFewElements", min));

    public static ValidationMessage InvalidDropdownElement { get; }
        = Error(Tooltip.CreateTranslatedOrNull("rysy.validate.invalidElement"));
    
    public static ValidationMessage InvalidTilesetId { get; }
        = Error(Tooltip.CreateTranslatedOrNull("rysy.validate.invalidTilesetId"));
    
    public static ValidationMessage DuplicateRoomName { get; }
        = Error(Tooltip.CreateTranslatedOrNull("rysy.validate.duplicateRoomName"));
    
    public static ValidationMessage PathNotUnique { get; }
        = Warn(Tooltip.CreateTranslatedOrNull("rysy.validate.pathNotUnique"));
}