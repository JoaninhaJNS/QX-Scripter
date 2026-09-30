using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the user's furni inventory.</summary>
/// <remarks>Sent as the Flash <c>RequestFurniInventory</c> message, which carries no fields.</remarks>
public sealed record FurniInventoryRequest : IParserComposer<FurniInventoryRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FurniInventoryRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FurniInventoryRequest ParseFlash(in PacketReader p)
    {
        InventoryWire.RequireEmpty(in p, nameof(FurniInventoryRequest));
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FurniInventoryRequest value, in PacketWriter p) { }
}

/// <summary>Requests the user's pet inventory.</summary>
/// <remarks>Sent as the Flash <c>GetPetInventory</c> message, which carries no fields.</remarks>
public sealed record PetInventoryRequest : IParserComposer<PetInventoryRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PetInventoryRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PetInventoryRequest ParseFlash(in PacketReader p)
    {
        InventoryWire.RequireEmpty(in p, nameof(PetInventoryRequest));
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PetInventoryRequest value, in PacketWriter p) { }
}

/// <summary>Sent when the user activates an avatar effect from the inventory.</summary>
/// <remarks>Sent as the Flash <c>AvatarEffectActivated</c> message.</remarks>
/// <param name="Effect">The type id of the effect to activate.</param>
public sealed record AvatarEffectActivationRequest(int Effect)
    : IParserComposer<AvatarEffectActivationRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AvatarEffectActivationRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AvatarEffectActivationRequest ParseFlash(in PacketReader p)
    {
        var value = new AvatarEffectActivationRequest(p.ReadInt());
        InventoryWire.RequireEmpty(in p, nameof(AvatarEffectActivationRequest));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AvatarEffectActivationRequest value, in PacketWriter p) =>
        p.WriteInt(value.Effect);
}
