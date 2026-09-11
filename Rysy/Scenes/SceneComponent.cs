using Rysy.Components;
using Rysy.Signals;

namespace Rysy.Scenes;

public abstract class SceneComponent : ISignalEmitter, ISignalListener<SceneChanged>, ISignalListener<ComponentAdded<Scene>>, ISignalListener<SelfAdded>, ISignalListener<SelfRemoved> {
    public Scene? Scene { get; internal set; }

    public virtual void Update() {
        
    }

    public virtual void Render() {
        
    }

    public virtual void RenderImGui() {
        
    }

    public virtual void OnAdded() {
        
    }

    public virtual void OnRemoved() {
        
    }

    public virtual void OnSceneBegin() {
        
    }

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
    
    public void OnSignal(SelfAdded signal) {
        SceneChanged(signal.Registry.Get<Scene>());
    }

    public void OnSignal(SelfRemoved signal) {
        SceneChanged(null);
    }

    public void OnSignal(ComponentAdded<Scene> signal) {
        SceneChanged(signal.Component);
    }

    public void OnSignal(SceneChanged signal) {
        SceneChanged(signal.NewScene);
    }

    private void SceneChanged(Scene? newScene) {
        if (newScene == Scene)
            return;
        
        if (Scene is not null)
            OnRemoved();
        Scene = newScene;
        if (Scene is not null)
            OnAdded();
    }
}