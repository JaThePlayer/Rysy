namespace Rysy;

public interface IPackable {
    /// <summary>
    /// Packs this object to a <see cref="BinaryPacker.Element"/>, for map saving.
    /// </summary>
    public BinaryPacker.Element Pack();

    /// <summary>
    /// Unpacks the element <paramref name="from"/> into the current object instance, for map loading.
    /// </summary>
    /// <param name="from"></param>
    public void Unpack(BinaryPacker.Element from);
}
