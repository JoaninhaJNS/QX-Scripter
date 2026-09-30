using Qx;
using Qx.Game;
using Qx.Game.Application;
using Qx.Game.Protocol;
using Qx.Messages;
using Qx.Model;
using Qx.Model.Messages.Incoming;
using Qx.Model.Messages.Outgoing;
using Qx.Protocol;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Plays an avatar expression.
    /// </summary>
    /// <remarks>Alias of <see cref="Expression"/>.</remarks>
    /// <param name="type">
    /// The expression: 0 clears the expression, 1 wave, 2 blow a kiss, 3 laugh, 4 cry, 5 go
    /// idle, 6 jump, 7 thumbs up.
    /// </param>
    public void Action(int type) => Expression(type);

    /// <summary>Blows a kiss.</summary>
    /// <remarks>Equivalent to <c>Expression(2)</c>.</remarks>
    public void Kiss() => Expression(2);

    /// <summary>Laughs.</summary>
    /// <remarks>Equivalent to <c>Expression(3)</c>.</remarks>
    public void Laugh() => Expression(3);

    /// <summary>Jumps.</summary>
    /// <remarks>Equivalent to <c>Expression(6)</c>.</remarks>
    public void Jump() => Expression(6);

    /// <summary>Gives a thumbs up.</summary>
    /// <remarks>Equivalent to <c>Expression(7)</c>.</remarks>
    public void ThumbsUp() => Expression(7);

    /// <summary>
    /// Puts the avatar to sleep immediately instead of waiting for the idle timer.
    /// </summary>
    /// <remarks>Equivalent to <c>Expression(5)</c>.</remarks>
    public void Idle() => Expression(5);

    /// <summary>
    /// Wakes the avatar from the idle state.
    /// </summary>
    /// <remarks>Equivalent to <c>Expression(0)</c>.</remarks>
    public void Unidle() => Expression(0);

    /// <summary>Walks to the given tile.</summary>
    /// <remarks>Alias of <see cref="Walk(int,int)"/>.</remarks>
    /// <param name="x">The target tile's X coordinate.</param>
    /// <param name="y">The target tile's Y coordinate.</param>
    public void Move(int x, int y) => Walk(x, y);

    /// <summary>
    /// Changes the local user's motto.
    /// </summary>
    /// <remarks>
    /// The server truncates or rejects it silently when it is too long or fails the filter;
    /// watch <see cref="OnProfileUpdated"/> for the accepted value.
    /// </remarks>
    /// <param name="motto">The new motto. An empty string clears it.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="motto"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="motto"/> is longer than 65535 bytes in UTF-8.
    /// </exception>
    public void SetMotto(string motto) =>
        Application.Invoke<ProfileMottoSetRequest, ProfileDispatchResult>(
            ApplicationMemberIds.ProfileMottoSet,
            new ProfileMottoSetRequest(motto),
            Ct);

    /// <summary>
    /// Sends a friend request to the named user.
    /// </summary>
    /// <remarks>
    /// Nothing is reported when the name does not exist, the user blocks requests, or either
    /// friend list is full.
    /// </remarks>
    /// <param name="name">The exact user name.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty or whitespace.</exception>
    public void AddFriend(string name) =>
        Application.Invoke<FriendRequestSendRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendRequestSend,
            new FriendRequestSendRequest(name),
            Ct);

    /// <summary>
    /// Asks the server to move the local user into the room a friend is currently in.
    /// </summary>
    /// <remarks>
    /// Nothing happens when the friend is offline or their room does not allow entry.
    /// </remarks>
    /// <param name="userId">The friend's user id.</param>
    public void FollowFriend(Id userId) =>
        Application.Invoke<FriendFollowRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendFollow,
            new FriendFollowRequest(userId),
            Ct);

    /// <summary>
    /// Requests to join a group.
    /// </summary>
    /// <remarks>
    /// Depending on the group this either joins immediately or creates a pending membership
    /// request.
    /// </remarks>
    /// <param name="groupId">The group id.</param>
    public void JoinGroup(Id groupId) =>
        Application.Invoke<GroupJoinRequest, GroupMembershipDispatchResult>(
            ApplicationMemberIds.GroupMembershipJoin,
            new GroupJoinRequest(groupId),
            Ct);

    /// <summary>
    /// Sends a private message to a friend through the messenger.
    /// </summary>
    /// <param name="userId">The friend's user id.</param>
    /// <param name="message">The message text.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is <see langword="null"/>, empty or whitespace.</exception>
    public void SendMessage(Id userId, string message) =>
    Application.Invoke<FriendMessageSendRequest, FriendOperationResult>(
        ApplicationMemberIds.FriendMessageSend,
        new FriendMessageSendRequest(userId, message),
        Ct);

    /// <summary>
    /// Changes the local user's look.
    /// </summary>
    /// <param name="gender">
    /// The gender: <c>"M"</c>, <c>"F"</c> or <c>"U"</c>, or <c>"male"</c>, <c>"female"</c> or
    /// <c>"unisex"</c>, in any case.
    /// </param>
    /// <param name="figure">The figure string.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="gender"/> or <paramref name="figure"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="gender"/> is not a known gender, or <paramref name="figure"/> is empty or
    /// whitespace.
    /// </exception>
    public void UpdateFigure(string gender, string figure) =>
    Application.Invoke<ProfileFigureSetRequest, ProfileDispatchResult>(
        ApplicationMemberIds.ProfileFigureSet,
        new ProfileFigureSetRequest(gender, figure),
        Ct);

    /// <summary>
    /// Shows the typing indicator above the local avatar.
    /// </summary>
    /// <remarks>
    /// The indicator does not clear on its own; pair it with <see cref="CancelTyping"/>.
    /// </remarks>
    public void StartTyping() =>
        Application.Invoke<RoomAvatarTypingRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarTyping,
            new RoomAvatarTypingRequest(true),
            Ct);

    /// <summary>Hides the typing indicator above the local avatar.</summary>
    public void CancelTyping() =>
        Application.Invoke<RoomAvatarTypingRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarTyping,
            new RoomAvatarTypingRequest(false),
            Ct);

    /// <summary>
    /// Accepts a pending friend request.
    /// </summary>
    /// <param name="userId">The requester's user id, as carried by <see cref="OnFriendRequest"/>.</param>
    public void AcceptFriendRequest(Id userId) =>
        Application.Invoke<FriendRequestIdsRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendRequestAccept,
            new FriendRequestIdsRequest([userId]),
            Ct);

    /// <summary>Declines one pending friend request.</summary>
    /// <param name="userId">The requester's user id.</param>
    public void DeclineFriendRequest(Id userId) => DeclineFriendRequests([userId]);

    /// <summary>
    /// Declines several pending friend requests in one message.
    /// </summary>
    /// <remarks>Duplicate ids are collapsed.</remarks>
    /// <param name="userIds">The requesters' user ids.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="userIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="userIds"/> is empty.</exception>
    public void DeclineFriendRequests(IEnumerable<Id> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        Id[] ids = userIds.Distinct().ToArray();
        Application.Invoke<FriendRequestDeclineRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendRequestDecline,
            new FriendRequestDeclineRequest(ids),
            Ct);
    }

    /// <summary>
    /// Declines every pending friend request at once.
    /// </summary>
    /// <remarks>
    /// It uses the protocol's "decline all" flag rather than an id list, so no request has to be
    /// known in advance.
    /// </remarks>
    public void DeclineAllFriendRequests() =>
        Application.Invoke<FriendRequestsDeclineAllRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendRequestsDeclineAll,
            new FriendRequestsDeclineAllRequest(),
            Ct);

    /// <summary>
    /// Kicks a user out of the current room.
    /// </summary>
    /// <remarks>
    /// It requires room rights or staff permissions; the server ignores it otherwise.
    /// </remarks>
    /// <param name="userId">The target user's account id, not their room index.</param>
    public void Kick(Id userId) =>
        Application.Invoke<RoomModerationTargetRequest, RoomModerationDispatchResult>(
            ApplicationMemberIds.RoomModerationKick,
            new RoomModerationTargetRequest(userId),
            Ct);

    /// <summary>
    /// Mutes a user in the current room for a number of minutes.
    /// </summary>
    /// <remarks>
    /// It requires rights, and the room's "who can mute" setting must allow it.
    /// </remarks>
    /// <param name="userId">The target user's account id.</param>
    /// <param name="minutes">The mute duration in minutes, from 0 to 1440.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minutes"/> is below 0 or above 1440.</exception>
    public void Mute(Id userId, int minutes) =>
        Application.Invoke<RoomModerationMuteRequest, RoomModerationDispatchResult>(
            ApplicationMemberIds.RoomModerationMute,
            new RoomModerationMuteRequest(userId, minutes),
            Ct);

    /// <summary>
    /// Bans a user from the current room.
    /// </summary>
    /// <remarks>
    /// It requires ownership or rights, subject to the room's "who can ban" setting.
    /// </remarks>
    /// <param name="userId">The target user's account id.</param>
    /// <param name="duration">
    /// The ban duration: <c>"RWUAM_BAN_USER_HOUR"</c> for one hour, <c>"RWUAM_BAN_USER_DAY"</c>
    /// for one day or <c>"RWUAM_BAN_USER_PERM"</c> for a permanent ban. The default
    /// <c>"Room_Session"</c> is sent as a one hour ban.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="duration"/> is not one of the listed values.</exception>
    public void Ban(Id userId, string duration = "Room_Session")
    {
        BanLength length = duration switch
        {
            "Room_Session" or "RWUAM_BAN_USER_HOUR" => BanLength.Hour,
            "RWUAM_BAN_USER_DAY" => BanLength.Day,
            "RWUAM_BAN_USER_PERM" => BanLength.Permanent,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, "Unknown room-ban duration.")
        };
        Application.Invoke<RoomModerationBanRequest, RoomModerationDispatchResult>(
            ApplicationMemberIds.RoomModerationBan,
            new RoomModerationBanRequest(userId, length),
            Ct);
    }

    /// <summary>
    /// Grants room rights to a user who is in the current room.
    /// </summary>
    /// <remarks>The local user must own the room.</remarks>
    /// <param name="userId">The target user's account id.</param>
    public void GiveRights(Id userId) =>
        Application.Invoke<RoomRightsGrantRequest, RoomPeopleDispatchResult>(
            ApplicationMemberIds.RoomPeopleRightsGrant,
            new RoomRightsGrantRequest(userId),
            Ct);

    /// <summary>
    /// Revokes a user's room rights.
    /// </summary>
    /// <remarks>
    /// The local user must own the room. The raw <c>RemoveRights</c> message is sent with a list
    /// holding the one id.
    /// </remarks>
    /// <param name="userId">The target user's account id.</param>
    public void RemoveRights(Id userId) => SendIds(Msg.Out.RemoveRights, userId);

    /// <summary>
    /// Answers a doorbell for a locked room by letting the waiting user in or turning them away.
    /// </summary>
    /// <param name="name">The waiting user's name, as reported by the doorbell event.</param>
    /// <param name="allow"><see langword="true"/> to let them in; <see langword="false"/> to refuse.</param>
    public void LetIn(string name, bool allow = true) =>
        Application.Invoke<RoomDoorbellAnswerRequest, RoomControlDispatchResult>(
            ApplicationMemberIds.RoomDoorbellAnswer,
            new RoomDoorbellAnswerRequest(name, allow),
            Ct);

    /// <summary>Removes a user from the friend list.</summary>
    /// <param name="userId">The friend's user id.</param>
    public void RemoveFriend(Id userId) =>
        Application.Invoke<FriendsRemoveRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendsRemove,
            new FriendsRemoveRequest([userId]),
            Ct);

    /// <summary>
    /// Gives a pet in the current room a respect.
    /// </summary>
    /// <remarks>
    /// The daily respect allowance is enforced by the server and its exhaustion is not reported
    /// here.
    /// </remarks>
    /// <param name="petId">The pet's id.</param>
    public void RespectPet(Id petId) =>
        Application.Invoke<RoomPetRespectRequest, RoomPeopleDispatchResult>(
            ApplicationMemberIds.RoomPetRespect,
            new RoomPetRespectRequest(petId),
            Ct);

    /// <summary>
    /// Adds a user to the ignore list by account id.
    /// </summary>
    /// <param name="userId">The target user's account id.</param>
    public void Ignore(Id userId)
    {
        Application.Invoke<ProfileUserRequest, ProfileDispatchResult>(
            ApplicationMemberIds.ProfileIgnoreAddById,
            new ProfileUserRequest(userId),
            Ct);
    }

    /// <summary>
    /// Adds a user to the ignore list by name.
    /// </summary>
    /// <remarks>
    /// The name is resolved to an account id from the users in the current room first, then from
    /// the friend list.
    /// </remarks>
    /// <param name="name">The target user's name.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the name is neither in the current room nor on the friend list, so it cannot be resolved
    /// to an id.
    /// </exception>
    public void Ignore(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Ignore(ResolveUserId(name));
    }

    /// <summary>
    /// Removes a user from the ignore list by account id.
    /// </summary>
    /// <param name="userId">The target user's account id.</param>
    public void Unignore(Id userId)
    {
        string identity = ((long)userId).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Application.Invoke<ProfileIgnoreRemoveRequest, ProfileDispatchResult>(
            ApplicationMemberIds.ProfileIgnoreRemove,
            new ProfileIgnoreRemoveRequest(ProfileIdentityKind.Id, identity),
            Ct);
    }

    /// <summary>
    /// Removes a user from the ignore list by name.
    /// </summary>
    /// <remarks>
    /// The name is resolved to an account id from the users in the current room first, then from
    /// the friend list.
    /// </remarks>
    /// <param name="name">The target user's name.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is <see langword="null"/>, empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the name is neither in the current room nor on the friend list, so it cannot be resolved
    /// to an id.
    /// </exception>
    public void Unignore(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Unignore(ResolveUserId(name));
    }

    /// <summary>
    /// Mounts or dismounts a rideable pet in the current room.
    /// </summary>
    /// <param name="petId">The pet's id.</param>
    /// <param name="mount"><see langword="true"/> to get on; <see langword="false"/> to get off.</param>
    public void MountPet(Id petId, bool mount = true) =>
        Application.Invoke<RoomPetMountRequest, RoomPeopleDispatchResult>(
            ApplicationMemberIds.RoomPetMountSet,
            new RoomPetMountRequest(petId, mount),
            Ct);

    /// <summary>Dismounts a pet.</summary>
    /// <remarks>Equivalent to <c>MountPet(petId, false)</c>.</remarks>
    /// <param name="petId">The pet's id.</param>
    public void DismountPet(Id petId) => MountPet(petId, false);

    /// <summary>
    /// Removes a member from a group.
    /// </summary>
    /// <remarks>It requires an administrator rank in that group.</remarks>
    /// <param name="groupId">The group id.</param>
    /// <param name="userId">The member's user id.</param>
    /// <param name="blockRejoin">
    /// <see langword="true"/> to also bar the member from applying again; otherwise,
    /// <see langword="false"/>.
    /// </param>
    public void KickGroupMember(Id groupId, Id userId, bool blockRejoin = false) =>
        Application.Invoke<GroupMemberKickRequest, GroupMembershipDispatchResult>(
            ApplicationMemberIds.GroupMembershipKick,
            new GroupMemberKickRequest(groupId, userId, blockRejoin),
            Ct);

    /// <summary>
    /// Approves a pending membership request.
    /// </summary>
    /// <remarks>It requires an administrator rank in that group.</remarks>
    /// <param name="groupId">The group id.</param>
    /// <param name="userId">The applicant's user id.</param>
    public void ApproveGroupMember(Id groupId, Id userId) =>
        Application.Invoke<GroupMemberRequest, GroupMembershipDispatchResult>(
            ApplicationMemberIds.GroupMembershipApprove,
            new GroupMemberRequest(groupId, userId),
            Ct);

    /// <summary>
    /// Rejects a pending membership request.
    /// </summary>
    /// <remarks>It requires an administrator rank in that group.</remarks>
    /// <param name="groupId">The group id.</param>
    /// <param name="userId">The applicant's user id.</param>
    public void RejectGroupMember(Id groupId, Id userId) =>
        Application.Invoke<GroupMemberRequest, GroupMembershipDispatchResult>(
            ApplicationMemberIds.GroupMembershipReject,
            new GroupMemberRequest(groupId, userId),
            Ct);

    /// <summary>
    /// Makes a group the favorite one, so its badge is shown next to the avatar.
    /// </summary>
    /// <remarks>The account must be a member.</remarks>
    /// <param name="groupId">The group id.</param>
    public void SetFavouriteGroup(Id groupId) =>
        Application.Invoke<ProfileFavoriteGroupRequest, ProfileDispatchResult>(
            ApplicationMemberIds.ProfileFavoriteGroupSelect,
            new ProfileFavoriteGroupRequest(groupId),
            Ct);

    /// <summary>Clears the favorite group, hiding its badge again.</summary>
    /// <param name="groupId">The group id currently marked as favorite.</param>
    public void UnsetFavouriteGroup(Id groupId) =>
        Application.Invoke<ProfileFavoriteGroupRequest, ProfileDispatchResult>(
            ApplicationMemberIds.ProfileFavoriteGroupDeselect,
            new ProfileFavoriteGroupRequest(groupId),
            Ct);

    /// <summary>
    /// Places a floor item from the inventory into the room.
    /// </summary>
    /// <remarks>
    /// The item is checked against the loaded furni inventory before anything is sent. The
    /// server still requires room rights and refuses a blocked tile silently.
    /// </remarks>
    /// <param name="itemId">The inventory item id, not a room item id.</param>
    /// <param name="x">The target tile's X coordinate.</param>
    /// <param name="y">The target tile's Y coordinate.</param>
    /// <param name="direction">The rotation, in eighths of a turn (0 to 7).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="itemId"/> is 0, <paramref name="x"/> or <paramref name="y"/> is negative,
    /// or <paramref name="direction"/> is outside 0 to 7.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no room is ready, the furni inventory is not loaded and current, or it holds no floor item
    /// with <paramref name="itemId"/>.
    /// </exception>
    public void PlaceFloorItem(Id itemId, int x, int y, int direction = 0) =>
        Application.Invoke<RoomPlacementFloorPlaceRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementFloorPlace,
            new RoomPlacementFloorPlaceRequest(
                itemId,
                new RoomPlacementFloorPosition(x, y, direction)),
            Ct);

    /// <summary>
    /// Places a wall item from the inventory onto a wall.
    /// </summary>
    /// <remarks>
    /// The item is checked against the loaded furni inventory before anything is sent. The
    /// server still requires room rights.
    /// </remarks>
    /// <param name="itemId">The inventory item id.</param>
    /// <param name="wallLocation">
    /// The wall position in the client's notation, <c>":w=x,y l=x,y direction"</c>, for example
    /// <c>":w=2,3 l=5,20 l"</c>.
    /// </param>
    /// <exception cref="FormatException">Thrown when <paramref name="wallLocation"/> is not a valid wall position.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no room is ready, the furni inventory is not loaded and current, or it holds no wall item
    /// with <paramref name="itemId"/>.
    /// </exception>
    public void PlaceWallItem(Id itemId, string wallLocation) =>
        Application.Invoke<RoomPlacementWallPlaceRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementWallPlace,
            new RoomPlacementWallPlaceRequest(itemId, PlacementWallPosition(wallLocation)),
            Ct);

    /// <summary>
    /// Moves a wall item that is already hanging in the room to a new wall position.
    /// </summary>
    /// <remarks>It requires room rights.</remarks>
    /// <param name="itemId">The item's room id.</param>
    /// <param name="wallLocation">The new wall position, in the <c>":w=x,y l=x,y direction"</c> notation.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="wallLocation"/> is not a valid wall position.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no room is ready, or the wall item is not in it.</exception>
    public void MoveWallItem(Id itemId, string wallLocation) =>
        Application.Invoke<RoomPlacementWallMoveRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementWallMove,
            new RoomPlacementWallMoveRequest(itemId, PlacementWallPosition(wallLocation)),
            Ct);

    /// <summary>
    /// Places a sticky note (post-it) from the inventory onto a wall, with no text.
    /// </summary>
    /// <param name="itemId">The inventory item id of the sticky pad.</param>
    /// <param name="wallLocation">The wall position, in the <c>":w=x,y l=x,y direction"</c> notation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="wallLocation"/> is <see langword="null"/>.</exception>
    public void PlacePostIt(Id itemId, string wallLocation) =>
        Application.Invoke<RoomPostItPlaceRequest, RoomItemDispatchResult>(
            ApplicationMemberIds.RoomItemPostItPlace,
            new RoomPostItPlaceRequest(itemId, wallLocation),
            Ct);

    /// <summary>
    /// Places a sticky note on a wall together with its color and initial text.
    /// </summary>
    /// <param name="itemId">The inventory item id of the sticky pad.</param>
    /// <param name="wallLocation">The wall position, in the <c>":w=x,y l=x,y direction"</c> notation.</param>
    /// <param name="color">The note color as a hexadecimal string, for example <c>"FFFF33"</c>.</param>
    /// <param name="text">The note text.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="wallLocation"/>, <paramref name="color"/> or <paramref name="text"/> is <see langword="null"/>.
    /// </exception>
    public void AddPostIt(Id itemId, string wallLocation, string color, string text) =>
        Application.Invoke<RoomPostItAddRequest, RoomItemDispatchResult>(
            ApplicationMemberIds.RoomItemPostItAdd,
            new RoomPostItAddRequest(itemId, wallLocation, color, text),
            Ct);

    /// <summary>Moves a wall item to a new wall position.</summary>
    /// <remarks>
    /// Unlike <see cref="MoveWallItem(Id, string)"/>, the item's current location is sent along
    /// and checked, so the move fails if the item has been moved in the meantime.
    /// </remarks>
    /// <param name="item">The placed wall item.</param>
    /// <param name="wallLocation">The new wall position, in the <c>":w=x,y l=x,y direction"</c> notation.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="wallLocation"/> is not a valid wall position.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no room is ready, the wall item is not in it, or it is no longer at the location
    /// <paramref name="item"/> holds.
    /// </exception>
    public void MoveWallItem(WallItem item, string wallLocation) =>
        Application.Invoke<RoomPlacementWallMoveRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementWallMove,
            new RoomPlacementWallMoveRequest(
                item.Id,
                PlacementWallPosition(wallLocation),
                PlacementWallPosition(item.Location)),
            Ct);

    /// <summary>
    /// Rotates a placed floor item without moving it.
    /// </summary>
    /// <remarks>
    /// It sends the item's current tile with the new rotation, along with its current position
    /// as the expected source.
    /// </remarks>
    /// <param name="item">The placed item; its current X and Y are reused.</param>
    /// <param name="direction">The new rotation, in eighths of a turn (0 to 7).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="direction"/> is outside 0 to 7.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no room is ready, the item is not in it, or it is no longer at the position
    /// <paramref name="item"/> holds.
    /// </exception>
    public void RotateFloorItem(FloorItem item, int direction) =>
        Application.Invoke<RoomPlacementFloorMoveRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementFloorMove,
            new RoomPlacementFloorMoveRequest(
                item.Id,
                new RoomPlacementFloorPosition(item.X, item.Y, direction),
                new RoomPlacementFloorPosition(item.X, item.Y, item.Direction)),
            Ct);

    /// <summary>
    /// Picks a floor item up into the inventory.
    /// </summary>
    /// <remarks>It requires room rights or ownership of the item.</remarks>
    /// <param name="item">The floor item to pick up.</param>
    /// <param name="confirmed">
    /// <see langword="true"/> to acknowledge the hotel's remove confirmation prompt; otherwise,
    /// <see langword="false"/>. The flag is part of the Flash pickup message.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when no room is ready, or the item is not in it.</exception>
    public void PickupFurni(FloorItem item, bool confirmed = false) =>
    SendPickup(2, item.Id, confirmed);

    /// <summary>
    /// Picks a wall item up into the inventory.
    /// </summary>
    /// <remarks>It requires room rights or ownership of the item.</remarks>
    /// <param name="item">The wall item to pick up.</param>
    /// <param name="confirmed">
    /// <see langword="true"/> to acknowledge the hotel's remove confirmation prompt; otherwise,
    /// <see langword="false"/>. The flag is part of the Flash pickup message.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when no room is ready, or the item is not in it.</exception>
    public void PickupFurni(WallItem item, bool confirmed = false) =>
        SendPickup(1, item.Id, confirmed);

    private void SendPickup(int category, Id itemId, bool confirmed) =>
        Application.Invoke<RoomPlacementPickupRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementPickup,
            new RoomPlacementPickupRequest(
                itemId,
                category == 2 ? RoomPlacementItemKind.Floor : RoomPlacementItemKind.Wall,
                confirmed),
            Ct);

    private static RoomPlacementWallPosition PlacementWallPosition(string value) =>
        PlacementWallPosition(WallLocation.ParseString(value));

    private static RoomPlacementWallPosition PlacementWallPosition(WallLocation value) =>
        new(
            value.Wall.X,
            value.Wall.Y,
            value.Offset.X,
            value.Offset.Y,
            value.Orientation.ToString());

    /// <summary>
    /// Buys a marketplace offer.
    /// </summary>
    /// <remarks>
    /// The purchase is refused silently when the offer has already been taken or the account
    /// cannot afford it.
    /// </remarks>
    /// <param name="offerId">The marketplace offer id from a search result.</param>
    public void BuyMarketplaceOffer(Id offerId) =>
        Application.Invoke<MarketplaceBuySendRequest, MarketplaceDispatchResult>(
            ApplicationMemberIds.MarketplaceOfferBuySend,
            new MarketplaceBuySendRequest(offerId),
            Ct);

    /// <summary>
    /// Withdraws one of the local user's own marketplace offers, returning the item to the
    /// inventory.
    /// </summary>
    /// <param name="offerId">The offer id from <see cref="GetMyMarketplaceOffers(int)"/>.</param>
    public void CancelMarketplaceOffer(Id offerId) =>
        Application.Invoke<MarketplaceCancelSendRequest, MarketplaceDispatchResult>(
            ApplicationMemberIds.MarketplaceOfferCancelSend,
            new MarketplaceCancelSendRequest(offerId),
            Ct);

    /// <summary>
    /// Collects the credits earned from sold marketplace offers into the wallet.
    /// </summary>
    public void RedeemMarketplaceCredits() =>
        CollectMarketplaceEarnings();

    /// <summary>
    /// Activates an avatar effect that is owned but not yet started, which begins consuming its
    /// duration.
    /// </summary>
    /// <remarks>
    /// Use <see cref="EnableEffect"/> to wear an effect that is already activated.
    /// </remarks>
    /// <param name="effectId">The effect id; <see cref="EffectName"/> resolves it to a name.</param>
    public void ActivateEffect(int effectId) =>
        Application.Invoke<InventoryAvatarEffectRequest, InventoryDispatchResult>(
            ApplicationMemberIds.InventoryAvatarEffectActivate,
            new InventoryAvatarEffectRequest(effectId),
            Ct);

    /// <summary>
    /// Wears one of the currently activated avatar effects.
    /// </summary>
    /// <param name="effectId">The effect id, or -1 to wear none.</param>
    public void EnableEffect(int effectId) =>
        Application.Invoke<RoomAvatarEffectRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarEffect,
            new RoomAvatarEffectRequest(effectId),
            Ct);

    /// <summary>Takes off the current avatar effect.</summary>
    /// <remarks>Equivalent to <c>EnableEffect(-1)</c>.</remarks>
    public void DisableEffect() => EnableEffect(-1);

    /// <summary>
    /// Stores a look in a wardrobe slot, overwriting whatever was in it.
    /// </summary>
    /// <param name="slot">The wardrobe slot number, as used by <see cref="GetWardrobe"/>.</param>
    /// <param name="figure">The figure string to store.</param>
    /// <param name="gender">
    /// The gender: <c>"M"</c>, <c>"F"</c> or <c>"U"</c>, or <c>"male"</c>, <c>"female"</c> or
    /// <c>"unisex"</c>, in any case.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="figure"/> or <paramref name="gender"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="slot"/> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="gender"/> is not a known gender, or <paramref name="figure"/> is empty or
    /// whitespace.
    /// </exception>
    public void SaveOutfit(int slot, string figure, string gender) =>
        Application.Invoke<ProfileOutfitSaveRequest, ProfileDispatchResult>(
            ApplicationMemberIds.ProfileWardrobeOutfitSave,
            new ProfileOutfitSaveRequest(slot, figure, gender),
            Ct);

    /// <summary>
    /// Throws a dice furni, making it roll to a new value.
    /// </summary>
    /// <remarks>
    /// The result arrives later as an item data change; register a handler with
    /// <see cref="OnFloorItemDataChanged"/> to read it.
    /// </remarks>
    /// <param name="itemId">The dice's room item id.</param>
    public void ThrowDice(Id itemId) =>
        Application.Invoke<RoomDiceRequest, RoomItemDispatchResult>(
            ApplicationMemberIds.RoomItemDiceThrow,
            new RoomDiceRequest(itemId),
            Ct);

    /// <summary>
    /// Clears a dice furni back to its blank face.
    /// </summary>
    /// <param name="itemId">The dice's room item id.</param>
    public void DiceOff(Id itemId) =>
        Application.Invoke<RoomDiceRequest, RoomItemDispatchResult>(
            ApplicationMemberIds.RoomItemDiceClear,
            new RoomDiceRequest(itemId),
            Ct);

    /// <summary>
    /// Creates a new room owned by the local user.
    /// </summary>
    /// <remarks>
    /// Nothing is returned; the new room shows up in the navigator's own rooms view, which
    /// <see cref="GetUserRooms"/> reads.
    /// </remarks>
    /// <param name="name">The room name.</param>
    /// <param name="description">The room description.</param>
    /// <param name="model">The floor plan model name, for example <c>"model_a"</c>.</param>
    /// <param name="category">The navigator category id the room is filed under.</param>
    /// <param name="maxVisitors">The visitor cap; the server clamps it to the values it allows.</param>
    /// <param name="tradeMode">The trading policy: 0 disabled, 1 rights holders only, 2 everyone.</param>
    public void CreateRoom(string name, string description, string model, int category, int maxVisitors, int tradeMode = 0) =>
        Application.Invoke<NavigatorRoomCreateInput, NavigatorRoomOperationResult>(
            ApplicationMemberIds.NavigatorRoomCreate,
            new NavigatorRoomCreateInput(name, description, model, category, maxVisitors, tradeMode),
            Ct);

    /// <summary>
    /// Permanently deletes a room owned by the local user, together with everything placed in
    /// it.
    /// </summary>
    /// <remarks>There is no confirmation step.</remarks>
    /// <param name="roomId">The room id.</param>
    public void DeleteRoom(Id roomId) =>
        Application.Invoke<NavigatorRoomDeleteInput, NavigatorRoomOperationResult>(
            ApplicationMemberIds.NavigatorRoomDelete,
            new NavigatorRoomDeleteInput(roomId),
            Ct);

    /// <summary>
    /// Sets the account's home room.
    /// </summary>
    /// <param name="roomId">The room id, or 0 to clear the home room.</param>
    public void SetHomeRoom(Id roomId) =>
        Application.Invoke<NavigatorHomeRoomSetInput, NavigatorRoomOperationResult>(
            ApplicationMemberIds.NavigatorHomeRoomSet,
            new NavigatorHomeRoomSetInput(roomId),
            Ct);

    /// <summary>
    /// Adds a room to or removes it from the staff picks.
    /// </summary>
    /// <remarks>
    /// It requires staff permissions; the server ignores it for ordinary accounts.
    /// </remarks>
    /// <param name="roomId">The room id.</param>
    /// <param name="pick"><see langword="true"/> to pick; <see langword="false"/> to unpick.</param>
    public void ToggleStaffPick(Id roomId, bool pick = true) =>
        Application.Invoke<RoomStaffPickRequest, RoomControlDispatchResult>(
            ApplicationMemberIds.RoomStaffPickSet,
            new RoomStaffPickRequest(roomId, pick),
            Ct);

    /// <summary>
    /// Rates the current room.
    /// </summary>
    /// <remarks>
    /// Each user may rate a given room once per visit; the server ignores further ratings.
    /// </remarks>
    /// <param name="rating">The signed rating value. The current client uses 1 for a positive rating.</param>
    public void RateRoom(int rating) =>
        Application.Invoke<RoomRatingRequest, RoomControlDispatchResult>(
            ApplicationMemberIds.RoomRatingSubmit,
            new RoomRatingRequest(rating),
            Ct);

    /// <summary>
    /// Removes a pet from the current room and returns it to the owner's inventory.
    /// </summary>
    /// <remarks>It requires ownership of the pet or room rights.</remarks>
    /// <param name="petId">The pet's id.</param>
    public void RemovePet(Id petId) =>
        Application.Invoke<RoomPetRemoveRequest, RoomPeopleDispatchResult>(
            ApplicationMemberIds.RoomPetRemove,
            new RoomPetRemoveRequest(petId),
            Ct);

    /// <summary>
    /// Removes a bot from the current room and returns it to the inventory.
    /// </summary>
    /// <remarks>It requires ownership of the bot or room rights.</remarks>
    /// <param name="botId">The bot's id.</param>
    public void RemoveBot(Id botId) =>
        Application.Invoke<RoomBotRemoveRequest, RoomPeopleDispatchResult>(
            ApplicationMemberIds.RoomBotRemove,
            new RoomBotRemoveRequest(botId),
            Ct);

    /// <summary>
    /// Saves the settings of a room and waits for the server to acknowledge them.
    /// </summary>
    /// <remarks>
    /// Read the current values with <see cref="GetRoomSettings"/>, change them and pass them back.
    /// Unlike most actions, the call blocks until the server confirms or rejects the save, for
    /// at most 10000 milliseconds.
    /// </remarks>
    /// <param name="settings">The complete room settings to save; its room id selects the room.</param>
    /// <param name="password">The room password, used when the door mode requires one; otherwise empty.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="Qx.Game.Application.RoomSettingsRejectedException">Thrown when the server rejected the settings.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when the server did not answer within 10000 milliseconds.</exception>
    public void SaveRoomSettings(RoomSettings settings, string password = "")
    {
        ArgumentNullException.ThrowIfNull(settings);
        Application.Invoke<RoomSettingsSaveRequest, RoomSettingsSaveReceipt>(
            ApplicationMemberIds.RoomSettingsSave,
            new RoomSettingsSaveRequest(ToApplicationRoomSettings(settings), password),
            Ct);
    }


    private Id ResolveUserId(string name)
    {
        User? user = FindUser(name);
        if (user is not null)
            return user.Id;
        Friend? friend = FindFriend(name);
        if (friend is not null)
            return friend.Id;
        throw new InvalidOperationException($"Cannot resolve user '{name}' to an identifier for this client layout.");
    }

    /// <summary>
    /// Buys an offer from the catalog.
    /// </summary>
    /// <remarks>
    /// Nothing is returned; failures such as insufficient credits or a stale offer are reported
    /// by the client's own purchase error message, not here.
    /// </remarks>
    /// <param name="pageId">The catalog page id, from <see cref="GetCatalogIndex"/>.</param>
    /// <param name="offerId">The offer id on that page, from <see cref="GetCatalogPage"/>.</param>
    /// <param name="extraData">
    /// The purchase parameter the offer expects: a color index, a pet name and color, a
    /// badge code, and so on. Empty for offers that take none.
    /// </param>
    /// <param name="amount">The number of items to buy in one purchase.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="extraData"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offerId"/> is negative, <paramref name="amount"/> is below 1, or
    /// <paramref name="extraData"/> is longer than 65535 bytes in UTF-8.
    /// </exception>
    public void PurchaseFromCatalog(int pageId, int offerId, string extraData = "", int amount = 1) =>
        Application.Invoke<CatalogPurchaseSendRequest, CatalogPurchaseDispatchReceipt>(
            ApplicationMemberIds.CatalogPurchaseSend,
            new CatalogPurchaseSendRequest(pageId, offerId, extraData, amount),
            Ct);

    /// <summary>
    /// Buys a catalog offer as a gift for another user.
    /// </summary>
    /// <remarks>
    /// Nothing is returned; the server reports the outcome through its own messages.
    /// </remarks>
    /// <param name="pageId">The catalog page id.</param>
    /// <param name="offerId">The offer id on that page.</param>
    /// <param name="extraData">The purchase parameter the offer expects, or empty for offers that take none.</param>
    /// <param name="receiverName">The name of the user who receives the gift.</param>
    /// <param name="giftMessage">The message shown with the gift.</param>
    /// <param name="spriteId">The sprite id of the gift wrapping.</param>
    /// <param name="boxType">The gift box type.</param>
    /// <param name="ribbonType">The gift ribbon type.</param>
    /// <param name="showPurchaserName">
    /// <see langword="true"/> to show the buyer's name to the receiver; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="amount">
    /// The quantity, which must be at least 1. The gift message has no quantity field, so one
    /// gift is bought regardless.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="extraData"/>, <paramref name="receiverName"/> or
    /// <paramref name="giftMessage"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="amount"/> is below 1, or a string argument is longer than 65535 bytes in
    /// UTF-8.
    /// </exception>
    public void PurchaseFromCatalogAsGift(
    int pageId, int offerId, string extraData, string receiverName, string giftMessage,
    int spriteId, int boxType, int ribbonType, bool showPurchaserName = false, int amount = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1);
        Application.Invoke<GiftPurchaseRequest, GiftPurchaseDispatchReceipt>(
            ApplicationMemberIds.GiftsPurchase,
            new GiftPurchaseRequest(
                pageId,
                offerId,
                extraData,
                receiverName,
                giftMessage,
                spriteId,
                boxType,
                ribbonType,
                showPurchaserName,
                amount),
            Ct);
    }

    private void SendIds(string name, params Id[] ids)
    {

        using Packet packet = NewPacket(Direction.Out, name);
        PacketWriter writer = packet.Writer();
        writer.WriteLength((Length)ids.Length);
        foreach (Id id in ids)
            writer.WriteId(id);
        Ext.Send(packet);
    }

    /// <summary>
    /// Shows a chat bubble above the local avatar on the local screen only.
    /// </summary>
    /// <remarks>
    /// The bubble is a whisper injected into the game client. Nothing is sent to the server and
    /// nobody else sees it.
    /// </remarks>
    /// <param name="message">The bubble text.</param>
    /// <param name="bubble">The chat bubble style id; 30 is the neutral gray bubble.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public void ShowBubble(string message, int bubble = 30) => ShowBubble(message, Me?.Index ?? -1, bubble);

    /// <summary>
    /// Shows a local only chat bubble above a specific avatar.
    /// </summary>
    /// <remarks>Nothing is sent to the server.</remarks>
    /// <param name="message">The bubble text.</param>
    /// <param name="index">
    /// The room index of the avatar the bubble appears above; -1 when the own avatar is
    /// unknown, in which case the client shows nothing.
    /// </param>
    /// <param name="bubble">The chat bubble style id.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <see langword="null"/>.</exception>
    public void ShowBubble(string message, int index, int bubble)
    {
        ArgumentNullException.ThrowIfNull(message);
        SendToClient(
            MessageContracts.Room.Chat.Whisper,
            new AvatarChat(
                index,
                message,
                0,
                bubble,
                [],
                0,
                ChatType.Whisper,
                null,
                null));
    }
}
