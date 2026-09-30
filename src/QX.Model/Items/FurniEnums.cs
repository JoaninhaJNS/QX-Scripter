namespace Qx.Model;

/// <summary>Specifies whether a furni stands on the floor or hangs on a wall.</summary>
/// <remarks>The values are the character codes of the letters the client uses for the two kinds.</remarks>
public enum ItemType
{
    /// <summary>0: not a furni, or a type string the hotel did not recognize.</summary>
    None,
    /// <summary>115, the character <c>'s'</c>: a floor item, placed on a tile.</summary>
    Floor = 's',
    /// <summary>105, the character <c>'i'</c>: a wall item, hung on a wall segment.</summary>
    Wall = 'i'
}

/// <summary>Provides conversions between <see cref="ItemType"/> and the one-letter form the wire uses.</summary>
public static class ItemTypes
{
    /// <summary>Converts the one-letter item type the hotel sends to an <see cref="ItemType"/>.</summary>
    /// <param name="value">The letter, matched case-insensitively.</param>
    /// <returns>
    /// <see cref="ItemType.Floor"/> for <c>S</c>, <see cref="ItemType.Wall"/> for <c>I</c>, and
    /// <see cref="ItemType.None"/> for anything else, including an empty string.
    /// </returns>
    public static ItemType FromShort(string value) => value.ToUpperInvariant() switch
    {
        "S" => ItemType.Floor,
        "I" => ItemType.Wall,
        _ => ItemType.None
    };

    /// <summary>Converts an <see cref="ItemType"/> to the one-letter form the hotel expects.</summary>
    /// <param name="type">The item type to encode.</param>
    /// <returns><c>S</c> for a floor item, <c>I</c> for a wall item, and an empty string otherwise.</returns>
    public static string ToShort(this ItemType type) => type switch
    {
        ItemType.Floor => "S",
        ItemType.Wall => "I",
        _ => ""
    };
}

/// <summary>Specifies who may interact with a furni standing in a room.</summary>
public enum FurniUsage
{
    /// <summary>0: the furni has no interaction at all.</summary>
    None = 0,
    /// <summary>1: only the room owner and holders of room rights may use it.</summary>
    Rights = 1,
    /// <summary>2: any visitor may use it.</summary>
    Anyone = 2
}

/// <summary>Specifies the special behavior a furni kind has beyond being plain furniture.</summary>
/// <remarks>
/// The values mirror the client's inventory category constants. This is the <c>specialtype</c>
/// field of the hotel's furni data and the category an inventory item is filed under.
/// </remarks>
public enum FurniCategory
{
    /// <summary>-100: a QX placeholder for a category the hotel did not send.</summary>
    /// <remarks>Not a wire value.</remarks>
    Unknown = -100,
    /// <summary>-1: badge furni.</summary>
    /// <remarks>
    /// Not one of the client's numbered categories, whose inventory enum starts at 1, so this
    /// value never arrives on the wire.
    /// </remarks>
    BadgeFurni = -1,
    /// <summary>1: ordinary furniture with no special behavior.</summary>
    Default = 1,
    /// <summary>2: a wallpaper the room's walls can be set to.</summary>
    Wallpaper = 2,
    /// <summary>3: a floor pattern the room's floor can be set to.</summary>
    Floor = 3,
    /// <summary>4: a landscape shown through the room's windows.</summary>
    Landscape = 4,
    /// <summary>5: a sticky note that carries free text.</summary>
    PostIt = 5,
    /// <summary>6: a poster hung on a wall.</summary>
    Poster = 6,
    /// <summary>7: a sound sample set for the Trax player.</summary>
    SoundSet = 7,
    /// <summary>8: a saved Trax song.</summary>
    TraxSong = 8,
    /// <summary>9: a wrapped gift that must be opened to reveal its contents.</summary>
    Present = 9,
    /// <summary>10: an Ecotron box that is opened for a random reward.</summary>
    EcotronBox = 10,
    /// <summary>11: a trophy engraved with a name, a date and a message.</summary>
    Trophy = 11,
    /// <summary>12: furni that is redeemed for credits instead of being placed.</summary>
    CreditFurni = 12,
    /// <summary>13: shampoo that recolors a pet.</summary>
    PetShampoo = 13,
    /// <summary>14: a customization part a pet can wear.</summary>
    PetCustomPart = 14,
    /// <summary>15: shampoo that recolors a pet's customization part.</summary>
    PetCustomPartShampoo = 15,
    /// <summary>16: a saddle that lets a pet be ridden.</summary>
    PetSaddle = 16,
    /// <summary>17: group furni whose look follows the group's badge and colors.</summary>
    GuildFurni = 17,
    /// <summary>18: furni belonging to a room game, such as a scoreboard or a gate.</summary>
    GameFurni = 18,
    /// <summary>19: a monsterplant seed that is planted to grow a plant.</summary>
    MonsterPlantSeed = 19,
    /// <summary>20: a revival potion for a dead monsterplant.</summary>
    MonsterPlantRevival = 20,
    /// <summary>21: an item that lets a monsterplant be bred again.</summary>
    MonsterPlantRebreed = 21,
    /// <summary>22: fertiliser that boosts a monsterplant's growth.</summary>
    MonsterPlantFertilize = 22,
    /// <summary>23: an item that unlocks a clothing set for the avatar.</summary>
    /// <remarks>The client calls this <c>FIGURE_PURCHASABLE_SET</c>.</remarks>
    ClothingFurni = 23,
    /// <summary>24: a chest that is opened for furni.</summary>
    FurniChest = 24,
    /// <summary>25: a chest that is opened for currency.</summary>
    CoinsChest = 25
}

/// <summary>Specifies the shape of a furni's payload.</summary>
/// <remarks>
/// It is sent as the low byte of the payload's leading integer and decides how the rest of the
/// payload is read.
/// </remarks>
public enum ItemDataType
{
    /// <summary>0: a single free-form string, which is what most furni carry.</summary>
    Legacy = 0,
    /// <summary>1: a string-to-string map.</summary>
    Map = 1,
    /// <summary>2: a list of strings.</summary>
    StringArray = 2,
    /// <summary>3: a string plus a vote tally.</summary>
    VoteResult = 3,
    /// <summary>4: no payload beyond the limited-rare tail.</summary>
    Empty = 4,
    /// <summary>5: a list of integers.</summary>
    IntArray = 5,
    /// <summary>6: a game furni's high-score table with its scoring and clearing modes.</summary>
    HighScore = 6,
    /// <summary>7: a crackable furni's hit count and the hits it needs in total.</summary>
    CrackableFurni = 7
}

/// <summary>
/// Specifies the modifier bits carried in the high bytes of a furni payload's leading integer,
/// above the <see cref="ItemDataType"/> byte.
/// </summary>
[Flags]
public enum ItemDataFlags
{
    /// <summary>0: no modifiers.</summary>
    None = 0,
    /// <summary>
    /// 1: the item belongs to a numbered limited series, which appends its serial number and
    /// series size to the payload.
    /// </summary>
    IsLimitedRare = 1
}
