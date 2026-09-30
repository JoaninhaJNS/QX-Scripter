using Qx.Messages;
using Qx.Protocol;

namespace Qx.Interception;

/// <summary>Represents the active session and its message catalog binding, captured together.</summary>
/// <param name="Session">The active session, or <see langword="null"/> when there is none.</param>
/// <param name="Catalog">The catalog binding of the session, or <see langword="null"/> when none is bound.</param>
public readonly record struct InterceptorSessionCatalog(
    Session? Session,
    SessionCatalogBinding? Catalog);

/// <summary>Defines an interceptor that observes, modifies and blocks the packets of a hotel connection.</summary>
public interface IInterceptor : IConnection
{
    /// <summary>Gets the message manager that maps message names to headers for the session.</summary>
    MessageManager Messages { get; }

    /// <summary>Occurs when a packet is intercepted, before the registered callbacks run.</summary>
    event Action<Intercept>? Intercepted;

    /// <summary>Waits until the message catalog for the session has been prepared.</summary>
    /// <remarks>The default implementation completes immediately.</remarks>
    /// <param name="cancellationToken">A token that cancels the wait.</param>
    /// <returns>A task that completes when the catalog is ready.</returns>
    Task WaitForCatalogBuildAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Gets the active session and its catalog binding as one consistent pair.</summary>
    /// <returns>The session and catalog binding at the moment of the call.</returns>
    InterceptorSessionCatalog CaptureSessionCatalog();

    /// <summary>Sends a packet only when the session and its catalog binding are still the expected ones.</summary>
    /// <param name="packet">The packet to send.</param>
    /// <param name="expected_session">The session the packet belongs to.</param>
    /// <param name="expected_catalog">The catalog binding the packet was built against.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session or the catalog binding changed.</exception>
    void Send(
        IPacket packet,
        Session? expected_session,
        SessionCatalogBinding? expected_catalog) =>
        Send(packet, expected_session, expected_catalog, null);

    /// <summary>Sends a packet only when the session and its catalog binding are still the expected ones, after running a guard.</summary>
    /// <remarks>The guard runs after both checks and right before the packet is written; it can throw to cancel the send.</remarks>
    /// <param name="packet">The packet to send.</param>
    /// <param name="expected_session">The session the packet belongs to.</param>
    /// <param name="expected_catalog">The catalog binding the packet was built against.</param>
    /// <param name="dispatch_guard">The action to run before the packet is written, or <see langword="null"/> for none.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session or the catalog binding changed.</exception>
    void Send(
        IPacket packet,
        Session? expected_session,
        SessionCatalogBinding? expected_catalog,
        Action? dispatch_guard);

    /// <summary>Registers a callback for packets with a header.</summary>
    /// <param name="header">The header to intercept.</param>
    /// <param name="callback">The callback that receives each matching packet.</param>
    /// <returns>A handle that removes the callback when disposed.</returns>
    IDisposable Intercept(Header header, Action<Intercept> callback);
    /// <summary>Registers a callback for packets of a named message.</summary>
    /// <param name="identifier">The message to intercept.</param>
    /// <param name="callback">The callback that receives each matching packet.</param>
    /// <returns>A handle that removes the callback when disposed.</returns>
    IDisposable Intercept(Identifier identifier, Action<Intercept> callback);
    /// <summary>Registers a callback for packets of a semantic message.</summary>
    /// <remarks>
    /// The default implementation resolves the key against <see cref="Messages"/> once. When it does not
    /// resolve, the callback is never called and the returned handle does nothing.
    /// </remarks>
    /// <param name="key">The semantic message key to intercept.</param>
    /// <param name="callback">The callback that receives each matching packet.</param>
    /// <returns>A handle that removes the callback when disposed.</returns>
    IDisposable Intercept(MessageKey key, Action<Intercept> callback)
    {
        if (!Messages.TryGetHeader(key, out Header header))
            return EmptySubscription.Instance;
        return Intercept(header, callback);
    }
}
