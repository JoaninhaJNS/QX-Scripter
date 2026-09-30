using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>PetInventory</c> message, received with one fragment of the user's pet inventory.</summary>
/// <remarks>
/// The server splits a large inventory into several fragments. The inventory is complete once every
/// fragment from 0 to <see cref="Total"/> minus one has been received.
/// </remarks>
public sealed record PetInventory : IParserComposer<PetInventory>
{
    private IReadOnlyList<InventoryPet> _pets = Array.Empty<InventoryPet>();

    /// <summary>Initializes a new instance of the <see cref="PetInventory"/> class.</summary>
    /// <param name="total">The total number of fragments.</param>
    /// <param name="index">The zero based index of this fragment.</param>
    /// <param name="pets">The pets in this fragment.</param>
    public PetInventory(int total, int index, IReadOnlyList<InventoryPet> pets)
    {
        Total = total;
        Index = index;
        Pets = pets;
    }

    /// <summary>Gets the total number of fragments the inventory is split into.</summary>
    public int Total { get; init; }

    /// <summary>Gets the zero based index of this fragment.</summary>
    public int Index { get; init; }

    /// <summary>Gets the pets in this fragment.</summary>
    public IReadOnlyList<InventoryPet> Pets
    {
        get => _pets;
        init => _pets = InventoryWire.FreezeReferences(value, nameof(Pets));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PetInventory Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PetInventory ParseFlash(in PacketReader p)
    {
        int total = p.ReadInt();
        int index = p.ReadInt();
        InventoryWire.RequireFragment(total, index, nameof(PetInventory));
        int count = InventoryWire.RequireCount(
            p.ReadInt(),
            p.Available,
            32,
            nameof(Pets));
        var pets = new InventoryPet[count];
        for (int pet_index = 0; pet_index < pets.Length; pet_index++)
            pets[pet_index] = p.Parse<InventoryPet>();
        InventoryWire.RequireEmpty(in p, nameof(PetInventory));
        return new PetInventory(total, index, pets);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PetInventory value, in PacketWriter p)
    {
        InventoryWire.RequireFragment(value.Total, value.Index, nameof(PetInventory));
        foreach (InventoryPet pet in value.Pets)
            pet.ValidateFlash(in p);
        p.WriteInt(value.Total);
        p.WriteInt(value.Index);
        p.WriteInt(value.Pets.Count);
        foreach (InventoryPet pet in value.Pets)
            p.Compose(pet);
    }
}

/// <summary>Represents the <c>PetAddedToInventory</c> message, received when a pet is added to the user's inventory.</summary>
/// <param name="Pet">The added pet.</param>
/// <param name="OpenInventory">Whether the client should open the inventory to show the pet.</param>
public sealed record PetAddedToInventory(InventoryPet Pet, bool OpenInventory) :
    IParserComposer<PetAddedToInventory>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PetAddedToInventory Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PetAddedToInventory ParseFlash(in PacketReader p)
    {
        var value = new PetAddedToInventory(p.Parse<InventoryPet>(), p.ReadBool());
        InventoryWire.RequireEmpty(in p, nameof(PetAddedToInventory));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PetAddedToInventory value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value.Pet);
        value.Pet.ValidateFlash(in p);
        p.Compose(value.Pet);
        p.WriteBool(value.OpenInventory);
    }
}

/// <summary>Represents the <c>PetRemovedFromInventory</c> message, received when a pet is removed from the user's inventory.</summary>
/// <param name="PetId">The ID of the removed pet.</param>
public sealed record PetRemovedFromInventory(Id PetId) : IParserComposer<PetRemovedFromInventory>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PetRemovedFromInventory Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PetRemovedFromInventory ParseFlash(in PacketReader p)
    {
        var value = new PetRemovedFromInventory(p.ReadInt());
        InventoryWire.RequireEmpty(in p, nameof(PetRemovedFromInventory));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PetRemovedFromInventory value, in PacketWriter p) =>
        p.WriteInt(InventoryWire.Int32Id(value.PetId));
}
