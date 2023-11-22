using Rysy.Selections;

namespace Rysy.Layers; 

public class FakeLayer : EditorLayer {
    public FakeLayer(string name, SelectionLayer layer = SelectionLayer.None) {
        Name = name;
        SelectionLayer = layer;
    }

    public override string Name { get; }

    public override SelectionLayer SelectionLayer { get; }

    public override IEnumerable<Placement> GetMaterials()
        => Array.Empty<Placement>();
}