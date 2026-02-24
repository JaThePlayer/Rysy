using Rysy.Components;
using Rysy.Mods;
using Rysy.Signals;
using System.Reflection;

namespace Rysy.Scripting;

public sealed class ScriptRegistry : ISignalEmitter, ISignalListener<ModAssemblyReloaded> {
    private List<Script>? _scriptsMutable;

    private readonly Dictionary<string, List<Script>> _modScripts = new();

    public IReadOnlyList<Script> Scripts => _scriptsMutable ??= LoadAll();

    private List<Script> LoadAll() {
        _scriptsMutable = new();

        foreach (var mod in ModRegistry.Mods.Values) {
            LoadFromAsm(mod.Name, mod.PluginAssembly);
        }

        return _scriptsMutable;
    }

    private void LoadFromAsm(string modName, Assembly? asm) {
        _modScripts[modName] = new();

        if (asm is null)
            return;

        foreach (var scriptType in asm.GetTypes().Where(t => t.IsSubclassOf(typeof(Script)))) {
            var script = (Script?)Activator.CreateInstance(scriptType) ?? throw new Exception("Huh?");
            
            lock (_scriptsMutable!) {
                _scriptsMutable.Add(script);
                _modScripts[modName].Add(script);
            }

            this.Emit(new ScriptReloaded(script));
        }
    }

    public void OnSignal(ModAssemblyReloaded signal) {
        var mod = signal.Mod;
        
        foreach (var oldScript in _modScripts[mod.Name]) {
            _scriptsMutable?.Remove(oldScript);
        }

        LoadFromAsm(mod.Name, signal.NewAssembly);
    }

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}

public record struct ScriptReloaded(Script NewScript) : ISignal;