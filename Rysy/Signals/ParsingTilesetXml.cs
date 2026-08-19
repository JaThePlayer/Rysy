using Rysy.Graphics;

namespace Rysy.Signals;

/// <summary>
/// Emitted by the Autotiler whenever a tileset xml is being parsed, right before parsing <set /> elements.
/// Xml contents can be accessed via <see cref="TilesetData.Xml"/>
/// </summary>
/// <param name="Autotiler">The autotiler this tileset is part of.</param>
/// <param name="Tileset">The tileset currently being parsed.</param>
public record struct ParsingTilesetXml(Autotiler Autotiler, TilesetData Tileset) : ISignal;
