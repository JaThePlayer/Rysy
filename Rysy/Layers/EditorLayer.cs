using Rysy.Selections;

namespace Rysy.Layers; 


public abstract class EditorLayer {
    public abstract string Name { get; }

    public virtual string LocalizedName => Name;
    
    public abstract SelectionLayer SelectionLayer { get; }

    public abstract IEnumerable<Placement> GetMaterials();
}