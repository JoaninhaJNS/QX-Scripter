using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>
/// Represents the <c>CallForHelpDisabledNotify</c> message, received when the call for help feature is disabled.
/// </summary>
/// <param name="InfoUrl">The URL of the page with more information.</param>
public sealed record CallForHelpDisabledNotify(string InfoUrl) : IParserComposer<CallForHelpDisabledNotify>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CallForHelpDisabledNotify Parse(in PacketReader p) => new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) => p.WriteString(InfoUrl);
}
