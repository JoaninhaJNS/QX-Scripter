using Qx;

namespace Qx.Interception;

/// <summary>Represents one hotel connection reported by the interceptor.</summary>
/// <remarks>A new instance is created for every connection, so reference equality identifies a session.</remarks>
public sealed record Session
{
    private ClientType _client;

    /// <summary>Initializes a new instance of the <see cref="Session"/> record.</summary>
    /// <param name="host">The hotel server host.</param>
    /// <param name="port">The hotel server port.</param>
    /// <param name="hotel_version">The client build version reported for the connection.</param>
    /// <param name="client_identifier">The client identifier reported for the connection.</param>
    /// <param name="client">The client type.</param>
    /// <exception cref="UnsupportedClientException">Thrown when <paramref name="client"/> is not a supported client.</exception>
    public Session(
        string host,
        int port,
        string hotel_version,
        string client_identifier,
        ClientType client)
    {
        Host = host;
        Port = port;
        HotelVersion = hotel_version;
        ClientIdentifier = client_identifier;
        Client = client;
    }

    /// <summary>Gets the hotel server host.</summary>
    public string Host { get; init; }
    /// <summary>Gets the hotel server port.</summary>
    public int Port { get; init; }
    /// <summary>Gets the client build version reported for the connection.</summary>
    public string HotelVersion { get; init; }
    /// <summary>Gets the client identifier reported for the connection.</summary>
    public string ClientIdentifier { get; init; }
    /// <summary>Gets the client type.</summary>
    /// <exception cref="UnsupportedClientException">Thrown on init when the value is not a supported client.</exception>
    public ClientType Client
    {
        get => _client;
        init
        {
            if (!ClientTypes.IsSupported(value))
                throw new UnsupportedClientException(value);
            _client = value;
        }
    }

    /// <summary>Deconstructs the session into its values.</summary>
    /// <param name="host">The hotel server host.</param>
    /// <param name="port">The hotel server port.</param>
    /// <param name="hotel_version">The client build version.</param>
    /// <param name="client_identifier">The client identifier.</param>
    /// <param name="client">The client type.</param>
    public void Deconstruct(
        out string host,
        out int port,
        out string hotel_version,
        out string client_identifier,
        out ClientType client)
    {
        host = Host;
        port = Port;
        hotel_version = HotelVersion;
        client_identifier = ClientIdentifier;
        client = Client;
    }
}
