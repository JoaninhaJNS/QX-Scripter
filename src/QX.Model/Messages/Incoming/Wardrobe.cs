using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents an outfit saved in a wardrobe slot.</summary>
/// <param name="SlotId">The wardrobe slot number.</param>
/// <param name="Figure">The figure string of the outfit.</param>
/// <param name="Gender">The gender of the outfit's figure.</param>
public readonly record struct WardrobeOutfit(int SlotId, string Figure, string Gender)
    : IParserComposer<WardrobeOutfit>
{
    /// <summary>Parses the outfit from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WardrobeOutfit Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WardrobeOutfit ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadString(), p.ReadString());

    /// <summary>Composes the outfit into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WardrobeOutfit value, in PacketWriter p)
    {
        Validate(value, in p);
        p.WriteInt(value.SlotId);
        p.WriteString(value.Figure);
        p.WriteString(value.Gender);
    }

    internal static void Validate(WardrobeOutfit value, in PacketWriter p)
    {
        WardrobeWire.RequireString(value.Figure, nameof(Figure), in p);
        WardrobeWire.RequireString(value.Gender, nameof(Gender), in p);
    }
}

/// <summary>Represents the <c>Wardrobe</c> message, received with the outfits saved in the user's wardrobe.</summary>
public sealed record Wardrobe : IParserComposer<Wardrobe>
{
    private IReadOnlyList<WardrobeOutfit> _outfits = Array.Empty<WardrobeOutfit>();

    /// <summary>Initializes a new instance of the <see cref="Wardrobe"/> class.</summary>
    /// <param name="state">The wardrobe state value sent by the server.</param>
    /// <param name="outfits">The saved outfits.</param>
    public Wardrobe(int state, IReadOnlyList<WardrobeOutfit> outfits)
    {
        State = state;
        Outfits = outfits;
    }

    /// <summary>Gets the wardrobe state value sent by the server.</summary>
    public int State { get; init; }

    /// <summary>Gets the saved outfits.</summary>
    public IReadOnlyList<WardrobeOutfit> Outfits
    {
        get => _outfits;
        init => _outfits = WardrobeWire.Freeze(value, nameof(Outfits));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static Wardrobe Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static Wardrobe ParseFlash(in PacketReader p)
    {
        int state = p.ReadInt();
        int count = WardrobeWire.ReadFlashCount(in p, nameof(Outfits));
        var outfits = new WardrobeOutfit[count];
        for (int i = 0; i < count; i++)
            outfits[i] = p.Parse<WardrobeOutfit>();
        return new Wardrobe(state, outfits);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(Wardrobe value, in PacketWriter p)
    {
        Validate(value, in p);
        p.WriteInt(value.State);
        p.WriteInt(value.Outfits.Count);
        foreach (WardrobeOutfit outfit in value.Outfits)
            p.Compose(outfit);
    }

    private static void Validate(Wardrobe value, in PacketWriter p)
    {
        foreach (WardrobeOutfit outfit in value.Outfits)
            WardrobeOutfit.Validate(outfit, in p);
    }
}

internal static class WardrobeWire
{
    public static IReadOnlyList<WardrobeOutfit> Freeze(
        IReadOnlyList<WardrobeOutfit> values,
        string name)
    {
        ArgumentNullException.ThrowIfNull(values, name);
        return Array.AsReadOnly(values.ToArray());
    }

    public static void RequireString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        if (p.Encoding.GetByteCount(value) > ushort.MaxValue)
            throw new ArgumentException($"{name} exceeds the wire string limit.", name);
    }

    public static int ReadFlashCount(in PacketReader p, string name)
    {
        int available = p.Available;
        int count = p.ReadInt();
        return RequireBoundedCount(count, available - sizeof(int), name);
    }

    private static int RequireBoundedCount(int count, int available, string name)
    {
        if (count < 0)
            throw new InvalidDataException($"{name} contains a negative count {count}.");
        if (available < 0 || count > available / 8)
            throw new InvalidDataException($"{name} count {count} exceeds the remaining payload capacity.");
        return count;
    }
}
