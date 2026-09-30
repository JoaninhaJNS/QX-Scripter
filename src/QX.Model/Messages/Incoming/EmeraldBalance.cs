using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>EmeraldBalance</c> message, received with the user's emerald balance.</summary>
/// <param name="Balance">The number of emeralds the user has.</param>
public sealed record EmeraldBalance(int Balance) : IParserComposer<EmeraldBalance>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static EmeraldBalance Parse(in PacketReader p) => new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteInt(Balance);
}
