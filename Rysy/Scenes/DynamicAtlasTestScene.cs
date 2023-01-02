using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;

namespace Rysy.Scenes;

internal class DynamicAtlasTestScene : Scene
{
    internal DynamicAtlas? _atlas;

    public Camera Camera { get; set; } = new();

    public override void Update()
    {
        base.Update();

        Camera.HandleMouseMovement();

        if (Input.Keyboard.Ctrl())
        {
            // Reload everything
            if (Input.Keyboard.IsKeyClicked(Keys.F5))
            {
                Task.Run(() =>
                {
                    RysyEngine.Instance.Reload();
                    GC.Collect(3);
                });
            }
        } else
        {
            // Reload atlas
            if (Input.Keyboard.IsKeyClicked(Keys.F5))
            {
                Task.Run(() =>
                {
                    _atlas?.DisposeTextures();
                    _atlas = null;
                });
            }
        }
    }

    ValueTask<bool> addTexture(string key)
    {
        return _atlas!.PackTexture(key, GFX.Atlas[key]);
    }

    public override void Render()
    {
        base.Render();

        if (_atlas is null)
        {
            _atlas = new(4098, 4098);

            foreach ((string virtPath, VirtTexture texture) in GFX.Atlas.GetTextures()
                .Where(t => t.texture is FileVirtTexture)
                .Where(t => !t.virtPath.StartsWith("bgs/") && !t.virtPath.StartsWith("mirrormasks/"))
                .OrderByDescending(t => t.texture.Height)
                .ThenByDescending(t => t.texture.Width))
            {
                var ret = addTexture(virtPath);
            }
        }

        var camera = Camera;
        RysyEngine.GDM.GraphicsDevice.SetRenderTarget(null);
        GFX.Batch.Begin(
            SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, 
            effect: null, 
            camera.Matrix
            );
        _atlas.DebugRender();
        GFX.Batch.End();
    }
}
