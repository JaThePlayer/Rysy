using Microsoft.Xna.Framework;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Shared.InteropMod;

namespace Rysy.InteropMod.InRysy.Extensions;

public static class SpriteDataExtensions {
    extension(SpriteData data) {
        public Sprite ToRysy(Vector2 pos) {
            return ISprite.FromTexture(pos, data.Texture) with {
                Scale = data.Scale.ToXna(),
                Origin = data.Origin.ToXna(),
                Rotation = data.Rotation,
                Color = new Color{PackedValue = data.Color},
            };
        }
    }
}
