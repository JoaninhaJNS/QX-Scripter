namespace Qx.Messages;

/// <summary>Represents a message identifier, made of a client type, a direction and a message name.</summary>
/// <remarks>Identifiers compare message names without regard to case.</remarks>
/// <param name="Client">The client type the name belongs to, or <see cref="ClientType.None"/> when it is not tied to a client.</param>
/// <param name="Direction">The direction of the message.</param>
/// <param name="Name">The message name.</param>
public readonly record struct Identifier(ClientType Client, Direction Direction, string Name)
{
    /// <summary>An unknown identifier, with no client type, no direction and an empty name.</summary>
    public static readonly Identifier Unknown = new();

    /// <summary>Initializes a new identifier with no client type, no direction and an empty name.</summary>
    public Identifier()
        : this(ClientType.None, Direction.None, "")
    { }

    /// <summary>Returns a hash code that ignores the case of the name.</summary>
    public override int GetHashCode() => (Client, Direction, Name.ToUpperInvariant()).GetHashCode();

    /// <summary>Gets whether this identifier has the same client type, direction and name as another, ignoring the case of the name.</summary>
    /// <param name="other">The identifier to compare with.</param>
    public bool Equals(Identifier other) =>
        Client == other.Client &&
        Direction == other.Direction &&
        string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);

    /// <summary>Returns the name with a client prefix and, optionally, a direction prefix.</summary>
    /// <param name="includeDirection">Whether to prefix <c>in:</c> or <c>out:</c> for the direction.</param>
    /// <returns>The formatted identifier, for example <c>in:flash:Chat</c>. Flash identifiers get the <c>flash:</c> prefix.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="includeDirection"/> is <see langword="true"/> and the direction is <see cref="Qx.Direction.Both"/>.</exception>
    /// <exception cref="UnsupportedClientException">Thrown when the client type is not <see cref="ClientType.None"/> or <see cref="ClientType.Flash"/>.</exception>
    public string ToString(bool includeDirection)
    {
        string result = "";
        if (includeDirection)
            result += Direction switch
            {
                Direction.None => "",
                Direction.In => "in:",
                Direction.Out => "out:",
                _ => throw new ArgumentOutOfRangeException(nameof(Direction))
            };
        result += Client switch
        {
            ClientType.None => "",
            ClientType.Flash => "flash:",
            _ => throw new UnsupportedClientException(Client)
        };
        return result + Name;
    }

    /// <summary>Returns the name with a client prefix and no direction prefix.</summary>
    public override string ToString() => ToString(false);

    /// <summary>Converts a direction and name tuple to an identifier with no client type.</summary>
    /// <param name="x">The direction and the message name.</param>
    public static implicit operator Identifier((Direction direction, string name) x) => new(ClientType.None, x.direction, x.name);
    /// <summary>Converts a client type, direction and name tuple to an identifier.</summary>
    /// <param name="x">The client type, the direction and the message name.</param>
    public static implicit operator Identifier((ClientType client, Direction direction, string name) x) => new(x.client, x.direction, x.name);

    /// <summary>Converts an identifier to a read-only span that contains only that identifier.</summary>
    /// <param name="identifier">The identifier to wrap.</param>
    public static implicit operator ReadOnlySpan<Identifier>(in Identifier identifier) => new(in identifier);
}
