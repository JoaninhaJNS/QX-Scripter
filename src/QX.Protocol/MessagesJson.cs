using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qx.Protocol;

/// <summary>Represents a JSON message list with incoming and outgoing header IDs and names.</summary>
/// <remarks>Property names are matched without regard to case.</remarks>
public sealed class MessagesJson
{
    /// <summary>Gets or sets the incoming messages.</summary>
    [JsonPropertyName("Incoming")]
    public List<MessageEntry> Incoming { get; set; } = [];

    /// <summary>Gets or sets the outgoing messages.</summary>
    [JsonPropertyName("Outgoing")]
    public List<MessageEntry> Outgoing { get; set; } = [];

    /// <summary>Parses a JSON message list.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed list, or an empty list when the JSON is <c>null</c>.</returns>
    /// <exception cref="JsonException">Thrown when <paramref name="json"/> is not valid JSON for a message list.</exception>
    public static MessagesJson Parse(string json) =>
        JsonSerializer.Deserialize(json, MessagesJsonContext.Default.MessagesJson) ?? new MessagesJson();

    /// <summary>Reads and parses a JSON message list from a file.</summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The parsed list, or an empty list when the JSON is <c>null</c>.</returns>
    public static MessagesJson Load(string path) => Parse(File.ReadAllText(path));
}

/// <summary>Represents one message in a <see cref="MessagesJson"/> list.</summary>
public sealed class MessageEntry
{
    /// <summary>Gets or sets the numeric header ID.</summary>
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    /// <summary>Gets or sets the message name.</summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";
}
