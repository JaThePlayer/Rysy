using Rysy.Graphics;
using Rysy.Mods;

namespace Rysy;

public static class ReplUtils {
    /*
csharprepl setup:
#r "MonoGame.Framework.dll"
#r "ImGui.NET.dll"
#r "YamlDotNet.dll"
#r "Rysy.dll"
NativeLibrary.Load("runtimes/win-x64/native/lua54.dll");

using Rysy;
await ReplUtils.LoadHeadless(true);
     */
    
    /// <summary>
    /// Loads everything needed for a headless run of Rysy.
    /// </summary>
    public static async Task LoadHeadless(bool luaEnabled) {
        Settings.Load(uiEnabled: false);
        Profile.Instance = Profile.Load();
        Persistence.Instance = Persistence.Load();

        if (Profile.Instance.CelesteDirectory is null or "") {
            Console.WriteLine("Celeste directory is missing, Rysy cannot run");
            return;
        }

        await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory, null);
        await EntityRegistry.RegisterAsync(task: null, loadLuaPlugins: luaEnabled);
        GFX.HeadlessSetup();
    }
}