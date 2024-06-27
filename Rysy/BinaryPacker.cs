using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;
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
    private Dictionary<Element, DetailedWriteInfo>? DetailedWriteInfos;
    private Dictionary<string, DetailedLookupWriteInfo>? DetailedWriteLookupInfos;

    public struct DetailedWriteInfo {
        public long SelfSize;
        public long TotalSize;
    }
    
    public struct DetailedLookupWriteInfo {
        public long Size;
    }

    public IReadOnlyDictionary<Element, DetailedWriteInfo>? GetDetailedWriteInfo() => DetailedWriteInfos;
    public IReadOnlyDictionary<string, DetailedLookupWriteInfo>? GetDetailedWriteLookupInfo() => DetailedWriteLookupInfos;

    public IReadOnlyDictionary<string, short> GetWritingLookupTable() => WritingLookup;

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

    public static void SaveToStream(Package package, Stream output)
        => SaveToStream(package, output, false, out _);
    
    internal static void SaveToStream(Package package, Stream output, bool saveDetailedInformation, out BinaryPacker packerInstance) {
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
        if (saveDetailedInformation) {
            packer.DetailedWriteInfos = [];
            packer.DetailedWriteLookupInfos = [];
        }
        
        packer.WriteElement(package.Data);

        // write lookup table
        headerWriter.Write((short) packer.WritingLookup.Count);
        var orderedLookup = packer.WritingLookup.OrderBy(k => k.Value);
        if (packer.DetailedWriteLookupInfos is { } detailed) {
            foreach (var item in orderedLookup) {
                var start = headerWriter.BaseStream.Position;
                headerWriter.Write(item.Key);
                detailed[item.Key] = new() { Size = headerWriter.BaseStream.Position - start };
            }
        } else {
            foreach (var item in orderedLookup) {
                headerWriter.Write(item.Key);
            }
        }
        


        headerStream.Seek(0, SeekOrigin.Begin);
        headerStream.CopyTo(output);
        
        contentStream.Seek(0, SeekOrigin.Begin);
        contentStream.CopyTo(output);

        packerInstance = packer;
    }

    public static void SaveToFile(Package package, string filename) {
        using var memStream = new MemoryStream();

        SaveToStream(package, memStream);

        // now that we know everything went well, time to write to file
        
        // make sure the directory exists...
        if (Path.GetDirectoryName(filename) is {} dir)
            Directory.CreateDirectory(dir);
        
        using var fileStream = File.Open(filename, FileMode.Create);

        memStream.Seek(0, SeekOrigin.Begin);
        memStream.CopyTo(fileStream);
    }
    
    internal string ReadLookup() => StringLookup[Reader.ReadInt16()];

    internal void WriteElement(Element el) {
        var writer = Writer;
        var start = writer.BaseStream.Position;

        WriteLookup(el.Name ?? "");

        var attrs = el.Attributes;
        if (attrs is null) {
            writer.Write((byte)0);
        } else {
            byte count = 0;
            foreach (var (k, v) in attrs) {
                if (v is { })
                    count++;
            }
            writer.Write(count);
            foreach (var (name, val) in attrs) {
                if (val is { }) {
                    WriteLookup(name);
                    WriteValue(val);
                }
            }
        }

        var children = el.Children ?? Array.Empty<Element>();
        writer.Write((short) children.Length);

        var selfSize = writer.BaseStream.Position - start;
        
        foreach (var item in children) {
            WriteElement(item);
        }
        
        if (DetailedWriteInfos is { } detailed) {
            var totalSize = writer.BaseStream.Position - start;
            detailed[el] = new() {
                SelfSize = selfSize,
                TotalSize = totalSize,
            };
        }
    }

    internal void WriteLookup(string str) {
        if (!WritingLookup.TryGetValue(str, out short id)) {
            id = nextLookupId;
            WritingLookup[str] = nextLookupId++;
        }
        Writer.Write(id);
    }

    private const byte ExtElementType = 255; 

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
                WriteNumber(b);
                break;
            case int b:
                WriteNumber(b);
                break;
            case float b:
                if (float.IsInteger(b)) {
                    WriteNumber((int)b);
                } else {
                    Writer.Write((byte) 4);
                    Writer.Write(b);
                }
                break;
            case double b:
                if (double.IsInteger(b)) {
                    WriteNumber((int)b);
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
            /*
             EXTENDED - not supported in game!
             */
            case Element el:
                Writer.Write(ExtElementType);
                WriteElement(el);
                break;
            default:
                throw new Exception($"Can't pack object into a binary package: {val}, {val.GetType()}");
        }
    }

    private void WriteNumber(int num) {
        switch (num) {
            case >= byte.MinValue and <= byte.MaxValue:
                Writer.Write((byte) 1);
                Writer.Write((byte) num);
                break;
            case >= short.MinValue and <= short.MaxValue:
                Writer.Write((byte) 2);
                Writer.Write((short) num);
                break;   
            default:
                Writer.Write((byte) 3);
                Writer.Write(num);
                break;
        }
    }
    
    private void EncodeString(string b) {
        if (TryEncodeRLE(b, out var rleEncode) && rleEncode.Length <= b.Length) {
            Writer.Write((byte) 7);
            Writer.Write((short) rleEncode.Length);
            Writer.Write(rleEncode);
        } else if (b.Length > 512) {
            // Strings that are this big are really unlikely to repeat themselves
            // They're most likely tilegrids we couldn't RLE
            Writer.Write((byte) 6);
            Writer.Write(b);
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
        Span<byte> rle = dataLen < 1024 ? stackalloc byte[dataLen] : new byte[dataLen];
        Reader.BaseStream.Read(rle);

        StringBuilder builder = new();
        for (int i = 0; i < rle.Length; i += 2) {
            builder.Append((char) rle[i + 1], rle[i]);
        }

        return builder.ToString();
    }

    private static readonly byte[] _rleEncodeBuffer = new byte[short.MaxValue];
    
    internal static bool TryEncodeRLE(string str, out ReadOnlySpan<byte> encoded) {
        if (str.Length < 128) {
            //Console.WriteLine($"Can't encode RLE: {str} - too short!");
            encoded = default;
            return false;
        }

        Span<byte> rle = _rleEncodeBuffer;
        int bufferIdx = 0;

        for (int i = 0; i < str.Length; i++) {
            byte repeatCount = 1;
            char c = str[i];
            if (!char.IsAscii(c)) {
                //Console.WriteLine($"Can't encode RLE: {str} - {c} is non ascii!");
                encoded = default;
                return false;
            }

            while (i + 1 < str.Length && str[i + 1] == c && repeatCount < 255) {
                repeatCount++;
                i++;
            }

            if (bufferIdx + 1 < rle.Length) {
                rle[bufferIdx++] = repeatCount;
                rle[bufferIdx++] = (byte) c;
            } else {
                encoded = default;
                return false;
            }
        }

        encoded = rle[0..bufferIdx];
        return true;
    }

    public class Package {
        public string Name { get; init; } = "";
        public Element Data { get; init; } = null!;

        /// <summary>
        /// The file this package comes from
        /// </summary>
        public string? Filename { get; init; }

        public IEnumerable<Element> Rooms => Data.Children.FirstOrDefault(e => e.Name == "levels")?.Children ?? [];

        public IEnumerable<Element> EntitiesOrTriggers => Rooms.SelectMany(r =>
            r.Children.Where(e => e.Name is "entities" or "triggers").SelectMany(e => e.Children));
    }

    public class Element : IUntypedData {
        public Element() { }

        public Element(string? name = null) {
            Name = name;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object> Attributes { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Element[] Children = null!;
        
        public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) {
            return Attributes.TryGetValue(key, out value);
        }
    }
}
