using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Celeste.Mod.Rysy.InteropMod;

internal sealed class DebugRcExtension : IModLifetimeScoped {
    private readonly List<RCEndPoint> _endPoints = [
    ];
    
    public void Load() {
        Everest.DebugRC.EndPoints.AddRange(_endPoints);
    }

    public void Unload() {
        Everest.DebugRC.EndPoints.RemoveAll(x => _endPoints.Contains(x));
    }
}