using Rysy.Extensions;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

/// <summary>
/// Represents a dictionary, in which the value is obtained by translating the key using the current language.
/// </summary>
public class TranslatedDictionary<TKey> : IDictionary<TKey, string>
    where TKey : notnull {
    public TranslatedDictionary(Func<TKey, string>? toString = null) {
        Converter = toString ?? ((TKey k) => k.ToString()!);
    }

    public TranslatedDictionary(string prefix, Func<TKey, string>? toString = null) : this(toString) {
        Prefix = prefix;
    }

    /// <summary>
    /// The prefix to use for generating a translation key in case the translation key wasn't specified for a given key.
    /// The language key is generated as follows: [prefix].[converter(key)]
    /// </summary>
    public string Prefix { get; set; }

    /// <summary>
    /// Converts a key into a string, used when generating translation keys.
    /// By default, it calls ToString on the key.
    /// </summary>
    public Func<TKey, string> Converter { get; set; }

    private Dictionary<TKey, string?> Items = new();

    private string Translate(TKey key) {
        if (!Items.TryGetValue(key, out var translationKey) || translationKey is null)
            translationKey = Converter(key);

        return translationKey.TranslateOrHumanize(Prefix);
    }

    public ICollection<TKey> Keys => Items.Keys;

    public ICollection<string> Values => Items.Select(p => Translate(p.Key)).ToList();

    public int Count => Items.Count;

    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the value associated with a given key. 
    /// The getter will automatically translate the key, while the setter will set the translation key to use.
    /// If the translation key gets set to null, it will be automatically generated using the <see cref="Prefix"/>.
    /// </summary>
    public string this[TKey key] { 
        get => Translate(key); 
        set => Items[key] = value; 
    }

    /// <summary>
    /// Sets the value associated with a given key. 
    /// If <paramref name="value"/> is null, it will be automatically generated using the <see cref="Prefix"/>.
    /// </summary>
    public void Add(TKey key, string? value) {
        Items.Add(key, value);
    }

    /// <summary>
    /// Adds a new value to the dictionary. The translation key will be automatically generated using the <see cref="Prefix"/>.
    /// </summary>
    public void Add(TKey key) {
        Items.Add(key, null);
    }

    public bool ContainsKey(TKey key) => Items.ContainsKey(key);

    public bool Remove(TKey key) {
        return Items.Remove(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out string value) {
        if (!Items.ContainsKey(key)) {
            value = null;
            return false;
        }

        value = Translate(key);
        return true;
    }

    public void Add(KeyValuePair<TKey, string> item) {
        Add(item.Key, item.Value);
    }

    public void Clear() {
        Items.Clear();
    }

    public bool Contains(KeyValuePair<TKey, string> item) {
        return ContainsKey(item.Key) && this[item.Key] == item.Value;
    }

    public void CopyTo(KeyValuePair<TKey, string>[] array, int arrayIndex) {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<TKey, string> item) {
        return Remove(item.Key);
    }

    public IEnumerator<KeyValuePair<TKey, string>> GetEnumerator() {
        foreach (var item in Items) {
            yield return new(item.Key, Translate(item.Key));
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
