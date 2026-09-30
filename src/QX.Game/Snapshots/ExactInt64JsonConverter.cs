using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qx.Game.Snapshots;

/// <summary>Provides JSON conversion that writes 64-bit integers as decimal strings and reads them from strings or numbers.</summary>
/// <remarks>A string keeps values beyond 2^53 exact for JSON readers that parse numbers as doubles.</remarks>
public sealed class ExactInt64JsonConverter : JsonConverter<long>
{
    /// <summary>Reads a 64-bit integer from a JSON number or a decimal string.</summary>
    /// <param name="reader">The reader positioned at the value.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options in use.</param>
    /// <returns>The value read.</returns>
    /// <exception cref="JsonException">Thrown when the token is not a number or string that holds a signed 64-bit integer.</exception>
    public override long Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt64(out long value) => value,
            JsonTokenType.String when long.TryParse(
                reader.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long value) => value,
            _ => throw new JsonException("Expected a signed 64-bit decimal value.")
        };

    /// <summary>Writes a 64-bit integer as a JSON string in invariant decimal form.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in use.</param>
    public override void Write(
        Utf8JsonWriter writer,
        long value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
}
