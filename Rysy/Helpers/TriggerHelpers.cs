using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

public static partial class TriggerHelpers
{
    private static ConditionalWeakTable<string, string> HumanizedNames = new();

    /// <summary>
    /// Humanizes a trigger name into something ready to be rendered.
    /// Removes mod name, the "trigger" suffix, and adds spaces between words.
    /// </summary>
    public static string Humanize(string name)
    {
        if (HumanizedNames.TryGetValue(name, out var result))
        {
            return result;
        }

        // trim "trigger" from the end of the name.
        result = name.TrimEnd("trigger", StringComparison.InvariantCultureIgnoreCase);

        // trim mod name
        var modSplit = result.IndexOf('/');
        if (modSplit != -1)
        {
            result = result[(modSplit + 1)..];
        }

        // Put spaces between words
        result = result.SplitPascalCase();

        HumanizedNames.Add(name, result);
        return result;
    }
}