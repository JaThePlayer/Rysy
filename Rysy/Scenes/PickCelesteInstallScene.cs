using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Scenes;

public class PickCelesteInstallScene : Scene {
    private Scene _nextScene;
    public PickCelesteInstallScene(Scene nextScene) {
        _nextScene = nextScene;
    }

    protected internal override void OnFileDrop(string filePath) {
        base.OnFileDrop(filePath);

        if (Path.GetFileName(filePath) is "Celeste.exe" or "Celeste.dll") {
            Profile.Instance.CelesteDirectory = Path.GetDirectoryName(filePath)!;
            Profile.Instance.Save();

            RysyEngine.Scene = _nextScene;
        }
    }

    public async ValueTask AwaitInstallPickedAsync() {
        while (string.IsNullOrWhiteSpace(Profile.Instance.CelesteDirectory)) {
            await Task.Delay(100);
        }
    }

    public override void Render() {
        base.Render();

        Gfx.BeginBatch();
        var windowSize = RysyState.Window.ClientBounds.Size();
        var height = 4 * 6;
        var center = windowSize.Y / 2;
        PicoFont.Print("Please drop the", new Rectangle(0, center - 32, windowSize.X, height), Color.White, scale: 4f);
        PicoFont.Print("Celeste.exe/Celeste.dll", new Rectangle(0, center, windowSize.X, height), Color.LightSkyBlue, scale: 4f);
        PicoFont.Print("file onto this window", new Rectangle(0, center + 32, windowSize.X, height), Color.White, scale: 4f);
        Gfx.EndBatch();
    }
}
