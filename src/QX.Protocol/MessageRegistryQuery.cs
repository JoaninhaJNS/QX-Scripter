namespace Qx.Protocol;

/// <summary>Provides a filtered, paged view of the message registry and how its messages resolve in the active session.</summary>
public static class MessageRegistryQuery
{
    /// <summary>Reads a page of registered messages, plus the session catalog headers that no registered message maps to.</summary>
    /// <remarks>
    /// Entries are ordered by direction and then by key. Headers of the session catalog that no registered
    /// message resolves to are listed with keys such as <c>unmapped.in.1234</c> unless
    /// <paramref name="explicit_only"/> is set.
    /// </remarks>
    /// <param name="messages">The message manager to read from.</param>
    /// <param name="query">Text to find in message keys and names, or in the names and IDs of unmapped headers, without regard to case. Empty matches everything.</param>
    /// <param name="direction"><c>in</c> or <c>incoming</c>, <c>out</c> or <c>outgoing</c>, or <c>both</c>, <c>all</c> or empty for both directions.</param>
    /// <param name="client"><c>flash</c>, or <c>all</c> or empty for every client.</param>
    /// <param name="explicit_only">Whether to include only messages with a declared stable key.</param>
    /// <param name="resolved_only">Whether to include only registered messages that resolve to a header in the session catalog.</param>
    /// <param name="limit">The maximum number of entries to return, from 1 to 500.</param>
    /// <param name="offset">The number of matching entries to skip.</param>
    /// <returns>The registry totals, the session catalog state, the applied filters, the number of matches and the requested page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messages"/>, <paramref name="query"/>, <paramref name="direction"/> or <paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="limit"/> is outside 1 to 500 or <paramref name="offset"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> or <paramref name="client"/> is not a recognized value.</exception>
    public static MessageRegistrySnapshot Read(
        MessageManager messages,
        string query,
        string direction,
        string client,
        bool explicit_only,
        bool resolved_only,
        int limit,
        int offset)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(direction);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 500);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        Direction? direction_filter = ParseDirection(direction);
        ClientType? client_filter = ParseClient(client);
        string search = query.Trim();
        SessionCatalogBinding? binding = messages.ActiveCatalogBinding;
        ClientType active_client = binding?.Client ?? messages.ActiveClient;

        IEnumerable<MessageDescriptor> filtered = messages.Registry.Descriptors;
        if (explicit_only)
            filtered = filtered.Where(descriptor => descriptor.HasExplicitKey);
        if (direction_filter is { } selected_direction)
            filtered = filtered.Where(descriptor => descriptor.Direction == selected_direction);
        if (client_filter is { } selected_client)
            filtered = filtered.Where(descriptor => descriptor.NamesFor(selected_client).Count != 0);
        if (search.Length != 0)
        {
            filtered = filtered.Where(descriptor =>
                descriptor.Key.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                descriptor.Aliases.Any(alias => alias.Name.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }
        MessageProjection[] registered = filtered
            .Select(descriptor => Registered(
                descriptor,
                ActiveBinding(binding, descriptor, active_client)))
            .Where(projection => !resolved_only || projection.Entry.Active.Resolved)
            .ToArray();
        MessageProjection[] unmapped = explicit_only
            ? []
            : Unmapped(
                messages.Registry,
                binding,
                active_client,
                direction_filter,
                client_filter,
                search);
        MessageProjection[] matched = registered
            .Concat(unmapped)
            .OrderBy(projection => projection.Direction)
            .ThenBy(projection => projection.Entry.Key, StringComparer.Ordinal)
            .ToArray();
        MessageRegistryEntry[] entries = matched
            .Skip(offset)
            .Take(limit)
            .Select(projection => projection.Entry)
            .ToArray();

        return new MessageRegistrySnapshot(
            "protocol_messages",
            new MessageRegistrySummary(
                messages.Registry.Count,
                messages.Registry.AliasCount,
                messages.Registry.Descriptors.Count(descriptor => descriptor.HasExplicitKey)),
            new MessageRegistrySession(
                ClientName(active_client),
                binding?.Catalog is not null,
                binding?.Provenance.Origin.ToString(),
                binding?.Provenance.Source,
                binding?.Provenance.ClientVersion,
                binding?.Catalog?.HeaderCount ?? 0,
                binding?.Provenance.SourceSha256,
                binding?.Build?.CatalogFingerprint ?? binding?.Catalog?.BuildFingerprint,
                binding?.Build?.SchemaFingerprint ?? binding?.Catalog?.SchemaFingerprint),
            new MessageRegistryFilters(
                search,
                direction_filter is null ? "both" : DirectionName(direction_filter.Value),
                client_filter is null ? "all" : ClientName(client_filter.Value),
                explicit_only,
                resolved_only),
            matched.Length,
            offset,
            limit,
            entries);
    }

    private static MessageProjection Registered(
        MessageDescriptor descriptor,
        MessageRegistryActiveBinding active)
    {
        return new MessageProjection(
            descriptor.Direction,
            new MessageRegistryEntry(
                descriptor.Key.Value,
                DirectionName(descriptor.Direction),
                descriptor.HasExplicitKey,
                descriptor.HasExplicitKey ? "semantic" : "legacy",
                new MessageRegistryDialects(
                    ProtocolDialect(descriptor, ProtocolClients.Flash)),
                active));
    }

    private static MessageProjection[] Unmapped(
        MessageRegistry registry,
        SessionCatalogBinding? binding,
        ClientType active_client,
        Direction? direction_filter,
        ClientType? client_filter,
        string search)
    {
        if (active_client is not (ProtocolClients.Flash) ||
            binding?.Client != active_client ||
            binding.Catalog is not { } catalog ||
            client_filter is { } selected_client && selected_client != active_client)
        {
            return [];
        }

        var mapped = new HashSet<(Direction Direction, int Id)>();
        foreach (MessageDescriptor descriptor in registry.Descriptors)
        {
            MessageRegistryActiveBinding active = ActiveBinding(binding, descriptor, active_client);
            foreach (int header in active.Headers)
                mapped.Add((descriptor.Direction, header));
        }

        return catalog.Headers
            .Where(header => !mapped.Contains((header.Direction, header.Id)))
            .Where(header => direction_filter is null || header.Direction == direction_filter)
            .Where(header => search.Length == 0 ||
                header.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                header.Id.ToString().Contains(search, StringComparison.Ordinal))
            .Select(header => Unmapped(header, active_client))
            .ToArray();
    }

    private static MessageProjection Unmapped(
        MessageCatalogHeader header,
        ClientType active_client)
    {
        var dialect = new MessageRegistryDialect(true, header.Name, [header.Name]);
        var unavailable = new MessageRegistryDialect(false, null, []);
        int[] headers = [header.Id];
        return new MessageProjection(
            header.Direction,
            new MessageRegistryEntry(
                $"unmapped.{DirectionName(header.Direction)}.{header.Id}",
                DirectionName(header.Direction),
                false,
                "unmapped",
                active_client == ProtocolClients.Flash
                    ? new MessageRegistryDialects(dialect)
                    : new MessageRegistryDialects(unavailable),
                new MessageRegistryActiveBinding(
                    ClientName(active_client),
                    true,
                    true,
                    headers,
                    [new MessageRegistryAliasBinding(header.Name, headers)])));
    }

    private static MessageRegistryActiveBinding ActiveBinding(
        SessionCatalogBinding? binding,
        MessageDescriptor descriptor,
        ClientType active_client)
    {
        IReadOnlyList<string> aliases = active_client == ClientType.None
            ? []
            : descriptor.NamesFor(active_client);
        var evidence = new List<MessageRegistryAliasBinding>();

        foreach (string alias in aliases)
        {
            int[] headers = ResolveAliasHeaders(
                binding,
                active_client,
                descriptor.Direction,
                alias);
            if (headers.Length != 0)
                evidence.Add(new MessageRegistryAliasBinding(alias, headers));
        }

        int[] resolved_headers = evidence
            .SelectMany(item => item.Headers)
            .Distinct()
            .Order()
            .ToArray();
        return new MessageRegistryActiveBinding(
            ClientName(active_client),
            aliases.Count != 0,
            resolved_headers.Length != 0,
            resolved_headers,
            evidence);
    }

    private static int[] ResolveAliasHeaders(
        SessionCatalogBinding? binding,
        ClientType active_client,
        Direction direction,
        string alias)
    {
        if (active_client == ClientType.None ||
            binding?.Client != active_client ||
            binding.Catalog is null ||
            !binding.Catalog.TryGetIds(direction, alias, out IReadOnlyList<short> ids))
        {
            return [];
        }
        return HeaderIds(ids);
    }

    private static int[] HeaderIds(IEnumerable<short> headers) => headers
        .Select(header => (int)unchecked((ushort)header))
        .Distinct()
        .Order()
        .ToArray();

    private static MessageRegistryDialect ProtocolDialect(
        MessageDescriptor descriptor,
        ClientType client)
    {
        IReadOnlyList<string> aliases = descriptor.NamesFor(client);
        return new MessageRegistryDialect(aliases.Count != 0, descriptor.NameFor(client), aliases);
    }

    private static Direction? ParseDirection(string value) => value.Trim().ToLowerInvariant() switch
    {
        "" or "both" or "all" => null,
        "in" or "incoming" => Direction.In,
        "out" or "outgoing" => Direction.Out,
        _ => throw new ArgumentException("'direction' must be in, out, or both.", nameof(value))
    };

    private static ClientType? ParseClient(string value) => value.Trim().ToLowerInvariant() switch
    {
        "" or "all" => null,
        "flash" => ProtocolClients.Flash,
        _ => throw new ArgumentException("'client' must be flash or all.", nameof(value))
    };

    private static string DirectionName(Direction direction) => direction switch
    {
        Direction.In => "in",
        Direction.Out => "out",
        _ => "none"
    };

    private static string ClientName(ClientType client) => client switch
    {
        ProtocolClients.Flash => "flash",
        _ => "none"
    };

    private sealed record MessageProjection(
        Direction Direction,
        MessageRegistryEntry Entry);
}

/// <summary>Represents a page of message registry entries with the registry totals, session state and applied filters.</summary>
/// <param name="Query">The name of the query, which is <c>protocol_messages</c>.</param>
/// <param name="Registry">The registry totals.</param>
/// <param name="Session">The state of the session catalog.</param>
/// <param name="Filters">The filters that were applied.</param>
/// <param name="Total">The number of entries that match the filters.</param>
/// <param name="Offset">The number of matching entries that were skipped.</param>
/// <param name="Limit">The maximum number of entries in the page.</param>
/// <param name="Entries">The entries in the page.</param>
public sealed record MessageRegistrySnapshot(
    string Query,
    MessageRegistrySummary Registry,
    MessageRegistrySession Session,
    MessageRegistryFilters Filters,
    int Total,
    int Offset,
    int Limit,
    IReadOnlyList<MessageRegistryEntry> Entries);

/// <summary>Represents the totals of the message registry.</summary>
/// <param name="Descriptors">The number of registered messages.</param>
/// <param name="Aliases">The number of client names across all registered messages.</param>
/// <param name="ExplicitDescriptors">The number of registered messages with a declared stable key.</param>
public sealed record MessageRegistrySummary(
    int Descriptors,
    int Aliases,
    int ExplicitDescriptors);

/// <summary>Represents the state of the message catalog bound to the active session.</summary>
/// <param name="ActiveClient">The active client type, <c>flash</c> or <c>none</c>.</param>
/// <param name="CatalogBound">Whether a session catalog is bound and has a catalog.</param>
/// <param name="CatalogOrigin">The name of the <see cref="Qx.Protocol.CatalogOrigin"/> of the session catalog, or <see langword="null"/> when no session catalog is bound.</param>
/// <param name="CatalogSource">The source of the session catalog, or <see langword="null"/>.</param>
/// <param name="ClientVersion">The client build version of the session catalog, or <see langword="null"/>.</param>
/// <param name="CatalogHeaders">The number of headers in the session catalog, or 0 when there is none.</param>
/// <param name="SourceFingerprint">The SHA-256 hash of the catalog source, or <see langword="null"/>.</param>
/// <param name="CatalogFingerprint">The build fingerprint of the catalog, or <see langword="null"/>.</param>
/// <param name="SchemaFingerprint">The fingerprint of the outgoing message schemas, or <see langword="null"/>.</param>
public sealed record MessageRegistrySession(
    string ActiveClient,
    bool CatalogBound,
    string? CatalogOrigin,
    string? CatalogSource,
    string? ClientVersion,
    int CatalogHeaders,
    string? SourceFingerprint,
    string? CatalogFingerprint,
    string? SchemaFingerprint);

/// <summary>Represents the filters applied to a message registry query.</summary>
/// <param name="Query">The trimmed search text.</param>
/// <param name="Direction">The direction filter, <c>in</c>, <c>out</c> or <c>both</c>.</param>
/// <param name="Client">The client filter, <c>flash</c> or <c>all</c>.</param>
/// <param name="ExplicitOnly">Whether only messages with a declared stable key were included.</param>
/// <param name="ResolvedOnly">Whether only registered messages that resolve to a header were included.</param>
public sealed record MessageRegistryFilters(
    string Query,
    string Direction,
    string Client,
    bool ExplicitOnly,
    bool ResolvedOnly);

/// <summary>Represents one message in a message registry query.</summary>
/// <param name="Key">The message key, or <c>unmapped.</c> followed by the direction and header ID for a header that no registered message maps to.</param>
/// <param name="Direction">The direction, <c>in</c> or <c>out</c>.</param>
/// <param name="Stable">Whether the key is a declared stable key.</param>
/// <param name="KeyKind">The kind of key, <c>semantic</c>, <c>legacy</c> or <c>unmapped</c>.</param>
/// <param name="Clients">The names of the message per client.</param>
/// <param name="Active">How the message resolves in the active session.</param>
public sealed record MessageRegistryEntry(
    string Key,
    string Direction,
    bool Stable,
    string KeyKind,
    MessageRegistryDialects Clients,
    MessageRegistryActiveBinding Active);

/// <summary>Represents the names of a message per client.</summary>
/// <param name="Flash">The Flash names.</param>
public sealed record MessageRegistryDialects(
    MessageRegistryDialect Flash);

/// <summary>Represents the names of a message in one client.</summary>
/// <param name="Supported">Whether the message has at least one name in the client.</param>
/// <param name="PrimaryName">The primary name, or <see langword="null"/> when there is none.</param>
/// <param name="Aliases">Every name, the first being the primary name.</param>
public sealed record MessageRegistryDialect(
    bool Supported,
    string? PrimaryName,
    IReadOnlyList<string> Aliases);

/// <summary>Represents how a message resolves to headers in the active session.</summary>
/// <param name="Client">The active client type, <c>flash</c> or <c>none</c>.</param>
/// <param name="Supported">Whether the message has a name for the active client.</param>
/// <param name="Resolved">Whether at least one name resolves to a header in the session catalog.</param>
/// <param name="Headers">The distinct header IDs the message resolves to, in ascending order.</param>
/// <param name="Evidence">The names that resolved, each with its header IDs.</param>
public sealed record MessageRegistryActiveBinding(
    string Client,
    bool Supported,
    bool Resolved,
    IReadOnlyList<int> Headers,
    IReadOnlyList<MessageRegistryAliasBinding> Evidence);

/// <summary>Represents the header IDs that one message name resolves to.</summary>
/// <param name="Name">The message name.</param>
/// <param name="Headers">The header IDs, in ascending order.</param>
public sealed record MessageRegistryAliasBinding(
    string Name,
    IReadOnlyList<int> Headers);
