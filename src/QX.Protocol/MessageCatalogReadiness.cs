namespace Qx.Protocol;

/// <summary>Defines a way to wait until message catalog preparation has finished.</summary>
public interface IMessageCatalogReadiness
{
    /// <summary>Waits until message catalog preparation has finished.</summary>
    /// <param name="cancellation_token">The token that cancels the wait.</param>
    /// <returns>A task that completes when no catalog preparation is pending.</returns>
    Task WaitUntilReadyAsync(CancellationToken cancellation_token = default);
}
