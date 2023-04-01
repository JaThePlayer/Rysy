namespace Rysy.Mods;

public interface IModAsset {
    public string? SourceModName { get; }

    public List<string>? DependencyModNames { get; }
}
