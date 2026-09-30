using Qx;
using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

/// <summary>
/// Represents a query over inventory furni.
/// </summary>
/// <remarks>
/// Every filter returns a new query with the same furni data. Identifier, name, category and
/// line values are read from the furni data by item type and kind, and those filters drop items
/// whose value is unknown, the negated filters included. Text matching ignores case,
/// <see langword="null"/> entries in text lists are skipped, and a <see langword="null"/>
/// argument throws <see cref="ArgumentNullException"/>.
/// </remarks>
public sealed class InventoryItemQuery : QueryCollection<InventoryItem>
{
    private readonly FurniData? _furniData;
    private readonly FurniMetadataResolver _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryItemQuery"/> class over the specified items.
    /// </summary>
    /// <param name="items">The inventory items to query.</param>
    /// <param name="furniData">The furni data used for metadata filters, or <see langword="null"/> when it is not loaded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    public InventoryItemQuery(IEnumerable<InventoryItem> items, FurniData? furniData)
        : base(items)
    {
        _furniData = furniData;
        _metadata = new FurniMetadataResolver(furniData);
    }

    /// <summary>
    /// Gets whether furni data is attached to the query.
    /// </summary>
    public bool HasMetadata => _furniData is not null;

    /// <summary>
    /// Filters the items with a predicate.
    /// </summary>
    /// <param name="predicate">The condition an item must meet to be kept.</param>
    /// <returns>A new query with the items that match <paramref name="predicate"/>.</returns>
    public InventoryItemQuery Where(Func<InventoryItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Next(Items.Where(predicate));
    }

    /// <summary>
    /// Filters the items to those with any of the specified inventory item ids.
    /// </summary>
    /// <param name="ids">The inventory item ids to keep, compared with <see cref="InventoryItem.ItemId"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery ByItemId(params Id[] ids) =>
        ByItemId((IEnumerable<Id>)ids);

    /// <summary>
    /// Filters the items to those with any of the specified inventory item ids.
    /// </summary>
    /// <param name="ids">The inventory item ids to keep, compared with <see cref="InventoryItem.ItemId"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery ByItemId(IEnumerable<Id> ids)
    {
        HashSet<Id> values = QueryValues.Set(ids);
        return Where(item => values.Contains(item.ItemId));
    }

    /// <summary>
    /// Filters the items to those whose <see cref="InventoryItem.Id"/> is any of the specified ids.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ByItemId(IEnumerable{Id})"/> to match the inventory item id instead.
    /// </remarks>
    /// <param name="ids">The ids to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery ById(params Id[] ids) =>
        ById((IEnumerable<Id>)ids);

    /// <summary>
    /// Filters the items to those whose <see cref="InventoryItem.Id"/> is any of the specified ids.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ByItemId(IEnumerable{Id})"/> to match the inventory item id instead.
    /// </remarks>
    /// <param name="ids">The ids to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery ById(IEnumerable<Id> ids)
    {
        HashSet<Id> values = QueryValues.Set(ids);
        return Where(item => values.Contains(item.Id));
    }

    /// <summary>
    /// Filters the items to those of any of the specified item types.
    /// </summary>
    /// <param name="types">The item types to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfType(params ItemType[] types) =>
        OfType((IEnumerable<ItemType>)types);

    /// <summary>
    /// Filters the items to those of any of the specified item types.
    /// </summary>
    /// <param name="types">The item types to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfType(IEnumerable<ItemType> types)
    {
        HashSet<ItemType> values = QueryValues.Set(types);
        return Where(item => values.Contains(item.Type));
    }

    /// <summary>
    /// Filters out the items of any of the specified item types.
    /// </summary>
    /// <param name="types">The item types to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public InventoryItemQuery NotOfType(params ItemType[] types) =>
        NotOfType((IEnumerable<ItemType>)types);

    /// <summary>
    /// Filters out the items of any of the specified item types.
    /// </summary>
    /// <param name="types">The item types to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public InventoryItemQuery NotOfType(IEnumerable<ItemType> types)
    {
        HashSet<ItemType> values = QueryValues.Set(types);
        return Where(item => !values.Contains(item.Type));
    }

    /// <summary>
    /// Filters the items to those of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfKind(params int[] kinds) =>
        OfKind((IEnumerable<int>)kinds);

    /// <summary>
    /// Filters the items to those of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfKind(IEnumerable<int> kinds)
    {
        HashSet<int> values = QueryValues.Set(kinds);
        return Where(item => values.Contains(item.Kind));
    }

    /// <summary>
    /// Filters out the items of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public InventoryItemQuery NotOfKind(params int[] kinds) =>
        NotOfKind((IEnumerable<int>)kinds);

    /// <summary>
    /// Filters out the items of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public InventoryItemQuery NotOfKind(IEnumerable<int> kinds)
    {
        HashSet<int> values = QueryValues.Set(kinds);
        return Where(item => !values.Contains(item.Kind));
    }

    /// <summary>
    /// Filters the items to those with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <param name="identifiers">The class identifiers to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfIdentifier(params string[] identifiers) =>
        OfIdentifier((IEnumerable<string>)identifiers);

    /// <summary>
    /// Filters the items to those with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <param name="identifiers">The class identifiers to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfIdentifier(IEnumerable<string> identifiers)
    {
        HashSet<string> values = QueryValues.Strings(identifiers);
        return Where(item => _metadata.Identifier(item, out string value) && values.Contains(value));
    }

    /// <summary>
    /// Filters out the items with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known identifier are dropped as well.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to drop.</param>
    /// <returns>A new query with the items whose identifier is known and not listed.</returns>
    public InventoryItemQuery NotOfIdentifier(params string[] identifiers) =>
        NotOfIdentifier((IEnumerable<string>)identifiers);

    /// <summary>
    /// Filters out the items with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known identifier are dropped as well.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to drop.</param>
    /// <returns>A new query with the items whose identifier is known and not listed.</returns>
    public InventoryItemQuery NotOfIdentifier(IEnumerable<string> identifiers)
    {
        HashSet<string> values = QueryValues.Strings(identifiers);
        return Where(item => _metadata.Identifier(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// The name is read from the furni data. When it is empty, the class identifier from the furni data is used.
    /// </remarks>
    /// <param name="names">The furni names to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Named(params string[] names) =>
        Named((IEnumerable<string>)names);

    /// <summary>
    /// Filters the items to those with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// The name is read from the furni data. When it is empty, the class identifier from the furni data is used.
    /// </remarks>
    /// <param name="names">The furni names to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Named(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(item => _metadata.Name(item, out string value) && values.Contains(value));
    }

    /// <summary>
    /// Filters out the items with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known name are dropped as well.
    /// </remarks>
    /// <param name="names">The furni names to drop.</param>
    /// <returns>A new query with the items whose name is known and not listed.</returns>
    public InventoryItemQuery NotNamed(params string[] names) =>
        NotNamed((IEnumerable<string>)names);

    /// <summary>
    /// Filters out the items with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known name are dropped as well.
    /// </remarks>
    /// <param name="names">The furni names to drop.</param>
    /// <returns>A new query with the items whose name is known and not listed.</returns>
    public InventoryItemQuery NotNamed(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(item => _metadata.Name(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those whose furni name contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery NameContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(item =>
            _metadata.Name(item, out string name) &&
            name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters out the items whose furni name contains the specified text, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known name are dropped as well.
    /// </remarks>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the items whose name is known and does not contain <paramref name="value"/>.</returns>
    public InventoryItemQuery NotNameContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(item =>
            _metadata.Name(item, out string name) &&
            !name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the items to those in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <remarks>
    /// Use <see cref="OfInventoryCategory(IEnumerable{int})"/> to match the numeric inventory category instead.
    /// </remarks>
    /// <param name="categories">The categories to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfCategory(params string[] categories) =>
        OfCategory((IEnumerable<string>)categories);

    /// <summary>
    /// Filters the items to those in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <remarks>
    /// Use <see cref="OfInventoryCategory(IEnumerable{int})"/> to match the numeric inventory category instead.
    /// </remarks>
    /// <param name="categories">The categories to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfCategory(IEnumerable<string> categories)
    {
        HashSet<string> values = QueryValues.Strings(categories);
        return Where(item => _metadata.Category(item, out string value) && values.Contains(value));
    }

    /// <summary>
    /// Filters out the items in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known category are dropped as well.
    /// </remarks>
    /// <param name="categories">The categories to drop.</param>
    /// <returns>A new query with the items whose category is known and not listed.</returns>
    public InventoryItemQuery NotOfCategory(params string[] categories) =>
        NotOfCategory((IEnumerable<string>)categories);

    /// <summary>
    /// Filters out the items in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known category are dropped as well.
    /// </remarks>
    /// <param name="categories">The categories to drop.</param>
    /// <returns>A new query with the items whose category is known and not listed.</returns>
    public InventoryItemQuery NotOfCategory(IEnumerable<string> categories)
    {
        HashSet<string> values = QueryValues.Strings(categories);
        return Where(item => _metadata.Category(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those in any of the specified inventory categories.
    /// </summary>
    /// <param name="categories">The inventory categories to keep, compared with <see cref="InventoryItem.Category"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfInventoryCategory(params int[] categories) =>
        OfInventoryCategory((IEnumerable<int>)categories);

    /// <summary>
    /// Filters the items to those in any of the specified inventory categories.
    /// </summary>
    /// <param name="categories">The inventory categories to keep, compared with <see cref="InventoryItem.Category"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfInventoryCategory(IEnumerable<int> categories)
    {
        HashSet<int> values = QueryValues.Set(categories);
        return Where(item => values.Contains(item.Category));
    }

    /// <summary>
    /// Filters the items to those in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <param name="lines">The furni lines to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfLine(params string[] lines) =>
        OfLine((IEnumerable<string>)lines);

    /// <summary>
    /// Filters the items to those in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <param name="lines">The furni lines to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfLine(IEnumerable<string> lines)
    {
        HashSet<string> values = QueryValues.Strings(lines);
        return Where(item => _metadata.Line(item, out string value) && values.Contains(value));
    }

    /// <summary>
    /// Filters out the items in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known line are dropped as well.
    /// </remarks>
    /// <param name="lines">The furni lines to drop.</param>
    /// <returns>A new query with the items whose line is known and not listed.</returns>
    public InventoryItemQuery NotOfLine(params string[] lines) =>
        NotOfLine((IEnumerable<string>)lines);

    /// <summary>
    /// Filters out the items in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known line are dropped as well.
    /// </remarks>
    /// <param name="lines">The furni lines to drop.</param>
    /// <returns>A new query with the items whose line is known and not listed.</returns>
    public InventoryItemQuery NotOfLine(IEnumerable<string> lines)
    {
        HashSet<string> values = QueryValues.Strings(lines);
        return Where(item => _metadata.Line(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those found in the furni data.
    /// </summary>
    /// <remarks>
    /// No item matches when no furni data is attached.
    /// </remarks>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery WithKnownMetadata() =>
        Where(item => _metadata.Info(item) is not null);

    /// <summary>
    /// Filters the items to those missing from the furni data.
    /// </summary>
    /// <remarks>
    /// Every item matches when no furni data is attached.
    /// </remarks>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery WithoutKnownMetadata() =>
        Where(item => _metadata.Info(item) is null);

    /// <summary>
    /// Filters the items to those in any of the specified states.
    /// </summary>
    /// <param name="states">The states to keep, compared with the state of <see cref="InventoryItem.Data"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfState(params int[] states) =>
        OfState((IEnumerable<int>)states);

    /// <summary>
    /// Filters the items to those in any of the specified states.
    /// </summary>
    /// <param name="states">The states to keep, compared with the state of <see cref="InventoryItem.Data"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery OfState(IEnumerable<int> states)
    {
        HashSet<int> values = QueryValues.Set(states);
        return Where(item => values.Contains(item.Data.State));
    }

    /// <summary>
    /// Filters out the items in any of the specified states.
    /// </summary>
    /// <param name="states">The states to drop, compared with the state of <see cref="InventoryItem.Data"/>.</param>
    /// <returns>A new query without the matching items.</returns>
    public InventoryItemQuery NotOfState(params int[] states) =>
        NotOfState((IEnumerable<int>)states);

    /// <summary>
    /// Filters out the items in any of the specified states.
    /// </summary>
    /// <param name="states">The states to drop, compared with the state of <see cref="InventoryItem.Data"/>.</param>
    /// <returns>A new query without the matching items.</returns>
    public InventoryItemQuery NotOfState(IEnumerable<int> states)
    {
        HashSet<int> values = QueryValues.Set(states);
        return Where(item => !values.Contains(item.Data.State));
    }

    /// <summary>
    /// Filters the items by whether they are rentals.
    /// </summary>
    /// <remarks>
    /// An item counts as a rental when its rent period has started or it has an expiration time
    /// of zero seconds or more.
    /// </remarks>
    /// <param name="value">Whether to keep rentals instead of other items.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Rental(bool value = true) =>
        Where(item => IsRental(item) == value);

    /// <summary>
    /// Filters the items by whether they can be traded.
    /// </summary>
    /// <param name="value">Whether to keep tradeable items instead of the others.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Tradeable(bool value = true) =>
        Where(item => item.IsTradeable == value);

    /// <summary>
    /// Filters the items by whether they can be sold.
    /// </summary>
    /// <param name="value">Whether to keep sellable items instead of the others.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Sellable(bool value = true) =>
        Where(item => item.IsSellable == value);

    /// <summary>
    /// Filters the items by whether they can be grouped in the inventory.
    /// </summary>
    /// <param name="value">Whether to keep groupable items instead of the others.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Groupable(bool value = true) =>
        Where(item => item.IsGroupable == value);

    /// <summary>
    /// Filters the items by whether they can be recycled.
    /// </summary>
    /// <param name="value">Whether to keep recyclable items instead of the others.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery Recyclable(bool value = true) =>
        Where(item => item.IsRecyclable == value);

    /// <summary>
    /// Filters the items to those whose <see cref="InventoryItem.RoomId"/> is the specified room id.
    /// </summary>
    /// <param name="roomId">The room id to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public InventoryItemQuery InRoom(Id roomId) =>
        Where(item => item.RoomId == roomId);

    private InventoryItemQuery Next(IEnumerable<InventoryItem> items) =>
        new(items, _furniData);

    private static bool IsRental(InventoryItem item) =>
        item.HasRentPeriodStarted || item.SecondsToExpiration >= 0;
}
