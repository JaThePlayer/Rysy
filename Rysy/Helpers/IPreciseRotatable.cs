namespace Rysy.Helpers;

public interface IPreciseRotatable {
    /// <summary>
    /// Rotates this entity by <paramref name="angleRad"/> degrees (in radians)
    /// </summary>
    public Entity? RotatePreciseBy(float angleRad);
}
