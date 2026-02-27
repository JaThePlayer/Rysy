using Rysy.Helpers;

namespace Rysy.Layers;

public interface ILonnSerializableLayer : IEditorLayer {
    /// <summary>
    /// Loenn's name of this layer.
    /// </summary>
    public string? LonnLayerName { get; }
    
    /// <summary>
    /// The name of the _type field on an instance of an element of this layer.
    /// </summary>
    public string? LoennInstanceTypeName { get; }
    
    public string? DefaultSid { get; }
    
    BinaryPacker.Element ConvertToLonnFormat(CopypasteHelper.CopiedSelection item);
}