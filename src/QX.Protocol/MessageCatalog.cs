using Qx;
using Qx.Messages;

namespace Qx.Protocol;

/// <summary>Represents one header of a message catalog.</summary>
/// <param name="Direction">The direction of the message.</param>
/// <param name="Id">The header ID, from 0 to 65535.</param>
/// <param name="Name">The primary name of the header.</param>
public sealed record MessageCatalogHeader(Direction Direction, int Id, string Name);

/// <summary>Represents the thread-safe map between message names and header IDs of one client build.</summary>
/// <remarks>
/// A name can map to several header IDs and a header can have several names, one of which is its
/// primary name. Names are matched without regard to case. A <see cref="Snapshot"/> is read-only, and
/// its setters and <c>Add</c> methods throw <see cref="InvalidOperationException"/>.
/// </remarks>
public sealed class MessageCatalog
{
    private readonly object _sync = new();
    private readonly Dictionary<(Direction, string), List<short>> _forward = new();
    private readonly Dictionary<(Direction, string), IReadOnlyList<short>> _forward_views = new();
    private readonly Dictionary<(Direction, short), string> _reverse = new();
    private readonly Dictionary<short, List<OutgoingMessageSchema>> _outgoing_schemas = new();
    private readonly Dictionary<short, IReadOnlyList<OutgoingMessageSchema>> _outgoing_schema_views = new();
    private bool _read_only;

    /// <summary>Gets the number of distinct message names, counted per direction.</summary>
    public int Count
    {
        get
        {
            lock (_sync)
                return _forward.Count;
        }
    }

    /// <summary>Gets the number of headers across both directions.</summary>
    public int HeaderCount
    {
        get
        {
            lock (_sync)
                return _reverse.Count;
        }
    }

    /// <summary>Gets every header with its primary name, ordered by direction and then by ID.</summary>
    public IReadOnlyList<MessageCatalogHeader> Headers
    {
        get
        {
            lock (_sync)
            {
                return Array.AsReadOnly(_reverse
                    .Select(entry => new MessageCatalogHeader(
                        entry.Key.Item1,
                        unchecked((ushort)entry.Key.Item2),
                        entry.Value))
                    .OrderBy(entry => entry.Direction)
                    .ThenBy(entry => entry.Id)
                    .ToArray());
            }
        }
    }

    /// <summary>Gets the build fingerprint in upper case, such as the SHA-256 hash of the client source, or <see langword="null"/> when not set.</summary>
    public string? BuildFingerprint
    {
        get
        {
            lock (_sync)
                return _build_fingerprint;
        }
        private set => _build_fingerprint = value;
    }

    /// <summary>Gets the fingerprint of the outgoing message schemas in upper case, or <see langword="null"/> when not set.</summary>
    public string? SchemaFingerprint
    {
        get
        {
            lock (_sync)
                return _schema_fingerprint;
        }
        private set => _schema_fingerprint = value;
    }

    /// <summary>Gets the wire profile of the client build, which is not analyzed until <see cref="SetWireProfile(MessageWireProfile)"/> is called.</summary>
    public MessageWireProfile WireProfile
    {
        get
        {
            lock (_sync)
                return _wire_profile;
        }
        private set => _wire_profile = value;
    }

    /// <summary>Gets whether the catalog is a read-only snapshot.</summary>
    public bool IsReadOnly
    {
        get
        {
            lock (_sync)
                return _read_only;
        }
    }

    private string? _build_fingerprint;
    private string? _schema_fingerprint;
    private MessageWireProfile _wire_profile;

    /// <summary>Creates a writable catalog from a JSON message list.</summary>
    /// <param name="json">The message list.</param>
    /// <returns>The new catalog.</returns>
    public static MessageCatalog FromJson(MessagesJson json)
    {
        var catalog = new MessageCatalog();
        foreach (MessageEntry entry in json.Incoming)
            catalog.Add(Direction.In, entry.Id, entry.Name);
        foreach (MessageEntry entry in json.Outgoing)
            catalog.Add(Direction.Out, entry.Id, entry.Name);
        return catalog;
    }

    /// <summary>Sets the build fingerprint, trimmed and in upper case.</summary>
    /// <param name="fingerprint">The build fingerprint.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fingerprint"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the catalog is read-only.</exception>
    public void SetBuildFingerprint(string fingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        lock (_sync)
        {
            RequireMutable();
            BuildFingerprint = fingerprint.Trim().ToUpperInvariant();
        }
    }

    /// <summary>Gets whether the build fingerprint is set and equals a value, ignoring case.</summary>
    /// <param name="fingerprint">The fingerprint to compare with.</param>
    public bool MatchesBuildFingerprint(string fingerprint)
    {
        lock (_sync)
        {
            return BuildFingerprint is not null &&
                BuildFingerprint.Equals(fingerprint, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Sets the fingerprint of the outgoing message schemas, trimmed and in upper case.</summary>
    /// <param name="fingerprint">The schema fingerprint.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fingerprint"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the catalog is read-only.</exception>
    public void SetSchemaFingerprint(string fingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        lock (_sync)
        {
            RequireMutable();
            SchemaFingerprint = fingerprint.Trim().ToUpperInvariant();
        }
    }

    /// <summary>Sets the wire profile of the client build.</summary>
    /// <param name="profile">The wire profile, which must be analyzed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="profile"/> is not analyzed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the catalog is read-only.</exception>
    public void SetWireProfile(MessageWireProfile profile)
    {
        if (!profile.IsAnalyzed)
            throw new ArgumentException("The message wire profile was not analyzed.", nameof(profile));
        lock (_sync)
        {
            RequireMutable();
            WireProfile = profile;
        }
    }

    /// <summary>Gets whether the schema fingerprint is set and equals a value, ignoring case.</summary>
    /// <param name="fingerprint">The fingerprint to compare with, or <see langword="null"/>, which never matches.</param>
    public bool MatchesSchemaFingerprint(string? fingerprint)
    {
        lock (_sync)
        {
            return SchemaFingerprint is not null &&
                fingerprint is not null &&
                SchemaFingerprint.Equals(fingerprint, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Maps a name to a header ID and makes it the primary name of that header.</summary>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="id">The header ID, stored as a 16-bit value.</param>
    /// <param name="name">The message name.</param>
    /// <exception cref="InvalidOperationException">Thrown when the catalog is read-only.</exception>
    public void Add(Direction direction, int id, string name)
    {
        lock (_sync)
        {
            RequireMutable();
            short value = (short)id;
            AddForward(direction, name, value);
            _reverse[(direction, value)] = name;
        }
    }

    /// <summary>Maps an additional name to a header ID, making it the primary name only when the header has none.</summary>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="id">The header ID, stored as a 16-bit value.</param>
    /// <param name="name">The message name.</param>
    /// <exception cref="InvalidOperationException">Thrown when the catalog is read-only.</exception>
    public void AddAlias(Direction direction, int id, string name)
    {
        lock (_sync)
        {
            RequireMutable();
            short value = (short)id;
            AddForward(direction, name, value);
            _reverse.TryAdd((direction, value), name);
        }
    }

    /// <summary>Tries to get the header ID of a message name.</summary>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="name">The message name, matched without regard to case.</param>
    /// <param name="id">The header ID that was mapped last, or 0 when the name is not mapped.</param>
    /// <returns><see langword="true"/> if the name is mapped; otherwise, <see langword="false"/>.</returns>
    public bool TryGetId(Direction direction, string name, out short id)
    {
        if (TryGetIds(direction, name, out IReadOnlyList<short> ids))
        {
            id = ids[^1];
            return true;
        }
        id = default;
        return false;
    }

    /// <summary>Tries to get every header ID mapped to a message name.</summary>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="name">The message name, matched without regard to case.</param>
    /// <param name="ids">The header IDs in the order they were mapped, or an empty list when the name is not mapped.</param>
    /// <returns><see langword="true"/> if the name is mapped; otherwise, <see langword="false"/>.</returns>
    public bool TryGetIds(Direction direction, string name, out IReadOnlyList<short> ids)
    {
        lock (_sync)
        {
            if (_forward_views.TryGetValue((direction, name.ToUpperInvariant()), out IReadOnlyList<short>? values) && values.Count > 0)
            {
                ids = values;
                return true;
            }
            ids = [];
            return false;
        }
    }

    /// <summary>Tries to get the primary name of a header.</summary>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="id">The header ID.</param>
    /// <param name="name">The primary name, or <see langword="null"/> when the header is not known.</param>
    /// <returns><see langword="true"/> if the header is known; otherwise, <see langword="false"/>.</returns>
    public bool TryGetName(Direction direction, short id, out string name)
    {
        lock (_sync)
            return _reverse.TryGetValue((direction, id), out name!);
    }

    /// <summary>Gets whether another catalog has headers and this catalog contains every one of them by direction and ID.</summary>
    /// <param name="catalog">The other catalog.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is <see langword="null"/>.</exception>
    public bool CoversHeaders(MessageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        (Direction, short)[] keys;
        lock (catalog._sync)
            keys = catalog._reverse.Keys.ToArray();
        lock (_sync)
            return keys.Length > 0 && keys.All(_reverse.ContainsKey);
    }

    /// <summary>Counts the headers of another catalog whose primary name maps to the same header ID in this catalog.</summary>
    /// <param name="catalog">The other catalog.</param>
    /// <returns>The number of matching headers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is <see langword="null"/>.</exception>
    public int MatchingHeaders(MessageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        KeyValuePair<(Direction, short), string>[] entries;
        lock (catalog._sync)
            entries = catalog._reverse.ToArray();
        return entries.Count(entry =>
            TryGetIds(entry.Key.Item1, entry.Value, out IReadOnlyList<short> ids) &&
            ids.Contains(entry.Key.Item2));
    }

    /// <summary>Gets whether both catalogs have the same non-empty set of headers and each maps every header name of the other to the same ID.</summary>
    /// <param name="catalog">The other catalog.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="catalog"/> is <see langword="null"/>.</exception>
    public bool HasExactHeaders(MessageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return HeaderCount > 0 &&
            HeaderCount == catalog.HeaderCount &&
            CoversHeaders(catalog) &&
            catalog.CoversHeaders(this) &&
            MatchingHeaders(catalog) == catalog.HeaderCount &&
            catalog.MatchingHeaders(this) == HeaderCount;
    }

    /// <summary>Adds an outgoing message schema to a header ID.</summary>
    /// <param name="id">The outgoing header ID, stored as a 16-bit value.</param>
    /// <param name="schema">The schema to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schema"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the catalog is read-only.</exception>
    public void AddOutgoingSchema(int id, OutgoingMessageSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        lock (_sync)
        {
            RequireMutable();
            short value = unchecked((short)id);
            if (!_outgoing_schemas.TryGetValue(value, out List<OutgoingMessageSchema>? schemas))
            {
                schemas = [];
                _outgoing_schemas.Add(value, schemas);
                _outgoing_schema_views.Add(value, schemas.AsReadOnly());
            }
            schemas.Add(schema);
        }
    }

    /// <summary>Tries to get the outgoing message schemas of a header ID.</summary>
    /// <param name="id">The outgoing header ID.</param>
    /// <param name="schemas">The schemas in the order they were added, or an empty list when there are none.</param>
    /// <returns><see langword="true"/> if the header ID has at least one schema; otherwise, <see langword="false"/>.</returns>
    public bool TryGetOutgoingSchemas(short id, out IReadOnlyList<OutgoingMessageSchema> schemas)
    {
        lock (_sync)
        {
            if (_outgoing_schema_views.TryGetValue(id, out IReadOnlyList<OutgoingMessageSchema>? values) && values.Count > 0)
            {
                schemas = values;
                return true;
            }
            schemas = [];
            return false;
        }
    }

    private void AddForward(Direction direction, string name, short value)
    {
        var key = (direction, name.ToUpperInvariant());
        if (!_forward.TryGetValue(key, out List<short>? values))
        {
            values = [];
            _forward.Add(key, values);
            _forward_views.Add(key, values.AsReadOnly());
        }
        if (!values.Contains(value))
            values.Add(value);
    }

    /// <summary>Creates a read-only copy of the catalog, including its fingerprints, wire profile and outgoing schemas.</summary>
    /// <returns>The read-only copy.</returns>
    public MessageCatalog Snapshot()
    {
        lock (_sync)
        {
            var snapshot = new MessageCatalog();
            foreach (((Direction direction, string name), List<short> values) in _forward)
            {
                var copied = new List<short>(values);
                var key = (direction, name);
                snapshot._forward.Add(key, copied);
                snapshot._forward_views.Add(key, copied.AsReadOnly());
            }
            foreach (((Direction direction, short id), string name) in _reverse)
                snapshot._reverse.Add((direction, id), name);
            foreach ((short id, List<OutgoingMessageSchema> schemas) in _outgoing_schemas)
            {
                List<OutgoingMessageSchema> copied = schemas.Select(Copy).ToList();
                snapshot._outgoing_schemas.Add(id, copied);
                snapshot._outgoing_schema_views.Add(id, copied.AsReadOnly());
            }
            snapshot.BuildFingerprint = BuildFingerprint;
            snapshot.SchemaFingerprint = SchemaFingerprint;
            snapshot.WireProfile = WireProfile;
            snapshot._read_only = true;
            return snapshot;
        }
    }

    static OutgoingMessageSchema Copy(OutgoingMessageSchema schema) => new(
        schema.SourceName,
        Array.AsReadOnly(schema.Parameters.Select(parameter => parameter with
        {
            ElementWireTypes = parameter.ElementWireTypes is null
                    ? null
                    : Array.AsReadOnly(parameter.ElementWireTypes.ToArray())
        })
            .ToArray()));

    void RequireMutable()
    {
        if (_read_only)
            throw new InvalidOperationException("The message catalog is read-only.");
    }
}
