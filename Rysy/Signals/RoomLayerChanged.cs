using Rysy.Layers;

namespace Rysy.Signals;

public record struct RoomLayerChanged(Room Room, EditorLayer Layer) : ISignal;
