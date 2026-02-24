namespace Rysy.Signals;

public record struct MapSwapped(EditorState State, Map? OldMap, Map? NewMap) : ISignal;