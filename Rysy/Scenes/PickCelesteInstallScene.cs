using Rysy.Graphics;

namespace Rysy.Scenes;

public class PickCelesteInstallScene : Scene
{
    private Scene NextScene;
    public PickCelesteInstallScene(Scene nextScene) {
        NextScene = nextScene;
    }

    public const string Text = "Please drop the Celeste.exe file onto this window.";

    public override void OnFileDrop(FileDropEventArgs args)
    {
        base.OnFileDrop(args);

        var file = args.Files[0];

        if (Path.GetFileName(file) == "Celeste.exe") {
            Settings.Instance.CelesteDirectory = Path.GetDirectoryName(file)!;
            Settings.Save(Settings.Instance);

            RysyEngine.Scene = NextScene;
        }
    }

    public async ValueTask AwaitInstallPickedAsync() {
        while (string.IsNullOrWhiteSpace(Settings.Instance.CelesteDirectory))
        {
            await Task.Delay(100);
        }
    }

    public override void Render()
    {
        base.Render();

        GFX.BeginBatch();
        var windowSize = RysyEngine.Instance.Window.ClientBounds.Size;
        var height = 4 * 6;
        var center = windowSize.Y / 2;
        PicoFont.Print("Please drop the", new Rectangle(0, center - 32, windowSize.X, height), Color.White, 4f);
        PicoFont.Print("Celeste.exe", new Rectangle(0, center, windowSize.X, height), Color.LightSkyBlue, 4f);
        PicoFont.Print("file onto this window", new Rectangle(0, center + 32, windowSize.X, height), Color.White, 4f);
        GFX.EndBatch();
    }
}
