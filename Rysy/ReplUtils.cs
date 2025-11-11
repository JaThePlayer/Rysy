using Rysy.Graphics;
using Rysy.Mods;
using Rysy.Platforms;

namespace Rysy;

public static class ReplUtils {
    /*
csharprepl setup:
csharprepl -r "Rysy.dll" -r "YamlDotNet.dll" -r "ImGui.NET.dll" -r "MonoGame.Framework.dll" -u "Rysy" -u "Rysy.Helpers" -u "Rysy.Mods" -u "Rysy.Extensions"

Logger.MinimumLevel = LogLevel.Error;
System.Runtime.InteropServices.NativeLibrary.Load("runtimes/win-x64/native/lua54.dll");
await ReplUtils.LoadHeadless(cSharpPlugins: true, luaPlugins: true);
     */
    
    /// <summary>
    /// Loads everything needed for a headless run of Rysy.
    /// </summary>
    public static async Task LoadHeadless(bool cSharpPlugins, bool luaPlugins) {
        RysyPlatform.Current.Init();
        Settings.Load(uiEnabled: false);
        Profile.Instance = Profile.Load();
        Persistence.Instance = Persistence.Load();

        if (Profile.Instance.CelesteDirectory is null or "") {
            Console.WriteLine("Celeste directory is missing, Rysy cannot run");
            return;
        }

        await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory, null, cSharpPlugins);
        await EntityRegistry.RegisterAsync(task: null, loadLuaPlugins: luaPlugins);
        Gfx.HeadlessSetup();
    }
}