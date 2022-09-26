namespace Rysy.Helpers;

/// <summary>
/// Dictates that this entity is a Solid. Functions checking for solids will take entities implementing this interface into account.
/// Implies <see cref="IWaterfallBlocker"/>
/// </summary>
public interface ISolid : IWaterfallBlocker
{

}
