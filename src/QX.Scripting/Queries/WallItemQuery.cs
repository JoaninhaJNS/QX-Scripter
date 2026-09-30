using Qx;
using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

/// <summary>
/// Represents a snapshot query over wall items.
/// </summary>
/// <remarks>
/// Each item is copied when the query is created, so the query does not follow later room
/// updates. Every filter and sort returns a new query with the same furni data. Identifier,
/// name, category and line filters drop items whose value is unknown, the negated filters
/// included. Text matching ignores case, <see langword="null"/> entries in text lists are
/// skipped, and a <see langword="null"/> argument throws <see cref="ArgumentNullException"/>.
/// </remarks>
public sealed class WallItemQuery : QueryCollection<WallItem>
{
    private readonly FurniData? _furniData;
    private readonly FurniMetadataResolver _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="WallItemQuery"/> class over copies of the specified items.
    /// </summary>
    /// <param name="items">The wall items to query.</param>
    /// <param name="furniData">The furni data used for metadata filters, or <see langword="null"/> when it is not loaded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    public WallItemQuery(IEnumerable<WallItem> items, FurniData? furniData)
        : base(items, RoomObjectSnapshot.Copy)
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
    public WallItemQuery Where(Func<WallItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Next(Items.Where(predicate));
    }

    /// <summary>
    /// Filters the items to those with any of the specified ids.
    /// </summary>
    /// <param name="ids">The item ids to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery ById(params Id[] ids) =>
        ById((IEnumerable<Id>)ids);

    /// <summary>
    /// Filters the items to those with any of the specified ids.
    /// </summary>
    /// <param name="ids">The item ids to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery ById(IEnumerable<Id> ids)
    {
        HashSet<Id> values = QueryValues.Set(ids);
        return Where(item => values.Contains(item.Id));
    }

    /// <summary>
    /// Filters the items to those of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfKind(params int[] kinds) =>
        OfKind((IEnumerable<int>)kinds);

    /// <summary>
    /// Filters the items to those of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfKind(IEnumerable<int> kinds)
    {
        HashSet<int> values = QueryValues.Set(kinds);
        return Where(item => values.Contains(item.Kind));
    }

    /// <summary>
    /// Filters out the items of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public WallItemQuery NotOfKind(params int[] kinds) =>
        NotOfKind((IEnumerable<int>)kinds);

    /// <summary>
    /// Filters out the items of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public WallItemQuery NotOfKind(IEnumerable<int> kinds)
    {
        HashSet<int> values = QueryValues.Set(kinds);
        return Where(item => !values.Contains(item.Kind));
    }

    /// <summary>
    /// Filters the items to those with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// The item's own identifier is used when it is set, otherwise the identifier from the furni data.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfIdentifier(params string[] identifiers) =>
        OfIdentifier((IEnumerable<string>)identifiers);

    /// <summary>
    /// Filters the items to those with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// The item's own identifier is used when it is set, otherwise the identifier from the furni data.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfIdentifier(IEnumerable<string> identifiers)
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
    public WallItemQuery NotOfIdentifier(params string[] identifiers) =>
        NotOfIdentifier((IEnumerable<string>)identifiers);

    /// <summary>
    /// Filters out the items with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known identifier are dropped as well.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to drop.</param>
    /// <returns>A new query with the items whose identifier is known and not listed.</returns>
    public WallItemQuery NotOfIdentifier(IEnumerable<string> identifiers)
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
    public WallItemQuery Named(params string[] names) =>
        Named((IEnumerable<string>)names);

    /// <summary>
    /// Filters the items to those with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// The name is read from the furni data. When it is empty, the class identifier from the furni data is used.
    /// </remarks>
    /// <param name="names">The furni names to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery Named(IEnumerable<string> names)
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
    public WallItemQuery NotNamed(params string[] names) =>
        NotNamed((IEnumerable<string>)names);

    /// <summary>
    /// Filters out the items with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known name are dropped as well.
    /// </remarks>
    /// <param name="names">The furni names to drop.</param>
    /// <returns>A new query with the items whose name is known and not listed.</returns>
    public WallItemQuery NotNamed(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(item => _metadata.Name(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those whose furni name contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery NameContains(string value)
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
    public WallItemQuery NotNameContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(item =>
            _metadata.Name(item, out string name) &&
            !name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the items to those in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <param name="categories">The categories to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfCategory(params string[] categories) =>
        OfCategory((IEnumerable<string>)categories);

    /// <summary>
    /// Filters the items to those in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <param name="categories">The categories to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfCategory(IEnumerable<string> categories)
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
    public WallItemQuery NotOfCategory(params string[] categories) =>
        NotOfCategory((IEnumerable<string>)categories);

    /// <summary>
    /// Filters out the items in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known category are dropped as well.
    /// </remarks>
    /// <param name="categories">The categories to drop.</param>
    /// <returns>A new query with the items whose category is known and not listed.</returns>
    public WallItemQuery NotOfCategory(IEnumerable<string> categories)
    {
        HashSet<string> values = QueryValues.Strings(categories);
        return Where(item => _metadata.Category(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <param name="lines">The furni lines to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfLine(params string[] lines) =>
        OfLine((IEnumerable<string>)lines);

    /// <summary>
    /// Filters the items to those in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <param name="lines">The furni lines to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfLine(IEnumerable<string> lines)
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
    public WallItemQuery NotOfLine(params string[] lines) =>
        NotOfLine((IEnumerable<string>)lines);

    /// <summary>
    /// Filters out the items in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known line are dropped as well.
    /// </remarks>
    /// <param name="lines">The furni lines to drop.</param>
    /// <returns>A new query with the items whose line is known and not listed.</returns>
    public WallItemQuery NotOfLine(IEnumerable<string> lines)
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
    public WallItemQuery WithKnownMetadata() =>
        Where(item => _metadata.Info(item) is not null);

    /// <summary>
    /// Filters the items to those missing from the furni data.
    /// </summary>
    /// <remarks>
    /// Every item matches when no furni data is attached.
    /// </remarks>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery WithoutKnownMetadata() =>
        Where(item => _metadata.Info(item) is null);

    /// <summary>
    /// Filters the items to those owned by the specified user id.
    /// </summary>
    /// <param name="ownerId">The owner's user id.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OwnedBy(Id ownerId) =>
        Where(item => item.OwnerId == ownerId);

    /// <summary>
    /// Filters the items to those whose owner has the specified name, ignoring case.
    /// </summary>
    /// <param name="ownerName">The owner's name.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OwnedBy(string ownerName)
    {
        ArgumentNullException.ThrowIfNull(ownerName);
        return Where(item => string.Equals(item.OwnerName, ownerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the items to those in any of the specified states.
    /// </summary>
    /// <param name="states">The states to keep, compared with <see cref="WallItem.State"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfState(params int[] states) =>
        OfState((IEnumerable<int>)states);

    /// <summary>
    /// Filters the items to those in any of the specified states.
    /// </summary>
    /// <param name="states">The states to keep, compared with <see cref="WallItem.State"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfState(IEnumerable<int> states)
    {
        HashSet<int> values = QueryValues.Set(states);
        return Where(item => values.Contains(item.State));
    }

    /// <summary>
    /// Filters out the items in any of the specified states.
    /// </summary>
    /// <param name="states">The states to drop, compared with <see cref="WallItem.State"/>.</param>
    /// <returns>A new query without the matching items.</returns>
    public WallItemQuery NotOfState(params int[] states) =>
        NotOfState((IEnumerable<int>)states);

    /// <summary>
    /// Filters out the items in any of the specified states.
    /// </summary>
    /// <param name="states">The states to drop, compared with <see cref="WallItem.State"/>.</param>
    /// <returns>A new query without the matching items.</returns>
    public WallItemQuery NotOfState(IEnumerable<int> states)
    {
        HashSet<int> values = QueryValues.Set(states);
        return Where(item => !values.Contains(item.State));
    }

    /// <summary>
    /// Filters the items to those at the specified wall location.
    /// </summary>
    /// <remarks>
    /// Only the values that are not <see langword="null"/> are compared. With no values, every item is kept.
    /// </remarks>
    /// <param name="wallX">The wall x coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="wallY">The wall y coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="offsetX">The local x offset, or <see langword="null"/> to match any.</param>
    /// <param name="offsetY">The local y offset, or <see langword="null"/> to match any.</param>
    /// <param name="orientation">The wall orientation, or <see langword="null"/> to match any.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery At(
        int? wallX = null,
        int? wallY = null,
        int? offsetX = null,
        int? offsetY = null,
        WallOrientation? orientation = null) =>
        Where(item =>
            (!wallX.HasValue || item.WX == wallX.Value) &&
            (!wallY.HasValue || item.WY == wallY.Value) &&
            (!offsetX.HasValue || item.LX == offsetX.Value) &&
            (!offsetY.HasValue || item.LY == offsetY.Value) &&
            (!orientation.HasValue || item.Orientation == orientation.Value));

    /// <summary>
    /// Filters out the items at the specified wall location.
    /// </summary>
    /// <remarks>
    /// An item is dropped only when every value that is not <see langword="null"/> matches.
    /// With no values, every item is kept.
    /// </remarks>
    /// <param name="wallX">The wall x coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="wallY">The wall y coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="offsetX">The local x offset, or <see langword="null"/> to match any.</param>
    /// <param name="offsetY">The local y offset, or <see langword="null"/> to match any.</param>
    /// <param name="orientation">The wall orientation, or <see langword="null"/> to match any.</param>
    /// <returns>A new query without the matching items.</returns>
    public WallItemQuery NotAt(
        int? wallX = null,
        int? wallY = null,
        int? offsetX = null,
        int? offsetY = null,
        WallOrientation? orientation = null) =>
        !wallX.HasValue &&
        !wallY.HasValue &&
        !offsetX.HasValue &&
        !offsetY.HasValue &&
        !orientation.HasValue
            ? Next(Items)
            : Where(item =>
                !(
                (!wallX.HasValue || item.WX == wallX.Value) &&
                (!wallY.HasValue || item.WY == wallY.Value) &&
                (!offsetX.HasValue || item.LX == offsetX.Value) &&
                (!offsetY.HasValue || item.LY == offsetY.Value) &&
                (!orientation.HasValue || item.Orientation == orientation.Value)
                ));

    /// <summary>
    /// Filters the items to those at exactly the specified wall location.
    /// </summary>
    /// <param name="location">The wall coordinates, offset and orientation to match.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery At(WallLocation location) =>
        At(
            location.Wall.X,
            location.Wall.Y,
            location.Offset.X,
            location.Offset.Y,
            location.Orientation);

    /// <summary>
    /// Filters out the items at exactly the specified wall location.
    /// </summary>
    /// <param name="location">The wall coordinates, offset and orientation to match.</param>
    /// <returns>A new query without the matching items.</returns>
    public WallItemQuery NotAt(WallLocation location) =>
        NotAt(
            location.Wall.X,
            location.Wall.Y,
            location.Offset.X,
            location.Offset.Y,
            location.Orientation);

    /// <summary>
    /// Filters the items to those with the specified wall orientation.
    /// </summary>
    /// <param name="orientation">The wall orientation to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery OfOrientation(WallOrientation orientation) =>
        Where(item => item.Orientation == orientation);

    /// <summary>
    /// Sorts the items by Euclidean distance from their local offset to the specified point, nearest first.
    /// </summary>
    /// <remarks>
    /// Only the local offset is compared; the wall coordinates are ignored. Items at the same
    /// distance keep their current order.
    /// </remarks>
    /// <param name="point">The local offset to measure from.</param>
    /// <returns>A new query with the sorted items.</returns>
    public WallItemQuery OrderByDistanceToOffset(Point point) =>
        Next(Items.OrderBy(item => QueryValues.DistanceSquared(item.Location.Offset, point)));

    /// <summary>
    /// Gets the item whose local offset is nearest to the specified point by Euclidean distance.
    /// </summary>
    /// <remarks>
    /// Only the local offset is compared; the wall coordinates are ignored. When several items
    /// are equally near, the first one in the query is returned.
    /// </remarks>
    /// <param name="point">The local offset to measure from.</param>
    /// <returns>The nearest item, or <see langword="null"/> when the query is empty.</returns>
    public WallItem? NearestToOffset(Point point) =>
        Items.MinBy(item => QueryValues.DistanceSquared(item.Location.Offset, point));

    /// <summary>
    /// Gets the item nearest to the specified wall location.
    /// </summary>
    /// <remarks>
    /// The distance is the squared distance between the wall coordinates plus the squared
    /// distance between the local offsets. The orientation is ignored. When several items are
    /// equally near, the first one in the query is returned.
    /// </remarks>
    /// <param name="location">The wall location to measure from.</param>
    /// <returns>The nearest item, or <see langword="null"/> when the query is empty.</returns>
    public WallItem? NearestTo(WallLocation location) =>
        Items.MinBy(item =>
            QueryValues.DistanceSquared(item.Location.Wall, location.Wall) +
            QueryValues.DistanceSquared(item.Location.Offset, location.Offset));

    /// <summary>
    /// Filters the items by whether they are hidden on the client.
    /// </summary>
    /// <param name="value">Whether to keep hidden items instead of visible ones.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery Hidden(bool value = true) =>
        Where(item => item.IsHidden == value);

    /// <summary>
    /// Filters the items by whether they have been removed from the room.
    /// </summary>
    /// <param name="value">Whether to keep removed items instead of items still in the room.</param>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery Removed(bool value = true) =>
        Where(item => item.IsRemoved == value);

    /// <summary>
    /// Filters the items to those that have not been removed from the room.
    /// </summary>
    /// <returns>A new query with the matching items.</returns>
    public WallItemQuery Present() =>
        Removed(false);

    private WallItemQuery Next(IEnumerable<WallItem> items) =>
        new(items, _furniData);
}
