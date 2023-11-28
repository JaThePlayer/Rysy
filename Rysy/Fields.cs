using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Mods;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rysy;

public static partial class Fields {
    [GeneratedRegex("^[0-9a-fA-F]{6,8},(?:[0-9a-fA-F]{6,8},?){1,}$")]
    private static partial Regex HexColorListRegex();

    private delegate Field? FieldGenerator(object? def, Dictionary<string, object> fieldInfo);

    private static Dictionary<string, FieldGenerator>? LonnFieldGenerators;

    public static BoolField Bool(bool def) => new() { Default = def };
    public static FloatField Float(float def) => new() { Default = def };
    public static IntField Int(int def) => new() { Default = def };
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

    public static DropdownField<char> TileDropdown(char def, bool bg) => new DropdownField<char>() {
        Default = def,
    }.SetValues(() => {
        if (EditorState.Map is not { } map) {
            return new Dictionary<char, string>();
        }

        var autotiler = bg ? map.BGAutotiler : map.FGAutotiler;

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
    public static PathField SpriteBankPath(string def, [StringSyntax(StringSyntaxAttribute.Regex)] string regex, Func<FoundPath, string>? captureConverter = null)
        => new PathField(def, EditorState.Map?.Sprites!, regex, captureConverter).AllowEdits();

    /// <summary>
    /// Creates a field with a dropdown that automatically gets populated with all files located at <paramref name="directory"/> in all mods (including vanilla).
    /// </summary>
    /// <param name="def"></param>
    /// <param name="directory"></param>
    /// <param name="fileExtension"></param>
    /// <param name="filesystem"></param>
    /// <returns></returns>
    public static PathField Path(string def, string directory, string fileExtension, IModFilesystem? filesystem = null)
        => new PathField(def, filesystem ?? GetPathFieldFilesystem(), directory, fileExtension, null).AllowEdits();

    private static IModFilesystem GetPathFieldFilesystem() {
        if (EditorState.Map is not { } map)
            return ModRegistry.VanillaMod.Filesystem;

        if (map.Mod is not { } mod) {
            return ModRegistry.VanillaMod.Filesystem;
        }

        return mod.GetAllDependenciesFilesystem();
    }
    
    public static ColorField RGBA(Color def) => new() { 
        Default = def,
        Format = ColorFormat.RGBA,
    };

    public static ColorField RGBA(string def) => new() {
        Default = def?.FromRGBA(),
        Format = ColorFormat.RGBA,
    };

    public static ColorField RGB(Color def) => new() {
        Default = def,
        Format = ColorFormat.RGB,
    };

    public static ColorField RGB(string? def) => new() {
        Default = def?.FromRGB(),
        Format = ColorFormat.RGB,
    };

    public static ColorField ARGB(Color def) => new() {
        Default = def,
        Format = ColorFormat.ARGB,
    };

    public static ColorField ARGB(string def) => new() {
        Default = def?.FromARGB(),
        Format = ColorFormat.ARGB,
    };

    public static ListField List(string def, Field baseField) => new(baseField, def);

    public static TilegridField Tilegrid(TileLayer layer) => new(layer) { };

    private static Field GuessStringFormat(string s) {
        if (HexColorListRegex().IsMatch(s))
            return new ListField(RGB(Color.White), s);

        return String(s);
    }

    public static EditorGroupListField EditorGroup(EditorGroupRegistry registry, EditorGroupList? def = null) {
        return new EditorGroupListField(registry, def);
    }

    public static DropdownField<string> SID(string def)
        => Dropdown(def, EntityRegistry.SIDToType.Keys.ToList());

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

    public static Field? CreateFromLonn(object? val, string? fieldType, Dictionary<string, object> fieldInfoEntry) {
        RegisterScannerIfNeeded();

        if (fieldType is "string" or "number" or "boolean" or "anything" or null)
            return GuessFromValue(val, fromMapData: true);

        if (LonnFieldGenerators!.TryGetValue(fieldType, out var generator)) {
            try {
                return generator(val, fieldInfoEntry);
            } catch (Exception ex) {
                if (Entity.LogErrors)
                    Logger.Write("Fields", LogLevel.Error, $"Failed to turn lua field {fieldType} with field information {fieldInfoEntry.ToJson()} into field: {ex}");
            }
        } else {
            if (Entity.LogErrors) {
                Logger.Write("Fields", LogLevel.Warning, $"Unknown field type: {fieldType}");
            }
        }

        return GuessFromValue(val, fromMapData: true);
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

                    var name = (string) t.GetProperty(nameof(ILonnField.Name))!.GetValue(null)!;
                    var generator = t.GetMethod(nameof(ILonnField.Create), new Type[] { typeof(object), typeof(Dictionary<string, object>) })!.CreateDelegate<FieldGenerator>();

                    LonnFieldGenerators[name] = generator;
                }
            });
        }
    }
}