using Rysy.Signals;

namespace Rysy.Scenes;

public abstract class SceneComponent : ISignalEmitter {
    public Scene Scene { get; internal set; }

    public virtual void Update() {
        
    }

    public virtual void Render() {
        
    }

    public virtual void RenderImGui() {
        
    }

    public virtual void OnBegin() {
        
    }

    public virtual void OnEnd() {
        
    }

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}