using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Mods;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Rysy.Helpers;

public sealed class DecalRegistry : IDisposable {
    public IReadOnlyList<DecalRegistryEntry> Entries => _Entries;

    public List<DecalRegistryEntry> GetEntriesForMod(ModMeta mod) {
        if (mod is null)
            return new List<DecalRegistryEntry>(0);

        if (EntriesByRoot.TryGetValue(mod.Filesystem.Root, out var entries))
            return entries;

        return new List<DecalRegistryEntry>(0);
    }

    public void AddEntryToMod(ModMeta mod, DecalRegistryEntry newEntry, int index = -1) {
        _Entries.Add(newEntry);
        
        if (EntriesByRoot.TryGetValue(mod.Filesystem.Root, out var entries)) {
            if (index >= 0) {
                entries.Insert(index, newEntry);
            } else {
                entries.Add(newEntry);
            }
        } else {
            EntriesByRoot[mod.Filesystem.Root] = [ newEntry ];
        }
    }
    
    public bool RemoveEntryFromMod(ModMeta mod, DecalRegistryEntry toRemove) {
        _Entries.Remove(toRemove);
        
        if (EntriesByRoot.TryGetValue(mod.Filesystem.Root, out var entries)) {
            return entries.Remove(toRemove);
        }

        return false;
    }

    public void SaveMod(ModMeta mod) {
        var entries = GetEntriesForMod(mod);
        var serialized = GFX.DecalRegistry.Serialize(entries);

        if (mod.Filesystem is IWriteableModFilesystem fs) {
            fs.TryWriteToFile("DecalRegistry.xml", s => serialized.Save(s));
        }
    }

    public XDocument Serialize(IReadOnlyList<DecalRegistryEntry> entries) {
        var doc = new XDocument();
        var decalList = new XElement("decals");
        doc.AddFirst(decalList);

        foreach (var entry in entries) {
            var entryXml = entry.Serialize();

            decalList.Add(entryXml);
        }

        return doc;
    }

    private static Dictionary<string, List<DecalRegistryEntry>> EntriesByRoot = new(StringComparer.Ordinal);
    private static List<DecalRegistryEntry> _Entries = new List<DecalRegistryEntry>();

    private static bool Disposed;

    public void ReadFileFromMod(IModFilesystem fs) {
        fs.TryWatchAndOpen("DecalRegistry.xml", (s) => {
            if (Disposed)
                return;

            var xml = XDocument.Load(s);

            if (xml.Element("decals") is not { } decalsListXml) {
                return;
            }

            var entries = new List<DecalRegistryEntry>();
            foreach (var decalNode in decalsListXml.Elements()) {
                if (decalNode.Name.LocalName != "decal") {
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

public record struct DecalRegistryPath {
    public string Value;

    public DecalRegistryEntry.Types Type;
    
    public string SavedName => Type switch {
        DecalRegistryEntry.Types.SingleTexture => Value,
        DecalRegistryEntry.Types.Directory => $"{Value}/",
        DecalRegistryEntry.Types.StartsWith => $"{Value}*",
    };

    public DecalRegistryPath(string path) {
        Type = path switch {
            [.., '/'] => DecalRegistryEntry.Types.Directory,
            [.., '*'] => DecalRegistryEntry.Types.StartsWith,
            _ => DecalRegistryEntry.Types.SingleTexture
        };
        
        Value = Type switch {
            DecalRegistryEntry.Types.SingleTexture => path,
            _ => path[..^1],
        };
    }
    
    public IEnumerable<VirtTexture> GetAffectedTextures() {
        return Type switch {
            DecalRegistryEntry.Types.SingleTexture => [ GFX.Atlas[Decal.MapTextureToPath(Value)] ],
            DecalRegistryEntry.Types.Directory => [],
            DecalRegistryEntry.Types.StartsWith => [],
            _ => []
        };
    }

    public bool AffectsDecalPath(string path) {
        return Type switch {
            DecalRegistryEntry.Types.SingleTexture => Value == path,
            DecalRegistryEntry.Types.Directory => path.AsSpan().StartsWith(Value),
            DecalRegistryEntry.Types.StartsWith => path.AsSpan().StartsWith(Value),
            _ => false,
        };
    }
}

public sealed class DecalRegistryEntry {
    public string Path { get; set; }

    public Types Type => Path switch {
        [.., '/'] => Types.Directory,
        [.., '*'] => Types.StartsWith,
        _ => Types.SingleTexture
    };

    public List<DecalRegistryProperty> Props { get; set; } = [];

    public static bool TryLoadFromNode(XElement decalNode, [NotNullWhen(true)] out DecalRegistryEntry? entry) {
        var newEntry = new DecalRegistryEntry();

        if (newEntry.LoadFromNode(decalNode)) {
            entry = newEntry;
            return true;
        }

        entry = null;
        return false;
    }
    
    public bool LoadFromNode(XElement decalNode) {
        Props.Clear();

        if (decalNode.Name.LocalName != "decal") {
            return false;
        }
        
        if (decalNode.Attribute("path")?.Value is not { } path) {
            return false;
        }

        Path = path;

        foreach (var ch in decalNode.Elements()) {
            Props.Add(DecalRegistryProperty.FromNode(ch));
        }

        return true;
    }

    public IEnumerable<ISprite> GetSprites() {
        var pos = Vector2.Zero;

        foreach (var tex in GetAffectedTextures()) {
            yield return ISprite.FromTexture(pos, tex).Centered();
        }
    }

    public IEnumerable<VirtTexture> GetAffectedTextures() => new DecalRegistryPath(Path).GetAffectedTextures();

    public XElement Serialize() {
        var e = new XElement("decal");
        e.Add(new XAttribute("path", Path));
        foreach (var p in Props) {
            e.Add(p.Serialize());
        }

        return e;
    }

    public DecalRegistryEntry Clone() {
        var newEntry = new DecalRegistryEntry();
        newEntry.LoadFromNode(Serialize());
        return newEntry;
    }
    
    public enum Types {
        SingleTexture,
        Directory,
        StartsWith,
    }
}

public abstract class DecalRegistryProperty {
    public string Name => Data.SID;

    public EntityData Data { get; private set; }

    public virtual IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        return ISprite.FromTexture(texture).Centered();
    }

    public static DecalRegistryProperty FromNode(XElement node) {
        var prop = CreateNewUninitialized(node.Name.LocalName);

        foreach (var attr in node.Attributes()) {
            prop.Data[attr.Name.LocalName] = EntityDataValueFromXmlInnerText(attr.Value);
        }

        return prop;
    }

    public static DecalRegistryProperty CreateFromPlacement(Placement placement) {
        var sid = placement.SID ?? "";
        var prop = CreateNewUninitialized(sid);
        
        if (EntityRegistry.GetFields(sid, RegisteredEntityType.DecalRegistryProperty) is {} fields) {
            foreach (var (name, field) in fields) {
                prop.Data[name] = field.GetDefault();
            }
        }
        
        prop.Data.BulkUpdate(placement.ValueOverrides);
        
        return prop;
    }

    public virtual XElement Serialize() {
        var e = new XElement(Name);
        foreach (var (k, v) in Data) {
            e.Add(new XAttribute(k, v));
        }

        return e;
    }

    public DecalRegistryProperty Clone() {
        var el = Serialize();

        return FromNode(el);
    }
    
    private static DecalRegistryProperty CreateNewUninitialized(string propType) {
        DecalRegistryProperty prop;

        if (EntityRegistry.Registered.TryGetValue(propType, out var type) && type.Type is RegisteredEntityType.DecalRegistryProperty) {
            prop = (DecalRegistryProperty)Activator.CreateInstance(type.CSharpType)!;
        } else {
            prop = new UnknownDecalRegistryProperty();
        }
        prop.Data = new(propType, new Dictionary<string, object>());

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
}

public sealed class UnknownDecalRegistryProperty : DecalRegistryProperty {

}

[CustomEntity("light")]
public sealed class LightDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
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

[CustomEntity("bloom")]
public sealed class BloomDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        offsetX = 0f,
        offsetY = 0f,
        alpha = Fields.Float(1f).WithMin(0f).WithMax(1f),
        radius = Fields.Float(1f).WithMin(0f)
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        var gradientSprite = GFX.Atlas["util/bloomgradient"];
        var scale = Data.Float("radius", 1f) * 2f * (1f / gradientSprite.Width);
        var alpha = Data.Float("alpha", 1f).Div(2f).AtLeast(0f).AtMost(0.6f);
        var offset = new Vector2(Data.Float("offsetX"), Data.Float("offsetY"));
        
        yield return ISprite.FromTexture(offset, gradientSprite).Centered() with {
            Scale = new(scale),
            Color = Color.White * alpha
        };
        
        yield return ISprite.FromTexture(texture).Centered();
    }
}

[CustomEntity("floaty")]
public sealed class FloatyDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
       // base.Add(this.wave = new SineWave(Calc.Random.Range(0.1f, 0.4f), Calc.Random.NextFloat() * 6.28318548f));
       float counter = MathF.PI * 2f * 0.1f * ctx.Time;

       return ISprite.FromTexture(new Vector2(0, float.Sin(counter) * 4f), texture).Centered();
    }
}

[CustomEntity("banner")]
public sealed class BannerDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        speed = 1f,
        amplitude = 1f,
        sliceSize = 1,
        sliceSinIncrement = 0.050f,
        easeDown = false,
        offset = 0f,
        onlyIfWindy = false,
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        var sliceSize = Data.Int("sliceSize", 1);
        var easeDown = Data.Bool("easeDown", false);
        var waveSpeed = Data.Float("speed", 1f);
        var waveAmplitude = Data.Float("amplitude", 1f);
        var sliceSinIncrement = Data.Float("sliceSinIncrement", 1f);
        var offset = Data.Float("offset");

        var count = texture.Height / sliceSize;

        var baseSprite = ISprite.FromTexture(texture);
        baseSprite.Origin = new Vector2(0.5f, 1);

        var sineTimer = ctx.Time;
        
        for (int i = 0; i < texture.Height; i += sliceSize)
        {
            var spr = baseSprite.CreateSubtexture(0, i, texture.Width, sliceSize);
            
            float percent = easeDown ? i / (float)count : 1f - i / (float)count;
            
            float x = float.Sin(sineTimer * waveSpeed + i * sliceSinIncrement) * percent * waveAmplitude + percent * offset;

            spr.Pos += new Vector2(x.Floor(), i - texture.Height / 2f);

            yield return spr;
        }
    }
}