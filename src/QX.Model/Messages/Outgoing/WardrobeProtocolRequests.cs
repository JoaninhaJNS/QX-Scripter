using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the outfits saved in the user's wardrobe.</summary>
/// <remarks>Sent as the Flash <c>GetWardrobe</c> message, which carries no fields.</remarks>
public sealed record WardrobeRequest : IParserComposer<WardrobeRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WardrobeRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WardrobeRequest ParseFlash(in PacketReader p)
    {
        RequireEmpty(in p);
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WardrobeRequest value, in PacketWriter p) { }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException(
                $"{nameof(WardrobeRequest)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Sent when the user saves an outfit to a wardrobe slot.</summary>
/// <remarks>Sent as the Flash <c>SaveWardrobeOutfit</c> message.</remarks>
/// <param name="SlotId">The wardrobe slot to save the outfit in.</param>
/// <param name="Figure">The figure string of the outfit.</param>
/// <param name="Gender">The gender code of the outfit, <c>M</c>, <c>F</c> or <c>U</c>.</param>
public sealed record SaveWardrobeOutfitRequest(
    int SlotId,
    string Figure,
    string Gender) : IParserComposer<SaveWardrobeOutfitRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static SaveWardrobeOutfitRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static SaveWardrobeOutfitRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadString(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(SaveWardrobeOutfitRequest value, in PacketWriter p)
    {
        ValidateStrings(value, in p);
        p.WriteInt(value.SlotId);
        p.WriteString(value.Figure);
        p.WriteString(value.Gender);
    }

    private static void ValidateStrings(SaveWardrobeOutfitRequest value, in PacketWriter p)
    {
        ValidateString(value.Figure, nameof(Figure), in p);
        ValidateString(value.Gender, nameof(Gender), in p);
    }

    private static void ValidateString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        int length = p.Encoding.GetByteCount(value);
        if (length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"String byte length ({length}) exceeds {ushort.MaxValue}.",
                name);
        }
    }
}
