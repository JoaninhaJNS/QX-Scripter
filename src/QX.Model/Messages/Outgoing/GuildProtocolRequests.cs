using Qx.Messages;
using Qx.Model.Messages.Incoming;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Sent when the user joins a group or asks to join it.</summary>
/// <remarks>Sent as the Flash <c>JoinHabboGroup</c> message.</remarks>
/// <param name="GroupId">The id of the group, written as a 32 bit integer.</param>
public sealed record JoinGroupRequest(Id GroupId)
    : IParserComposer<JoinGroupRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static JoinGroupRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static JoinGroupRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(JoinGroupRequest value, in PacketWriter p) =>
        p.WriteInt(checked((int)value.GroupId));
}

/// <summary>Sent when a group admin removes a member from a group.</summary>
/// <remarks>Sent as the Flash <c>KickMember</c> message.</remarks>
/// <param name="GroupId">The id of the group, written as a 32 bit integer.</param>
/// <param name="UserId">The user id of the member to remove, written as a 32 bit integer.</param>
/// <param name="BlockRejoin">Whether the member is also blocked from joining again.</param>
public sealed record KickGroupMemberRequest(
    Id GroupId,
    Id UserId,
    bool BlockRejoin) : IParserComposer<KickGroupMemberRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static KickGroupMemberRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static KickGroupMemberRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt(), p.ReadBool());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(KickGroupMemberRequest value, in PacketWriter p)
    {
        int group_id = checked((int)value.GroupId);
        int user_id = checked((int)value.UserId);
        p.WriteInt(group_id);
        p.WriteInt(user_id);
        p.WriteBool(value.BlockRejoin);
    }
}

/// <summary>Sent when a group admin approves a pending membership request.</summary>
/// <remarks>Sent as the Flash <c>ApproveMembershipRequest</c> message.</remarks>
/// <param name="GroupId">The id of the group, written as a 32 bit integer.</param>
/// <param name="UserId">The user id of the applicant, written as a 32 bit integer.</param>
public sealed record ApproveGroupMemberRequest(Id GroupId, Id UserId)
    : IParserComposer<ApproveGroupMemberRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ApproveGroupMemberRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ApproveGroupMemberRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ApproveGroupMemberRequest value, in PacketWriter p)
    {
        int group_id = checked((int)value.GroupId);
        int user_id = checked((int)value.UserId);
        p.WriteInt(group_id);
        p.WriteInt(user_id);
    }
}

/// <summary>Sent when a group admin rejects a pending membership request.</summary>
/// <remarks>Sent as the Flash <c>RejectMembershipRequest</c> message.</remarks>
/// <param name="GroupId">The id of the group, written as a 32 bit integer.</param>
/// <param name="UserId">The user id of the applicant, written as a 32 bit integer.</param>
public sealed record RejectGroupMemberRequest(Id GroupId, Id UserId)
    : IParserComposer<RejectGroupMemberRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RejectGroupMemberRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RejectGroupMemberRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RejectGroupMemberRequest value, in PacketWriter p)
    {
        int group_id = checked((int)value.GroupId);
        int user_id = checked((int)value.UserId);
        p.WriteInt(group_id);
        p.WriteInt(user_id);
    }
}

/// <summary>Requests a page of a group's member list.</summary>
/// <remarks>Sent as the Flash <c>GetGuildMembers</c> message. Composing throws when <paramref name="PageIndex"/> is negative, <paramref name="SearchType"/> is undefined or <paramref name="UserNameFilter"/> exceeds 65535 bytes.</remarks>
/// <param name="GroupId">The id of the group, written as a 32 bit integer.</param>
/// <param name="PageIndex">The zero based index of the page to return.</param>
/// <param name="UserNameFilter">The text that member names must match, or an empty string for no filter.</param>
/// <param name="SearchType">The kind of members to list, written as an integer.</param>
public sealed record GetGuildMembersRequest(
    Id GroupId,
    int PageIndex,
    string UserNameFilter,
    GuildMemberSearchType SearchType) : IParserComposer<GetGuildMembersRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetGuildMembersRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GetGuildMembersRequest ParseFlash(in PacketReader p)
    {
        var value = new GetGuildMembersRequest(
            p.ReadInt(),
            p.ReadInt(),
            p.ReadString(),
            (GuildMemberSearchType)p.ReadInt());
        RequireEmpty(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetGuildMembersRequest value, in PacketWriter p)
    {
        Validate(value, in p);
        int group_id = checked((int)value.GroupId);
        p.WriteInt(group_id);
        p.WriteInt(value.PageIndex);
        p.WriteString(value.UserNameFilter);
        p.WriteInt((int)value.SearchType);
    }

    private static void Validate(GetGuildMembersRequest value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(value.PageIndex);
        if (!Enum.IsDefined(value.SearchType))
            throw new ArgumentOutOfRangeException(nameof(SearchType));
        ArgumentNullException.ThrowIfNull(value.UserNameFilter);
        int length = p.Encoding.GetByteCount(value.UserNameFilter);
        if (length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"String byte length ({length}) exceeds {ushort.MaxValue}.",
                nameof(UserNameFilter));
        }
    }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException(
                $"{nameof(GetGuildMembersRequest)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Requests the details of a group.</summary>
/// <remarks>Sent as the Flash <c>GetHabboGroupDetails</c> message.</remarks>
/// <param name="GroupId">The id of the group, written as a 32 bit integer.</param>
/// <param name="OpenInClient">Whether the reply asks the client to open the group details window.</param>
public sealed record GroupDetailsRequest(Id GroupId, bool OpenInClient)
    : IParserComposer<GroupDetailsRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GroupDetailsRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GroupDetailsRequest ParseFlash(in PacketReader p)
    {
        var value = new GroupDetailsRequest(p.ReadInt(), p.ReadBool());
        RequireEmpty(in p);
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GroupDetailsRequest value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        int group_id = checked((int)(long)value.GroupId);
        p.WriteInt(group_id);
        p.WriteBool(value.OpenInClient);
    }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException(
                $"{nameof(GroupDetailsRequest)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Requests the groups the user is a member of.</summary>
/// <remarks>Sent as the Flash <c>GetGuildMemberships</c> message, which carries no fields.</remarks>
public sealed record GuildMembershipsRequest : IParserComposer<GuildMembershipsRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GuildMembershipsRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static GuildMembershipsRequest ParseFlash(in PacketReader p)
    {
        RequireEmpty(in p);
        return new GuildMembershipsRequest();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GuildMembershipsRequest value, in PacketWriter p) =>
        ArgumentNullException.ThrowIfNull(value);

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException(
                $"{nameof(GuildMembershipsRequest)} contains {p.Available} unexpected bytes.");
    }
}
