using System.Text;

namespace Rysy;

public sealed class BinaryPacker {
    string[] StringLookup = null!;
    string PackageName = null!;

    BinaryReader Reader = null!;

    internal BinaryPacker() { }

    public static Package FromBinary(string filename) {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        if (!File.Exists(filename))
            throw new FileNotFoundException(filename);

        var packer = new BinaryPacker();
        using var stream = File.OpenRead(filename);
        using var reader = new BinaryReader(stream);

        if (reader.ReadString() != "CELESTE MAP") {
            throw new InvalidDataException("Map does not start with the CELESTE MAP header");
        }

        string package = reader.ReadString();

        short lookupCount = reader.ReadInt16();
        var stringLookup = new string[lookupCount];
        for (int i = 0; i < lookupCount; i++) {
            stringLookup[i] = reader.ReadString();
        }

        packer.StringLookup = stringLookup;
        packer.PackageName = package;
        packer.Reader = reader;

        var element = packer.ReadElement();

        return new() {
            Name = packer.PackageName,
            Data = element
        };
    }

    internal string ReadLookup() => StringLookup[Reader.ReadInt16()];

    internal Element ReadElement() {
        var reader = Reader;

        var element = new Element(ReadLookup());

        var attrCount = Reader.ReadByte();
        var attrs = element.Attributes = new(attrCount);
        for (int i = 0; i < attrCount; i++) {
            string attrName = ReadLookup();

            attrs[attrName] = Reader.ReadByte() switch {
                0 => reader.ReadBoolean(),
                1 => Convert.ToInt32(reader.ReadByte()),
                2 => Convert.ToInt32(reader.ReadInt16()),
                3 => Convert.ToInt32(reader.ReadInt32()),
                4 => reader.ReadSingle(),
                5 => ReadLookup(),
                6 => reader.ReadString(),
                7 => DecodeRLE(),
                var unkType => throw new InvalidDataException($"Unknown attribute type: {unkType}")
            };
        }

        var childCount = Reader.ReadInt16();
        var children = element.Children = new Element[childCount];
        for (int i = 0; i < childCount; i++) {
            children[i] = ReadElement();
        }

        return element;
    }

    internal string DecodeRLE() {
        var dataLen = Reader.ReadInt16();
        Span<byte> rle = stackalloc byte[dataLen];
        Reader.BaseStream.Read(rle);

        StringBuilder builder = new();
        for (int i = 0; i < rle.Length; i += 2) {
            builder.Append((char) rle[i + 1], rle[i]);
        }

        return builder.ToString();
    }

    public class Package {
        public string Name { get; init; } = "";
        public Element Data { get; init; } = null!;
    }

    public class Element {
        internal Element(string? name = null) {
            Name = name;
        }

        public readonly string? Name;
        public Dictionary<string, object> Attributes = null!;
        public Element[] Children = null!;

        public int Int(string attrName, int def = 0) {
            if (Attributes.TryGetValue(attrName, out var obj)) {
                return Convert.ToInt32(obj);
            }

            return def;
        }

        public string Attr(string attrName, string def = "") {
            if (Attributes.TryGetValue(attrName, out var obj)) {
                return obj.ToString()!;
            }

            return def;
        }

        public float Float(string attrName, float def = 0f) {
            if (Attributes.TryGetValue(attrName, out var obj)) {
                return Convert.ToSingle(obj);
            }

            return def;
        }
    }
}
