namespace Rysy.Helpers;

/// <summary>
/// Entities implementing this interface will block waterfalls. Equivalent to Solid.BlockWaterfalls = true in Celeste.
/// If the entity only blocks waterfalls based on a condition, override <see cref="BlockWaterfalls"/>
/// </summary>
public interface IWaterfallBlocker
{
    public virtual bool BlockWaterfalls => true;
}
