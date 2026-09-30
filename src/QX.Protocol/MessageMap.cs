using Qx;

namespace Qx.Protocol;

/// <summary>Represents a lookup from Flash message names to entries that group the equivalent names of each registered message.</summary>
public sealed class MessageMap
{
    private readonly Dictionary<(Direction, string), MessageMapEntry> _by_name = [];

    internal MessageMap(MessageRegistry registry)
    {
        Registry = registry;
        foreach (MessageDescriptor descriptor in registry.Descriptors)
        {
            var entry = new MessageMapEntry();
            foreach (MessageAlias alias in descriptor.Aliases)
                entry.Set(alias.Client, alias.Name);
            AddEntry(descriptor.Direction, entry);
        }
    }

    /// <summary>Gets the message registry the map was built from.</summary>
    public MessageRegistry Registry { get; }

    private void AddEntry(Direction direction, MessageMapEntry entry)
    {
        foreach (string name in entry.NamesFor(ClientType.Flash))
        {
            var key = (direction, name.ToUpperInvariant());
            if (!_by_name.TryAdd(key, entry))
            {
                throw new InvalidDataException(
                    $"Alias '{name}' for Flash {direction} is assigned to multiple message entries.");
            }
        }
    }

    /// <summary>Tries to get the entry that contains a message name.</summary>
    /// <param name="client">The client type, which must be <see cref="ClientType.Flash"/> to find an entry.</param>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="name">The message name, matched without regard to case.</param>
    /// <param name="entry">The entry, or <see langword="null"/> when none is found.</param>
    /// <returns><see langword="true"/> if an entry was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetEntry(ClientType client, Direction direction, string name, out MessageMapEntry entry)
    {
        entry = null!;
        return client is ClientType.Flash &&
            _by_name.TryGetValue((direction, name.ToUpperInvariant()), out entry!);
    }

    /// <summary>Gets every name of the entry that contains a message name, including that name.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="direction">The direction of the message.</param>
    /// <param name="name">The message name, matched without regard to case.</param>
    /// <returns>The names of the entry, or an empty list when the name is not mapped.</returns>
    public IReadOnlyList<string> EquivalentNames(ClientType client, Direction direction, string name) =>
        TryGetEntry(client, direction, name, out MessageMapEntry entry)
            ? entry.NamesFor(client)
            : [];

    /// <summary>Gets whether two message names belong to the same entry.</summary>
    /// <param name="client">The client type.</param>
    /// <param name="direction">The direction of the messages.</param>
    /// <param name="first">The first message name.</param>
    /// <param name="second">The second message name.</param>
    public bool AreEquivalent(ClientType client, Direction direction, string first, string second) =>
        TryGetEntry(client, direction, first, out MessageMapEntry first_entry) &&
        TryGetEntry(client, direction, second, out MessageMapEntry second_entry) &&
        ReferenceEquals(first_entry, second_entry);

    /// <summary>Gets the number of mapped names across both directions.</summary>
    public int Count => _by_name.Count;
}
