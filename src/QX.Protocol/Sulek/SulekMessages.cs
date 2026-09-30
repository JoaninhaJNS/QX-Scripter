using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Qx;

namespace Qx.Protocol.Sulek;

/// <summary>Represents a message list returned by the Sulek API.</summary>
public sealed partial class SulekMessages
{
    /// <summary>Gets or sets the incoming and outgoing messages.</summary>
    [JsonPropertyName("messages")]
    public SulekDirections Messages { get; set; } = new();

    /// <summary>Parses a Sulek message list from JSON.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed list, or an empty list when the JSON is <c>null</c>.</returns>
    /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not valid JSON for a message list.</exception>
    public static SulekMessages Parse(string json) =>
        JsonSerializer.Deserialize(json, SulekJsonContext.Default.SulekMessages) ?? new SulekMessages();

    /// <summary>Creates a message catalog from the list, with names cleaned by <see cref="CleanName(string)"/>.</summary>
    /// <returns>A new, writable message catalog.</returns>
    public MessageCatalog ToCatalog()
    {
        var catalog = new MessageCatalog();
        foreach (SulekEntry entry in Messages.Incoming)
            catalog.Add(Direction.In, entry.Id, CleanName(entry.Name));
        foreach (SulekEntry entry in Messages.Outgoing)
            catalog.Add(Direction.Out, entry.Id, CleanName(entry.Name));
        return catalog;
    }

    /// <summary>Removes a trailing <c>Composer</c>, <c>MessageComposer</c>, <c>Event</c> or <c>MessageEvent</c> suffix from a message class name.</summary>
    /// <param name="name">The message class name.</param>
    /// <returns>The name without the suffix.</returns>
    public static string CleanName(string name) => SuffixRegex().Replace(name, "");

    [GeneratedRegex(@"(((Message)?Composer)|((Message)?Event))$")]
    private static partial Regex SuffixRegex();
}

/// <summary>Represents the incoming and outgoing messages of a Sulek message list.</summary>
public sealed class SulekDirections
{
    /// <summary>Gets or sets the incoming messages.</summary>
    [JsonPropertyName("incoming")]
    public List<SulekEntry> Incoming { get; set; } = [];

    /// <summary>Gets or sets the outgoing messages.</summary>
    [JsonPropertyName("outgoing")]
    public List<SulekEntry> Outgoing { get; set; } = [];
}

/// <summary>Represents one message in a Sulek message list.</summary>
public sealed class SulekEntry
{
    /// <summary>Gets or sets the numeric header ID.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the message class name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SulekMessages))]
internal partial class SulekJsonContext : JsonSerializerContext;
