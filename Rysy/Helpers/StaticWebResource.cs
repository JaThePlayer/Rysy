using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Rysy.Helpers;

/// <summary>
/// Represents a static resource obtained from the web.
/// It will only be obtained once per program lifetime.
/// </summary>
public abstract class StaticWebResource<T> {
    private readonly Lazy<Task<T>> _value;
    private readonly Uri _uri;
    
    public abstract Formats Format { get; }

    protected StaticWebResource(string uri) : this(new Uri(uri)) {}
    
    protected StaticWebResource(Uri uri) {
        _uri = uri;
        _value = new(GetResource, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<T> GetResourceAsync() {
        return _value.Value;
    }
    
    protected abstract T GetFallback();

    private async Task<T> GetResource() {
        try {
            Logger.Write($"StaticWebResource<{typeof(T).Name}>", LogLevel.Info, $"Getting resource from {_uri}");
            using var client = new HttpClient();
            
            if (Format == Formats.Text) {
                var str = await client.GetStringAsync(_uri);
                str = str.Replace('“', '"');
                str = str.Replace('”', '"');
                str = str.Replace('’', '\'');
                
                if (typeof(T) == typeof(string)) {
                    Logger.Write($"StaticWebResource<{typeof(T).Name}>", LogLevel.Info, $"Successfully got resource from {_uri}.");
                    return (T)(object)str;
                }
                
                throw new Exception("Can't parse string to type " + typeof(T).Name);
            }
            
            await using var stream = await client.GetStreamAsync(_uri);
            using var reader = new StreamReader(stream);

            var item = Format switch {
                Formats.Yaml => YamlHelper.Deserializer.Deserialize<T>(reader),
                Formats.Json => await JsonSerializer.DeserializeAsync<T>(stream),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (item is null) {
                throw new Exception($"Failed to deserialize resource as {Format}");
            }
        
            Logger.Write($"StaticWebResource<{typeof(T).Name}>", LogLevel.Info, $"Successfully got resource from {_uri}.");
            return item;
        } catch (Exception e) {
            Logger.Write($"StaticWebResource<{typeof(T).Name}>", LogLevel.Error, $"Failed to get resource from {_uri}: {e}");
            return GetFallback();
        }
    }

    public enum Formats {
        Json,
        Yaml,
        Text,
    }
}

public sealed class TextStaticWebResource : StaticWebResource<string> {
    public TextStaticWebResource(string uri) : base(uri)
    {
    }

    public TextStaticWebResource(Uri uri) : base(uri)
    {
    }

    public override Formats Format => Formats.Text;

    protected override string GetFallback() => "";
}

public abstract class StaticWebResourceRepo<T, TResource>
    where TResource : StaticWebResource<T> {
    private readonly ConcurrentDictionary<string, TResource> _cache = new();
    
    protected abstract Uri GetResourceUri(string key);
    
    protected abstract TResource CreateResource(Uri uri);

    public TResource GetResource(string key) {
        return _cache.GetOrAdd(key, (k) => CreateResource(GetResourceUri(k)));
    }
}

public sealed class TextWebResourceRepo(CompositeFormat format) : StaticWebResourceRepo<string, TextStaticWebResource> {
    protected override Uri GetResourceUri(string key) {
        var u = string.Format(CultureInfo.InvariantCulture, format, key);
        return new Uri(u);
    }

    protected override TextStaticWebResource CreateResource(Uri uri) => new(uri);
}