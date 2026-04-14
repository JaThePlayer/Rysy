using Rysy.Helpers;

namespace Rysy.Signals;

/// <summary>
/// Fired whenever <see cref="Map.Rooms"/> is changed.
/// </summary>
public record struct MapRoomListChanged(Map Map, ListenableListChanged<Room> Changed) : ISignal;
