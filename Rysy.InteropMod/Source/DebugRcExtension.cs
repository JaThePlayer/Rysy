using Rysy.Shared.InteropMod;
using Rysy.Shared.Networking;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Celeste.Mod.Rysy.InteropMod;

internal sealed class DebugRcExtension : IModLifetimeScoped {
    private readonly List<RCEndPoint> _endPoints = [
        new() {
            Name = "Change Rysy Interop Mod Settings",
            Path = InGameSettings.DebugRcSetPath,
            InfoHTML = "Changes Rysy Interop Mod's settings remotely",
            Handle = c => {
                if (float.TryParse(c.Request.QueryString.Get(InGameSettings.DebugRcSetSamplingIntervalQueryString) ?? "", CultureInfo.InvariantCulture, out var samplingRate)) {
                    InteropModModule.Settings.RemoteSettings.SamplingInterval = samplingRate;
                    InteropModModule.Instance.SaveSettings();
                }
                
                Everest.DebugRC.Write(c, "OK");
            }
        },
        new() {
            Name = "Get Rysy Interop Mod Settings",
            Path = InGameSettings.DebugRcGetPath,
            InfoHTML = "Get Rysy Interop Mod's settings remotely",
            Handle = c => {
                WriteJson(c, InteropModModule.Settings.RemoteSettings);
            }
        },
        new() {
            Name = "Get Rysy Interop Mod Current Recording",
            Path = DebugRcEndpoints.CurrentRecording,
            InfoHTML = "Gets the current recording as JSON.",
            Handle = c => {
                WriteJson(c, InteropModModule.Instance.RecordingManager.GetRecordingDataForRysy());
            }
        }
    ];

    private static void WriteJson<T>(HttpListenerContext context, T value) {
        Everest.DebugRC.Write(context, JsonSerializer.Serialize(value, NetworkingJsonOptions.IncludeFields));
    }
    
    public void Load() {
        Everest.DebugRC.EndPoints.AddRange(_endPoints);
    }

    public void Unload() {
        Everest.DebugRC.EndPoints.RemoveAll(x => _endPoints.Contains(x));
    }
}