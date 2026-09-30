using Qx.Messages;

namespace Qx.Protocol;

/// <summary>Defines a resolver that maps stable message keys to the headers of the active client.</summary>
public interface ISemanticMessageResolver
{
    /// <summary>Gets whether a message key is declared in the message registry.</summary>
    /// <param name="key">The message key.</param>
    bool IsKnown(MessageKey key);

    /// <summary>Gets whether a message key is declared and has a message name for the active client.</summary>
    /// <param name="key">The message key.</param>
    bool IsApplicable(MessageKey key);

    /// <summary>Tries to get the single header that a message key resolves to for the active client.</summary>
    /// <param name="key">The message key.</param>
    /// <param name="header">The header, or the default header when it cannot be resolved.</param>
    /// <returns><see langword="true"/> if the key resolves to exactly one header; otherwise, <see langword="false"/>.</returns>
    bool TryGetHeader(MessageKey key, out Header header);

    /// <summary>Tries to get every header that a message key resolves to for the active client.</summary>
    /// <param name="key">The message key.</param>
    /// <param name="headers">The headers, or an empty list when none can be resolved.</param>
    /// <returns><see langword="true"/> if at least one header was resolved; otherwise, <see langword="false"/>.</returns>
    bool TryGetHeaders(MessageKey key, out IReadOnlyList<Header> headers);
}
