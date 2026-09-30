using Qx;

namespace Qx.Protocol;

/// <summary>Represents a client-specific name of a message.</summary>
/// <param name="Client">The client type the name belongs to.</param>
/// <param name="Name">The message name in that client.</param>
public readonly record struct MessageAlias(ClientType Client, string Name);

/// <summary>Represents a message declared in the message registry, with its key, direction and client names.</summary>
public sealed class MessageDescriptor
{
    private readonly IReadOnlyList<string> _names;

    /// <summary>Initializes a new instance of the <see cref="MessageDescriptor"/> class.</summary>
    /// <param name="key">The message key.</param>
    /// <param name="direction">The direction, which must be <see cref="Qx.Direction.In"/> or <see cref="Qx.Direction.Out"/>.</param>
    /// <param name="aliases">The Flash names of the message, the first being the primary name.</param>
    /// <param name="has_explicit_key">Whether the key is declared in the registry rather than generated.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty, <paramref name="aliases"/> is empty, or an alias name is blank or not trimmed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="direction"/> is not a single direction or an alias is not for Flash.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="aliases"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidDataException">Thrown when two aliases have the same name, ignoring case.</exception>
    public MessageDescriptor(
        MessageKey key,
        Direction direction,
        IEnumerable<MessageAlias> aliases,
        bool has_explicit_key)
    {
        if (key.IsEmpty)
            throw new ArgumentException("A message descriptor requires a key.", nameof(key));
        if (direction is not (Direction.In or Direction.Out))
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "A message descriptor requires one direction.");
        ArgumentNullException.ThrowIfNull(aliases);

        var names = new List<string>();
        foreach (MessageAlias alias in aliases)
        {
            if (alias.Client is not ClientType.Flash)
                throw new ArgumentOutOfRangeException(nameof(aliases), alias.Client, "A message alias requires Flash.");
            if (string.IsNullOrWhiteSpace(alias.Name) || alias.Name != alias.Name.Trim())
                throw new ArgumentException("A message alias requires a trimmed non-empty name.", nameof(aliases));
            if (names.Contains(alias.Name, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException($"Message '{key}' declares duplicate alias '{alias.Name}' for {alias.Client}.");
            names.Add(alias.Name);
        }

        if (names.Count == 0)
            throw new ArgumentException("A message descriptor requires at least one alias.", nameof(aliases));

        Key = key;
        Direction = direction;
        HasExplicitKey = has_explicit_key;
        Aliases = Array.AsReadOnly(names.Select(name => new MessageAlias(ClientType.Flash, name)).ToArray());
        _names = names.AsReadOnly();
    }

    /// <summary>Gets the message key.</summary>
    public MessageKey Key { get; }

    /// <summary>Gets the direction of the message.</summary>
    public Direction Direction { get; }

    /// <summary>Gets whether the key is declared with <c>k:</c> in the registry rather than generated as a <c>legacy.</c> key.</summary>
    public bool HasExplicitKey { get; }

    /// <summary>Gets the client names of the message, the first being the primary name.</summary>
    public IReadOnlyList<MessageAlias> Aliases { get; }

    /// <summary>Gets the primary name of the message in a client.</summary>
    /// <param name="client">The client type.</param>
    /// <returns>The primary name, or <see langword="null"/> when <paramref name="client"/> is not <see cref="ClientType.Flash"/>.</returns>
    public string? NameFor(ClientType client) =>
        client is ClientType.Flash
            ? _names[0]
            : null;

    /// <summary>Gets every name of the message in a client, the first being the primary name.</summary>
    /// <param name="client">The client type.</param>
    /// <returns>The names, or an empty list when <paramref name="client"/> is not <see cref="ClientType.Flash"/>.</returns>
    public IReadOnlyList<string> NamesFor(ClientType client) =>
        client is ClientType.Flash ? _names : [];
}
