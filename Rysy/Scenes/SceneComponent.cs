using Rysy.Components;
using Rysy.Signals;

namespace Rysy.Scenes;

public abstract class SceneComponent : ISignalEmitter, ISignalListener<ComponentAdded<Scene>>, ISignalListener<SelfAdded>, ISignalListener<SelfRemoved> {
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

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
    
    public void OnSignal(SelfAdded signal) {
        Scene = signal.Registry.Get<Scene>();
        if (Scene is {})
            OnAdded();
    }

    public void OnSignal(SelfRemoved signal) {
        if (Scene is {})
            OnRemoved();
        Scene = null!;
    }

    public void OnSignal(ComponentAdded<Scene> signal) {
        if (Scene is {})
            OnRemoved();
        Scene = signal.Component;
        if (Scene is {})
            OnAdded();
    }
}