using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>LatencyPingResponse</c> message, received in answer to a <c>LatencyPingRequest</c>.</summary>
/// <param name="RequestId">The identifier of the ping request being answered.</param>
public sealed record LatencyPingResponse(int RequestId) : IParserComposer<LatencyPingResponse>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static LatencyPingResponse Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(RequestId);
}
