using System.Collections.Concurrent;
using Qx;
using Qx.Messages;

namespace Qx.Protocol;

/// <summary>Represents the message manager that resolves message names, keys and headers against the catalogs of the connected client build.</summary>
/// <remarks>
/// When a session catalog is bound for a client, name and header lookups for that client use only that
/// catalog. Otherwise the catalog selected by <see cref="BindCatalogBuild"/>, or else the live catalog, is used
/// first, and header IDs from the fallback catalogs are added only where they agree with it. Only Flash is
/// supported.
/// </remarks>
public sealed class MessageManager : IMessageManager, ISemanticMessageResolver
{
    private const int CompatibleReferenceParts = 20;
    private const int CompatibleReferenceRequiredParts = 19;
    private readonly MessageMap _map;
    private readonly MessageRegistry _registry;
    private MessageCatalog? _catalog;
    private MessageCatalog? _fallback_catalog;
    private MessageCatalog? _default_versioned_catalog;
    private readonly ConcurrentDictionary<
        (string CatalogFingerprint, string SchemaFingerprint),
        MessageCatalog> _versioned_catalogs = new();
    private ClientBuildIdentity? _catalog_build;
    private readonly object _session_catalog_sync = new();
    private SessionCatalogState? _session_catalog;
    private long _session_catalog_generation;
    private ClientType _active_client;

    /// <summary>Initializes a new instance of the <see cref="MessageManager"/> class over a message map.</summary>
    /// <param name="map">The message map, which also supplies the message registry.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> is <see langword="null"/>.</exception>
    public MessageManager(MessageMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _map = map;
        _registry = map.Registry;
    }

    /// <summary>Creates a message manager over the embedded <c>messages.ini</c> registry.</summary>
    /// <returns>The new message manager, with no catalogs loaded.</returns>
    public static MessageManager CreateWithEmbeddedMap() => new(MessagesIniParser.ParseEmbedded());

    /// <summary>Gets or sets the client type of the active session.</summary>
    /// <remarks>While a session catalog is bound, the getter returns the client type of that binding.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is neither <see cref="ClientType.None"/> nor a supported client type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a session catalog for another client type is bound.</exception>
    public ClientType ActiveClient
    {
        get => Volatile.Read(ref _session_catalog)?.Binding.Client ?? _active_client;
        set
        {
            if (value != ClientType.None)
                RequireSupportedClient(value);
            lock (_session_catalog_sync)
            {
                if (_session_catalog is { } session && session.Binding.Client != value)
                    throw new InvalidOperationException("The active client is fixed for the bound session.");
                _active_client = value;
            }
        }
    }

    /// <summary>Gets the message map.</summary>
    public MessageMap Map => _map;

    /// <summary>Gets the message registry.</summary>
    public MessageRegistry Registry => _registry;

    /// <summary>Gets the catalog binding of the active session, or <see langword="null"/> when none is bound.</summary>
    public SessionCatalogBinding? ActiveCatalogBinding =>
        Volatile.Read(ref _session_catalog)?.Binding;

    /// <summary>Binds a catalog to the active session, replacing any current binding.</summary>
    /// <param name="binding">The session catalog binding.</param>
    /// <returns>The lease that replaces or clears this binding later.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is <see langword="null"/>.</exception>
    public SessionCatalogLease BindSessionCatalog(SessionCatalogBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_session_catalog_sync)
        {
            long generation = ++_session_catalog_generation;
            Volatile.Write(ref _session_catalog, new SessionCatalogState(generation, binding));
            return new SessionCatalogLease(generation);
        }
    }

    /// <summary>Replaces the session catalog binding when a lease is still current and the client type is unchanged.</summary>
    /// <param name="expected">The lease of the binding to replace.</param>
    /// <param name="binding">The new binding, for the same client type.</param>
    /// <param name="replacement">The lease of the new binding, or an empty lease when nothing was replaced.</param>
    /// <returns><see langword="true"/> if the binding was replaced; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="binding"/> is <see langword="null"/>.</exception>
    public bool TryReplaceSessionCatalog(
        SessionCatalogLease expected,
        SessionCatalogBinding binding,
        out SessionCatalogLease replacement)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_session_catalog_sync)
        {
            SessionCatalogState? current = _session_catalog;
            if (expected.IsEmpty ||
                current is null ||
                current.Generation != expected.Value ||
                current.Binding.Client != binding.Client)
            {
                replacement = default;
                return false;
            }

            long generation = ++_session_catalog_generation;
            Volatile.Write(ref _session_catalog, new SessionCatalogState(generation, binding));
            replacement = new SessionCatalogLease(generation);
            return true;
        }
    }

    /// <summary>Removes the session catalog binding when a lease is still current.</summary>
    /// <param name="lease">The lease of the binding to remove.</param>
    /// <returns><see langword="true"/> if the binding was removed; otherwise, <see langword="false"/>.</returns>
    public bool ClearSessionCatalog(SessionCatalogLease lease)
    {
        lock (_session_catalog_sync)
        {
            SessionCatalogState? current = _session_catalog;
            if (lease.IsEmpty || current is null || current.Generation != lease.Value)
                return false;
            Volatile.Write(ref _session_catalog, null);
            return true;
        }
    }

    /// <summary>Loads the live catalog from a JSON message list, replacing the current live catalog.</summary>
    /// <param name="client">The client type, which must be supported.</param>
    /// <param name="json">The message list.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not supported.</exception>
    public void LoadCatalog(ClientType client, MessagesJson json)
    {
        RequireSupportedClient(client);
        Volatile.Write(ref _catalog, MessageCatalog.FromJson(json));
    }

    /// <summary>Loads the live catalog, replacing the current live catalog.</summary>
    /// <param name="client">The client type, which must be supported.</param>
    /// <param name="catalog">The catalog.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not supported.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is <see langword="null"/>.</exception>
    public void LoadCatalog(ClientType client, MessageCatalog catalog)
    {
        RequireSupportedClient(client);
        ArgumentNullException.ThrowIfNull(catalog);
        Volatile.Write(ref _catalog, catalog);
    }

    /// <summary>Removes the live catalog.</summary>
    /// <param name="client">The client type, which must be supported.</param>
    /// <returns><see langword="true"/> if a live catalog was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not supported.</exception>
    public bool ClearCatalog(ClientType client)
    {
        RequireSupportedClient(client);
        return Interlocked.Exchange(ref _catalog, null) is not null;
    }

    /// <summary>Loads the fallback catalog, replacing the current fallback catalog.</summary>
    /// <param name="client">The client type, which must be supported.</param>
    /// <param name="catalog">The catalog.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not supported.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is <see langword="null"/>.</exception>
    public void LoadFallbackCatalog(ClientType client, MessageCatalog catalog)
    {
        RequireSupportedClient(client);
        ArgumentNullException.ThrowIfNull(catalog);
        Volatile.Write(ref _fallback_catalog, catalog);
    }

    /// <summary>Registers a catalog prepared for a specific client build, keyed by its build and schema fingerprints.</summary>
    /// <remarks>
    /// A catalog with a build fingerprint replaces any catalog registered under the same fingerprints. The
    /// catalog also becomes the default versioned catalog when <paramref name="preferred"/> is
    /// <see langword="true"/> or it has no build fingerprint.
    /// </remarks>
    /// <param name="client">The client type, which must be supported.</param>
    /// <param name="catalog">The catalog.</param>
    /// <param name="preferred">Whether the catalog becomes the default versioned catalog.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not supported.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is <see langword="null"/>.</exception>
    public void LoadVerifiedFallbackCatalog(
        ClientType client,
        MessageCatalog catalog,
        bool preferred = true)
    {
        RequireSupportedClient(client);
        ArgumentNullException.ThrowIfNull(catalog);
        MessageCatalog registered = catalog;
        if (catalog.BuildFingerprint is { } fingerprint)
        {
            registered = _versioned_catalogs.AddOrUpdate(
                (fingerprint, catalog.SchemaFingerprint ?? ""),
                catalog,
                (_, _) => catalog);
        }
        if (preferred || catalog.BuildFingerprint is null)
            Volatile.Write(ref _default_versioned_catalog, registered);
    }

    /// <summary>Gets whether a catalog with a build fingerprint is available for a client.</summary>
    /// <remarks>
    /// While a session catalog is bound for the client, only that catalog is checked. Otherwise the registered
    /// versioned catalogs are checked, ignoring case and surrounding whitespace.
    /// </remarks>
    /// <param name="client">The client type.</param>
    /// <param name="fingerprint">The build fingerprint.</param>
    public bool HasCatalogBuild(ClientType client, string fingerprint)
    {
        if (!IsSupportedClient(client))
            return false;
        if (ActiveCatalogBinding is { } binding && binding.Client == client)
            return binding.Catalog?.MatchesBuildFingerprint(fingerprint) is true;
        return _versioned_catalogs.Keys.Any(key =>
            key.CatalogFingerprint.Equals(fingerprint.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Selects the registered versioned catalog to use by client build identity.</summary>
    /// <remarks>The fingerprints are trimmed and upper-cased. A blank schema fingerprint is treated as none.</remarks>
    /// <param name="client">The client type, which must be supported.</param>
    /// <param name="identity">The build identity, or <see langword="null"/> or a blank catalog fingerprint to clear the selection.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not supported.</exception>
    public void BindCatalogBuild(ClientType client, ClientBuildIdentity? identity)
    {
        RequireSupportedClient(client);
        if (identity is null || string.IsNullOrWhiteSpace(identity.CatalogFingerprint))
        {
            Volatile.Write(ref _catalog_build, null);
            return;
        }
        Volatile.Write(ref _catalog_build, identity with
        {
            CatalogFingerprint = identity.CatalogFingerprint.Trim().ToUpperInvariant(),
            SchemaFingerprint = string.IsNullOrWhiteSpace(identity.SchemaFingerprint)
                ? null
                : identity.SchemaFingerprint.Trim().ToUpperInvariant()
        });
    }

    /// <summary>Gets whether a catalog is available for a client.</summary>
    /// <remarks>
    /// While a session catalog is bound for the client, this is whether that catalog has headers. Otherwise it is
    /// whether any live, fallback or versioned catalog is loaded.
    /// </remarks>
    /// <param name="client">The client type.</param>
    public bool HasCatalog(ClientType client)
    {
        if (!IsSupportedClient(client))
            return false;
        if (Volatile.Read(ref _session_catalog) is { } session)
        {
            if (session.Binding.Client == client)
                return session.Binding.Catalog?.HeaderCount > 0;
        }
        return Volatile.Read(ref _catalog) is not null ||
            Volatile.Read(ref _fallback_catalog) is not null ||
            Volatile.Read(ref _default_versioned_catalog) is not null ||
            !_versioned_catalogs.IsEmpty;
    }

    /// <summary>Gets the wire profile of the client build in use for a client.</summary>
    /// <remarks>
    /// While a session catalog is bound for the client, its profile is used, combined with the profile of a
    /// matching analyzed versioned catalog whose known layouts take precedence. Otherwise the profile of the
    /// catalog selected by build identity is used, or else the first analyzed profile of the live, versioned and
    /// fallback catalogs.
    /// </remarks>
    /// <param name="client">The client type.</param>
    /// <returns>The wire profile, or the <see langword="default"/> profile, which is not analyzed, when none is available.</returns>
    public MessageWireProfile GetWireProfile(ClientType client)
    {
        if (!IsSupportedClient(client))
            return default;
        if (Volatile.Read(ref _session_catalog) is { } session)
        {
            if (session.Binding.Client == client)
            {
                MessageWireProfile profile = session.Binding.Catalog?.WireProfile ?? default;
                if (TryGetSessionMetadataCatalog(session.Binding, out MessageCatalog? enriched) &&
                    enriched.WireProfile.IsAnalyzed)
                {
                    return MergeWireProfiles(enriched.WireProfile, profile);
                }
                return profile;
            }
        }
        if (TryGetBoundVersionedCatalog(client, out MessageCatalog? bound, out _))
            return bound.WireProfile;
        if (Volatile.Read(ref _catalog) is { } catalog && catalog.WireProfile.IsAnalyzed)
            return catalog.WireProfile;
        if (TryGetUsableVersionedCatalog(client, out MessageCatalog? versioned) && versioned.WireProfile.IsAnalyzed)
            return versioned.WireProfile;
        if (Volatile.Read(ref _fallback_catalog) is { } fallback && fallback.WireProfile.IsAnalyzed)
            return fallback.WireProfile;
        return default;
    }

    static MessageWireProfile MergeWireProfiles(
        MessageWireProfile preferred,
        MessageWireProfile fallback)
    {
        if (!preferred.IsAnalyzed)
            return fallback;
        if (!fallback.IsAnalyzed)
            return preferred;
        return new MessageWireProfile(
            preferred.WiredContextLayout is MessageWiredContextLayout.Unknown
                ? fallback.WiredContextLayout
                : preferred.WiredContextLayout,
            preferred.WiredConditionHasSeparateInvert ?? fallback.WiredConditionHasSeparateInvert,
            FlashGuestRoomResultLayout:
                preferred.FlashGuestRoomResultLayout ?? fallback.FlashGuestRoomResultLayout,
            FlashMarketplaceLayout:
                preferred.FlashMarketplaceLayout is FlashMarketplaceWireLayout.Unknown
                    ? fallback.FlashMarketplaceLayout
                    : preferred.FlashMarketplaceLayout);
    }

    /// <summary>Gets whether a message name resolves to at least one header for a client.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="name">The message name, matched without regard to case.</param>
    public bool HasMessage(ClientType client, Direction direction, string name) =>
        IsSupportedClient(client) && TryGetIds(client, direction, name, out _);

    /// <summary>Gets whether a message key resolves to at least one header for the active client.</summary>
    /// <param name="key">The message key.</param>
    public bool HasMessage(MessageKey key) => HasMessage(ActiveClient, key);

    /// <summary>Gets whether a message key is declared in the message registry.</summary>
    /// <param name="key">The message key.</param>
    public bool IsKnown(MessageKey key) => _registry.TryGet(key, out _);

    /// <summary>Gets whether a message key is declared and has a message name for the active client.</summary>
    /// <param name="key">The message key.</param>
    public bool IsApplicable(MessageKey key) =>
        IsSupportedClient(ActiveClient) &&
        _registry.TryGet(key, out MessageDescriptor descriptor) &&
        descriptor.NamesFor(ActiveClient).Count != 0;

    /// <summary>Gets whether a message key resolves to at least one header for a client.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="key">The message key.</param>
    public bool HasMessage(ClientType client, MessageKey key) =>
        TryGetHeaders(client, key, out _);

    /// <summary>Tries to get the single header that a message key resolves to for the active client.</summary>
    /// <param name="key">The message key.</param>
    /// <param name="header">The header, or the default header when the key does not resolve to exactly one header.</param>
    /// <returns><see langword="true"/> if the key resolves to exactly one header; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeader(MessageKey key, out Header header) =>
        TryGetHeader(ActiveClient, key, out header);

    /// <summary>Tries to get the single header that a message key resolves to for a client.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="key">The message key.</param>
    /// <param name="header">The header, or the default header when the key does not resolve to exactly one header.</param>
    /// <returns><see langword="true"/> if the key resolves to exactly one header; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeader(ClientType client, MessageKey key, out Header header)
    {
        if (TryGetHeaders(client, key, out IReadOnlyList<Header> headers) && headers.Count == 1)
        {
            header = headers[^1];
            return true;
        }
        header = default;
        return false;
    }

    /// <summary>Tries to get every header that a message key resolves to for the active client.</summary>
    /// <param name="key">The message key.</param>
    /// <param name="headers">The headers, or an empty list when none can be resolved.</param>
    /// <returns><see langword="true"/> if at least one header was resolved; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeaders(MessageKey key, out IReadOnlyList<Header> headers) =>
        TryGetHeaders(ActiveClient, key, out headers);

    /// <summary>Tries to get every header that a message key resolves to for a client.</summary>
    /// <remarks>
    /// Only keys declared with <c>k:</c> in the registry resolve. Header IDs whose catalog name is one of the
    /// key's names are preferred, and IDs whose catalog name belongs to another declared key are left out.
    /// </remarks>
    /// <param name="client">The client type.</param>
    /// <param name="key">The message key.</param>
    /// <param name="headers">The headers, or an empty list when none can be resolved.</param>
    /// <returns><see langword="true"/> if at least one header was resolved; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeaders(
        ClientType client,
        MessageKey key,
        out IReadOnlyList<Header> headers)
    {
        headers = [];
        if (!IsSupportedClient(client) ||
            !_registry.TryGet(key, out MessageDescriptor descriptor) ||
            !descriptor.HasExplicitKey ||
            !HasCatalog(client))
        {
            return false;
        }

        IReadOnlyList<string> names = descriptor.NamesFor(client);
        var values = new List<short>();
        foreach (string name in names)
        {
            if (!TryGetIds(client, descriptor.Direction, name, out IReadOnlyList<short> found))
                continue;
            foreach (short value in found)
                if (!values.Contains(value))
                    values.Add(value);
        }

        var primary_values = new List<short>();
        var fallback_values = new List<short>();
        foreach (short value in values)
        {
            if (!TryGetName(client, descriptor.Direction, value, out string primary_name))
            {
                fallback_values.Add(value);
                continue;
            }
            if (names.Any(name => name.Equals(primary_name, StringComparison.OrdinalIgnoreCase)))
            {
                primary_values.Add(value);
                continue;
            }
            if (!_registry.TryGet(
                    client,
                    descriptor.Direction,
                    primary_name,
                    out MessageDescriptor primary_descriptor) ||
                !primary_descriptor.HasExplicitKey ||
                primary_descriptor.Key == key)
            {
                fallback_values.Add(value);
            }
        }
        IReadOnlyList<short> resolved = primary_values.Count == 0
            ? fallback_values
            : primary_values;
        headers = resolved.Select(value => new Header(descriptor.Direction, value)).ToArray();
        return headers.Count > 0;
    }

    /// <summary>Tries to get the header of a message identifier.</summary>
    /// <remarks>When the identifier resolves to several headers, the last one is returned.</remarks>
    /// <param name="id">The message identifier.</param>
    /// <param name="header">The header, or the default header when none can be resolved.</param>
    /// <returns><see langword="true"/> if at least one header was resolved; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeader(Identifier id, out Header header)
    {
        if (TryGetHeaders(id, out IReadOnlyList<Header> headers))
        {
            header = headers[^1];
            return true;
        }
        header = default;
        return false;
    }

    /// <summary>Tries to get every header that a message identifier resolves to, including the headers of equivalent names.</summary>
    /// <remarks>The active client is used, or the client of the identifier when no client is active.</remarks>
    /// <param name="id">The message identifier.</param>
    /// <param name="headers">The headers, or an empty list when none can be resolved.</param>
    /// <returns><see langword="true"/> if at least one header was resolved; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeaders(Identifier id, out IReadOnlyList<Header> headers)
    {
        headers = [];

        ClientType target = ActiveClient == ClientType.None ? id.Client : ActiveClient;
        if (!IsSupportedClient(target) || !HasCatalog(target))
            return false;

        var values = new List<short>();
        foreach (string name in ResolveNames(id, target))
        {
            if (!TryGetIds(target, id.Direction, name, out IReadOnlyList<short> found))
                continue;
            foreach (short value in found)
                if (!values.Contains(value))
                    values.Add(value);
        }
        headers = values.Select(value => new Header(id.Direction, value)).ToArray();
        return headers.Count > 0;
    }

    /// <summary>Tries to get the identifier of a header for the active client.</summary>
    /// <param name="header">The header to look up.</param>
    /// <param name="id">The identifier with the active client type and the primary name of the header, or <see cref="Identifier.Unknown"/> when the header is not known.</param>
    /// <returns><see langword="true"/> if the header is known; otherwise, <see langword="false"/>.</returns>
    public bool TryGetIdentifier(Header header, out Identifier id)
    {
        id = Identifier.Unknown;
        if (TryGetName(ActiveClient, header.Direction, header.Value, out string name))
        {
            id = new Identifier(ActiveClient, header.Direction, name);
            return true;
        }
        return false;
    }

    /// <summary>Tries to get the outgoing message schemas of a message identifier.</summary>
    /// <remarks>The active client is used, or the client of the identifier when no client is active.</remarks>
    /// <param name="identifier">The identifier of an outgoing message.</param>
    /// <param name="schemas">The schemas, or an empty list when there are none.</param>
    /// <returns><see langword="true"/> if at least one schema was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOutgoingSchemas(
        Identifier identifier,
        out IReadOnlyList<OutgoingMessageSchema> schemas)
    {
        ClientType target = ActiveClient == ClientType.None ? identifier.Client : ActiveClient;
        return TryGetOutgoingSchemas(target, identifier, out schemas);
    }

    /// <summary>Tries to get the outgoing message schemas of an outgoing message name for a client.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="name">The outgoing message name.</param>
    /// <param name="schemas">The schemas, or an empty list when there are none.</param>
    /// <returns><see langword="true"/> if at least one schema was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOutgoingSchemas(
        ClientType client,
        string name,
        out IReadOnlyList<OutgoingMessageSchema> schemas) =>
        TryGetOutgoingSchemas(
            client,
            new Identifier(ClientType.None, Direction.Out, name),
            out schemas);

    /// <summary>Tries to get the outgoing message schemas of a message identifier for a client, across every header of its equivalent names.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="identifier">The identifier, whose direction must be <see cref="Qx.Direction.Out"/>.</param>
    /// <param name="schemas">The schemas, or an empty list when there are none.</param>
    /// <returns><see langword="true"/> if at least one schema was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOutgoingSchemas(
        ClientType client,
        Identifier identifier,
        out IReadOnlyList<OutgoingMessageSchema> schemas)
    {
        schemas = [];
        if (!IsSupportedClient(client) || identifier.Direction != Direction.Out || !HasCatalog(client))
            return false;

        var resolved = new List<OutgoingMessageSchema>();
        var headers = new HashSet<short>();
        foreach (string name in ResolveNames(identifier, client))
        {
            if (!TryGetIds(client, Direction.Out, name, out IReadOnlyList<short> ids))
                continue;
            foreach (short id in ids)
            {
                if (!headers.Add(id) ||
                    !TryGetOutgoingSchemas(client, new Header(Direction.Out, id), out IReadOnlyList<OutgoingMessageSchema> found))
                    continue;
                resolved.AddRange(found);
            }
        }

        schemas = resolved;
        return resolved.Count > 0;
    }

    /// <summary>Tries to get the outgoing message schemas of a header for the active client.</summary>
    /// <param name="header">The header, whose direction must be <see cref="Qx.Direction.Out"/>.</param>
    /// <param name="schemas">The schemas, or an empty list when there are none.</param>
    /// <returns><see langword="true"/> if at least one schema was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOutgoingSchemas(
        Header header,
        out IReadOnlyList<OutgoingMessageSchema> schemas) =>
        TryGetOutgoingSchemas(ActiveClient, header, out schemas);

    /// <summary>Tries to get the outgoing message schemas of a header for a client.</summary>
    /// <remarks>
    /// Schemas come from the session catalog when one is bound for the client. Otherwise they come from the
    /// catalog in use or from a fallback catalog whose name for the header agrees with it.
    /// </remarks>
    /// <param name="client">The client type.</param>
    /// <param name="header">The header, whose direction must be <see cref="Qx.Direction.Out"/>.</param>
    /// <param name="schemas">The schemas, or an empty list when there are none.</param>
    /// <returns><see langword="true"/> if at least one schema was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOutgoingSchemas(
        ClientType client,
        Header header,
        out IReadOnlyList<OutgoingMessageSchema> schemas)
    {
        schemas = [];
        if (!IsSupportedClient(client) || header.Direction != Direction.Out)
            return false;
        if (Volatile.Read(ref _session_catalog) is { } session)
        {
            if (session.Binding.Client == client)
            {
                if (session.Binding.Catalog is not { } session_catalog)
                    return false;
                if (session_catalog.TryGetOutgoingSchemas(header.Value, out schemas))
                    return true;
                return TryGetSessionMetadataCatalog(session.Binding, out MessageCatalog? enriched) &&
                    enriched.TryGetOutgoingSchemas(header.Value, out schemas);
            }
        }
        if (TryGetBoundVersionedCatalog(client, out MessageCatalog? bound, out ClientBuildIdentity? identity))
        {
            if (bound.MatchesSchemaFingerprint(identity.SchemaFingerprint) &&
                bound.TryGetOutgoingSchemas(header.Value, out schemas))
                return true;
            if (!bound.TryGetName(header.Direction, header.Value, out string build_name))
                return false;
            return Volatile.Read(ref _fallback_catalog) is { } stable_for_build &&
                TryGetCompatibleSchemas(client, stable_for_build, header, build_name, out schemas);
        }
        if (Volatile.Read(ref _catalog) is { } catalog)
        {
            if (catalog.TryGetOutgoingSchemas(header.Value, out schemas))
                return true;
            if (TryGetCoveredFallbackSchemas(Volatile.Read(ref _fallback_catalog), client, catalog, header, out schemas) ||
                TryGetCoveredFallbackSchemas(Volatile.Read(ref _default_versioned_catalog), client, catalog, header, out schemas))
                return true;
            if (!catalog.TryGetName(header.Direction, header.Value, out string live_name))
                return false;
            return (Volatile.Read(ref _fallback_catalog) is { } compatible_stable &&
                    TryGetCompatibleSchemas(client, compatible_stable, header, live_name, out schemas)) ||
                (Volatile.Read(ref _default_versioned_catalog) is { } compatible_versioned &&
                    TryGetCompatibleSchemas(client, compatible_versioned, header, live_name, out schemas));
        }
        if (TryGetUsableVersionedCatalog(client, out MessageCatalog? versioned) &&
            versioned.TryGetOutgoingSchemas(header.Value, out schemas))
            return true;
        return Volatile.Read(ref _fallback_catalog) is { } stable &&
            stable.TryGetOutgoingSchemas(header.Value, out schemas);
    }

    bool TryGetId(ClientType client, Direction direction, string name, out short id)
    {
        if (TryGetIds(client, direction, name, out IReadOnlyList<short> ids))
        {
            id = ids[^1];
            return true;
        }
        id = default;
        return false;
    }

    bool TryGetIds(ClientType client, Direction direction, string name, out IReadOnlyList<short> ids)
    {
        if (Volatile.Read(ref _session_catalog) is { } session)
        {
            if (session.Binding.Client == client)
            {
                if (session.Binding.Catalog is { } session_catalog)
                    return session_catalog.TryGetIds(direction, name, out ids);
                ids = [];
                return false;
            }
        }
        var values = new List<short>();
        if (TryGetBoundVersionedCatalog(client, out MessageCatalog? bound, out _))
        {
            if (bound.TryGetIds(direction, name, out IReadOnlyList<short> build_ids))
                values.AddRange(build_ids);
            AppendCompatibleFallbackIds(Volatile.Read(ref _fallback_catalog), client, direction, name, bound, values);
            AppendCompatibleFallbackIds(Volatile.Read(ref _catalog), client, direction, name, bound, values);
        }
        else if (Volatile.Read(ref _catalog) is { } catalog)
        {
            if (catalog.TryGetIds(direction, name, out IReadOnlyList<short> catalog_ids))
                values.AddRange(catalog_ids);
            AppendCompatibleFallbackIds(Volatile.Read(ref _fallback_catalog), client, direction, name, catalog, values);
            AppendCompatibleFallbackIds(Volatile.Read(ref _default_versioned_catalog), client, direction, name, catalog, values);
        }
        else
        {
            AppendStandaloneFallbackIds(Volatile.Read(ref _fallback_catalog), client, direction, name, values);
            if (TryGetUsableVersionedCatalog(client, out MessageCatalog? versioned))
                AppendStandaloneIds(versioned, direction, name, values);
        }
        ids = values;
        return values.Count > 0;
    }

    bool TryGetName(ClientType client, Direction direction, short id, out string name)
    {
        if (client is not ClientType.Flash)
        {
            name = "";
            return false;
        }
        if (Volatile.Read(ref _session_catalog) is { } session)
        {
            if (session.Binding.Client == client)
            {
                if (session.Binding.Catalog is { } session_catalog)
                    return session_catalog.TryGetName(direction, id, out name);
                name = "";
                return false;
            }
        }
        if (TryGetBoundVersionedCatalog(client, out MessageCatalog? bound, out _))
            return bound.TryGetName(direction, id, out name!);
        if (Volatile.Read(ref _catalog) is { } catalog)
        {
            if (catalog.TryGetName(direction, id, out name))
                return true;
            if (TryGetCoveredFallbackName(Volatile.Read(ref _fallback_catalog), client, catalog, direction, id, out name) ||
                TryGetCoveredFallbackName(Volatile.Read(ref _default_versioned_catalog), client, catalog, direction, id, out name))
                return true;
            return false;
        }
        if (TryGetUsableVersionedCatalog(client, out MessageCatalog? versioned) &&
            versioned.TryGetName(direction, id, out name))
            return true;
        if (Volatile.Read(ref _fallback_catalog) is { } stable &&
            stable.TryGetName(direction, id, out name))
            return true;
        name = "";
        return false;
    }

    bool TryGetCompatibleSchemas(
        ClientType client,
        MessageCatalog fallback,
        Header header,
        string live_name,
        out IReadOnlyList<OutgoingMessageSchema> schemas)
    {
        schemas = [];
        if (fallback.TryGetName(header.Direction, header.Value, out string fallback_name) &&
            !NamesMatch(client, header.Direction, live_name, fallback_name))
            return false;
        return fallback.TryGetOutgoingSchemas(header.Value, out schemas);
    }

    void AppendCompatibleFallbackIds(
        MessageCatalog? fallback,
        ClientType client,
        Direction direction,
        string name,
        MessageCatalog live,
        List<short> values)
    {
        if (fallback is null)
            return;
        if (!fallback.TryGetIds(direction, name, out IReadOnlyList<short> fallback_ids))
            return;
        bool exact_catalog = IsExactFallback(fallback, live);
        bool compatible_reference = IsCompatibleReference(client, fallback, live);
        foreach (short fallback_id in fallback_ids)
        {
            if (!live.TryGetName(direction, fallback_id, out string live_name))
            {
                if ((exact_catalog || compatible_reference) && !values.Contains(fallback_id))
                    values.Add(fallback_id);
                continue;
            }
            if (values.Contains(fallback_id))
                continue;
            if (!NamesMatch(client, direction, name, live_name) &&
                !CatalogBindsName(fallback, direction, fallback_id, live_name) &&
                (!fallback.TryGetName(direction, fallback_id, out string fallback_name) ||
                 !NamesMatch(client, direction, fallback_name, live_name)))
                continue;
            values.Add(fallback_id);
        }
    }

    static bool CatalogBindsName(
        MessageCatalog catalog,
        Direction direction,
        short id,
        string name) =>
        catalog.TryGetIds(direction, name, out IReadOnlyList<short> ids) && ids.Contains(id);

    static bool TryGetCoveredFallbackName(
        MessageCatalog? fallback,
        ClientType client,
        MessageCatalog live,
        Direction direction,
        short id,
        out string name)
    {
        if (fallback is not null &&
            (IsExactFallback(fallback, live) || IsCompatibleReference(client, fallback, live)) &&
            fallback.TryGetName(direction, id, out name))
            return true;
        name = "";
        return false;
    }

    static bool TryGetCoveredFallbackSchemas(
        MessageCatalog? fallback,
        ClientType client,
        MessageCatalog live,
        Header header,
        out IReadOnlyList<OutgoingMessageSchema> schemas)
    {
        if (fallback is not null &&
            !live.TryGetName(header.Direction, header.Value, out _) &&
            IsExactFallback(fallback, live) &&
            fallback.TryGetOutgoingSchemas(header.Value, out schemas))
            return true;
        schemas = [];
        return false;
    }

    static bool IsExactFallback(MessageCatalog fallback, MessageCatalog live) =>
        live.HeaderCount >= 64 &&
        fallback.CoversHeaders(live) &&
        fallback.MatchingHeaders(live) * 4 >= live.HeaderCount * 3;

    static bool IsCompatibleReference(
        ClientType client,
        MessageCatalog fallback,
        MessageCatalog live)
    {
        if (client is not (ProtocolClients.Flash) ||
            fallback.BuildFingerprint is not null ||
            live.HeaderCount < 64 ||
            fallback.HeaderCount < 64)
        {
            return false;
        }

        int matching = fallback.MatchingHeaders(live);
        return matching * CompatibleReferenceParts >=
                live.HeaderCount * CompatibleReferenceRequiredParts &&
            matching * CompatibleReferenceParts >=
                fallback.HeaderCount * CompatibleReferenceRequiredParts;
    }

    static void AppendStandaloneFallbackIds(
        MessageCatalog? fallback,
        ClientType client,
        Direction direction,
        string name,
        List<short> values)
    {
        if (fallback is null ||
            !fallback.TryGetIds(direction, name, out IReadOnlyList<short> fallback_ids))
            return;
        foreach (short fallback_id in fallback_ids)
            if (!values.Contains(fallback_id))
                values.Add(fallback_id);
    }

    static void AppendStandaloneIds(
        MessageCatalog catalog,
        Direction direction,
        string name,
        List<short> values)
    {
        if (!catalog.TryGetIds(direction, name, out IReadOnlyList<short> ids))
            return;
        foreach (short id in ids)
            if (!values.Contains(id))
                values.Add(id);
    }

    bool TryGetBoundVersionedCatalog(
        ClientType client,
        out MessageCatalog catalog,
        out ClientBuildIdentity identity)
    {
        catalog = null!;
        identity = null!;
        if (client is not ClientType.Flash || Volatile.Read(ref _catalog_build) is not { } found_identity)
        {
            return false;
        }
        MessageCatalog? found_catalog = null;
        if (found_identity.SchemaFingerprint is { } schema_fingerprint)
        {
            _versioned_catalogs.TryGetValue(
                (found_identity.CatalogFingerprint, schema_fingerprint),
                out found_catalog);
        }
        if (found_catalog is null)
        {
            _versioned_catalogs.TryGetValue(
                (found_identity.CatalogFingerprint, ""),
                out found_catalog);
        }
        if (found_catalog is null)
            return false;
        catalog = found_catalog;
        identity = found_identity;
        return true;
    }

    bool TryGetUsableVersionedCatalog(ClientType client, out MessageCatalog catalog)
    {
        if (TryGetBoundVersionedCatalog(client, out catalog, out _))
            return true;
        if (Volatile.Read(ref _default_versioned_catalog) is not { } fallback)
        {
            catalog = null!;
            return false;
        }
        if (ActiveClient == client && fallback.BuildFingerprint is not null)
        {
            catalog = null!;
            return false;
        }
        catalog = fallback;
        return true;
    }

    bool TryGetSessionMetadataCatalog(
        SessionCatalogBinding binding,
        out MessageCatalog catalog)
    {
        catalog = null!;
        if (binding.Provenance.Origin != CatalogOrigin.ClientExtraction ||
            binding.Provenance.SourceSha256 is not { } source_fingerprint ||
            binding.Catalog is not { } session_catalog ||
            !session_catalog.MatchesBuildFingerprint(source_fingerprint) ||
            Volatile.Read(ref _catalog_build) is not { } identity ||
            identity.SchemaFingerprint is null ||
            !identity.CatalogFingerprint.Equals(source_fingerprint, StringComparison.OrdinalIgnoreCase) ||
            !TryGetBoundVersionedCatalog(binding.Client, out MessageCatalog? candidate, out _) ||
            !candidate.MatchesBuildFingerprint(source_fingerprint) ||
            !candidate.MatchesSchemaFingerprint(identity.SchemaFingerprint) ||
            !session_catalog.HasExactHeaders(candidate))
        {
            return false;
        }
        catalog = candidate;
        return true;
    }

    bool NamesMatch(ClientType client, Direction direction, string first, string second)
    {
        if (first.Equals(second, StringComparison.OrdinalIgnoreCase))
            return true;
        return _map.AreEquivalent(client, direction, first, second);
    }

    IReadOnlyList<string> ResolveNames(Identifier identifier, ClientType target)
    {
        if (target is not ClientType.Flash ||
            identifier.Client is not (ClientType.None or ClientType.Flash))
            return [];
        IReadOnlyList<string> equivalents =
            _map.EquivalentNames(target, identifier.Direction, identifier.Name);
        return equivalents.Count == 0 ? [identifier.Name] : equivalents;
    }

    static bool IsSupportedClient(ClientType client) =>
        ClientTypes.IsSupported(client);

    static void RequireSupportedClient(ClientType client)
    {
        if (!IsSupportedClient(client))
            throw new ArgumentOutOfRangeException(nameof(client), client, "Only Flash catalogs are supported.");
    }

    sealed record SessionCatalogState(long Generation, SessionCatalogBinding Binding);
}
