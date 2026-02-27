using Rysy.Components;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Signals;

namespace Rysy.InteropMod.InRysy;

public sealed class InteropModModule : ModModule, ISignalListener<SceneChanged>
{
    public override void Load() {
        base.Load();
        
        //ComponentRegistry.Add(new TileEditorLayer(TileLayer.Bg, Depths.Above));
    }

    public override void Unload() {
        base.Unload();
    }

    public void OnSignal(SceneChanged signal) {
        if (signal.NewScene is EditorScene editorScene) {
            editorScene.Add(new PlayerTrailRenderer());
        }
    }
}
