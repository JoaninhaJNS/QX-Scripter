using Qx;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

/// <summary>
/// Represents a query over group members.
/// </summary>
/// <remarks>
/// Every filter and sort returns a new query. Text matching ignores case,
/// <see langword="null"/> entries in text lists are skipped, and a <see langword="null"/>
/// argument throws <see cref="ArgumentNullException"/>.
/// </remarks>
public sealed class GuildMemberQuery : QueryCollection<GuildMember>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuildMemberQuery"/> class over the specified members.
    /// </summary>
    /// <param name="members">The group members to query.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="members"/> is <see langword="null"/>.</exception>
    public GuildMemberQuery(IEnumerable<GuildMember> members) : base(members)
    {
    }

    /// <summary>
    /// Filters the members with a predicate.
    /// </summary>
    /// <param name="predicate">The condition a member must meet to be kept.</param>
    /// <returns>A new query with the members that match <paramref name="predicate"/>.</returns>
    public GuildMemberQuery Where(Func<GuildMember, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Next(Items.Where(predicate));
    }

    /// <summary>
    /// Filters the members to those with any of the specified user ids.
    /// </summary>
    /// <param name="ids">The user ids to keep.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery ById(params Id[] ids) =>
        ById((IEnumerable<Id>)ids);

    /// <summary>
    /// Filters the members to those with any of the specified user ids.
    /// </summary>
    /// <param name="ids">The user ids to keep.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery ById(IEnumerable<Id> ids)
    {
        HashSet<Id> values = QueryValues.Set(ids);
        return Where(member => values.Contains(member.Id));
    }

    /// <summary>
    /// Filters the members to those with any of the specified names, ignoring case.
    /// </summary>
    /// <param name="names">The names to keep.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery Named(params string[] names) =>
        Named((IEnumerable<string>)names);

    /// <summary>
    /// Filters the members to those with any of the specified names, ignoring case.
    /// </summary>
    /// <param name="names">The names to keep.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery Named(IEnumerable<string> names)
    {
        HashSet<string> values = QueryValues.Strings(names);
        return Where(member => values.Contains(member.Name));
    }

    /// <summary>
    /// Filters the members to those whose name contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery NameContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(member => member.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the members to those of any of the specified member types.
    /// </summary>
    /// <param name="types">The member types to keep.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery OfType(params GuildMemberType[] types) =>
        OfType((IEnumerable<GuildMemberType>)types);

    /// <summary>
    /// Filters the members to those of any of the specified member types.
    /// </summary>
    /// <param name="types">The member types to keep.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery OfType(IEnumerable<GuildMemberType> types)
    {
        HashSet<GuildMemberType> values = QueryValues.Set(types);
        return Where(member => values.Contains(member.Type));
    }

    /// <summary>
    /// Filters the members to the group owners.
    /// </summary>
    /// <returns>A new query with the members of type <see cref="GuildMemberType.Owner"/>.</returns>
    public GuildMemberQuery Owners() =>
        OfType(GuildMemberType.Owner);

    /// <summary>
    /// Filters the members to the group administrators.
    /// </summary>
    /// <remarks>
    /// Owners are not included.
    /// </remarks>
    /// <returns>A new query with the members of type <see cref="GuildMemberType.Administrator"/>.</returns>
    public GuildMemberQuery Administrators() =>
        OfType(GuildMemberType.Administrator);

    /// <summary>
    /// Filters the members to the accepted members of the group.
    /// </summary>
    /// <remarks>
    /// Owners, administrators and regular members are kept; pending and blocked users are dropped.
    /// </remarks>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery Members() =>
        Where(member => member.IsMember);

    /// <summary>
    /// Filters the members to the users with a pending membership request.
    /// </summary>
    /// <returns>A new query with the members of type <see cref="GuildMemberType.Pending"/>.</returns>
    public GuildMemberQuery Pending() =>
        OfType(GuildMemberType.Pending);

    /// <summary>
    /// Filters the members to the blocked users.
    /// </summary>
    /// <returns>A new query with the members of type <see cref="GuildMemberType.Blocked"/>.</returns>
    public GuildMemberQuery Blocked() =>
        OfType(GuildMemberType.Blocked);

    /// <summary>
    /// Filters the members to those whose figure string contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery FigureContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(member => member.Figure.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters the members to those whose member since text contains the specified text, ignoring case.
    /// </summary>
    /// <param name="value">The text to search for in <see cref="GuildMember.MemberSince"/>.</param>
    /// <returns>A new query with the matching members.</returns>
    public GuildMemberQuery MemberSinceContains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Where(member => member.MemberSince.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Sorts the members by name, ignoring case.
    /// </summary>
    /// <remarks>
    /// Members with equal names keep their current order.
    /// </remarks>
    /// <returns>A new query with the sorted members.</returns>
    public GuildMemberQuery OrderByName() =>
        Next(Items.OrderBy(member => member.Name, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Sorts the members by member type, then by name ignoring case.
    /// </summary>
    /// <remarks>
    /// Types are sorted by their numeric value: owners, administrators, members, pending users,
    /// then blocked users.
    /// </remarks>
    /// <returns>A new query with the sorted members.</returns>
    public GuildMemberQuery OrderByType() =>
        Next(Items.OrderBy(member => member.Type).ThenBy(member => member.Name, StringComparer.OrdinalIgnoreCase));

    private static GuildMemberQuery Next(IEnumerable<GuildMember> members) => new(members);
}

/// <summary>
/// Provides extension methods that create group member queries.
/// </summary>
public static class GuildMemberQueryExtensions
{
    /// <summary>
    /// Creates a group member query over the members.
    /// </summary>
    /// <param name="members">The group members to query.</param>
    /// <returns>A new query over the members.</returns>
    public static GuildMemberQuery Query(this IEnumerable<GuildMember> members) =>
        new(members);
}
