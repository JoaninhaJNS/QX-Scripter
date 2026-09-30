namespace Qx;

/// <summary>Specifies the type of game client.</summary>
[Flags]
public enum ClientType
{
    /// <summary>No client.</summary>
    None = 0,
    /// <summary>The Flash client.</summary>
    Flash = 2,
    /// <summary>Every supported client, which is only <see cref="Flash"/>.</summary>
    All = Flash
}

/// <summary>Provides checks for <see cref="ClientType"/> values.</summary>
public static class ClientTypes
{
    /// <summary>Gets whether a client type is supported.</summary>
    /// <param name="client">The client type to check.</param>
    /// <returns><see langword="true"/> if <paramref name="client"/> is <see cref="ClientType.Flash"/>; otherwise, <see langword="false"/>.</returns>
    public static bool IsSupported(ClientType client) =>
        IsFlash(client);
    /// <summary>Gets whether a client type is <see cref="ClientType.Flash"/>.</summary>
    /// <param name="client">The client type to check.</param>
    public static bool IsFlash(ClientType client) => client is ClientType.Flash;
}
