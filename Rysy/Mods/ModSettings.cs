using Rysy.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Rysy.Mods;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class ModSettings {
    [JsonIgnore]
    public ModMeta Meta { get; internal set; }

    /// <summary>
    /// Used when Lonn tries to store something into settings that cannot be mapped to a c# property.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> OtherValues { get; set; } = new();

    public void Save() {
        var fs = SettingsHelper.GetFilesystem(perProfile: false);
        fs.TryWriteToFile(Meta.SettingsFileLocation, this.ToJsonUtf8(GetType(), minified: false));
    }

    public bool HasAnyData() => GetType() != typeof(ModSettings) || OtherValues.Count > 0;
}

/// <summary>
/// Allows lonn plugins to interact with this property by using modSettings[<see cref="LonnName"/>]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LonnBindingAttribute : Attribute {
    public string LonnName { get; }

    public LonnBindingAttribute(string lonnName) {
        LonnName = lonnName;
    }
}

internal static class LonnBindingHelper {
    private static Dictionary<Type, Dictionary<string, PropertyInfo>> AllBindings = new();

    public static Dictionary<string, PropertyInfo> GetAllBindings([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type settingsType) {
        if (AllBindings.TryGetValue(settingsType, out var bindings)) {
            return bindings;
        }

        bindings = new();

        foreach (var p in settingsType.GetProperties()) {
            if (p.GetCustomAttribute<LonnBindingAttribute>() is not { } bindAttr)
                continue;

            bindings[bindAttr.LonnName] = p;
        }

        AllBindings[settingsType] = bindings;

        return bindings;
    }
}
