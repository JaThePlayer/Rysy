namespace Rysy;

public class Profile {
    public static Profile CurrentProfile { get; internal set; }

    public string Name { get; set; } = "Default";
}
