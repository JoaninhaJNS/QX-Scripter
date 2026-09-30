namespace Qx.Protocol.Sulek;

/// <summary>Provides access to the message lists of the Sulek API.</summary>
public static class SulekClient
{
    /// <summary>The base URL of the Sulek API.</summary>
    public const string BaseUrl = "https://api.sulek.dev";

    /// <summary>Downloads the message list of a client release from <c>/releases/{variant}/{version}/messages</c>.</summary>
    /// <param name="variant">The client variant in the URL path.</param>
    /// <param name="version">The release version in the URL path.</param>
    /// <param name="http">The HTTP client that sends the request.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The parsed message list.</returns>
    /// <exception cref="HttpRequestException">Thrown when the request fails or returns an unsuccessful status code.</exception>
    public static async Task<SulekMessages> FetchMessagesAsync(string variant, string version, HttpClient http, CancellationToken cancellationToken = default)
    {
        string url = $"{BaseUrl}/releases/{variant}/{version}/messages";
        string json = await http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return SulekMessages.Parse(json);
    }
}
