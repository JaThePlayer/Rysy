using YamlDotNet.Serialization.ObjectFactories;
using YamlDotNet.Serialization;

namespace Rysy.Helpers;

// taken from https://github.com/EverestAPI/Everest/blob/dev/Celeste.Mod.mm/Mod/Helpers/YamlHelper.cs
public static class YamlHelper {

    public static IDeserializer Deserializer { get; private set; } = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
    public static ISerializer Serializer { get; private set; } = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve).Build();

    /// <summary>
    /// Builds a deserializer that will provide YamlDotNet with the given object instead of creating a new one.
    /// This will make YamlDotNet update this object when deserializing.
    /// </summary>
    /// <param name="objectToBind">The object to set fields on</param>
    /// <returns>The newly-created deserializer</returns>
    public static IDeserializer DeserializerUsing(object objectToBind) {
        IObjectFactory defaultObjectFactory = new DefaultObjectFactory();
        Type objectType = objectToBind.GetType();

        return new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            // provide the given object if type matches, fall back to default behavior otherwise.
            .WithObjectFactory(type => type == objectType ? objectToBind : defaultObjectFactory.Create(type))
            .Build();
    }

}
