using Rysy.Mods;
using System.Reflection;

namespace Rysy.Scripting;

public static class ScriptRegistry {
    private static List<Script>? ScriptsMutable;

    private static Dictionary<string, List<Script>> ModScripts = new();

    /// <summary>
    /// Called whenever any script gets (re)loaded
    /// </summary>
    public static event Action OnScriptReloaded;

    public static IReadOnlyList<Script> Scripts => ScriptsMutable ??= LoadAll();

    private static List<Script> LoadAll() {
        ScriptsMutable = new();

        foreach (var mod in ModRegistry.Mods.Values) {
            mod.OnAssemblyReloaded += (asm) => {
                foreach (var oldScript in ModScripts[mod.Name]) {
                    ScriptsMutable.Remove(oldScript);
                }

                LoadFromAsm(mod.Name, asm);
            };


            LoadFromAsm(mod.Name, mod.PluginAssembly);
        }

        return ScriptsMutable;
    }

    private static void LoadFromAsm(string modName, Assembly? asm) {
        ModScripts[modName] = new();

        if (asm is null)
            return;

        foreach (var scriptType in asm.GetTypes().Where(t => t.IsSubclassOf(typeof(Script)))) {
            var script = (Script?)Activator.CreateInstance(scriptType) ?? throw new Exception("Huh?");
            
            lock (ScriptsMutable!) {
                ScriptsMutable.Add(script);
                ModScripts[modName].Add(script);
            }
        }


        OnScriptReloaded();
    }
}
