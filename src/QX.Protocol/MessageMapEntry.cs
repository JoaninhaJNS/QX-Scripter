using Qx;

namespace Qx.Protocol;

/// <summary>Represents a group of equivalent Flash names for one message.</summary>
public sealed class MessageMapEntry
{
    private readonly List<string> _names = [];

    /// <summary>Gets or sets the primary Flash name, or <see langword="null"/> when the entry has no names.</summary>
    /// <remarks>
    /// Setting a name replaces the current primary name and removes any other copy of it, ignoring case.
    /// Setting <see langword="null"/> or a blank name removes every name.
    /// </remarks>
    public string? FlashName
    {
        get => NameFor(ProtocolClients.Flash);
        set => SetPrimary(value);
    }

    /// <summary>Gets the primary name of the entry in a client.</summary>
    /// <param name="client">The client type.</param>
    /// <returns>The primary name, or <see langword="null"/> when <paramref name="client"/> is not <see cref="ClientType.Flash"/> or the entry has no names.</returns>
    public string? NameFor(ClientType client) =>
        client is ClientType.Flash && _names.Count > 0
            ? _names[0]
            : null;

    /// <summary>Gets every name of the entry in a client, the first being the primary name.</summary>
    /// <param name="client">The client type.</param>
    /// <returns>The names, or an empty list when <paramref name="client"/> is not <see cref="ClientType.Flash"/>.</returns>
    public IReadOnlyList<string> NamesFor(ClientType client) =>
        client is ClientType.Flash ? _names : [];

    /// <summary>Adds a name to the entry unless it is blank or already present, ignoring case.</summary>
    /// <param name="client">The client type, which must be <see cref="ClientType.Flash"/>.</param>
    /// <param name="name">The name to add.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="client"/> is not <see cref="ClientType.Flash"/>.</exception>
    public void Set(ClientType client, string name)
    {
        if (client is not ClientType.Flash)
            throw new ArgumentOutOfRangeException(nameof(client), client, "A message alias requires Flash.");
        if (string.IsNullOrWhiteSpace(name))
            return;
        if (!_names.Contains(name, StringComparer.OrdinalIgnoreCase))
            _names.Add(name);
    }

    private void SetPrimary(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _names.Clear();
            return;
        }
        int duplicate = _names.FindIndex(value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (duplicate > 0)
            _names.RemoveAt(duplicate);
        if (_names.Count == 0)
            _names.Add(name);
        else
            _names[0] = name;
    }
}
