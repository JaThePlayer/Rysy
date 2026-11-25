using Monocle;
using SpriteData = Rysy.Shared.InteropMod.SpriteData;

namespace Celeste.Mod.Rysy.InteropMod.Extensions;

public static class RecordingDataExtensions {
    extension(SpriteData) {
        public static SpriteData FromMonocle(Sprite spr) {
            return new() {
                Rotation = spr.Rotation,
                Origin = (spr.Origin / new Microsoft.Xna.Framework.Vector2(spr.Texture.Width, spr.Texture.Height)).ToNumVec(),
                Scale = spr.Scale.ToNumVec(),
                Texture = spr.Texture.AtlasPath ?? "",
                Color = spr.Color.PackedValue,
            };
        }
    }
}