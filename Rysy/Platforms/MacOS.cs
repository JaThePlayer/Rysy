namespace Rysy.Platforms;

public class MacOs : RysyPlatform {
    public override void Init() {
        base.Init();

        Logger.UseColorsInConsole = true;
    }
}
