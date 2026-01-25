namespace Rysy.Scenes;

public abstract class SceneComponent {
    public abstract void Update();
    
    public abstract void Render();

    public abstract void OnBegin();
    
    public abstract void OnEnd();
}