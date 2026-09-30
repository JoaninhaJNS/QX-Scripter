namespace Qx.Interception.GEarth;

/// <summary>Provides conversion from G-Earth client type names to <see cref="ClientType"/>.</summary>
/// <remarks>Only <c>FLASH</c> is supported, matched case-insensitively.</remarks>
public static class HClientType
{
    /// <summary>Gets the client type for a G-Earth client type name.</summary>
    /// <param name="name">The client type name that G-Earth reports.</param>
    /// <returns>The matching client type.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is not a supported client type.</exception>
    public static ClientType FromName(string? name)
    {
        if (!TryFromName(name, out ClientType client))
            throw new ArgumentException("The G-Earth client type is not supported.", nameof(name));
        return client;
    }

    /// <summary>Tries to get the client type for a G-Earth client type name.</summary>
    /// <param name="name">The client type name that G-Earth reports.</param>
    /// <param name="client">The matching client type, or <see cref="ClientType.None"/> when there is none.</param>
    /// <returns><see langword="true"/> when the name is a supported client type.</returns>
    public static bool TryFromName(string? name, out ClientType client)
    {
        if (string.Equals(name, "FLASH", StringComparison.OrdinalIgnoreCase))
        {
            client = ClientType.Flash;
            return true;
        }
        client = ClientType.None;
        return false;
    }
}
