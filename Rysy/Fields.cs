﻿using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.FieldTypes;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.LuaSupport;
using Rysy.Mods;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rysy;

public static partial class Fields {
    [GeneratedRegex("^[0-9a-fA-F]{6,8},(?:[0-9a-fA-F]{6,8},?){1,}$")]
    private static partial Regex HexColorListRegex();

    private delegate Field? FieldGenerator(object? def, IUntypedData fieldInfo);

    private static Dictionary<string, FieldGenerator>? LonnFieldGenerators;

    public static BoolField Bool(bool def) => new() { Default = def };
    public static FloatField Float(float def) => new() { Default = def.ToStringInvariant() };
    public static IntField Int(int def) => new() { Default = def.ToStringInvariant() };
    public static IntField IntNullable(int? def = null) => new IntField { Default = def.ToStringInvariant() }.AllowNull();
    
    public static CharField Char(char def) => new() { Default = def };
    public static StringField String(string def) => new() { Default = def };

    public static DropdownField<string> EnumNamesDropdown<T>(T def, Func<string, string>? keySelector = null) where T : struct, Enum
    => new DropdownField<string>() {
        Default = def.ToString(),
    }.SetValues(Enum.GetNames<T>().ToDictionary(k => keySelector?.Invoke(k) ?? k, v => v));

    public static DropdownField<string> EnumNamesDropdown(object def, Type enumType) {
        if (!enumType.IsEnum) {
            throw new ArgumentException($"Non-enum type {enumType} cannot be used in {nameof(Fields)}.{nameof(EnumNamesDropdown)}", nameof(enumType));
        }

        return new DropdownField<string>() {
            Default = def.ToString()!,
        }.SetValues(Enum.GetNames(enumType).ToDictionary(k => k, v => v, StringComparer.OrdinalIgnoreCase));
    }

    public static DropdownField<string> EnumNamesDropdown<T>(string def) where T : struct, Enum
    => new DropdownField<string>() {
        Default = def,
    }.SetValues(Enum.GetNames<T>().ToDictionary(k => k, v => v, StringComparer.OrdinalIgnoreCase));

    public static DropdownField<T> Dropdown<T>(T def, IDictionary<T, string> values, bool editable = false) where T : notnull
    => new DropdownField<T>() {
        Default = def,
        Editable = editable,
    }.SetValues(values);

    public static DropdownField<T> Dropdown<T>(T def, Func<IDictionary<T, string>> values, bool editable = false) where T : notnull
    => new DropdownField<T>() {
        Default = def,
        Editable = editable,
    }.SetValues(values);

    public static DropdownField<T> Dropdown<T>(T def, Func<List<T>> values, Func<T, string>? toString = null, bool editable = false) 
        where T : notnull
    => new DropdownField<T>() {
        Default = def,
        Editable = editable,
    }.SetValues(() => values().ToDictionary(k => k, k => toString is { } ? toString(k) : k.ToString()!));

    public static DropdownField<T> Dropdown<T>(T def, List<T> values, Func<T, string>? toString = null, bool editable = false)
        where T : notnull 
    => new DropdownField<T>() {
        Default = def,
        Editable = editable,
    }.SetValues(values.ToDictionary(k => k, k => toString is { } ? toString(k) : k.ToString()!));

    public static DropdownField<string> TileDropdown(char def, bool bg, bool addDontCopyOption = false, bool addWildcardOption = false) => new DropdownField<string>() {
        Default = def.ToString(),
    }.SetValues(() => {
        if (EditorState.Map is not { } map) {
            return new Dictionary<string, string>();
        }

        var autotiler = bg ? map.BGAutotiler : map.FGAutotiler;

        var dict = autotiler.Tilesets
            .ToDictionary(t => t.Key.ToString(), t => $"[{t.Key}] {autotiler.GetTilesetDisplayName(t.Key)}");

        if (addDontCopyOption) {
            dict["\0"] = "rysy.tilesetImport.dontCopy".Translate();
        }

        if (addWildcardOption) {
            dict["*"] = "rysy.tilesetImport.wildcard".Translate();
        }
        
        return dict;
    }).AddSpriteTooltips((key, display) => {
        if (key is not [var id] || EditorState.Map is not { } map) {
            return null;
        }

        var autotiler = bg ? map.BGAutotiler : map.FGAutotiler;

        return autotiler.Tilesets.TryGetValue(id, out var tileset) ? tileset.GetPreview(8 * 3) : null;
    });
    
    public static DropdownField<char> TileDropdown(char def, Func<FormContext, bool> bg) => new DropdownField<char>() {
        Default = def,
    }.SetValues(ctx => {
        if (EditorState.Map is not { } map) {
            return new Dictionary<char, string>();
        }

        var autotiler = bg(ctx) ? map.BGAutotiler : map.FGAutotiler;

        return autotiler.Tilesets.ToDictionary(t => t.Key, t => autotiler.GetTilesetDisplayName(t.Key));
    });

    /// <summary>
    /// Creates a field with a dropdown that automatically gets populated with possible texture paths matching the provided <paramref name="regex"/>.
    /// If the regex has a capture, the captured value will be stored into the mapdata.
    /// For example, a regex: ^objects/crumbleBlock/(.*) will give a dropdown for all sprites in the objects/crumbleBlock directory (or its subdirectories), and store the only the parts after objects/crumbleBlock/ into the mapdata.
    /// </summary>
    /// <param name="def">The default value to use for this field</param>
    /// <param name="regex">The regex to use to find texture paths</param>
    /// <param name="captureConverter">A function which converts a texture found by the regex into the key to use to save to mapdata. By default, it returns texture.Captured</param>
    public static PathField AtlasPath(string def, [StringSyntax(StringSyntaxAttribute.Regex)] string regex, Func<FoundPath, string>? captureConverter = null)
        => new PathField(def, GFX.Atlas, regex, captureConverter).AllowEdits();

    /// <summary>
    /// Creates a field with a dropdown that automatically gets populated with possible sprite bank paths matching the provided <paramref name="regex"/>.
    /// If the regex has a capture, the captured value will be stored into the mapdata.
    /// The regex works the same way as <see cref="AtlasPath"/>
    /// </summary>
    /// <param name="def">The default value to use for this field</param>
    /// <param name="regex">The regex to use to find texture paths</param>
    /// <param name="captureConverter">A function which converts a texture found by the regex into the key to use to save to mapdata. By default, it returns texture.Captured</param>
    /// <param name="previewAnimation">Animation name to use for creating the preview in the dropdown.</param>
    public static PathField SpriteBankPath(string def, [StringSyntax(StringSyntaxAttribute.Regex)] string regex,
        Func<FoundPath, string>? captureConverter = null, string? previewAnimation = null) {
        var field = new PathField(def, EditorState.Map?.Sprites!, regex, captureConverter).AllowEdits();

        if (previewAnimation is { }) {
            field.PreviewSpriteGetter = (path) => ISprite.FromSpriteBank(default, path.Path, previewAnimation);
        } else {
            field.PreviewSpriteGetter = null;
        }
        
        return field;
    }

    /// <summary>
    /// Creates a field with a dropdown that automatically gets populated with all files located at <paramref name="directory"/> in all mods (including vanilla).
    /// </summary>
    /// <param name="def">Default value.</param>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="fileExtension">File extension to search for, without the dot.</param>
    /// <param name="filesystem">Filesystem to search in. If null, a filesystem containing all dependencies of the current map are used.</param>
    /// <param name="filter">Allows blocking certain results from appearing in the dropdown.</param>
    public static PathField Path(string def, string directory, string fileExtension, IModFilesystem? filesystem = null, 
        Func<FoundPath, bool>? filter = null)
        => new PathField(def, filesystem ?? GetPathFieldFilesystem(), directory, fileExtension, null, filter).AllowEdits();

    private static IModFilesystem GetPathFieldFilesystem() {
        if (EditorState.Map is not { } map)
            return ModRegistry.VanillaMod.Filesystem;

        if (map.Mod is not { } mod) {
            return ModRegistry.VanillaMod.Filesystem;
        }

        return mod.GetAllDependenciesFilesystem();
    }
    
    public static ColorField RGBA(Color def) => new() { 
        Default = def.ToString(ColorFormat.RGBA),
        Format = ColorFormat.RGBA,
    };

    public static ColorField RGBA(string def) => new() {
        Default = def,
        Format = ColorFormat.RGBA,
    };

    public static ColorField RGB(Color def) => new() {
        Default = def.ToString(ColorFormat.RGB),
        Format = ColorFormat.RGB,
    };

    public static ColorField RGB(string? def) => new() {
        Default = def,
        Format = ColorFormat.RGB,
    };

    public static ColorField ARGB(Color def) => new() {
        Default = def.ToString(ColorFormat.ARGB),
        Format = ColorFormat.ARGB,
    };

    public static ColorField ARGB(string def) => new() {
        Default = def,
        Format = ColorFormat.ARGB,
    };

    public static ListField List(string def, Field baseField) => new(baseField, def);

    public static TilegridField Tilegrid(TileLayer layer) => new(layer) { };

    private static Field GuessStringFormat(string s) {
        // TODO: remove
        if (HexColorListRegex().IsMatch(s))
            return new ListField(RGB(Color.White), s);

        return String(s);
    }

    public static EditorGroupListField EditorGroup(EditorGroupRegistry registry, EditorGroupList? def = null) {
        return new EditorGroupListField(registry, def);
    }

    public static ConditionalField Conditional(List<Field> fields, Func<FormContext, int> fieldPicker) {
        return new((ctx) => fields[fieldPicker(ctx)]);
    }

    public static EntitySidField Sid(string def, RegisteredEntityType allowedTypes) {
        return new(def, allowedTypes);
    }
    
    internal static StringField TilesetDisplayName(string def, Func<bool> bg, bool selfIsTileset) => String(def).WithValidator(x => {
        var map = EditorState.Map;
        if (map is null)
            return ValidationResult.Ok;
        
        var autotiler = bg() ? map.BGAutotiler : map.FGAutotiler;

        if (autotiler.Tilesets.Count(kv => kv.Value.GetDisplayName() == x) > (selfIsTileset ? 1 : 0))
            return ValidationResult.TilesetDisplayNameInUse;
            
        return ValidationResult.Ok;
    });

    internal static StringField NewPath(string def, Func<string, string> userInputToRealPath) => String(def).WithValidator(x => {
        if (string.IsNullOrWhiteSpace(x))
            return ValidationResult.CantBeNull;

        var real = userInputToRealPath(x);
        if (ModRegistry.Filesystem.FindFirstModContaining(real) is { } containingMod)
            return ValidationResult.AssetPathInUse(containingMod.Name);

        if (real.Contains("//", StringComparison.Ordinal))
            return ValidationResult.PathNotValidEmptyDir;

        if (real.AsSpan().ContainsAny(StringExt.InvalidFilePathChars))
            return ValidationResult.PathNotValidInvalidChars;

        if (!x.Contains('/', StringComparison.Ordinal))
            return ValidationResult.PathNotUnique;

        return ValidationResult.Ok;
    }).WithUserInputFinalizer(x => x?.Unbackslash().Trim('/'));
    
    internal static StringField NewAtlasPath(string def, string prefix) => String(def).WithValidator(x => {
        if (string.IsNullOrWhiteSpace(x))
            return ValidationResult.CantBeNull;

        var real = $"{prefix}{x}";
        if (GFX.Atlas.TryGet(real, out var tex)) {
            var mod = tex is IModAsset modAsset ? modAsset.SourceModName : null;
            return ValidationResult.TexturePathInUse(mod);
        }
        
        if (real.Contains("//", StringComparison.Ordinal))
            return ValidationResult.PathNotValidEmptyDir;

        if (real.AsSpan().ContainsAny(StringExt.InvalidFilePathChars))
            return ValidationResult.PathNotValidInvalidChars;

        if (!x.Contains('/', StringComparison.Ordinal))
            return ValidationResult.PathNotUnique;

        return ValidationResult.Ok;
    }).WithUserInputFinalizer(x => x?.Unbackslash().Trim('/').TrimPostfix(".png"));

    public static Field? GuessFromValue(object? val, bool fromMapData) => val switch {
        bool b => Bool(b),
        float b => Float(b),
        double d => Float((float)d),
        int i => fromMapData
            ? Float(i) // floats that represent integer values get saved as integers to map data, so we can't return Int(i)
            : Int(i),
        long l => Int((int)l),
        char c => Char(c),
        string s => GuessStringFormat(s),
        _ => null,
    };
    
    public static string LonnNameFromValue(object? val) => val switch {
        bool => "boolean",
        float => "number",
        int => "integer",
        long => "integer",
        char => "string",
        _ => "string",
    };

    public static Field? CreateFromLonn(object? val, string? fieldType, Dictionary<string, object> fieldInfoEntry, string fieldName) {
        RegisterScannerIfNeeded();

        fieldType ??= LonnNameFromValue(val);
        if (fieldType == "anything")
            fieldType = "string";

        Field? field = null;

        if (LonnFieldGenerators!.TryGetValue(fieldType, out var generator)) {
            try {
                field = generator(val, new DictionaryUntypedData(fieldInfoEntry));
            } catch (Exception ex) {
                if (Entity.LogErrors && Settings.Instance?.LogMissingFieldTypes is true)
                    Logger.Write("Fields", LogLevel.Error, $"Failed to turn lua field {fieldType} with field information {fieldInfoEntry.ToJson()} into field: {ex}");
            }
        } else {
            if (Entity.LogErrors && Settings.Instance?.LogMissingFieldTypes is true) {
                Logger.Write("Fields", LogLevel.Warning, $"Unknown field type: {fieldType}");
            }
        }

        field ??= GuessFromValue(val, fromMapData: true);

        if (field is {} && fieldInfoEntry.TryGetValue("validator", out var validatorObj) && validatorObj is LuaFunctionRef func) {
            field.WithValidator((x) => {
                var lua = func.Lua;
                func.PushToStack();
                lua.Push(x);
                lua.Call(1, 1);
                var isValid = lua.ToBoolean(lua.GetTop());
                lua.Pop(1);
                return isValid ? ValidationResult.Ok : ValidationResult.GenericError;
            });
        }

        return field;
    }
    
    public static Field? CreateLonnDropdown<T>(IUntypedData info, object def, Func<object, (bool, T)> converter) where T : notnull {
        var editable = info.Bool("editable", true);

        if (converter(def) is not (true, var parsedDef))
            return null;

        Field? field = null;
        
        if (info.TryGetValue("options", out var options)) {
            IEnumerable<(object, string)>? values = null;

            if (options is ILuaDictionaryWrapper dictWrapper)
                options = dictWrapper.Dictionary;
            
            switch (options) {
                case List<object> dropdownOptions:
                    if (dropdownOptions.First() is List<object>) {
                        // {text, value},
                        // {text, value2},
                        values = dropdownOptions
                            .OfType<List<object>>()
                            .Where(l => l.Count >= 2)
                            .Select(l => (l[1], l[0].ToString() ?? ""));
                    } else {
                        values = dropdownOptions.Select(x => (x, x?.ToString() ?? ""));
                    }
                    break;
                case Dictionary<string, object> dropdownOptions: {
                    values = dropdownOptions.Select(v => (v.Value, v.Key));
                    break;
                }
            }

            if (values is { }) {
                var parsedValues = values.Select(x => (converter(x.Item1), x.Item2, x.Item1)).ToList();
                if (parsedValues.All(p => p.Item1.Item1)) {
                    //field = Dropdown(parsedDef, parsedValues.SafeToDictionary(p => (p.Item1.Item2, p.Item2)), editable);
                    field = new DropdownField() {
                        Default = parsedDef,
                        Values = parsedValues.SafeToDictionary(p => ((object)p.Item1.Item2, p.Item2)),
                        ValueTransformer = x => converter(x).Item2,
                        Editable = editable
                    };
                } else {
                    // Not all values could be parsed to the target type.
                    // This happens for tilesets for example, if the plugin is faulty and sets '3' (number) as the default value.
                    //field = Dropdown<object>(def.ToString() ?? "", parsedValues.SafeToDictionary(p => (p.Item3, p.Item2)), editable);
                    field = new DropdownField() {
                        Default = def,
                        Values = parsedValues.SafeToDictionary(p => (p.Item3, p.Item2)),
                        Editable = editable
                    };
                }
            }
        }

        if (field is DropdownField fieldInfoEntry) {
            if (info.TryGetValue("valueTransformer", out var transformerObj) &&
                transformerObj is LuaFunctionRef transformer) {
                fieldInfoEntry.ValueTransformer = x => {
                    var lua = transformer.Lua;
                    transformer.PushToStack();
                    lua.PushString(x);
                    lua.Call(1, 1);
                    var res = lua.ToCSharp(lua.GetTop());
                    lua.Pop(1);
                    return res;
                };
            }
        
            if (info.TryGetValue("displayTransformer", out var displayTransformerObj) &&
                displayTransformerObj is LuaFunctionRef displayTransformer) {
                fieldInfoEntry.DisplayTransformer = x => {
                    var lua = displayTransformer.Lua;
                    displayTransformer.PushToStack();
                    lua.Push(x);
                    lua.Call(1, 1);
                    var res = lua.FastToString(lua.GetTop());
                    lua.Pop(1);
                    
                    return res;
                };
            }
        }
        
        return field;
    }

    private static void RegisterScannerIfNeeded() {
        if (LonnFieldGenerators is null) {
            LonnFieldGenerators = new();

            ModRegistry.RegisterModAssemblyScanner((mod, oldAsm) => {
                if (oldAsm is { }) {
                    LonnFieldGenerators = LonnFieldGenerators.Where(g => g.Value.Method.DeclaringType?.Assembly != oldAsm).ToDictionary();
                }

                if (mod.PluginAssembly is not { }) {
                    return;
                }

                foreach (var t in mod.PluginAssembly.GetTypes()) {
                    if (t == typeof(ILonnField) || !t.IsAssignableTo(typeof(ILonnField)))
                        continue;

                    if (t.GetProperty(nameof(ILonnField.Name)) is not { } nameProp) {
                        // A field type extends from a ILonnField without its own impl, skip.
                        continue;
                    }

                    var name = (string) nameProp.GetValue(null)!;
                    Type[] types = [ typeof(object), typeof(IUntypedData) ];
                    var bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
                    
                    var generatorMethod = t.GetMethod(nameof(ILonnField.Create), bindingFlags, types) 
                                          ?? t.GetMethod("ILonnField.Create", bindingFlags, types);

                    if (generatorMethod is null) {
                        continue;
                    }
                    
                    var generator = generatorMethod.CreateDelegate<FieldGenerator>();

                    LonnFieldGenerators[name] = generator;
                }
            });
        }
    }
}