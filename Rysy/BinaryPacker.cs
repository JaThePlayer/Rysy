using System.Data;
using System.Text;
using System.Text.Json.Serialization;

namespace Rysy;

public sealed class BinaryPacker {
    string[] StringLookup = null!;
    string PackageName = null!;

    BinaryReader Reader = null!;
    BinaryWriter Writer = null!;

    short nextLookupId = 0;
    Dictionary<string, short> WritingLookup = new();

    internal BinaryPacker() { }

    public static Package FromBinary(string filename) {
#if DEBUG
        using var watch = new ScopedStopwatch("FromBinary");
#endif

        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        if (!File.Exists(filename))
            throw new FileNotFoundException(filename);

        using var stream = File.OpenRead(filename);
        return FromBinary(stream, filename);
    }

    public static Package FromBinary(Stream stream, string filename) {
        var packer = new BinaryPacker();
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
            Data = element,
            Filename = filename,
        };
    }

    public static void SaveToStream(Package package, Stream output) {
        // contains the map header and lookup table
        using var headerStream = new MemoryStream();
        using var headerWriter = new BinaryWriter(headerStream);

        // contains the rest of the map (needed because the lookup table occurs before the map, and we don't know how long that lookup will be)
        using var contentStream = new MemoryStream();
        using var contentWriter = new BinaryWriter(contentStream);

        headerWriter.Write("CELESTE MAP");
        headerWriter.Write(package.Name);

        var packer = new BinaryPacker();
        // Write the map to a different writer, as we now need the lookup table.
        packer.Writer = contentWriter;
        packer.WriteElement(package.Data);

        // write lookup table
        headerWriter.Write((short) packer.WritingLookup.Count);
        foreach (var item in packer.WritingLookup.OrderBy(k => k.Value)) {
            headerWriter.Write(item.Key);
        }

        output.Write(headerStream.ToArray());
        output.Write(contentStream.ToArray());
        //headerStream.CopyTo(output);
        //contentStream.CopyTo(output);
    }

    public static void SaveToFile(Package package, string filename) {
        using var memStream = new MemoryStream();

        SaveToStream(package, memStream);

        // now that we know everything went well, time to write to file
        using var fileStream = File.Open(filename, FileMode.Create);

        fileStream.Write(memStream.ToArray());
        //memStream.CopyTo(fileStream);
    }

    internal string ReadLookup() => StringLookup[Reader.ReadInt16()];

    internal void WriteElement(Element el) {
        var writer = Writer;

        WriteLookup(el.Name ?? "");

        var attrs = (el.Attributes ?? new()).Where(p => p.Value is { }).ToList();
        writer.Write((byte) attrs.Count);
        foreach (var (name, val) in attrs) {
            WriteLookup(name);
            WriteValue(val);
        }

        var children = el.Children ?? Array.Empty<Element>();
        writer.Write((short) children.Length);
        foreach (var item in children) {
            WriteElement(item);
        }
    }

    internal void WriteLookup(string str) {
        if (!WritingLookup.TryGetValue(str, out short id)) {
            id = nextLookupId;
            WritingLookup[str] = nextLookupId++;
        }
        Writer.Write(id);
    }

    internal void WriteValue(object val) {
        switch (val) {
            case bool b:
                Writer.Write((byte) 0);
                Writer.Write(b);
                break;
            case byte b:
                Writer.Write((byte) 1);
                Writer.Write(b);
                break;
            case short b:
                WriteNumber((uint) b);
                break;
            case int b:
                WriteNumber((uint) b);
                break;
            case float b:
                if (float.IsEvenInteger(b)) {
                    WriteNumber((uint) b);
                } else {
                    Writer.Write((byte) 4);
                    Writer.Write(b);
                }
                break;
            case double b:
                if (double.IsEvenInteger(b)) {
                    WriteNumber((uint) b);
                } else {
                    Writer.Write((byte) 4);
                    Writer.Write((float) b);
                }
                break;
            case Enum e:
                EncodeString(e.ToString());
                break;
            case char c:
                EncodeString(c.ToString());
                break;
            case string b:
                EncodeString(b);
                break;
            default:
                throw new Exception($"Can't pack object into a binary package: {val}, {val.GetType()}");
        }

        void WriteNumber(uint num) {
            switch (num) {
                case <= byte.MaxValue:
                    Writer.Write((byte) 1);
                    Writer.Write((byte) num);
                    break;
                case <= (uint) short.MaxValue:
                    Writer.Write((byte) 2);
                    Writer.Write((short) num);
                    break;
                default:
                    Writer.Write((byte) 3);
                    Writer.Write(num);
                    break;
            }
        }
    }

    private void EncodeString(string b) {
        var rleEncode = TryEncodeRLE(b);
        if (rleEncode is { }) {
            Writer.Write((byte) 7);
            Writer.Write((short) rleEncode.Length);
            Writer.Write(rleEncode);
        } else {
            Writer.Write((byte) 5);
            WriteLookup(b);
        }
    }

    internal Element ReadElement() {
        var reader = Reader;

        var element = new Element(ReadLookup());

        var attrCount = Reader.ReadByte();
        var attrs = element.Attributes = new(attrCount, StringComparer.Ordinal);
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

    internal static byte[]? TryEncodeRLE(string str) {
        if (str.Length < 128 || str.Length * 2 > Math.Pow(2, 15)) {
            //Console.WriteLine($"Can't encode RLE: {str} - too short!");
            return null;
        }

        Span<byte> rle = str.Length < 4198 ? stackalloc byte[str.Length * 2] : new byte[str.Length * 2];
        int bufferIdx = 0;

        for (int i = 0; i < str.Length; i++) {
            byte repeatCount = 1;
            char c = str[i];
            if (!char.IsAscii(c)) {
                //Console.WriteLine($"Can't encode RLE: {str} - {c} is non ascii!");
                return null;
            }

            while (i + 1 < str.Length && str[i + 1] == c && repeatCount < 255) {
                repeatCount += 1;
                i++;
            }

            if (bufferIdx + 1 < rle.Length) {
                rle[bufferIdx++] = repeatCount;
                rle[bufferIdx++] = (byte) c;
            } else {
                return null;
            }
        }
        return rle[0..bufferIdx].ToArray();
    }

    public class Package {
        public string Name { get; init; } = "";
        public Element Data { get; init; } = null!;

        /// <summary>
        /// The file this package comes from
        /// </summary>
        public string? Filename { get; init; } = null;
    }

    public class Element {
        public Element() { }

        public Element(string? name = null) {
            Name = name;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object> Attributes { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Element[] Children = null!;

        public int Int(string attrName, int def = 0) {
            if (Attributes.TryGetValue(attrName, out var obj)) {
                return Convert.ToInt32(obj);
            }

            return def;
        }

        public bool Bool(string attrName, bool def = false) {
            if (Attributes.TryGetValue(attrName, out var obj)) {
                return Convert.ToBoolean(obj);
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

        public T Enum<T>(string attrName, T def) where T : struct, Enum {
            if (Attributes.TryGetValue(attrName, out var obj)) {
                return System.Enum.Parse<T>(obj.ToString()!);
            }

            return def;
        }
    }
}
