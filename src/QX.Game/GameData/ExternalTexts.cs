using System.Collections;

namespace Qx.Game;

/// <summary>
/// Represents the hotel's <c>external_flash_texts</c> file, the localized strings the client
/// looks up by key.
/// </summary>
/// <remarks>Keys are compared case-insensitively.</remarks>
public sealed class ExternalTexts : IReadOnlyDictionary<string, string>
{
    private readonly Dictionary<string, string> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the number of texts.</summary>
    public int Count => _entries.Count;

    /// <summary>Gets the text stored under a key, or <see langword="null"/> when the key is absent.</summary>
    /// <param name="key">The text key, matched case-insensitively.</param>
    public string? this[string key] => _entries.GetValueOrDefault(key);

    string IReadOnlyDictionary<string, string>.this[string key] => _entries[key];

    /// <summary>Gets the text keys.</summary>
    public IEnumerable<string> Keys => _entries.Keys;

    /// <summary>Gets the text values.</summary>
    public IEnumerable<string> Values => _entries.Values;

    /// <summary>Gets whether a text exists for a key.</summary>
    /// <param name="key">The text key, matched case-insensitively.</param>
    public bool ContainsKey(string key) => _entries.ContainsKey(key);

    /// <summary>Gets the text stored under a key.</summary>
    /// <param name="key">The text key, matched case-insensitively.</param>
    /// <param name="value">The text, or <see langword="null"/> when the key is absent.</param>
    /// <returns><see langword="true"/> when the key exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(string key, out string value) => _entries.TryGetValue(key, out value!);

    /// <summary>Gets the text stored under a key.</summary>
    /// <param name="key">The text key, matched case-insensitively.</param>
    /// <param name="value">The text, or <see langword="null"/> when the key is absent.</param>
    /// <returns><see langword="true"/> when the key exists; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(string key, out string value) =>
        _entries.TryGetValue(key, out value!);

    /// <summary>Gets the display name of a badge from its <c>badge_name_</c> text.</summary>
    /// <param name="code">The badge code.</param>
    /// <returns>The badge name, or <see langword="null"/> when the hotel has no text for the badge.</returns>
    public string? BadgeName(string code) => _entries.GetValueOrDefault($"badge_name_{code}");

    /// <summary>Gets the display name of an avatar effect from its <c>fx_</c> text.</summary>
    /// <param name="id">The effect identifier.</param>
    /// <returns>The effect name, or <see langword="null"/> when the hotel has no text for the effect.</returns>
    public string? EffectName(int id) => _entries.GetValueOrDefault($"fx_{id}");

    /// <summary>Gets the display name of a hand item from its <c>handitem</c> text.</summary>
    /// <param name="id">The hand item identifier.</param>
    /// <returns>The hand item name, or <see langword="null"/> when the hotel has no text for the item.</returns>
    public string? HandItemName(int id) => _entries.GetValueOrDefault($"handitem{id}");

    /// <summary>Returns an enumerator over the key and text pairs.</summary>
    /// <returns>An enumerator over every entry.</returns>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Parses the contents of an <c>external_flash_texts</c> file.</summary>
    /// <remarks>
    /// Each line is read as <c>key=value</c>. Lines without an <c>=</c> or with an empty key are
    /// skipped, the key is trimmed, and everything after the first <c>=</c> is the value with a
    /// trailing carriage return removed. When a key appears more than once, the first entry wins.
    /// </remarks>
    /// <param name="content">The file contents.</param>
    /// <returns>The parsed texts.</returns>
    public static ExternalTexts Load(string content)
    {
        var texts = new ExternalTexts();
        foreach (string line in content.Split('\n'))
        {
            int split = line.IndexOf('=');
            if (split <= 0)
                continue;
            string key = line[..split].Trim();
            string value = line[(split + 1)..].TrimEnd('\r');
            if (key.Length > 0)
                texts._entries.TryAdd(key, value);
        }
        return texts;
    }
}
