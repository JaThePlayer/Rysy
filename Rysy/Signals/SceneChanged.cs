namespace Rysy.Signals;

public record struct SceneChanged(Scene NewScene) : ISignal;