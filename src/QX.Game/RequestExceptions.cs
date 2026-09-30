namespace Qx.Game;

/// <summary>
/// Thrown when a request receives no matching response within its timeout.
/// </summary>
/// <param name="outgoing_name">The name of the outgoing message that was sent.</param>
/// <param name="incoming_name">The name of the incoming message that was awaited.</param>
/// <param name="timeout_ms">The timeout that elapsed, in milliseconds.</param>
public sealed class RequestTimeoutException(
    string outgoing_name,
    string incoming_name,
    int timeout_ms)
    : TimeoutException(
        $"Request '{outgoing_name}' timed out after {timeout_ms} ms while waiting for '{incoming_name}'.")
{
    /// <summary>Gets the name of the outgoing message that was sent.</summary>
    public string OutgoingName { get; } = outgoing_name;
    /// <summary>Gets the name of the incoming message that was awaited.</summary>
    public string IncomingName { get; } = incoming_name;
    /// <summary>Gets the timeout that elapsed, in milliseconds.</summary>
    public int TimeoutMs { get; } = timeout_ms;
}

/// <summary>
/// Thrown when the hotel connection is unavailable or closes before a request receives its response.
/// </summary>
/// <remarks>
/// Also thrown when the hotel session changes while a request is waiting.
/// </remarks>
/// <param name="outgoing_name">The name of the outgoing message of the request.</param>
/// <param name="incoming_name">The name of the incoming message that was awaited.</param>
public sealed class RequestDisconnectedException(string outgoing_name, string incoming_name)
    : InvalidOperationException(
        $"The connection closed while request '{outgoing_name}' was waiting for '{incoming_name}'.")
{
    /// <summary>Gets the name of the outgoing message of the request.</summary>
    public string OutgoingName { get; } = outgoing_name;
    /// <summary>Gets the name of the incoming message that was awaited.</summary>
    public string IncomingName { get; } = incoming_name;
}

/// <summary>
/// Thrown when a response message cannot be parsed as the expected model.
/// </summary>
/// <remarks>
/// A message that leaves unread bytes after parsing is treated as a parse failure.
/// </remarks>
/// <param name="incoming_name">The name of the incoming message.</param>
/// <param name="response_type">The name of the model type the message was parsed as.</param>
/// <param name="detail">A description of the failure.</param>
/// <param name="inner_exception">The exception that caused the failure, or <see langword="null"/>.</param>
public sealed class ResponseParseException(
    string incoming_name,
    string response_type,
    string detail,
    Exception? inner_exception = null)
    : Exception(
        $"Response '{incoming_name}' could not be parsed as '{response_type}': {detail}",
        inner_exception)
{
    /// <summary>Gets the name of the incoming message.</summary>
    public string IncomingName { get; } = incoming_name;
    /// <summary>Gets the name of the model type the message was parsed as.</summary>
    public string ResponseType { get; } = response_type;
}

/// <summary>
/// Thrown when the predicate that matches a response to its request throws an exception.
/// </summary>
/// <param name="incoming_name">The name of the incoming message.</param>
/// <param name="response_type">The name of the model type the message was parsed as.</param>
/// <param name="inner_exception">The exception thrown by the predicate.</param>
public sealed class ResponseMatchException(
    string incoming_name,
    string response_type,
    Exception inner_exception)
    : InvalidOperationException(
        $"The correlation predicate for response '{incoming_name}' and model '{response_type}' failed.",
        inner_exception)
{
    /// <summary>Gets the name of the incoming message.</summary>
    public string IncomingName { get; } = incoming_name;
    /// <summary>Gets the name of the model type the message was parsed as.</summary>
    public string ResponseType { get; } = response_type;
}

/// <summary>
/// Thrown when the fragments of a multi-part load can no longer be matched to the request that started it.
/// </summary>
/// <remarks>
/// Raised by fragmented loads such as the inventory, the badge inventory and the friend list after the
/// request that owned a load expired before all fragments arrived. The data recovers when a later
/// complete load arrives or after reconnecting.
/// </remarks>
/// <param name="resource_name">The name of the resource being loaded, such as <c>inventory</c>.</param>
/// <param name="retired_request_epoch">The epoch of the request that expired.</param>
/// <param name="active_request_epoch">The epoch of the request that was active and not completed.</param>
public sealed class FragmentedLoadCorrelationException(
    string resource_name,
    long retired_request_epoch,
    long active_request_epoch)
    : InvalidOperationException(
        $"The '{resource_name}' baseline cannot be correlated after request epoch {retired_request_epoch} expired. Request epoch {active_request_epoch} was not completed; wait for the successor baseline or reconnect.")
{
    /// <summary>Gets the name of the resource being loaded.</summary>
    public string ResourceName { get; } = resource_name;
    /// <summary>Gets the epoch of the request that expired.</summary>
    public long RetiredRequestEpoch { get; } = retired_request_epoch;
    /// <summary>Gets the epoch of the request that was active and not completed.</summary>
    public long ActiveRequestEpoch { get; } = active_request_epoch;
}
