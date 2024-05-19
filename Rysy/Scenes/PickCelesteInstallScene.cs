using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Scenes;

public class PickCelesteInstallScene : Scene {
    private Scene NextScene;
    public PickCelesteInstallScene(Scene nextScene) {
        NextScene = nextScene;
    }

    internal protected override void OnFileDrop(FileDropEventArgs args) {
        base.OnFileDrop(args);

        var file = args.Files[0];

        if (Path.GetFileName(file) is "Celeste.exe" or "Celeste.dll") {
            Profile.Instance.CelesteDirectory = Path.GetDirectoryName(file)!;
            Profile.Instance.Save();

            RysyEngine.Scene = NextScene;
        }
    }

    public async ValueTask AwaitInstallPickedAsync() {
        while (string.IsNullOrWhiteSpace(Profile.Instance.CelesteDirectory)) {
            await Task.Delay(100);
        }
    }

    public override void Render() {
        base.Render();

        GFX.BeginBatch();
        var windowSize = RysyState.Window.ClientBounds.Size();
        var height = 4 * 6;
        var center = windowSize.Y / 2;
        PicoFont.Print("Please drop the", new Rectangle(0, center - 32, windowSize.X, height), Color.White, 4f);
        PicoFont.Print("Celeste.exe/Celeste.dll", new Rectangle(0, center, windowSize.X, height), Color.LightSkyBlue, 4f);
        PicoFont.Print("file onto this window", new Rectangle(0, center + 32, windowSize.X, height), Color.White, 4f);
        GFX.EndBatch();
    }
}
