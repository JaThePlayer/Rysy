using Rysy.Graphics.TextureTypes;
using Rysy.Mods;

namespace Rysy.Graphics;

public sealed class Colorgrade {
    public VirtTexture? Texture { get; init; }
    
    public bool IsNone { get; init; }
    
    public static Effect? Effect { get; private set; }

    private static Colorgrade? _none;
    public static Colorgrade None => _none ??= new(new ModTexture(ModRegistry.VanillaMod, $"Graphics/ColorGrading/none.png")) {
        IsNone = true,
    };

    private static readonly Dictionary<string, Colorgrade> Colorgrades = new() { ["none"] = None, };
    
    public Colorgrade(VirtTexture? texture) {
        Texture = texture;

        if (Effect is null) {
            var vanilla = ModRegistry.VanillaMod.Filesystem;
            var path = Path.Combine(vanilla.Root, "Effects", "ColorGrade");
            
            Effect = RysyEngine.Instance.Content.Load<Effect>(path);
        }
    }

    public static Colorgrade FromPath(string path) {
        if (Colorgrades.TryGetValue(path, out var res))
            return res;
        
        var fullPath = $"Graphics/ColorGrading/{path}.png";
        
        var mod = ModRegistry.Filesystem.FindFirstModContaining(fullPath);
        if (mod is { }) {
            var texture = new ModTexture(mod, fullPath);
            
            return Colorgrades[path] = new Colorgrade(texture);
        }

        return Colorgrades[path] = None;
    }

    public Effect? Set() {
        if (IsNone)
            return null;
        
        Set(this, this, 0f);

        return Effect;
    }
    
    public static void Set(Colorgrade fromTex, Colorgrade toTex, float p) {
        if (Effect is null) {
            return;
        }
        
        VirtTexture from, to;
        
        if (fromTex.Texture == null || toTex.Texture == null)
        {
            from = None.Texture!;
            to = None.Texture!;
        }
        else
        {
            from = fromTex.Texture;
            to = toTex.Texture;
        }
        
        var percent = float.Clamp(p, 0f, 1f);
        var graphics = Gfx.Batch.GraphicsDevice;
        
        if (from == to || percent <= 0f)
        {
            Effect.CurrentTechnique = Effect.Techniques["ColorGradeSingle"];
            graphics.Textures[1] = from.Texture;
            return;
        }
        if (percent >= 1f)
        {
            Effect.CurrentTechnique = Effect.Techniques["ColorGradeSingle"];
            graphics.Textures[1] = to.Texture;
            return;
        }
        Effect.CurrentTechnique = Effect.Techniques["ColorGrade"];
        Effect.Parameters["percent"].SetValue(percent);
        graphics.Textures[1] = from.Texture;
        graphics.Textures[2] = to.Texture;
    }
}