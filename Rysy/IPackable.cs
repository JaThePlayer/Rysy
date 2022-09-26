namespace Rysy;

public interface IPackable
{
    public BinaryPacker.Element Pack();
    public void Unpack(BinaryPacker.Element from);
}
