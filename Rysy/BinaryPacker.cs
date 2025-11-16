using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace Rysy;

public sealed class BinaryPacker {
    string[] _stringLookup = null!;
    string _packageName = null!;

    BinaryReader _reader = null!;
    BinaryWriter _writer = null!;
    BinaryWriter _headerWriter = null!;

    short _nextLookupId = 0;
    Dictionary<string, short> _writingLookup = new();
    private Dictionary<Element, DetailedWriteInfo>? _detailedWriteInfos;
    private Dictionary<string, DetailedLookupWriteInfo>? _detailedWriteLookupInfos;

    public struct DetailedWriteInfo {
        public long SelfSize;
        public long TotalSize;
    }
    
    public struct DetailedLookupWriteInfo {
        public long Size;
    }

    public IReadOnlyDictionary<Element, DetailedWriteInfo>? GetDetailedWriteInfo() => _detailedWriteInfos;
    public IReadOnlyDictionary<string, DetailedLookupWriteInfo>? GetDetailedWriteLookupInfo() => _detailedWriteLookupInfos;

    public IReadOnlyDictionary<string, short> GetWritingLookupTable() => _writingLookup;

    internal BinaryPacker() { }

    public static Package FromBinary(string filename) {
#if DEBUG
        using var watch = new ScopedStopwatch("FromBinary");
#endif

        ArgumentNullException.ThrowIfNull(filename);
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

        packer._stringLookup = stringLookup;
        packer._packageName = package;
        packer._reader = reader;

        var element = packer.ReadElement();

        return new() {
            Name = packer._packageName,
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
        packer._writer = contentWriter;
        packer._headerWriter = headerWriter;
        
        if (saveDetailedInformation) {
            packer._detailedWriteInfos = [];
            packer._detailedWriteLookupInfos = [];
        }
        
        // reserve space for string lookup length, we'll write it later
        var lookupLenPos = headerWriter.BaseStream.Position;
        headerWriter.Write((short)0);
        
        // write the data, which will populate the string lookup table as well
        packer.WriteElement(package.Data);

        // write lookup table length now
        headerWriter.Seek((int)lookupLenPos, SeekOrigin.Begin);
        headerWriter.Write((short) packer._writingLookup.Count);

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
    
    internal string ReadLookup() => _stringLookup[_reader.ReadInt16()];

    internal void WriteElement(Element el) {
        var writer = _writer;
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
        
        if (_detailedWriteInfos is { } detailed) {
            var totalSize = writer.BaseStream.Position - start;
            detailed[el] = new() {
                SelfSize = selfSize,
                TotalSize = totalSize,
            };
        }
    }

    internal void WriteLookup(string str) {
        if (!_writingLookup.TryGetValue(str, out short id)) {
            id = _nextLookupId;
            _writingLookup[str] = _nextLookupId++;

            // Write the string to the lookup table in the map header
            var headerWriter = _headerWriter;
            if (_detailedWriteLookupInfos is { } detailed) {
                var start = headerWriter.BaseStream.Position;
                headerWriter.Write(str);
                detailed[str] = new() { Size = headerWriter.BaseStream.Position - start };
            } else {
                headerWriter.Write(str);
            }
        }
        _writer.Write(id);
    }

    private const byte ExtElementType = 255; 

    internal void WriteValue(object val) {
        switch (val) {
            case bool b:
                _writer.Write((byte) 0);
                _writer.Write(b);
                break;
            case byte b:
                _writer.Write((byte) 1);
                _writer.Write(b);
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
                    _writer.Write((byte) 4);
                    _writer.Write(b);
                }
                break;
            case double b:
                if (double.IsInteger(b)) {
                    WriteNumber((int)b);
                } else {
                    _writer.Write((byte) 4);
                    _writer.Write((float) b);
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
                _writer.Write(ExtElementType);
                WriteElement(el);
                break;
            default:
                throw new Exception($"Can't pack object into a binary package: {val}, {val.GetType()}");
        }
    }

    private void WriteNumber(int num) {
        switch (num) {
            case >= byte.MinValue and <= byte.MaxValue:
                _writer.Write((byte) 1);
                _writer.Write((byte) num);
                break;
            case >= short.MinValue and <= short.MaxValue:
                _writer.Write((byte) 2);
                _writer.Write((short) num);
                break;   
            default:
                _writer.Write((byte) 3);
                _writer.Write(num);
                break;
        }
    }
    
    private void EncodeString(string b) {
        if (TryEncodeRle(b, out var rleEncode) && rleEncode.Length <= b.Length) {
            _writer.Write((byte) 7);
            _writer.Write((short) rleEncode.Length);
            _writer.Write(rleEncode);
        } else if (b.Length > 512) {
            // Strings that are this big are really unlikely to repeat themselves
            // They're most likely tilegrids we couldn't RLE
            _writer.Write((byte) 6);
            _writer.Write(b);
        } else {
            _writer.Write((byte) 5);
            WriteLookup(b);
        }
    }

    internal Element ReadElement() {
        var reader = _reader;

        var element = new Element(ReadLookup());

        var attrCount = _reader.ReadByte();
        var attrs = element.Attributes = new(attrCount, StringComparer.Ordinal);
        for (int i = 0; i < attrCount; i++) {
            string attrName = ReadLookup();

            attrs[attrName] = _reader.ReadByte() switch {
                0 => reader.ReadBoolean(),
                1 => Convert.ToInt32(reader.ReadByte()),
                2 => Convert.ToInt32(reader.ReadInt16()),
                3 => Convert.ToInt32(reader.ReadInt32()),
                4 => reader.ReadSingle(),
                5 => ReadLookup(),
                6 => reader.ReadString(),
                7 => DecodeRle(),
                var unkType => throw new InvalidDataException($"Unknown attribute type: {unkType}")
            };
        }

        var childCount = _reader.ReadInt16();
        var children = element.Children = new Element[childCount];
        for (int i = 0; i < childCount; i++) {
            children[i] = ReadElement();
        }

        return element;
    }

    internal string DecodeRle() {
        var dataLen = _reader.ReadInt16();
        Span<byte> rle = dataLen < 1024 ? stackalloc byte[dataLen] : new byte[dataLen];
        _reader.BaseStream.ReadExactly(rle);

        StringBuilder builder = new();
        for (int i = 0; i < rle.Length; i += 2) {
            builder.Append((char) rle[i + 1], rle[i]);
        }

        return builder.ToString();
    }

    private static readonly byte[] RleEncodeBuffer = new byte[short.MaxValue];
    
    internal static bool TryEncodeRle(string str, out ReadOnlySpan<byte> encoded) {
        if (str.Length < 16) {
            //Console.WriteLine($"Can't encode RLE: {str} - too short!");
            encoded = default;
            return false;
        }

        Span<byte> rle = RleEncodeBuffer;
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

        public IEnumerable<Element> GetRooms() => Data.Children.FirstOrDefault(e => e.Name == "levels")?.Children ?? [];

        public IEnumerable<Element> GetEntitiesAndTriggers() => GetRooms().SelectMany(r =>
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
        public Dictionary<string, object> Attributes { get; set; } = [];

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Element[] Children = [];
        
        public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) {
            if (Attributes is null) {
                value = null;
                return false;
            }
            
            return Attributes.TryGetValue(key, out value);
        }

        public Element CreateWithComparer(IEqualityComparer<string> comparer) {
            var element = new Element(Name) {
                Attributes = new(Attributes ?? [], comparer), 
                Children = Children.Select(c => c.CreateWithComparer(comparer)).ToArray()
            };

            return element;
        }

        public Element AddChild(Element element) {
            Array.Resize(ref Children, Children.Length + 1);
            Children[^1] = element;

            return element;
        }

        public void SetNullableObj(string key, object? o) {
            if (o is null) {
                Attributes.Remove(key);
            } else {
                Attributes[key] = o;
            }
        }
        
        public void SetNullableStruct<T>(string key, T? o) where T : struct {
            if (o is null) {
                Attributes.Remove(key);
            } else {
                Attributes[key] = o.Value;
            }
        }
    }
}
