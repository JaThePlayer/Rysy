using Rysy.Selections;

namespace Rysy.Layers; 

public abstract class EditorLayer {
    protected EditorLayer(string name) {
        Name = name;
        EditorLayers.KnownLayers[name] = this;
    }
    
    public virtual string Name { get; }

    public virtual string LocalizedName => Name.TranslateOrHumanize("rysy.editorLayers.name");
    
    public abstract SelectionLayer SelectionLayer { get; }

    public abstract IEnumerable<Placement> GetMaterials();

    /// <summary>
    /// Prefix to use for language entries for materials. Leave null to not use translations.
    /// </summary>
    public virtual string? MaterialLangPrefix => null;
    
    public virtual bool SupportsPreciseMoveMode => true;
}