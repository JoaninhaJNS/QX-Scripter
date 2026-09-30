using Qx;

namespace Qx.Protocol;

/// <summary>Represents the message names that a session catalog took from a fallback catalog of the same client build.</summary>
public sealed record CatalogSupplement
{
    /// <summary>Initializes a new instance of the <see cref="CatalogSupplement"/> record.</summary>
    /// <param name="provenance">The provenance of the fallback catalog, which must come from <see cref="CatalogOrigin.GEarthHandshake"/> or <see cref="CatalogOrigin.Sulek"/>.</param>
    /// <param name="alias_count">The number of message names taken from the fallback catalog, greater than 0.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provenance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="provenance"/> has another origin.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="alias_count"/> is 0 or less.</exception>
    public CatalogSupplement(CatalogProvenance provenance, int alias_count)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        if (provenance.Origin is not (CatalogOrigin.GEarthHandshake or CatalogOrigin.Sulek))
            throw new ArgumentException("A catalog supplement requires fallback provenance.", nameof(provenance));
        if (alias_count <= 0)
            throw new ArgumentOutOfRangeException(nameof(alias_count));
        Provenance = provenance;
        AliasCount = alias_count;
    }

    /// <summary>Gets the provenance of the fallback catalog.</summary>
    public CatalogProvenance Provenance { get; }
    /// <summary>Gets the number of message names taken from the fallback catalog.</summary>
    public int AliasCount { get; }
}

/// <summary>Represents the message catalog bound to a hotel session, with its provenance and client build.</summary>
public sealed record SessionCatalogBinding
{
    /// <summary>Initializes a new instance of the <see cref="SessionCatalogBinding"/> record with a read-only snapshot of the catalog.</summary>
    /// <remarks>
    /// An extracted catalog must match the source hash of its provenance, a build identity must match the
    /// catalog fingerprints, and a supplement must have the same client type and client version as the provenance.
    /// </remarks>
    /// <param name="client">The client type of the session, which must be supported and match <paramref name="provenance"/>.</param>
    /// <param name="catalog">The message catalog, which is <see langword="null"/> only when the origin is <see cref="CatalogOrigin.Unavailable"/>.</param>
    /// <param name="provenance">The provenance of the catalog.</param>
    /// <param name="build">The build identity of the catalog, or <see langword="null"/> when not known.</param>
    /// <param name="supplement">The names added to the catalog from a fallback catalog, or <see langword="null"/> when there are none.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provenance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the client type, catalog, provenance, build identity or supplement do not match each other.</exception>
    public SessionCatalogBinding(
        ClientType client,
        MessageCatalog? catalog,
        CatalogProvenance provenance,
        ClientBuildIdentity? build = null,
        CatalogSupplement? supplement = null)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        if (!ClientTypes.IsSupported(client) ||
            provenance.Client != client)
            throw new ArgumentException("The catalog provenance does not match the bound client.", nameof(provenance));
        if ((provenance.Origin == CatalogOrigin.Unavailable) != (catalog is null))
            throw new ArgumentException("Only an unavailable binding can omit its catalog.", nameof(catalog));
        if (provenance.Origin == CatalogOrigin.ClientExtraction)
        {
            if (provenance.SourceSha256 is null ||
                catalog is null ||
                !catalog.MatchesBuildFingerprint(provenance.SourceSha256))
            {
                throw new ArgumentException("An extracted catalog must match its source hash.", nameof(catalog));
            }
        }
        if (build is not null &&
            (catalog is null ||
             !catalog.MatchesBuildFingerprint(build.CatalogFingerprint) ||
             build.SchemaFingerprint is not null &&
             !catalog.MatchesSchemaFingerprint(build.SchemaFingerprint)))
        {
            throw new ArgumentException("The catalog does not match its build identity.", nameof(build));
        }
        if (supplement is not null &&
            (catalog is null ||
             supplement.Provenance.Client != client ||
             provenance.ClientVersion is null ||
             !string.Equals(
                 supplement.Provenance.ClientVersion,
                 provenance.ClientVersion,
                 StringComparison.Ordinal)))
        {
            throw new ArgumentException("The catalog supplement does not match the bound client.", nameof(supplement));
        }
        Client = client;
        Catalog = catalog?.Snapshot();
        Provenance = provenance;
        Build = build;
        Supplement = supplement;
    }

    /// <summary>Gets the client type of the session.</summary>
    public ClientType Client { get; }
    /// <summary>Gets a read-only snapshot of the message catalog, or <see langword="null"/> when no catalog is available.</summary>
    public MessageCatalog? Catalog { get; }
    /// <summary>Gets the provenance of the catalog.</summary>
    public CatalogProvenance Provenance { get; }
    /// <summary>Gets the build identity of the catalog, or <see langword="null"/> when not known.</summary>
    public ClientBuildIdentity? Build { get; }
    /// <summary>Gets the names added to the catalog from a fallback catalog, or <see langword="null"/> when there are none.</summary>
    public CatalogSupplement? Supplement { get; }
}

/// <summary>Represents the lease on a bound session catalog, used to replace or clear that binding later.</summary>
/// <param name="Value">The generation number of the binding, or 0 for an empty lease.</param>
public readonly record struct SessionCatalogLease(long Value)
{
    /// <summary>Gets whether the lease is empty, which is the case for the <see langword="default"/> value.</summary>
    public bool IsEmpty => Value == 0;
}
