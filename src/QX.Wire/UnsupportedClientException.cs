namespace Qx;

/// <summary>Thrown when an operation is not supported for a client type.</summary>
/// <param name="client">The client type that is not supported.</param>
public sealed class UnsupportedClientException(ClientType client)
    : Exception($"This operation is not supported for the {client} client.")
{
    /// <summary>Gets the client type that is not supported.</summary>
    public ClientType Client { get; } = client;
}
