using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.LuaSupport;

namespace Rysy.Gui.FieldTypes;

public record StringField : Field, IFieldConvertible<string>, ILonnField {
    public string Default { get; set; }

    public bool NullAllowed { get; set; }
    public bool EmptyIsNull { get; set; }
    
    public Func<string?, string?> UserInputFinalizer { get; set; } = static x => x;

    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault)
        => Default = Convert.ToString(newDefault, CultureInfo.InvariantCulture) ?? "";

    private string? RealValue(string? from)
        => (EmptyIsNull && string.IsNullOrWhiteSpace(from)) ? null : from;

    public override ValidationResult IsValid(object? value) {
        if (value is string s)
            value = RealValue(s);
        
        if (NullAllowed && value is null)
            return base.IsValid(value);
        
        return value is string ? base.IsValid(value) : ValidationResult.GenericError;
    }
    
    public override object? RenderGui(string fieldName, object value) {
        var b = (value ?? "").ToString() ?? "";
        //if (ImGui.InputText(fieldName, ref b, 256).WithTooltip(Tooltip)) {
        if (ImGuiManager.ExpandingTextInput(fieldName, ref b, 256, Tooltip)) {
            if (UserInputFinalizer(RealValue(b)) is { } ret)
                return ret;

            return new FieldNullReturn();
        }

        return null;
    }

    /// <summary>
    /// Allows null to be considered a valid value for this field.
    /// </summary>
    /// <returns>this</returns>
    public StringField AllowNull() {
        NullAllowed = true;

        return this;
    }

    public StringField ConvertEmptyToNull() {
        EmptyIsNull = true;

        return this;
    }

    /// <summary>
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public StringField WithValidator(Func<string?, ValidationResult> validator) {
        Validator += (ctx, v) => validator(v?.ToString());

        return this;
    }
    
    
    /// <summary>
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public StringField WithValidator(Func<FormContext, string?, ValidationResult> validator) {
        Validator += (ctx, v) => validator(ctx, v?.ToString());

        return this;
    }
    
    /// <summary>
    /// Adds a finalizer which converts user input into a format stored in the binary.
    /// </summary>
    public StringField WithUserInputFinalizer(Func<string?, string?> validator) {
        UserInputFinalizer += validator;

        return this;
    }

    public override Field CreateClone() => this with { };

    public string ConvertMapDataValue(object value) => RealValue(value?.ToString()!)!;

    public static string Name => "string";

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        if (fieldInfoEntry.TryGetValue("options", out var options))
        {
            // Special-handling for options table created via fakeTilesHelper.getTilesOptions to create a tileset dropdown.
            // TODO: maybe an interface for this if the need arises?
            if (options is LuaTilesetsDictionaryWrapper { MutatedByLua: false } tilesetsDictionaryWrapper)
                return tilesetsDictionaryWrapper.CreateField(def.ToStringInvariant()[0]);
            
            if (Fields.CreateLonnDropdown(fieldInfoEntry, def ?? "", x => (true, x.ToStringInvariant())) is {} dropdown)
                return dropdown;
        }
        
        return new StringField {
            Default = def?.ToString()!
        };
    }
}