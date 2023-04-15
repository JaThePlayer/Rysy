using Rysy.Graphics;

namespace Rysy.Entities;

/// <summary>
/// Provides a base implementation for entities Rysy doesn't know about
/// </summary>
public sealed class UnknownEntity : Entity {
    public override int Depth => 0;
}