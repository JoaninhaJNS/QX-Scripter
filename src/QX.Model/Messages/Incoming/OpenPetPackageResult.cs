using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>OpenPetPackageResult</c> message, received with the outcome of naming and opening a pet package.</summary>
/// <param name="ObjectId">The identifier of the pet package furni.</param>
/// <param name="NameValidationStatus">The result code of the pet name validation.</param>
/// <param name="NameValidationInfo">Additional information about the name validation result.</param>
public sealed record OpenPetPackageResult(Id ObjectId, int NameValidationStatus, string NameValidationInfo)
    : IParserComposer<OpenPetPackageResult>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static OpenPetPackageResult Parse(in PacketReader p) => new(p.ReadId(), p.ReadInt(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteId(ObjectId);
        p.WriteInt(NameValidationStatus);
        p.WriteString(NameValidationInfo);
    }
}
