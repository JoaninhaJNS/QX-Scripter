using Qx.Messages;

namespace Qx.Model;

/// <summary>Represents a furni placed in the room, either on the floor or on a wall.</summary>
public abstract class Furni
{
    /// <summary>Gets whether the item stands on the floor or hangs on a wall.</summary>
    public abstract ItemType Type { get; }

    /// <summary>Gets whether the item is a floor item.</summary>
    public bool IsFloorItem => Type == ItemType.Floor;
    /// <summary>Gets whether the item is a wall item.</summary>
    public bool IsWallItem => Type == ItemType.Wall;

    /// <summary>Gets or sets the furni kind identifier.</summary>
    /// <remarks>A negative kind means the packet carries the kind as <see cref="Identifier"/> instead.</remarks>
    public int Kind { get; set; }
    /// <summary>Gets or sets the item identifier.</summary>
    public Id Id { get; set; }
    /// <summary>Gets or sets the identifier of the item's owner.</summary>
    public Id OwnerId { get; set; }
    /// <summary>Gets or sets the name of the item's owner.</summary>
    /// <remarks>Filled from the owner names the room item packets carry; empty when none was sent.</remarks>
    public string OwnerName { get; set; } = "";

    /// <summary>Gets the item's state as an integer, or -1 when its data is not a state.</summary>
    public abstract int State { get; }

    /// <summary>Gets or sets the seconds until a rented item expires, or -1 when it does not expire.</summary>
    public int SecondsToExpiration { get; set; } = -1;
    /// <summary>Gets or sets who may use the item.</summary>
    public FurniUsage Usage { get; set; } = FurniUsage.None;
    /// <summary>Gets or sets the furni class name, or <see langword="null"/> when it is not known.</summary>
    /// <remarks>
    /// Taken from the packet when <see cref="Kind"/> is negative and otherwise filled from the furni
    /// definitions when they are loaded.
    /// </remarks>
    public string? Identifier { get; set; }
    /// <summary>Gets or sets whether the item is hidden from view locally.</summary>
    /// <remarks>Hiding only affects the local client; the item stays in the room.</remarks>
    public bool IsHidden { get; set; }
    /// <summary>Gets whether the item is no longer in the room.</summary>
    /// <remarks>
    /// Set when the item is picked up, replaced or the room session ends, so a kept reference can
    /// tell that it is no longer live.
    /// </remarks>
    public bool IsRemoved { get; internal set; }
}
