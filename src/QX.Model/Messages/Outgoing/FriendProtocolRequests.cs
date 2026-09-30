using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the initial state of the messenger, including the friend list.</summary>
/// <remarks>Sent as the Flash <c>MessengerInit</c> message, which carries no fields.</remarks>
public sealed record FriendInitializationRequest : IParserComposer<FriendInitializationRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FriendInitializationRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FriendInitializationRequest ParseFlash(in PacketReader p) => new();

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FriendInitializationRequest value, in PacketWriter p) { }
}

/// <summary>Requests the friend requests that are waiting for an answer.</summary>
/// <remarks>Sent as the Flash <c>GetFriendRequests</c> message, which carries no fields.</remarks>
public sealed record PendingFriendRequestsRequest : IParserComposer<PendingFriendRequestsRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PendingFriendRequestsRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static PendingFriendRequestsRequest ParseFlash(in PacketReader p) => new();

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PendingFriendRequestsRequest value, in PacketWriter p) { }
}

/// <summary>Sent when the user sends a friend request.</summary>
/// <remarks>Sent as the Flash <c>RequestFriend</c> message.</remarks>
/// <param name="Name">The name of the user to send the request to.</param>
public sealed record FriendRequest(string Name) : IParserComposer<FriendRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FriendRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FriendRequest ParseFlash(in PacketReader p) => new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FriendRequest value, in PacketWriter p) =>
        p.WriteString(value.Name);
}

/// <summary>Sent when the user follows a friend to the room the friend is in.</summary>
/// <remarks>Sent as the Flash <c>FollowFriend</c> message.</remarks>
/// <param name="FriendId">The user id of the friend to follow.</param>
public sealed record FollowFriendRequest(Id FriendId) : IParserComposer<FollowFriendRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FollowFriendRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FollowFriendRequest ParseFlash(in PacketReader p) => new(p.ReadId());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FollowFriendRequest value, in PacketWriter p) =>
        p.WriteId(value.FriendId);
}

/// <summary>Requests a search for users by name.</summary>
/// <remarks>Sent as the Flash <c>HabboSearch</c> message.</remarks>
/// <param name="Query">The name, or part of a name, to search for.</param>
public sealed record FriendSearchRequest(string Query) : IParserComposer<FriendSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FriendSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FriendSearchRequest ParseFlash(in PacketReader p) => new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FriendSearchRequest value, in PacketWriter p) =>
        p.WriteString(value.Query);
}

/// <summary>Sent when the user sets the relationship shown for a friend.</summary>
/// <remarks>Sent as the Flash <c>SetRelationshipStatus</c> message.</remarks>
/// <param name="FriendId">The user id of the friend.</param>
/// <param name="Relationship">The relationship to show, written as an integer. <see cref="RelationshipType.None"/> clears it.</param>
public sealed record SetFriendRelationshipRequest(Id FriendId, RelationshipType Relationship)
    : IParserComposer<SetFriendRelationshipRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static SetFriendRelationshipRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static SetFriendRelationshipRequest ParseFlash(in PacketReader p) =>
        new(p.ReadId(), (RelationshipType)p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(SetFriendRelationshipRequest value, in PacketWriter p)
    {
        p.WriteId(value.FriendId);
        p.WriteInt((int)value.Relationship);
    }
}

/// <summary>Sent when the user accepts pending friend requests.</summary>
/// <remarks>Sent as the Flash <c>AcceptFriend</c> message.</remarks>
/// <param name="RequestIds">The ids of the friend requests to accept.</param>
public sealed record AcceptFriends(IReadOnlyList<Id> RequestIds) : IParserComposer<AcceptFriends>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AcceptFriends Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AcceptFriends ParseFlash(in PacketReader p) => new(p.ReadIdArray());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AcceptFriends value, in PacketWriter p) =>
        p.WriteIdArray(value.RequestIds);
}

/// <summary>Sent when the user declines pending friend requests.</summary>
/// <remarks>Sent as the Flash <c>DeclineFriend</c> message. Use <see cref="DeclineFriends.All"/> or <see cref="DeclineFriends.Only(IReadOnlyList{Id})"/> to create an instance.</remarks>
/// <param name="DeclineAll">Whether every pending request is declined, in which case <paramref name="RequestIds"/> is empty.</param>
/// <param name="RequestIds">The ids of the friend requests to decline.</param>
public sealed record DeclineFriends(bool DeclineAll, IReadOnlyList<Id> RequestIds)
    : IParserComposer<DeclineFriends>
{
    /// <summary>Creates a request that declines every pending friend request.</summary>
    /// <returns>A request with <see cref="DeclineAll"/> set and no request ids.</returns>
    public static DeclineFriends All() => new(true, []);

    /// <summary>Creates a request that declines the specified friend requests.</summary>
    /// <param name="requestIds">The ids of the friend requests to decline.</param>
    /// <returns>A request with <see cref="DeclineAll"/> cleared.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestIds"/> is <see langword="null"/>.</exception>
    public static DeclineFriends Only(IReadOnlyList<Id> requestIds)
    {
        ArgumentNullException.ThrowIfNull(requestIds);
        return new DeclineFriends(false, requestIds);
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DeclineFriends Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DeclineFriends ParseFlash(in PacketReader p) =>
        new(p.ReadBool(), p.ReadIdArray());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DeclineFriends value, in PacketWriter p)
    {
        p.WriteBool(value.DeclineAll);
        p.WriteIdArray(value.RequestIds);
    }
}

/// <summary>Sent when the user removes friends from the friend list.</summary>
/// <remarks>Sent as the Flash <c>RemoveFriend</c> message.</remarks>
/// <param name="FriendIds">The user ids of the friends to remove.</param>
public sealed record RemoveFriends(IReadOnlyList<Id> FriendIds) : IParserComposer<RemoveFriends>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RemoveFriends Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RemoveFriends ParseFlash(in PacketReader p) => new(p.ReadIdArray());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RemoveFriends value, in PacketWriter p) =>
        p.WriteIdArray(value.FriendIds);
}

/// <summary>Sent when the user sends a private message to a friend.</summary>
/// <remarks>Sent as the Flash <c>SendMsg</c> message.</remarks>
/// <param name="RecipientId">The user id of the friend.</param>
/// <param name="Text">The message text.</param>
/// <param name="MessageIndex">The sequence number of the message in the conversation, starting at 0. The Flash composer throws <see cref="InvalidOperationException"/> when it is <see langword="null"/>.</param>
public sealed record SendPrivateMessage(Id RecipientId, string Text, int? MessageIndex)
    : IParserComposer<SendPrivateMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static SendPrivateMessage Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static SendPrivateMessage ParseFlash(in PacketReader p) =>
        new(p.ReadId(), p.ReadString(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(SendPrivateMessage value, in PacketWriter p)
    {
        p.WriteId(value.RecipientId);
        p.WriteString(value.Text);
        p.WriteInt(value.MessageIndex ??
            throw new InvalidOperationException("A Flash private message requires a message index."));
    }
}
