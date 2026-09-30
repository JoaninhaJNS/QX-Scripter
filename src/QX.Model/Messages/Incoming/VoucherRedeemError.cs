using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>VoucherRedeemError</c> message, received when a voucher code cannot be redeemed.</summary>
/// <param name="ErrorCode">The error code, sent as a string.</param>
public sealed record VoucherRedeemError(string ErrorCode) : IParserComposer<VoucherRedeemError>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static VoucherRedeemError Parse(in PacketReader p) => new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteString(ErrorCode);
}
