namespace Rysy.Platforms;

public class Windows : RysyPlatform
{
    private static string SaveLocation = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rysy"
    ).Unbackslash();

    public override string GetSaveLocation() => SaveLocation;
}
