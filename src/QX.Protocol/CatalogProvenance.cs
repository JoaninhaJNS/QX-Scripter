using Qx;

namespace Qx.Protocol;

/// <summary>Specifies where a message catalog came from.</summary>
public enum CatalogOrigin
{
    /// <summary>The catalog was extracted from the files of the client build.</summary>
    ClientExtraction,
    /// <summary>The catalog came from the G-Earth connection handshake.</summary>
    GEarthHandshake,
    /// <summary>The catalog came from the Sulek message API.</summary>
    Sulek,
    /// <summary>The catalog is a reference catalog embedded in the application.</summary>
    EmbeddedReference,
    /// <summary>No catalog is available.</summary>
    Unavailable
}

/// <summary>Represents the origin, source and client build of a message catalog.</summary>
public sealed record CatalogProvenance
{
    /// <summary>Initializes a new instance of the <see cref="CatalogProvenance"/> record with validated and trimmed values.</summary>
    /// <param name="origin">The origin of the catalog.</param>
    /// <param name="client">The client type the catalog belongs to, which must be supported.</param>
    /// <param name="source">The source of the catalog, such as a file path or <c>G-Earth</c>, at most 1024 characters.</param>
    /// <param name="client_version">The client build version, at most 256 characters, or <see langword="null"/> when not known.</param>
    /// <param name="source_sha256">The SHA-256 hash of the source as 64 hexadecimal characters, or <see langword="null"/> when not known.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="origin"/> is not a defined value or <paramref name="client"/> is not supported.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="client_version"/> is blank, too long or contains control characters, or <paramref name="source_sha256"/> is not 64 hexadecimal characters.</exception>
    public CatalogProvenance(
        CatalogOrigin origin,
        ClientType client,
        string source,
        string? client_version = null,
        string? source_sha256 = null)
    {
        if (!Enum.IsDefined(origin))
            throw new ArgumentOutOfRangeException(nameof(origin));
        if (!ClientTypes.IsSupported(client))
            throw new ArgumentOutOfRangeException(nameof(client));
        string normalized_source = Normalize(source, nameof(source), 1024);
        string? normalized_version = client_version is null
            ? null
            : Normalize(client_version, nameof(client_version), 256);
        if (source_sha256 is not null &&
            (source_sha256.Length != 64 || source_sha256.Any(character => !Uri.IsHexDigit(character))))
        {
            throw new ArgumentException("The catalog source hash is invalid.", nameof(source_sha256));
        }
        Origin = origin;
        Client = client;
        Source = normalized_source;
        ClientVersion = normalized_version;
        SourceSha256 = source_sha256?.ToUpperInvariant();
    }

    /// <summary>Gets the origin of the catalog.</summary>
    public CatalogOrigin Origin { get; }
    /// <summary>Gets the client type the catalog belongs to.</summary>
    public ClientType Client { get; }
    /// <summary>Gets the trimmed source of the catalog, such as a file path or <c>G-Earth</c>.</summary>
    public string Source { get; }
    /// <summary>Gets the trimmed client build version, or <see langword="null"/> when not known.</summary>
    public string? ClientVersion { get; }
    /// <summary>Gets the SHA-256 hash of the source in upper-case hexadecimal, or <see langword="null"/> when not known.</summary>
    public string? SourceSha256 { get; }

    static string Normalize(string value, string parameter_name, int maximum_length)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter_name);
        string normalized = value.Trim();
        if (normalized.Length > maximum_length || normalized.Any(char.IsControl))
            throw new ArgumentException("The catalog provenance value is invalid.", parameter_name);
        return normalized;
    }
}
