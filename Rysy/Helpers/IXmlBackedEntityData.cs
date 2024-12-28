using System.Xml;

namespace Rysy.History;

public interface IXmlBackedEntityData {
    public XmlNode? Xml { get; set; }
    
    public string EntityDataName { get; }
    
    public EntityData FakeData { get; }

    internal void OnXmlChanged();
}

public static class XmlBackedEntityDataExtensions {
    public static EntityData CreateFakeData(this IXmlBackedEntityData self) {
        var attrs = new Dictionary<string, object>() {  };

        if (self.Xml is { Attributes: { } xmlAttrs }) {
            foreach (XmlAttribute attr in xmlAttrs) {
                var k = attr.Name;
                var value = attr.Value;
                object v = value switch {
                    _ when float.TryParse(value, CultureInfo.InvariantCulture, out var i) => i,
                    _ when int.TryParse(value, CultureInfo.InvariantCulture, out var i) => i,
                    _ => value
                };
                
                attrs[k] = v;
            }
        }

        var data = new EntityData(self.EntityDataName, attrs);

        return data;
    }

    private static string ToXmlString(object v) {
        return v is IFormattable f 
            ? f.ToString(null, CultureInfo.InvariantCulture)
            : v.ToString() ?? "";
    }
    
    public static List<XmlAttribute> UpdateData(this IXmlBackedEntityData self, IDictionary<string, object?> values) {
        List<XmlAttribute> added = [];
        
        if (self.Xml is not { Attributes: {} attributes } xml)
            return added;

        bool anyChanges = false;
        
        foreach (var (k, v) in values) {
            if (attributes[k] is { } existing) {
                if (v is null) {
                    attributes.Remove(existing);
                    anyChanges = true;
                    continue;
                }

                var newVal = ToXmlString(v);
                
                if (existing.Value != newVal) {
                    existing.Value = newVal;
                    anyChanges = true;
                }
                
            } else if (v is not null) {
                var attr = xml.OwnerDocument!.CreateAttribute(k);
                attr.Value = ToXmlString(v);
                attributes.Append(attr);
                added.Add(attr);
                anyChanges = true;
            }
        }
        
        if (anyChanges)
            self.OnXmlChanged();

        return added;
    }
}