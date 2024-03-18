using Rysy.Selections;

namespace Rysy.Layers; 

public abstract class EditorLayer {
    protected EditorLayer() {
        EditorLayers.KnownLayers.Add(this);
    }
    
    public abstract string Name { get; }

    public virtual string LocalizedName => Name;
    
    public abstract SelectionLayer SelectionLayer { get; }

    public abstract IEnumerable<Placement> GetMaterials();
}