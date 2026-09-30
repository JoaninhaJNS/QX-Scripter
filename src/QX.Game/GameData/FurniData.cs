using System.Text.Json;
using System.Text.Json.Serialization;
using Qx.Model;

namespace Qx.Game;

/// <summary>Represents the definition of one furni kind from the hotel's furni data.</summary>
/// <param name="Type">Whether the kind is a floor or a wall item.</param>
/// <param name="Kind">The furni kind identifier, which is what room and inventory packets carry.</param>
/// <param name="Identifier">
/// The full class name from the furni data, including a <c>*</c> color suffix such as
/// <c>rare_dragonlamp*4</c> when the kind has one; empty when the entry has none.
/// </param>
/// <param name="Name">The localized display name, empty when the entry has none.</param>
/// <param name="Width">The footprint along X in tiles at direction 0, at least 1.</param>
/// <param name="Length">The footprint along Y in tiles at direction 0, at least 1.</param>
/// <param name="Category">The category string from the furni data, empty when the entry has none.</param>
/// <param name="Line">The furni line the kind belongs to, empty when the entry has none.</param>
public sealed record FurniInfo(
    ItemType Type,
    int Kind,
    string Identifier,
    string Name,
    int Width,
    int Length,
    string Category,
    string Line)
{
    /// <summary>Gets the class name without the <c>*</c> color suffix of <see cref="Identifier"/>.</summary>
    public string ClassName { get; init; } = Identifier;
    /// <summary>Gets the asset revision from the furni data.</summary>
    public int Revision { get; init; }
    /// <summary>Gets the direction the kind is placed at by default.</summary>
    public int DefaultDirection { get; init; }
    /// <summary>Gets the part colors from the furni data, empty when the kind lists none.</summary>
    public IReadOnlyList<string> PartColors { get; init; } = [];
    /// <summary>Gets the localized description, empty when the entry has none.</summary>
    public string Description { get; init; } = "";
    /// <summary>Gets the advertisement URL attached to the kind, empty when none.</summary>
    public string AdUrl { get; init; } = "";
    /// <summary>Gets the catalog offer identifier from the furni data.</summary>
    public int OfferId { get; init; }
    /// <summary>Gets the furni data's <c>buyout</c> flag for the catalog offer.</summary>
    public bool BuyOut { get; init; }
    /// <summary>Gets the rental offer identifier from the furni data.</summary>
    public int RentOfferId { get; init; }
    /// <summary>Gets the furni data's <c>rentbuyout</c> flag for the rental offer.</summary>
    public bool RentBuyOut { get; init; }
    /// <summary>Gets whether the kind is a Builders Club item.</summary>
    public bool IsBuildersClub { get; init; }
    /// <summary>Gets the Builders Club offer identifier from the furni data.</summary>
    public int BuildersClubOfferId { get; init; }
    /// <summary>Gets the furni data's <c>excludeddynamic</c> flag.</summary>
    public bool ExcludedDynamic { get; init; }
    /// <summary>Gets the free-form parameter string the hotel attaches to the kind, empty when none.</summary>
    public string CustomParams { get; init; } = "";
    /// <summary>Gets the special behavior of the kind, taken from the furni data's <c>specialtype</c> field.</summary>
    public FurniCategory SpecialType { get; init; }
    /// <summary>Gets whether avatars may stand on the kind.</summary>
    public bool CanStandOn { get; init; }
    /// <summary>Gets whether avatars may sit on the kind.</summary>
    public bool CanSitOn { get; init; }
    /// <summary>Gets whether avatars may lie on the kind.</summary>
    public bool CanLayOn { get; init; }
    /// <summary>Gets whether other furni may be stacked on the kind.</summary>
    public bool CanPutStuffOn { get; init; }
    /// <summary>Gets the kind's own height in tile units.</summary>
    public double Height { get; init; }
    /// <summary>Gets the environment tag from the furni data, empty when none.</summary>
    public string Environment { get; init; } = "";
    /// <summary>Gets whether the hotel marks the kind as rare.</summary>
    public bool IsRare { get; init; }
    /// <summary>Gets whether items of the kind may be traded.</summary>
    public bool Tradeable { get; init; }
    /// <summary>Gets whether items of the kind may be recycled.</summary>
    public bool Recyclable { get; init; }
    /// <summary>Gets whether <see cref="Identifier"/> carries a numeric <c>*</c> color suffix.</summary>
    public bool HasIndexedColor { get; init; }
    /// <summary>Gets the numeric color suffix of <see cref="Identifier"/>, or 0 when <see cref="HasIndexedColor"/> is <see langword="false"/>.</summary>
    public int ColorIndex { get; init; }

    /// <summary>Gets whether an avatar can stand, sit or lie on the kind.</summary>
    public bool IsWalkable => CanStandOn || CanSitOn || CanLayOn;
    /// <summary>Gets whether an avatar can neither stand, sit nor lie on the kind.</summary>
    public bool IsUnwalkable => !IsWalkable;
}

/// <summary>Represents the hotel's furni data, the definitions of every floor and wall furni kind.</summary>
public sealed partial class FurniData
{
    private readonly Dictionary<int, FurniInfo> _floor = [];
    private readonly Dictionary<int, FurniInfo> _wall = [];
    private readonly Dictionary<string, FurniInfo> _byIdentifier = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the floor item definitions.</summary>
    public IReadOnlyCollection<FurniInfo> FloorItems => _floor.Values;
    /// <summary>Gets the wall item definitions.</summary>
    public IReadOnlyCollection<FurniInfo> WallItems => _wall.Values;

    /// <summary>Gets the definition of a furni kind.</summary>
    /// <param name="type">The item type, floor or wall.</param>
    /// <param name="kind">The furni kind identifier.</param>
    /// <returns>
    /// The definition, or <see langword="null"/> when the kind is unknown or
    /// <paramref name="type"/> is <see cref="ItemType.None"/>.
    /// </returns>
    public FurniInfo? GetInfo(ItemType type, int kind) => type switch
    {
        ItemType.Floor => _floor.GetValueOrDefault(kind),
        ItemType.Wall => _wall.GetValueOrDefault(kind),
        _ => null
    };

    /// <summary>Gets the definition of a furni kind by its class name.</summary>
    /// <remarks>
    /// The full identifier with a color suffix, such as <c>rare_dragonlamp*4</c>, matches that
    /// color exactly. A bare class name matches the kind without a suffix, or the first color
    /// variant loaded when no such kind exists.
    /// </remarks>
    /// <param name="identifier">The class name, matched case-insensitively.</param>
    /// <returns>The definition, or <see langword="null"/> when no kind matches.</returns>
    public FurniInfo? GetInfo(string identifier) => _byIdentifier.GetValueOrDefault(identifier);

    /// <summary>Gets the definition of a furni item.</summary>
    /// <remarks>
    /// The item's <see cref="Furni.Identifier"/> is tried first, and its type and kind are used
    /// when the identifier is empty or unknown.
    /// </remarks>
    /// <param name="item">The furni item.</param>
    /// <returns>The definition, or <see langword="null"/> when the kind is unknown.</returns>
    public FurniInfo? GetInfo(Furni item) =>
        item.Identifier is { Length: > 0 } id && _byIdentifier.TryGetValue(id, out FurniInfo? byId)
            ? byId
            : GetInfo(item.Type, item.Kind);

    /// <summary>Parses the hotel's <c>furnidata_json</c> document.</summary>
    /// <remarks>
    /// When a kind appears more than once for the same item type, the last entry wins.
    /// </remarks>
    /// <param name="json">The JSON document.</param>
    /// <returns>The parsed furni data.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the document is malformed, empty, or contains neither a floor nor a wall item
    /// collection.
    /// </exception>
    public static FurniData LoadJson(string json)
    {
        var data = new FurniData();
        FurniJson root = JsonSerializer.Deserialize(
            json,
            FurniJsonContext.Default.FurniJson)
            ?? throw new JsonException("Furniture data is empty.");
        if (root.RoomItemTypes is null && root.WallItemTypes is null)
            throw new JsonException("Furniture data contains no item collections.");

        foreach (FurniTypeJson entry in root.RoomItemTypes?.FurniType ?? [])
            data.Add(ItemType.Floor, entry);
        foreach (FurniTypeJson entry in root.WallItemTypes?.FurniType ?? [])
            data.Add(ItemType.Wall, entry);

        return data;
    }

    private void Add(ItemType type, FurniTypeJson entry)
    {
        string identifier = entry.ClassName ?? "";
        string[] identifierParts = identifier.Split('*', 2);
        string className = identifierParts[0];
        int colorIndex = 0;
        bool hasIndexedColor = identifierParts.Length == 2 &&
            int.TryParse(identifierParts[1], out colorIndex);

        var info = new FurniInfo(
            type,
            entry.Id,
            identifier,
            entry.Name ?? "",
            Math.Max(1, entry.XDim),
            Math.Max(1, entry.YDim),
            entry.Category ?? "",
            entry.FurniLine ?? "")
        {
            ClassName = className,
            Revision = entry.Revision,
            DefaultDirection = entry.DefaultDirection,
            PartColors = entry.PartColors?.Colors?.AsReadOnly() ?? [],
            Description = entry.Description ?? "",
            AdUrl = entry.AdUrl ?? "",
            OfferId = entry.OfferId,
            BuyOut = entry.Buyout,
            RentOfferId = entry.RentOfferId,
            RentBuyOut = entry.RentBuyout,
            IsBuildersClub = entry.BuildersClub,
            BuildersClubOfferId = entry.BuildersClubOfferId,
            ExcludedDynamic = entry.ExcludedDynamic,
            CustomParams = entry.CustomParameters ?? "",
            SpecialType = (FurniCategory)entry.SpecialType,
            CanStandOn = entry.CanStandOn,
            CanSitOn = entry.CanSitOn,
            CanLayOn = entry.CanLayOn,
            CanPutStuffOn = entry.CanPutStuffOn,
            Height = entry.Height,
            Environment = entry.Environment ?? "",
            IsRare = entry.Rare,
            Tradeable = entry.Tradeable,
            Recyclable = entry.Recyclable,
            HasIndexedColor = hasIndexedColor,
            ColorIndex = colorIndex
        };

        (type == ItemType.Floor ? _floor : _wall)[info.Kind] = info;
        if (info.Identifier.Length > 0)
            _byIdentifier[info.Identifier] = info;
        if (info.ClassName.Length > 0)
            _byIdentifier.TryAdd(info.ClassName, info);
    }

    private sealed class FurniJson
    {
        [JsonPropertyName("roomitemtypes")] public FurniTypeList? RoomItemTypes { get; set; }
        [JsonPropertyName("wallitemtypes")] public FurniTypeList? WallItemTypes { get; set; }
    }

    private sealed class FurniTypeList
    {
        [JsonPropertyName("furnitype")] public List<FurniTypeJson>? FurniType { get; set; }
    }

    private sealed class FurniTypeJson
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("classname")] public string? ClassName { get; set; }
        [JsonPropertyName("revision")] public int Revision { get; set; }
        [JsonPropertyName("defaultdir")] public int DefaultDirection { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("xdim")] public int XDim { get; set; }
        [JsonPropertyName("ydim")] public int YDim { get; set; }
        [JsonPropertyName("partcolors")] public PartColorsJson? PartColors { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("adurl")] public string? AdUrl { get; set; }
        [JsonPropertyName("offerid")] public int OfferId { get; set; }
        [JsonPropertyName("buyout")] public bool Buyout { get; set; }
        [JsonPropertyName("rentofferid")] public int RentOfferId { get; set; }
        [JsonPropertyName("rentbuyout")] public bool RentBuyout { get; set; }
        [JsonPropertyName("bc")] public bool BuildersClub { get; set; }
        [JsonPropertyName("bcofferid")] public int BuildersClubOfferId { get; set; }
        [JsonPropertyName("excludeddynamic")] public bool ExcludedDynamic { get; set; }
        [JsonPropertyName("customparams")] public string? CustomParameters { get; set; }
        [JsonPropertyName("specialtype")] public int SpecialType { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("canstandon")] public bool CanStandOn { get; set; }
        [JsonPropertyName("cansiton")] public bool CanSitOn { get; set; }
        [JsonPropertyName("canlayon")] public bool CanLayOn { get; set; }
        [JsonPropertyName("canputstuffon")] public bool CanPutStuffOn { get; set; }
        [JsonPropertyName("height")] public double Height { get; set; }
        [JsonPropertyName("furniline")] public string? FurniLine { get; set; }
        [JsonPropertyName("environment")] public string? Environment { get; set; }
        [JsonPropertyName("rare")] public bool Rare { get; set; }
        [JsonPropertyName("tradeable")] public bool Tradeable { get; set; }
        [JsonPropertyName("recyclable")] public bool Recyclable { get; set; }
    }

    private sealed class PartColorsJson
    {
        [JsonPropertyName("color")] public List<string>? Colors { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(FurniJson))]
    private sealed partial class FurniJsonContext : JsonSerializerContext;
}
