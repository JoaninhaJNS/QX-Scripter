using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>TryVerificationCodeResult</c> message, received with the outcome of submitting a verification code.</summary>
/// <param name="ResultCode">The result code of the verification attempt.</param>
/// <param name="MillisecondsToAllowProcessReset">The time in milliseconds until the verification process may be reset.</param>
public sealed record TryVerificationCodeResult(int ResultCode, int MillisecondsToAllowProcessReset)
    : IParserComposer<TryVerificationCodeResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static TryVerificationCodeResult Parse(in PacketReader p) => new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(ResultCode);
        p.WriteInt(MillisecondsToAllowProcessReset);
    }
}
