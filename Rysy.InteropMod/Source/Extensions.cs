using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.Rysy.InteropMod;

internal static class Extensions {
    public static System.Numerics.Vector2 ToNumVec(this Microsoft.Xna.Framework.Vector2 x)
        => Unsafe.BitCast<Vector2, System.Numerics.Vector2>(x);
}