using Qx.Model;

namespace Qx.Scripting;

/// <summary>
/// Represents a snapshot query over room avatars.
/// </summary>
/// <remarks>
/// Each avatar is copied when the query is created, so the query does not follow later room
/// updates. Every filter and sort returns a new query. Name matching ignores case,
/// <see langword="null"/> entries in name lists are skipped, and a <see langword="null"/>
/// argument throws <see cref="ArgumentNullException"/>.
/// </remarks>
public sealed class AvatarQuery : QueryCollection<Avatar>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AvatarQuery"/> class over copies of the specified avatars.
    /// </summary>
    /// <param name="avatars">The avatars to query.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="avatars"/> is <see langword="null"/>.</exception>
    public AvatarQuery(IEnumerable<Avatar> avatars)
        : base(avatars, RoomObjectSnapshot.Copy)
    {
    }

    /// <summary>
    /// Filters the avatars with a predicate.
    /// </summary>
    /// <param name="predicate">The condition an avatar must meet to be kept.</param>
    /// <returns>A new query with the avatars that match <paramref name="predicate"/>.</returns>
    public AvatarQuery Where(Func<Avatar, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new AvatarQuery(Items.Where(predicate));
    }

    /// <summary>
    /// Filters the avatars to those with any of the specified ids.
    /// </summary>
    /// <param name="ids">The avatar ids to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery ById(params Id[] ids) =>
        ById((IEnumerable<Id>)ids);

    /// <summary>
    /// Filters the avatars to those with any of the specified ids.
    /// </summary>
    /// <param name="ids">The avatar ids to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery ById(IEnumerable<Id> ids)
    {
        HashSet<Id> values = QueryValues.Set(ids);
        return Where(avatar => values.Contains(avatar.Id));
    }

    /// <summary>
    /// Filters the avatars to those with any of the specified room indexes.
    /// </summary>
    /// <param name="indices">The room indexes to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery ByIndex(params int[] indices) =>
        ByIndex((IEnumerable<int>)indices);

    /// <summary>
    /// Filters the avatars to those with any of the specified room indexes.
    /// </summary>
    /// <param name="indices">The room indexes to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery ByIndex(IEnumerable<int> indices)
    {
        HashSet<int> values = QueryValues.Set(indices);
        return Where(avatar => values.Contains(avatar.Index));
    }

    /// <summary>
    /// Filters the avatars to those with any of the specified names, ignoring case.
    /// </summary>
    /// <param name="names">The names to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Named(params string[] names) =>
        Named((IEnumerable<string>)names);

    /// <summary>
    /// Filters the avatars to those with any of the specified names, ignoring case.
    /// </summary>
    /// <param name="names">The names to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Named(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(avatar => values.Contains(avatar.Name));
    }

    /// <summary>
    /// Filters out the avatars with any of the specified names, ignoring case.
    /// </summary>
    /// <param name="names">The names to drop.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotNamed(params string[] names) =>
        NotNamed((IEnumerable<string>)names);

    /// <summary>
    /// Filters out the avatars with any of the specified names, ignoring case.
    /// </summary>
    /// <param name="names">The names to drop.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotNamed(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(avatar => !values.Contains(avatar.Name));
    }

    /// <summary>
    /// Filters the avatars to those whose name contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery NameContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(avatar => avatar.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters out the avatars whose name contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotNameContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(avatar => !avatar.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the avatars to those of any of the specified types.
    /// </summary>
    /// <param name="types">The avatar types to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery OfType(params AvatarType[] types) =>
        OfType((IEnumerable<AvatarType>)types);

    /// <summary>
    /// Filters the avatars to those of any of the specified types.
    /// </summary>
    /// <param name="types">The avatar types to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery OfType(IEnumerable<AvatarType> types)
    {
        HashSet<AvatarType> values = QueryValues.Set(types);
        return Where(avatar => values.Contains(avatar.Type));
    }

    /// <summary>
    /// Filters out the avatars of any of the specified types.
    /// </summary>
    /// <param name="types">The avatar types to drop.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotOfType(params AvatarType[] types) =>
        NotOfType((IEnumerable<AvatarType>)types);

    /// <summary>
    /// Filters out the avatars of any of the specified types.
    /// </summary>
    /// <param name="types">The avatar types to drop.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotOfType(IEnumerable<AvatarType> types)
    {
        HashSet<AvatarType> values = QueryValues.Set(types);
        return Where(avatar => !values.Contains(avatar.Type));
    }

    /// <summary>
    /// Filters the avatars to the pets and bots owned by the specified user id.
    /// </summary>
    /// <remarks>
    /// Users have no owner and are always dropped.
    /// </remarks>
    /// <param name="ownerId">The owner's user id.</param>
    /// <returns>A new query with the matching pets and bots.</returns>
    public AvatarQuery OwnedBy(Id ownerId) =>
        Where(avatar => avatar switch
        {
            Pet pet => pet.OwnerId == ownerId,
            Bot bot => bot.OwnerId == ownerId,
            _ => false
        });

    /// <summary>
    /// Filters the avatars to the pets and bots whose owner has the specified name, ignoring case.
    /// </summary>
    /// <remarks>
    /// Users have no owner and are always dropped.
    /// </remarks>
    /// <param name="ownerName">The owner's name.</param>
    /// <returns>A new query with the matching pets and bots.</returns>
    public AvatarQuery OwnedBy(string ownerName)
    {
        ArgumentNullException.ThrowIfNull(ownerName);
        return Where(avatar => avatar switch
        {
            Pet pet => string.Equals(pet.OwnerName, ownerName, StringComparison.OrdinalIgnoreCase),
            Bot bot => string.Equals(bot.OwnerName, ownerName, StringComparison.OrdinalIgnoreCase),
            _ => false
        });
    }

    /// <summary>
    /// Filters the avatars to those at the specified position.
    /// </summary>
    /// <remarks>
    /// Only the values that are not <see langword="null"/> are compared. With no values, every avatar is kept.
    /// </remarks>
    /// <param name="x">The tile x coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="y">The tile y coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="z">The height, or <see langword="null"/> to match any.</param>
    /// <param name="direction">The body direction, or <see langword="null"/> to match any.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query with the matching avatars.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or <paramref name="z"/> is not finite.</exception>
    public AvatarQuery At(
        int? x = null,
        int? y = null,
        float? z = null,
        int? direction = null,
        float epsilon = 0.001f)
    {
        ValidatePosition(z, epsilon);
        return Where(avatar => QueryValues.Position(avatar.Location, x, y, z, direction, avatar.Direction, epsilon));
    }

    /// <summary>
    /// Filters out the avatars at the specified position.
    /// </summary>
    /// <remarks>
    /// An avatar is dropped only when every value that is not <see langword="null"/> matches.
    /// With no values, every avatar is kept.
    /// </remarks>
    /// <param name="x">The tile x coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="y">The tile y coordinate, or <see langword="null"/> to match any.</param>
    /// <param name="z">The height, or <see langword="null"/> to match any.</param>
    /// <param name="direction">The body direction, or <see langword="null"/> to match any.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query without the matching avatars.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or <paramref name="z"/> is not finite.</exception>
    public AvatarQuery NotAt(
        int? x = null,
        int? y = null,
        float? z = null,
        int? direction = null,
        float epsilon = 0.001f)
    {
        ValidatePosition(z, epsilon);
        if (!x.HasValue && !y.HasValue && !z.HasValue && !direction.HasValue)
            return new AvatarQuery(Items);
        return Where(avatar => !QueryValues.Position(avatar.Location, x, y, z, direction, avatar.Direction, epsilon));
    }

    /// <summary>
    /// Filters the avatars to those on the specified tile, at any height.
    /// </summary>
    /// <param name="point">The tile coordinates.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery At(Point point) =>
        At(point.X, point.Y);

    /// <summary>
    /// Filters the avatars to those on the specified tile and height.
    /// </summary>
    /// <param name="tile">The tile coordinates and height.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query with the matching avatars.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or the tile height is not finite.</exception>
    public AvatarQuery At(Tile tile, float epsilon = 0.001f) =>
        At(tile.X, tile.Y, tile.Z, epsilon: epsilon);

    /// <summary>
    /// Filters the avatars to those on any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery At(params Point[] points) =>
        At((IEnumerable<Point>)points);

    /// <summary>
    /// Filters the avatars to those on any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to keep.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery At(IEnumerable<Point> points)
    {
        HashSet<Point> values = QueryValues.Set(points);
        return Where(avatar => values.Contains(avatar.XY));
    }

    /// <summary>
    /// Filters out the avatars on the specified tile, at any height.
    /// </summary>
    /// <param name="point">The tile coordinates.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotAt(Point point) =>
        NotAt(point.X, point.Y);

    /// <summary>
    /// Filters out the avatars on the specified tile and height.
    /// </summary>
    /// <param name="tile">The tile coordinates and height.</param>
    /// <param name="epsilon">The largest height difference that still counts as a match.</param>
    /// <returns>A new query without the matching avatars.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="epsilon"/> is negative or not finite, or the tile height is not finite.</exception>
    public AvatarQuery NotAt(Tile tile, float epsilon = 0.001f) =>
        NotAt(tile.X, tile.Y, tile.Z, epsilon: epsilon);

    /// <summary>
    /// Filters out the avatars on any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to drop.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotAt(params Point[] points) =>
        NotAt((IEnumerable<Point>)points);

    /// <summary>
    /// Filters out the avatars on any of the specified tiles.
    /// </summary>
    /// <param name="points">The tile coordinates to drop.</param>
    /// <returns>A new query without the matching avatars.</returns>
    public AvatarQuery NotAt(IEnumerable<Point> points)
    {
        HashSet<Point> values = QueryValues.Set(points);
        return Where(avatar => !values.Contains(avatar.XY));
    }

    /// <summary>
    /// Filters the avatars to those standing inside the specified area.
    /// </summary>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Inside(Area area) =>
        Where(avatar => area.Contains(avatar.Location));

    /// <summary>
    /// Filters the avatars to those standing on a tile covered by the specified areas.
    /// </summary>
    /// <param name="areas">The areas to test.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Inside(AreaSet areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        return Where(avatar => areas.Contains(avatar.Location));
    }

    /// <summary>
    /// Filters the avatars to those standing outside the specified area.
    /// </summary>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Outside(Area area) =>
        Where(avatar => !area.Contains(avatar.Location));

    /// <summary>
    /// Filters the avatars to those standing on a tile not covered by any of the specified areas.
    /// </summary>
    /// <param name="areas">The areas to test.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Outside(AreaSet areas)
    {
        ArgumentNullException.ThrowIfNull(areas);
        return Where(avatar => !areas.Contains(avatar.Location));
    }

    /// <summary>
    /// Filters the avatars to those standing inside the specified area.
    /// </summary>
    /// <remarks>
    /// An avatar occupies a single tile, so the result is the same as <see cref="Inside(Area)"/>.
    /// </remarks>
    /// <param name="area">The area to test.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Intersecting(Area area) =>
        Inside(area);

    /// <summary>
    /// Filters the avatars to those standing next to the specified tile.
    /// </summary>
    /// <remarks>
    /// Avatars on the tile itself are dropped.
    /// </remarks>
    /// <param name="point">The tile to test against.</param>
    /// <param name="diagonals">Whether diagonal neighbors count as adjacent.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery AdjacentTo(Point point, bool diagonals = true) =>
        Where(avatar => QueryValues.Adjacent(new Area(avatar.Location), point, diagonals));

    /// <summary>
    /// Filters the avatars to those standing next to the edge of the specified area.
    /// </summary>
    /// <remarks>
    /// Avatars inside the area are dropped.
    /// </remarks>
    /// <param name="area">The area to test against.</param>
    /// <param name="diagonals">Whether diagonal neighbors count as adjacent.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery AdjacentTo(Area area, bool diagonals = true) =>
        Where(avatar => QueryValues.Adjacent(new Area(avatar.Location), area, diagonals));

    /// <summary>
    /// Filters the avatars to those within the specified Euclidean distance of a tile.
    /// </summary>
    /// <param name="point">The tile to measure from.</param>
    /// <param name="distance">The largest distance in tiles, inclusive.</param>
    /// <returns>A new query with the matching avatars.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="distance"/> is negative or not finite.</exception>
    public AvatarQuery WithinDistance(Point point, double distance)
    {
        if (!double.IsFinite(distance) || distance < 0)
            throw new ArgumentOutOfRangeException(nameof(distance), distance, "Distance must be finite and non-negative.");
        double squared = distance * distance;
        return Where(avatar => QueryValues.DistanceSquared(avatar.XY, point) <= squared);
    }

    /// <summary>
    /// Sorts the avatars by Euclidean distance to the specified tile, nearest first.
    /// </summary>
    /// <remarks>
    /// Avatars at the same distance keep their current order.
    /// </remarks>
    /// <param name="point">The tile to measure from.</param>
    /// <returns>A new query with the sorted avatars.</returns>
    public AvatarQuery OrderByDistanceTo(Point point) =>
        new(Items.OrderBy(avatar => QueryValues.DistanceSquared(avatar.XY, point)));

    /// <summary>
    /// Gets the avatar nearest to the specified tile by Euclidean distance.
    /// </summary>
    /// <remarks>
    /// When several avatars are equally near, the first one in the query is returned.
    /// </remarks>
    /// <param name="point">The tile to measure from.</param>
    /// <returns>The nearest avatar, or <see langword="null"/> when the query is empty.</returns>
    public Avatar? NearestTo(Point point) =>
        Items.MinBy(avatar => QueryValues.DistanceSquared(avatar.XY, point));

    /// <summary>
    /// Filters the avatars by whether they have been removed from the room.
    /// </summary>
    /// <param name="value">Whether to keep removed avatars instead of avatars still in the room.</param>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Removed(bool value = true) =>
        Where(avatar => avatar.IsRemoved == value);

    /// <summary>
    /// Filters the avatars to those that have not been removed from the room.
    /// </summary>
    /// <returns>A new query with the matching avatars.</returns>
    public AvatarQuery Present() =>
        Removed(false);

    private static void ValidatePosition(float? z, float epsilon)
    {
        QueryValues.Epsilon(epsilon);
        if (z.HasValue && !float.IsFinite(z.Value))
            throw new ArgumentOutOfRangeException(nameof(z), z, "Z must be finite.");
    }
}
