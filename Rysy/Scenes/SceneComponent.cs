namespace Rysy.Scenes;

public abstract class SceneComponent {
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
}