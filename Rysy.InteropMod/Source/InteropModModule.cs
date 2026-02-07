using Rysy.Shared;
using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.Rysy.InteropMod;

public sealed class InteropModModule : EverestModule {
    public static InteropModModule Instance { get; private set; }
    
    public override Type SettingsType => typeof(InteropModModuleSettings);
    
    public static InteropModModuleSettings Settings => (InteropModModuleSettings) Instance._Settings;

    public InteropModModule() {
        Instance = this;
    }
    
    private readonly List<IModLifetimeScoped> _modules = [];

    internal void Register(IModLifetimeScoped module) {
        _modules.Add(module);
        
        module.Load();
    }

    internal T Register<T>() where T : IModLifetimeScoped, new() {
        var t = new T();
        Register(t);
        return t;
    }

    internal IRysyLogger MakeLogger(string tag) {
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(tag, LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(tag, LogLevel.Info);
#endif
        return new CelesteRysyLogger(tag);
    }

    public override void Load() {
        //Register<DebugRcExtension>();
        var pipeServer = new OutPipeServer<PlaybackTrailData>(MakeLogger("Rysy.Pipes.PlayerTrail"));
        Register(new ModLifetimeOutPipeServer<PlaybackTrailData>(pipeServer));
        Register(new PlayerTrailDataCollector(pipeServer));
    }

    public override void Unload() {
        foreach (var module in _modules) {
            module.Unload();
        }
    }
}