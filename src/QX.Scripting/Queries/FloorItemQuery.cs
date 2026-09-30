using Qx;
using Qx.Game;
using Qx.Model;

namespace Qx.Scripting;

/// <summary>
/// Represents a snapshot query over floor items.
/// </summary>
/// <remarks>
/// Each item is copied when the query is created, so the query does not follow later room
/// updates. Every filter and sort returns a new query with the same furni data. Identifier,
/// name, category and line filters drop items whose value is unknown, the negated filters
/// included. Text matching ignores case, <see langword="null"/> entries in text lists are
/// skipped, and a <see langword="null"/> argument throws <see cref="ArgumentNullException"/>.
/// Area and distance filters use the footprint from <see cref="AreaOf(FloorItem)"/>.
/// </remarks>
public sealed class FloorItemQuery : QueryCollection<FloorItem>
{
    private readonly FurniData? _furniData;
    private readonly FurniMetadataResolver _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloorItemQuery"/> class over copies of the specified items.
    /// </summary>
    /// <param name="items">The floor items to query.</param>
    /// <param name="furniData">The furni data used for metadata filters and footprints, or <see langword="null"/> when it is not loaded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    public FloorItemQuery(IEnumerable<FloorItem> items, FurniData? furniData)
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
    public FloorItemQuery Where(Func<FloorItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Next(Items.Where(predicate));
    }

    /// <summary>
    /// Filters the items to those with any of the specified ids.
    /// </summary>
    /// <param name="ids">The item ids to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery ById(params Id[] ids) =>
        ById((IEnumerable<Id>)ids);

    /// <summary>
    /// Filters the items to those with any of the specified ids.
    /// </summary>
    /// <param name="ids">The item ids to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery ById(IEnumerable<Id> ids)
    {
        HashSet<Id> values = QueryValues.Set(ids);
        return Where(item => values.Contains(item.Id));
    }

    /// <summary>
    /// Filters the items to those of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfKind(params int[] kinds) =>
        OfKind((IEnumerable<int>)kinds);

    /// <summary>
    /// Filters the items to those of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfKind(IEnumerable<int> kinds)
    {
        HashSet<int> values = QueryValues.Set(kinds);
        return Where(item => values.Contains(item.Kind));
    }

    /// <summary>
    /// Filters out the items of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotOfKind(params int[] kinds) =>
        NotOfKind((IEnumerable<int>)kinds);

    /// <summary>
    /// Filters out the items of any of the specified furni kinds.
    /// </summary>
    /// <param name="kinds">The furni kinds to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotOfKind(IEnumerable<int> kinds)
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
    public FloorItemQuery OfIdentifier(params string[] identifiers) =>
        OfIdentifier((IEnumerable<string>)identifiers);

    /// <summary>
    /// Filters the items to those with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// The item's own identifier is used when it is set, otherwise the identifier from the furni data.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfIdentifier(IEnumerable<string> identifiers)
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
    public FloorItemQuery NotOfIdentifier(params string[] identifiers) =>
        NotOfIdentifier((IEnumerable<string>)identifiers);

    /// <summary>
    /// Filters out the items with any of the specified class identifiers, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known identifier are dropped as well.
    /// </remarks>
    /// <param name="identifiers">The class identifiers to drop.</param>
    /// <returns>A new query with the items whose identifier is known and not listed.</returns>
    public FloorItemQuery NotOfIdentifier(IEnumerable<string> identifiers)
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
    public FloorItemQuery Named(params string[] names) =>
        Named((IEnumerable<string>)names);

    /// <summary>
    /// Filters the items to those with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// The name is read from the furni data. When it is empty, the class identifier from the furni data is used.
    /// </remarks>
    /// <param name="names">The furni names to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Named(IEnumerable<string> names)
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
    public FloorItemQuery NotNamed(params string[] names) =>
        NotNamed((IEnumerable<string>)names);

    /// <summary>
    /// Filters out the items with any of the specified furni names, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known name are dropped as well.
    /// </remarks>
    /// <param name="names">The furni names to drop.</param>
    /// <returns>A new query with the items whose name is known and not listed.</returns>
    public FloorItemQuery NotNamed(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(item => _metadata.Name(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those whose furni name contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery NameContains(string value)
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
    public FloorItemQuery NotNameContains(string value)
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
    public FloorItemQuery OfCategory(params string[] categories) =>
        OfCategory((IEnumerable<string>)categories);

    /// <summary>
    /// Filters the items to those in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <param name="categories">The categories to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfCategory(IEnumerable<string> categories)
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
    public FloorItemQuery NotOfCategory(params string[] categories) =>
        NotOfCategory((IEnumerable<string>)categories);

    /// <summary>
    /// Filters out the items in any of the specified furni data categories, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known category are dropped as well.
    /// </remarks>
    /// <param name="categories">The categories to drop.</param>
    /// <returns>A new query with the items whose category is known and not listed.</returns>
    public FloorItemQuery NotOfCategory(IEnumerable<string> categories)
    {
        HashSet<string> values = QueryValues.Strings(categories);
        return Where(item => _metadata.Category(item, out string value) && !values.Contains(value));
    }

    /// <summary>
    /// Filters the items to those in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <param name="lines">The furni lines to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfLine(params string[] lines) =>
        OfLine((IEnumerable<string>)lines);

    /// <summary>
    /// Filters the items to those in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <param name="lines">The furni lines to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfLine(IEnumerable<string> lines)
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
    public FloorItemQuery NotOfLine(params string[] lines) =>
        NotOfLine((IEnumerable<string>)lines);

    /// <summary>
    /// Filters out the items in any of the specified furni lines, ignoring case.
    /// </summary>
    /// <remarks>
    /// Items without a known line are dropped as well.
    /// </remarks>
    /// <param name="lines">The furni lines to drop.</param>
    /// <returns>A new query with the items whose line is known and not listed.</returns>
    public FloorItemQuery NotOfLine(IEnumerable<string> lines)
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
    public FloorItemQuery WithKnownMetadata() =>
        Where(item => _metadata.Info(item) is not null);

    /// <summary>
    /// Filters the items to those missing from the furni data.
    /// </summary>
    /// <remarks>
    /// Every item matches when no furni data is attached.
    /// </remarks>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery WithoutKnownMetadata() =>
        Where(item => _metadata.Info(item) is null);

    /// <summary>
    /// Filters the items to those owned by the specified user id.
    /// </summary>
    /// <param name="ownerId">The owner's user id.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OwnedBy(Id ownerId) =>
        Where(item => item.OwnerId == ownerId);

    /// <summary>
    /// Filters the items to those whose owner has the specified name, ignoring case.
    /// </summary>
    /// <param name="ownerName">The owner's name.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OwnedBy(string ownerName)
    {
        ArgumentNullException.ThrowIfNull(ownerName);
        return Where(item => string.Equals(item.OwnerName, ownerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the items to those in any of the specified states.
    /// </summary>
    /// <param name="states">The states to keep, compared with <see cref="FloorItem.State"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfState(params int[] states) =>
        OfState((IEnumerable<int>)states);

    /// <summary>
    /// Filters the items to those in any of the specified states.
    /// </summary>
    /// <param name="states">The states to keep, compared with <see cref="FloorItem.State"/>.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery OfState(IEnumerable<int> states)
    {
        HashSet<int> values = QueryValues.Set(states);
        return Where(item => values.Contains(item.State));
    }

    /// <summary>
    /// Filters out the items in any of the specified states.
    /// </summary>
    /// <param name="states">The states to drop, compared with <see cref="FloorItem.State"/>.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotOfState(params int[] states) =>
        NotOfState((IEnumerable<int>)states);

    /// <summary>
    /// Filters out the items in any of the specified states.
    /// </summary>
    /// <param name="states">The states to drop, compared with <see cref="FloorItem.State"/>.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotOfState(IEnumerable<int> states)
    {
        HashSet<int> values = QueryValues.Set(states);
        return Where(item => !values.Contains(item.State));
    }

    /// <summary>
    /// Filters the items to those at the specified position.
    /// </summary>
    /// <remarks>
    /// The values are compared with the item's origin tile and direction, and only the values
    /// that are not <see langword="null"/> are compared. With no values, every item is kept.
    /// </remarks>
    /// <param name="x">The tile x coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="y">The tile y coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="z">The height, or <see langword="null"/> to match any.</param>
    /// <param name="direction">The direction, or <see langword="null"/> to match any.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query with the matching items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or <paramref name="z"/> is not finite.</exception>
    public FloorItemQuery At(
        int? x = null,
        int? y = null,
        float? z = null,
        int? direction = null,
        float epsilon = 0.001f)
    {
        ValidatePosition(z, epsilon);
        return Where(item => QueryValues.Position(item.Location, x, y, z, direction, item.Direction, epsilon));
    }

    /// <summary>
    /// Filters out the items at the specified position.
    /// </summary>
    /// <remarks>
    /// The values are compared with the item's origin tile and direction. An item is dropped only
    /// when every value that is not <see langword="null"/> matches. With no values, every item is kept.
    /// </remarks>
    /// <param name="x">The tile x coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="y">The tile y coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="z">The height, or <see langword="null"/> to match any.</param>
    /// <param name="direction">The direction, or <see langword="null"/> to match any.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query without the matching items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or <paramref name="z"/> is not finite.</exception>
    public FloorItemQuery NotAt(
        int? x = null,
        int? y = null,
        float? z = null,
        int? direction = null,
        float epsilon = 0.001f)
    {
        ValidatePosition(z, epsilon);
        if (!x.HasValue && !y.HasValue && !z.HasValue && !direction.HasValue)
            return Next(Items);
        return Where(item => !QueryValues.Position(item.Location, x, y, z, direction, item.Direction, epsilon));
    }

    /// <summary>
    /// Filters the items to those whose origin tile is the specified tile, at any height.
    /// </summary>
    /// <param name="point">The tile coordinates.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery At(Point point) =>
        At(point.X, point.Y);

    /// <summary>
    /// Filters the items to those whose origin tile and height match the specified tile.
    /// </summary>
    /// <param name="tile">The tile coordinates and height.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query with the matching items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or the tile height is not finite.</exception>
    public FloorItemQuery At(Tile tile, float epsilon = 0.001f) =>
        At(tile.X, tile.Y, tile.Z, epsilon: epsilon);

    /// <summary>
    /// Filters the items to those whose origin tile is any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery At(params Point[] points) =>
        At((IEnumerable<Point>)points);

    /// <summary>
    /// Filters the items to those whose origin tile is any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to keep.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery At(IEnumerable<Point> points)
    {
        HashSet<Point> values = QueryValues.Set(points);
        return Where(item => values.Contains(item.Location.XY));
    }

    /// <summary>
    /// Filters out the items whose origin tile is the specified tile, at any height.
    /// </summary>
    /// <param name="point">The tile coordinates.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotAt(Point point) =>
        NotAt(point.X, point.Y);

    /// <summary>
    /// Filters out the items whose origin tile and height match the specified tile.
    /// </summary>
    /// <param name="tile">The tile coordinates and height.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query without the matching items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or the tile height is not finite.</exception>
    public FloorItemQuery NotAt(Tile tile, float epsilon = 0.001f) =>
        NotAt(tile.X, tile.Y, tile.Z, epsilon: epsilon);

    /// <summary>
    /// Filters out the items whose origin tile is any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotAt(params Point[] points) =>
        NotAt((IEnumerable<Point>)points);

    /// <summary>
    /// Filters out the items whose origin tile is any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to drop.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotAt(IEnumerable<Point> points)
    {
        HashSet<Point> values = QueryValues.Set(points);
        return Where(item => !values.Contains(item.Location.XY));
    }

    /// <summary>
    /// Filters the items to those whose whole footprint lies inside the specified area.
    /// </summary>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Inside(Area area) =>
        Where(item => area.Contains(AreaOf(item)));

    /// <summary>
    /// Filters the items to those whose whole footprint is covered by the specified areas.
    /// </summary>
    /// <param name="areas">The areas to test.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Inside(AreaSet areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        return Where(item => areas.Contains(AreaOf(item)));
    }

    /// <summary>
    /// Filters out the items whose whole footprint lies inside the specified area.
    /// </summary>
    /// <remarks>
    /// Items that only partly overlap the area are kept.
    /// </remarks>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query without the matching items.</returns>
    public FloorItemQuery NotInside(Area area) =>
        Where(item => !area.Contains(AreaOf(item)));

    /// <summary>
    /// Filters the items to those whose footprint does not overlap the specified area.
    /// </summary>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Outside(Area area) =>
        Where(item => !area.Intersects(AreaOf(item)));

    /// <summary>
    /// Filters the items to those whose footprint does not overlap any of the specified areas.
    /// </summary>
    /// <param name="areas">The areas to test.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Outside(AreaSet areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        return Where(item => !areas.Intersects(AreaOf(item)));
    }

    /// <summary>
    /// Filters the items to those whose footprint overlaps the specified area.
    /// </summary>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Intersecting(Area area) =>
        Where(item => area.Intersects(AreaOf(item)));

    /// <summary>
    /// Filters the items to those whose footprint overlaps any of the specified areas.
    /// </summary>
    /// <param name="areas">The areas to test.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Intersecting(AreaSet areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        return Where(item => areas.Intersects(AreaOf(item)));
    }

    /// <summary>
    /// Filters the items to those whose footprint borders the specified tile.
    /// </summary>
    /// <remarks>
    /// Items that cover the tile are dropped.
    /// </remarks>
    /// <param name="point">The tile to test against.</param>
    /// <param name="diagonals">Whether diagonal neighbors count as adjacent.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery AdjacentTo(Point point, bool diagonals = true) =>
        Where(item => QueryValues.Adjacent(AreaOf(item), point, diagonals));

    /// <summary>
    /// Filters the items to those whose footprint borders the specified area.
    /// </summary>
    /// <remarks>
    /// Items that overlap the area are dropped.
    /// </remarks>
    /// <param name="area">The area to test against.</param>
    /// <param name="diagonals">Whether diagonal neighbors count as adjacent.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery AdjacentTo(Area area, bool diagonals = true) =>
        Where(item => QueryValues.Adjacent(AreaOf(item), area, diagonals));

    /// <summary>
    /// Filters the items to those whose footprint is within the specified Euclidean distance of a tile.
    /// </summary>
    /// <remarks>
    /// The distance is measured to the nearest tile of the footprint, so an item that covers the tile has distance zero.
    /// </remarks>
    /// <param name="point">The tile to measure from.</param>
    /// <param name="distance">The largest distance in tiles, inclusive.</param>
    /// <returns>A new query with the matching items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="distance"/> is negative or not finite.</exception>
    public FloorItemQuery WithinDistance(Point point, double distance)
    {
        if (!double.IsFinite(distance) || distance < 0)
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance must be finite and non-negative.");
        double squared = distance * distance;
        return Where(item => QueryValues.DistanceSquared(AreaOf(item), point) <= squared);
    }

    /// <summary>
    /// Sorts the items by Euclidean distance from their footprint to the specified tile, nearest first.
    /// </summary>
    /// <remarks>
    /// Items at the same distance keep their current order.
    /// </remarks>
    /// <param name="point">The tile to measure from.</param>
    /// <returns>A new query with the sorted items.</returns>
    public FloorItemQuery OrderByDistanceTo(Point point) =>
        Next(Items.OrderBy(item => QueryValues.DistanceSquared(AreaOf(item), point)));

    /// <summary>
    /// Gets the item whose footprint is nearest to the specified tile by Euclidean distance.
    /// </summary>
    /// <remarks>
    /// When several items are equally near, the first one in the query is returned.
    /// </remarks>
    /// <param name="point">The tile to measure from.</param>
    /// <returns>The nearest item, or <see langword="null"/> when the query is empty.</returns>
    public FloorItem? NearestTo(Point point) =>
        Items.MinBy(item => QueryValues.DistanceSquared(AreaOf(item), point));

    /// <summary>
    /// Filters the items by whether they are hidden on the client.
    /// </summary>
    /// <param name="value">Whether to keep hidden items instead of visible ones.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Hidden(bool value = true) =>
        Where(item => item.IsHidden == value);

    /// <summary>
    /// Filters the items by whether they have been removed from the room.
    /// </summary>
    /// <param name="value">Whether to keep removed items instead of items still in the room.</param>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Removed(bool value = true) =>
        Where(item => item.IsRemoved == value);

    /// <summary>
    /// Filters the items to those that have not been removed from the room.
    /// </summary>
    /// <returns>A new query with the matching items.</returns>
    public FloorItemQuery Present() =>
        Removed(false);

    /// <summary>
    /// Gets the tiles the specified floor item covers.
    /// </summary>
    /// <remarks>
    /// The size comes from the furni data when the item is found there, otherwise from the item
    /// itself, and a size below one counts as one. Width and length are swapped when the
    /// direction modulo 4 is 2, such as directions 2 and 6.
    /// </remarks>
    /// <param name="item">The floor item.</param>
    /// <returns>The item's footprint, starting at its location.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public Area AreaOf(FloorItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _metadata.Area(item);
    }

    private FloorItemQuery Next(IEnumerable<FloorItem> items) =>
        new(items, _furniData);

    private static void ValidatePosition(float? z, float epsilon)
    {
        QueryValues.Epsilon(epsilon);
        if (z.HasValue && !float.IsFinite(z.Value))
            throw new ArgumentOutOfRangeException(nameof(z), z, "Z must be finite.");
    }
}
