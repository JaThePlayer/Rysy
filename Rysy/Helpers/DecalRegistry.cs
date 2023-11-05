using Microsoft.Xna.Framework;
using Rysy.Graphics;
using Rysy.Mods;
using System.Xml;
using System.Xml.Linq;

namespace Rysy.Helpers;

public sealed class DecalRegistry : IDisposable {
    public IReadOnlyList<DecalRegistryEntry> Entries => _Entries;

    public IReadOnlyList<DecalRegistryEntry> GetEntriesForMod(ModMeta mod) {
        if (mod is null)
            return new List<DecalRegistryEntry>(0);

        if (EntriesByRoot.TryGetValue(mod.Filesystem.Root, out var entries))
            return entries;

        return new List<DecalRegistryEntry>(0);
    }

    public XDocument? Serialize(IReadOnlyList<DecalRegistryEntry> entries) {
        XDocument doc = new XDocument();
        var decalList = new XElement("decals");
        doc.AddFirst(decalList);

        foreach (var entry in entries) {
            var entryXml = entry.Serialize();

            decalList.Add(entryXml);
        }

        Console.WriteLine(doc.ToString());

        return doc;
    }

    private static Dictionary<string, List<DecalRegistryEntry>> EntriesByRoot = new(StringComparer.Ordinal);
    private static List<DecalRegistryEntry> _Entries = new List<DecalRegistryEntry>();

    private static bool Disposed;

    public void ReadFileFromMod(IModFilesystem fs) {
        fs.TryWatchAndOpen("DecalRegistry.xml", (s) => {
            if (Disposed)
                return;
            var xml = new XmlDocument();
            xml.Load(s);

            if (xml["decals"] is not { } decalsListXml) {
                return;
            }


            var entries = new List<DecalRegistryEntry>();
            foreach (var n in decalsListXml.ChildNodes) {
                if (n is not XmlNode { Name: "decal" } decalNode) {
                    continue;
                }
                var entry = new DecalRegistryEntry();
                if (entry.LoadFromNode(decalNode))
                    entries.Add(entry);
            }

            lock (_Entries) {
                lock (EntriesByRoot) {
                    EntriesByRoot[fs.Root] = entries;
                    _Entries = EntriesByRoot.SelectMany(kv => kv.Value).ToList();
                }
            }
        });
    }

    public void Dispose() {
        _Entries.Clear();
        EntriesByRoot.Clear();
        Disposed = true;
    }
}

public class DecalRegistryEntry {
    public string Path { get; set; }

    public List<DecalRegistryProperty> Props { get; set; } = new();

    public bool LoadFromNode(XmlNode n) {
        Props.Clear();

        if (n is XmlNode { Name: "decal" } decalNode) {
            if (decalNode.Attributes?["path"]?.InnerText is not { } path) {
                return false;
            }

            Path = path;

            foreach (var ch in decalNode.ChildNodes) {
                if (ch is XmlNode { } childNode && ch is not XmlComment) {
                    Props.Add(DecalRegistryProperty.FromNode(childNode));
                }
            }
        }

        return true;
    }

    public IEnumerable<ISprite> GetSprites() {
        var realPath = Decal.MapTextureToPath(Path);
        var pos = Vector2.Zero;

        if (GFX.Atlas.TryGet(realPath, out var tex)) {
            yield return ISprite.FromTexture(pos, tex).Centered();
            yield break;
        }

        var textures = GFX.Atlas.GetSubtextures(realPath);

        foreach (var t in textures) {
            yield return ISprite.FromTexture(pos, t).Centered();

            pos.X += t.Width;
        }
    }

    public XElement Serialize() {
        var e = new XElement("decal");
        e.Add(new XAttribute("path", Path));
        foreach (var p in Props) {
            e.Add(p.Serialize());
        }

        return e;
    }
}

public abstract class DecalRegistryProperty {
    public string Name => Data.SID;

    public EntityData Data { get; private set; }

    public static DecalRegistryProperty FromNode(XmlNode node) {
        DecalRegistryProperty prop;

        if (NameToType.TryGetValue(node.Name, out var type)) {
            prop = (DecalRegistryProperty)Activator.CreateInstance(type)!;
        } else {
            prop = new UnknownDecalRegistryProperty();
        }
        prop.Data = new(node.Name, new Dictionary<string, object>());

        if (node.Attributes is { })
            foreach (XmlAttribute attr in node.Attributes.OfType<XmlAttribute>()) {
                prop.Data[attr.Name] = EntityDataValueFromXmlInnerText(attr.Value);
            }
        //(prop).LogAsJson();

        return prop;
    }

    private static object EntityDataValueFromXmlInnerText(string text) {
        if (int.TryParse(text, CultureInfo.InvariantCulture, out var i)) {
            return i;
        }
        if (float.TryParse(text, CultureInfo.InvariantCulture, out var f)) {
            return f;
        }
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        return text;
    }

    private static Dictionary<string, Type> NameToType = new(StringComparer.Ordinal) {
        ["light"] = typeof(LightDecalRegistryProperty),
        ["bloom"] = typeof(BloomDecalRegistryProperty),
    };

    public virtual XElement Serialize() {
        var e = new XElement(Name);
        foreach (var (k, v) in Data) {
            e.Add(new XAttribute(k, v));
        }

        return e;
    }
}

public class UnknownDecalRegistryProperty : DecalRegistryProperty {

}

public class LightDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        offsetX = 0f,
        offsetY = 0f,
        color = Fields.RGB("ffffff").AllowNull(),
        alpha = Fields.Float(1f).WithMin(0f).WithMax(1f),
        startFade = Fields.Int(16).WithMin(0),
        endFade = Fields.Int(24).WithMin(0)
    });

    public static PlacementList GetPlacements() => new("default");
}

public class BloomDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        offsetX = 0f,
        offsetY = 0f,
        alpha = Fields.Float(1f).WithMin(0f).WithMax(1f),
        radius = Fields.Float(1f).WithMin(0f)
    });

    public static PlacementList GetPlacements() => new("default");
}