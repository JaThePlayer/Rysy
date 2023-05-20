using Rysy.Graphics;

namespace Rysy.Helpers;

public static class MovingSpinnerHelper {
    public static IEnumerable<ISprite> GetSprites(Entity entity) {
        var pos = entity.Pos;

        if (entity.Bool("star")) {
            yield return ISprite.FromTexture(pos, "danger/starfish13").Centered();
            yield break;
        }

        if (entity.Bool("dust")) {
            yield return ISprite.FromTexture(pos, "danger/dustcreature/base00").Centered();
            yield return ISprite.FromTexture(pos, "Rysy:dust_creature_outlines/base00").Centered() with {
                Color = Color.Red,
            };
            yield break;
        }

        yield return ISprite.FromTexture(pos, "danger/blade00").Centered();
    }

    public static List<PlacementTemplate> PlacementTemplates = new() {
        new() { Name = "blade", Dust = false, Star = false },
        new() { Name = "dust", Dust = true, Star = false },
        new() { Name = "starfish", Dust = false, Star = true }
    };

    public record struct PlacementTemplate(string Name, bool Dust, bool Star);
}
