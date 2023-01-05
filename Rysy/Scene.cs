namespace Rysy;

public abstract class Scene {
    public float TimeActive { get; private set; }

    public virtual void Update() {
        TimeActive += Time.Delta;
    }

    public virtual void Render() {

    }

    public virtual void OnFileDrop(FileDropEventArgs args) {
        RysyEngine.ForceActiveTimer = 0.75f;
    }

    public bool OnInterval(double interval) {
        if (interval < Time.Delta * 2f)
            interval = Time.Delta * 2f;
        //return true;

        var time = Time.Elapsed;
        return time % interval < Time.Delta;
    }
}
