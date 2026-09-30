namespace Qx.Protocol;

/// <summary>Represents the identity of a client build by the fingerprints of its message catalog and outgoing message schemas.</summary>
/// <param name="CatalogFingerprint">The build fingerprint of the message catalog, matched against <see cref="MessageCatalog.BuildFingerprint"/>.</param>
/// <param name="SchemaFingerprint">The fingerprint of the outgoing message schemas, matched against <see cref="MessageCatalog.SchemaFingerprint"/>, or <see langword="null"/> when not known.</param>
public sealed record ClientBuildIdentity(
    string CatalogFingerprint,
    string? SchemaFingerprint = null);

/// <summary>Represents a client build identity together with its message catalog and the state of its schema upgrade.</summary>
/// <param name="Identity">The identity of the client build.</param>
/// <param name="Catalog">The message catalog of the build, or <see langword="null"/> when none is available.</param>
/// <param name="SchemaUpgrade">A task that completes with the binding after outgoing message schemas are added, or <see langword="null"/> when no upgrade is pending.</param>
/// <param name="SchemaError">The error that stopped the schema upgrade, or <see langword="null"/> when there is none.</param>
public sealed record ClientBuildBinding(
    ClientBuildIdentity Identity,
    MessageCatalog? Catalog = null,
    Task<ClientBuildBinding>? SchemaUpgrade = null,
    Exception? SchemaError = null);
