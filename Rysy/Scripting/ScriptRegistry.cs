using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Mods;
using System.Reflection;

namespace Rysy.Scripting;

public static class ScriptRegistry {
    private static List<Script> _Scripts;

    private static Dictionary<string, List<Script>> ModScripts = new();

    /// <summary>
    /// Called whenever any script gets (re)loaded
    /// </summary>
    public static event Action OnScriptReloaded;

    public static List<Script> Scripts => _Scripts ??= LoadAll();

    private static List<Script> LoadAll() {
        _Scripts = new();

        foreach (var mod in ModRegistry.Mods.Values) {
            mod.OnAssemblyReloaded += (asm) => {
                foreach (var oldScript in ModScripts[mod.Name]) {
                    _Scripts.Remove(oldScript);
                }

                LoadFromAsm(mod.Name, asm);
            };


            LoadFromAsm(mod.Name, mod.PluginAssembly);
        }

        return _Scripts;
    }

    private static void LoadFromAsm(string modName, Assembly? asm) {
        ModScripts[modName] = new();

        if (asm is null)
            return;

        foreach (var scriptType in asm.GetTypes().Where(t => t.IsSubclassOf(typeof(Script)))) {
            var script = (Script?)Activator.CreateInstance(scriptType) ?? throw new Exception("Huh?");
            
            lock (_Scripts) {
                _Scripts.Add(script);
                ModScripts[modName].Add(script);
            }
        }


        OnScriptReloaded();
    }
}
