using Qx.Game;
using Qx.Game.Application;
using Qx.Model.Messages.Incoming;
using Qx.Model.Messages.Outgoing;
using Qx.Model;
using Qx.Protocol;

namespace Qx.Scripting;

/// <summary>
/// Specifies which slice of a group's member list to fetch, in the older API's vocabulary.
/// </summary>
/// <remarks>
/// It is translated to the native <see cref="GuildMemberSearchType"/>: <c>Members</c> becomes all
/// members, <c>Admins</c> becomes administrators and <c>Requests</c> becomes pending join
/// requests. The native enum also has a blocked users value that this one cannot express.
/// </remarks>
public enum GroupMemberSearchType
{
    /// <summary>Every member, mapped to the native <see cref="GuildMemberSearchType.All"/>.</summary>
    Members,

    /// <summary>Administrators only, mapped to the native <see cref="GuildMemberSearchType.Administrators"/>.</summary>
    Admins,

    /// <summary>Pending join requests, mapped to the native <see cref="GuildMemberSearchType.Pending"/>.</summary>
    Requests
}

/// <content>
/// A source compatibility layer: aliases and blocking wrappers that let scripts written against
/// the older Xabbo Scripter globals compile and run unchanged. Most members forward to a native
/// member of this class.
/// <para>
/// <b>Prefer the native members in new scripts.</b> The documentation of each alias names the
/// native member it forwards to.
/// </para>
/// <para>
/// <b>Two behavioral differences.</b> First, the request wrappers here are
/// <em>blocking</em>: they await the native task with <c>GetAwaiter().GetResult()</c>, so they tie
/// up the calling thread until the reply arrives or the timeout expires, and a failure surfaces as
/// the underlying exception rather than as a faulted task. Their <c>timeout</c> parameter is in
/// milliseconds. Second, several state properties here <em>throw</em>
/// <see cref="InvalidOperationException"/> when the underlying data has not been received, where
/// the native member returns <see langword="null"/>.
/// </para>
/// </content>
public partial class ScriptGlobals
{
    /// <summary>
    /// Gets the local user's own account data.
    /// </summary>
    /// <remarks>
    /// Non-nullable form of <see cref="Self"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the user's data has not been received yet.</exception>
    public Qx.Model.UserData UserData =>
        Self ?? throw new InvalidOperationException("The user's data has not been loaded.");

    /// <summary>Gets whether the local user may still change their name for free.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the user's data has not been received yet.</exception>
    public bool UserNameChangeable => UserData.IsNameChangeable;

    /// <summary>
    /// Gets the local user's achievements.
    /// </summary>
    /// <remarks>
    /// Non-nullable form of the <c>Achievements</c> property.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the achievements have not been received yet.</exception>
    public IReadOnlyCollection<Achievement> UserAchievements =>
        IsAchievementsLoaded
            ? Achievements
            : throw new InvalidOperationException("The user's achievements have not been loaded.");

    /// <summary>
    /// Gets the local user's credit balance.
    /// </summary>
    /// <remarks>
    /// Same value as <see cref="Credits"/>, but it refuses to report a wallet that has never been
    /// seen as 0.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no wallet balance has been observed yet.</exception>
    public int UserCredits
    {
        get
        {
            WalletStateView state = ReadWalletState();
            return state.CreditsLoaded && state.Credits is int credits
                ? credits
                : throw new InvalidOperationException("The user's credits have not been loaded.");
        }
    }

    /// <summary>
    /// Gets every activity point currency the local user holds, keyed by currency type id.
    /// </summary>
    /// <remarks>
    /// Every read builds a new read-only dictionary from the complete wallet state.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no activity points have been observed yet.</exception>
    public IReadOnlyDictionary<int, int> UserPoints
    {
        get
        {
            WalletStateView state = ReadWalletState(point_limit: 500);
            if (!state.PointsLoaded)
                throw new InvalidOperationException("The user's activity points have not been loaded.");
            state = WalletApplicationPages.Complete(Application, state, cancellation_token: Ct);
            return new System.Collections.ObjectModel.ReadOnlyDictionary<int, int>(
                state.ActivityPoints.Points.ToDictionary(
                    point => point.Type,
                    point => point.Amount));
        }
    }

    /// <summary>Gets the local user's diamond balance, which is activity point currency type 5.</summary>
    /// <remarks>Alias for <see cref="Diamonds"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when no activity points have been observed yet.</exception>
    public int UserDiamonds => Diamonds;

    /// <summary>Gets the local user's ducket balance, which is activity point currency type 0.</summary>
    /// <remarks>Alias for <see cref="Duckets"/>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when no activity points have been observed yet.</exception>
    public int UserDuckets => Duckets;

    /// <summary>Gets whether the local user is inside a room.</summary>
    /// <remarks>Alias for <see cref="InRoom"/>.</remarks>
    public bool IsInRoom => InRoom;

    /// <summary>
    /// Gets whether the local user is in a door queue.
    /// </summary>
    /// <remarks>Alias for <see cref="IsInRoomQueue"/>.</remarks>
    public bool IsInQueue => IsInRoomQueue;

    /// <summary>
    /// Gets the local user's place in the door queue, or -1 when they are not queued.
    /// </summary>
    /// <remarks>
    /// Same value as <see cref="RoomQueuePosition"/> with the <see langword="null"/> case folded
    /// into -1.
    /// </remarks>
    public int QueuePosition => RoomQueuePosition ?? -1;

    /// <summary>
    /// Gets whether a room is being entered right now, that is, whether the room session state is
    /// <see cref="RoomSessionState.Entering"/>.
    /// </summary>
    public bool IsLoadingRoom => RoomState is RoomSessionState.Entering;

    /// <summary>Gets whether the local user owns the room they are in.</summary>
    public bool IsRoomOwner => Room.IsOwner;

    /// <summary>
    /// Gets whether the local user may unban from this room, which the hotel grants to the owner only.
    /// </summary>
    /// <remarks>Identical to <see cref="IsRoomOwner"/>.</remarks>
    public bool CanUnban => IsRoomOwner;

    /// <summary>
    /// Gets whether the local user may mute others in the current room.
    /// </summary>
    /// <remarks>
    /// Same as <see cref="CanMuteInRoom"/>, except that room details that have not been loaded yet
    /// are reported as <see langword="false"/> rather than <see langword="null"/>.
    /// </remarks>
    public bool CanMute => CanMuteInRoom ?? false;

    /// <summary>Gets the room's door tile, or <see langword="null"/> before the heightmap arrives.</summary>
    /// <remarks>Alias for the <c>RoomEntryTile</c> property.</remarks>
    public RoomEntryTile? DoorTile => RoomEntryTile;

    /// <summary>
    /// Gets every avatar in the room: users, pets and bots.
    /// </summary>
    /// <remarks>Alias for <see cref="Avatars"/>.</remarks>
    public IEnumerable<Avatar> Entities => Avatars;

    /// <summary>
    /// Gets every item in the room, floor items followed by wall items, as one sequence of the
    /// shared base type.
    /// </summary>
    /// <remarks>Concatenates <see cref="FloorItems"/> and <see cref="WallItems"/>.</remarks>
    public IEnumerable<Furni> Furni => FloorItems.Cast<Furni>().Concat(WallItems);

    /// <summary>
    /// Gets the hotel's furniture definitions.
    /// </summary>
    /// <remarks>
    /// Non-nullable form of <see cref="Qx.Game.GameData.Furni"/> on <see cref="GameData"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the furniture data has not been loaded.</exception>
    public Qx.Game.FurniData FurniData =>
        GameData.Furni ?? throw new InvalidOperationException("Furniture data has not been loaded.");

    /// <summary>
    /// Gets the hotel's catalog product definitions.
    /// </summary>
    /// <remarks>
    /// Non-nullable form of <see cref="Qx.Game.GameData.Products"/> on <see cref="GameData"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the product data has not been loaded.</exception>
    public Qx.Game.ProductData ProductData =>
        GameData.Products ?? throw new InvalidOperationException("Product data has not been loaded.");

    /// <summary>
    /// Gets the hotel's external text table.
    /// </summary>
    /// <remarks>
    /// The table holds every localized string, including furniture, badge, effect and hand item
    /// names. Non-nullable form of <see cref="Qx.Game.GameData.Texts"/> on <see cref="GameData"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public ExternalTexts Texts =>
        GameData.Texts ?? throw new InvalidOperationException("External texts have not been loaded.");

    /// <summary>Finds a room avatar by its room index.</summary>
    /// <param name="index">The avatar's per-room index, not an account id.</param>
    /// <returns>The avatar, or <see langword="null"/> when the room holds no such index.</returns>
    public Avatar? GetEntityByIndex(int index) => Room.AvatarByIndex(index);

    /// <summary>Finds a room avatar by name, case-insensitively.</summary>
    /// <param name="name">The avatar's name.</param>
    /// <returns>The first avatar with that name, or <see langword="null"/> when none matches.</returns>
    public Avatar? GetEntity(string name) =>
        Avatars.FirstOrDefault(avatar =>
            string.Equals(avatar.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds a room avatar by its identifier.</summary>
    /// <param name="id">The user account id, pet id or bot id.</param>
    /// <returns>The avatar, or <see langword="null"/> when it is not in the room.</returns>
    public Avatar? GetEntityById(Id id) => Room.AvatarById(id);

    /// <summary>Finds a user in the room by room index.</summary>
    /// <param name="index">The avatar's per-room index.</param>
    /// <returns>
    /// The user, or <see langword="null"/> when that index is absent or holds a pet or bot.
    /// </returns>
    public User? GetUser(int index) => Room.AvatarByIndex(index) as User;

    /// <summary>Finds a user in the room by account id.</summary>
    /// <param name="id">The user's account id.</param>
    /// <returns>The user, or <see langword="null"/> when they are not in the room.</returns>
    public User? GetUserById(Id id) => GetUser(id);

    /// <summary>Finds a pet in the room by room index.</summary>
    /// <param name="index">The avatar's per-room index.</param>
    /// <returns>
    /// The pet, or <see langword="null"/> when that index is absent or holds a user or bot.
    /// </returns>
    public Pet? GetPet(int index) => Room.AvatarByIndex(index) as Pet;

    /// <summary>Finds a pet in the room by pet id.</summary>
    /// <param name="id">The pet id.</param>
    /// <returns>The pet, or <see langword="null"/> when it is not in the room.</returns>
    public Pet? GetPetById(Id id) => Room.AvatarById(id) as Pet;

    /// <summary>Finds a bot in the room by room index.</summary>
    /// <param name="index">The avatar's per-room index.</param>
    /// <returns>
    /// The bot, or <see langword="null"/> when that index is absent or holds a user or pet.
    /// </returns>
    public Bot? GetBot(int index) => Room.AvatarByIndex(index) as Bot;

    /// <summary>Finds a bot in the room by bot id.</summary>
    /// <param name="id">The bot id.</param>
    /// <returns>The bot, or <see langword="null"/> when it is not in the room.</returns>
    public Bot? GetBotById(Id id) => Room.AvatarById(id) as Bot;

    /// <summary>Finds a floor item in the room by item id.</summary>
    /// <param name="id">The floor item id.</param>
    /// <returns>The item, or <see langword="null"/> when it is not in the room.</returns>
    public FloorItem? GetFloorItem(Id id) => Room.FloorItem(id);

    /// <summary>Finds a wall item in the room by item id.</summary>
    /// <param name="id">The wall item id.</param>
    /// <returns>The item, or <see langword="null"/> when it is not in the room.</returns>
    public WallItem? GetWallItem(Id id) => Room.WallItem(id);

    /// <summary>Changes the local user's motto.</summary>
    /// <remarks>Alias for <see cref="SetMotto"/>.</remarks>
    /// <param name="motto">The new motto.</param>
    public void SetUserMotto(string motto) => SetMotto(motto);

    /// <summary>
    /// Changes the local user's figure and gender.
    /// </summary>
    /// <remarks>
    /// Wraps <see cref="UpdateFigure"/>, converting the gender to the single letter code the wire
    /// uses.
    /// </remarks>
    /// <param name="figure">The figure string.</param>
    /// <param name="gender">The avatar's gender.</param>
    public void SetUserFigure(string figure, Gender gender) =>
        UpdateFigure(gender.ToClientString(), figure);

    /// <summary>Sends a friend request to a user by name.</summary>
    /// <remarks>Alias for <see cref="AddFriend(string)"/>.</remarks>
    /// <param name="name">The target user's name.</param>
    public void FriendRequest(string name) => AddFriend(name);

    /// <summary>Sends a friend request to a user in the room.</summary>
    /// <param name="user">The target user; only its name is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    public void FriendRequest(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        AddFriend(user.Name);
    }

    /// <summary>Sends a friend request to a user in the room.</summary>
    /// <param name="user">The target user; only its name is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    public void AddFriend(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        AddFriend(user.Name);
    }

    /// <summary>Gets whether a user is on the local user's friend list.</summary>
    /// <param name="id">The user's account id.</param>
    /// <returns>
    /// <see langword="true"/> when the friend list holds that id. The friend list has to have been
    /// received; before that the result is always <see langword="false"/>.
    /// </returns>
    public bool IsFriend(Id id) => Game.Friends.FriendById(id) is not null;

    /// <summary>Gets whether a user in the room is on the local user's friend list.</summary>
    /// <param name="user">The user; only its id is used.</param>
    /// <returns><see langword="true"/> when the friend list holds that user.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    public bool IsFriend(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return IsFriend(user.Id);
    }

    /// <summary>Accepts several pending friend requests in one message.</summary>
    /// <param name="user_ids">The account ids of the requesters to accept.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user_ids"/> is <see langword="null"/>.</exception>
    public void AcceptFriendRequests(IEnumerable<Id> user_ids)
    {
        ArgumentNullException.ThrowIfNull(user_ids);
        Application.Invoke<FriendRequestIdsRequest, FriendOperationResult>(
            ApplicationMemberIds.FriendRequestAccept,
            new FriendRequestIdsRequest(user_ids.ToArray()),
            Ct);
    }

    /// <summary>Removes a friend from the friend list.</summary>
    /// <param name="friend">The friend to remove; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="friend"/> is <see langword="null"/>.</exception>
    public void RemoveFriend(Friend friend)
    {
        ArgumentNullException.ThrowIfNull(friend);
        RemoveFriend(friend.Id);
    }

    /// <summary>Removes several friends in one message.</summary>
    /// <param name="user_ids">The account ids to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user_ids"/> is <see langword="null"/>.</exception>
    public void RemoveFriends(IEnumerable<Id> user_ids)
    {
        ArgumentNullException.ThrowIfNull(user_ids);
        RemoveFriends(user_ids.ToArray());
    }

    /// <summary>Removes several friends in one message, skipping null entries.</summary>
    /// <param name="friends">The friends to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="friends"/> is <see langword="null"/>.</exception>
    public void RemoveFriends(IEnumerable<Friend> friends)
    {
        ArgumentNullException.ThrowIfNull(friends);
        RemoveFriends(friends
            .Where(friend => friend is not null)
            .Select(friend => friend.Id));
    }

    /// <summary>Sends a console (private) message to a friend.</summary>
    /// <param name="friend">The recipient; only its id is used.</param>
    /// <param name="message">The message text.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="friend"/> is <see langword="null"/>.</exception>
    public void SendMessage(Friend friend, string message)
    {
        ArgumentNullException.ThrowIfNull(friend);
        SendMessage(friend.Id, message);
    }

    /// <summary>Adds a user in the room to the ignore list.</summary>
    /// <param name="user">The user; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    public void Ignore(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Ignore(user.Id);
    }

    /// <summary>Removes a user in the room from the ignore list.</summary>
    /// <param name="user">The user; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    public void Unignore(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        Unignore(user.Id);
    }

    /// <summary>Gives a respect to a user.</summary>
    /// <remarks>Alias for <see cref="RespectUser(Id)"/>.</remarks>
    /// <param name="user_id">The target user's account id.</param>
    public void Respect(Id user_id) => RespectUser(user_id);

    /// <summary>Gives a respect to a user in the room.</summary>
    /// <remarks>Alias for <see cref="RespectUser(User)"/>.</remarks>
    /// <param name="user">The target user.</param>
    public void Respect(User user) => RespectUser(user);

    /// <summary>
    /// Scratches (respects) a pet, which is what raises its happiness.
    /// </summary>
    /// <remarks>Alias for <see cref="RespectPet"/>.</remarks>
    /// <param name="pet_id">The pet id.</param>
    public void Scratch(Id pet_id) => RespectPet(pet_id);

    /// <summary>Scratches a pet in the room.</summary>
    /// <param name="pet">The pet; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pet"/> is <see langword="null"/>.</exception>
    public void Scratch(Pet pet)
    {
        ArgumentNullException.ThrowIfNull(pet);
        RespectPet(pet.Id);
    }

    /// <summary>Mounts or dismounts a rideable pet.</summary>
    /// <remarks>Alias for <see cref="MountPet"/>.</remarks>
    /// <param name="pet_id">The pet id.</param>
    /// <param name="mount"><see langword="true"/> to get on; <see langword="false"/> to get off.</param>
    public void Ride(Id pet_id, bool mount) => MountPet(pet_id, mount);

    /// <summary>Mounts or dismounts a rideable pet in the room.</summary>
    /// <param name="pet">The pet; only its id is used.</param>
    /// <param name="mount"><see langword="true"/> to get on; <see langword="false"/> to get off.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pet"/> is <see langword="null"/>.</exception>
    public void Ride(Pet pet, bool mount)
    {
        ArgumentNullException.ThrowIfNull(pet);
        MountPet(pet.Id, mount);
    }

    /// <summary>Gets on a rideable pet.</summary>
    /// <remarks>Alias for <see cref="MountPet"/>.</remarks>
    /// <param name="pet_id">The pet id.</param>
    public void Mount(Id pet_id) => MountPet(pet_id);

    /// <summary>Gets on a rideable pet in the room.</summary>
    /// <param name="pet">The pet; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pet"/> is <see langword="null"/>.</exception>
    public void Mount(Pet pet)
    {
        ArgumentNullException.ThrowIfNull(pet);
        MountPet(pet.Id);
    }

    /// <summary>Gets off a rideable pet.</summary>
    /// <remarks>Alias for <see cref="DismountPet"/>.</remarks>
    /// <param name="pet_id">The pet id.</param>
    public void Dismount(Id pet_id) => DismountPet(pet_id);

    /// <summary>Gets off a rideable pet in the room.</summary>
    /// <param name="pet">The pet; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pet"/> is <see langword="null"/>.</exception>
    public void Dismount(Pet pet)
    {
        ArgumentNullException.ThrowIfNull(pet);
        DismountPet(pet.Id);
    }

    /// <summary>
    /// Leaves a group.
    /// </summary>
    /// <remarks>
    /// There is no dedicated message for leaving: the client kicks the local user out of the
    /// group, so the call forwards to <see cref="KickGroupMember"/> with the own account id.
    /// </remarks>
    /// <param name="group_id">The id of the group to leave.</param>
    /// <exception cref="InvalidOperationException">Thrown when the user's data has not been received yet.</exception>
    public void LeaveGroup(Id group_id) =>
        KickGroupMember(group_id, UserData.Id);

    /// <summary>
    /// Makes a group the local user's favorite, shown on their avatar.
    /// </summary>
    /// <remarks>Alias for <see cref="SetFavouriteGroup"/>.</remarks>
    /// <param name="group_id">The group id.</param>
    public void SetGroupFavourite(Id group_id) =>
        SetFavouriteGroup(group_id);

    /// <summary>
    /// Clears the local user's favorite group.
    /// </summary>
    /// <remarks>Alias for <see cref="UnsetFavouriteGroup"/>.</remarks>
    /// <param name="group_id">The id of the group to clear as favorite.</param>
    public void RemoveGroupFavourite(Id group_id) =>
        UnsetFavouriteGroup(group_id);

    /// <summary>
    /// Approves a pending group join request.
    /// </summary>
    /// <remarks>Alias for <see cref="ApproveGroupMember"/>.</remarks>
    /// <param name="group_id">The group id.</param>
    /// <param name="user_id">The requesting user's account id.</param>
    public void AcceptGroupMember(Id group_id, Id user_id) =>
        ApproveGroupMember(group_id, user_id);

    /// <summary>
    /// Requests one page of a group's member list, blocking the calling thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetGuildMembers"/> that takes the older
    /// <see cref="GroupMemberSearchType"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="group_id">The group id.</param>
    /// <param name="page">The zero-based page index.</param>
    /// <param name="filter">The user name filter; empty means no filter.</param>
    /// <param name="search_type">The slice of the member list to fetch.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The requested page of members.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="page"/> is negative or <paramref name="search_type"/> is not a defined value.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/> is <see langword="null"/>.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no reply arrived in time.</exception>
    public GuildMembers GetGroupMembers(
    Id group_id,
    int page = 0,
    string filter = "",
    GroupMemberSearchType search_type = GroupMemberSearchType.Members,
    int timeout = 10000) =>
    GetGuildMembers(
        group_id,
        page,
        filter,
        search_type switch
        {
            GroupMemberSearchType.Members => GuildMemberSearchType.All,
            GroupMemberSearchType.Admins => GuildMemberSearchType.Administrators,
            GroupMemberSearchType.Requests => GuildMemberSearchType.Pending,
            _ => throw new ArgumentOutOfRangeException(
                nameof(search_type),
                search_type,
                "Unsupported group member search type.")
        },
        timeout)
        .GetAwaiter()
        .GetResult();

    /// <summary>
    /// Requests the groups the local user belongs to, blocking the calling thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetGuildMemberships"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The user's group memberships, or an empty list when the user is in no group.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no reply arrived in time.</exception>
    public IReadOnlyList<GuildMembership> GetUserGroups(int timeout = 10000) =>
        GetGuildMemberships(timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Requests the local user's achievement list, blocking the calling thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetAchievements"/>. The reply also refreshes the tracked
    /// achievement state. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The achievements.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no reply arrived in time.</exception>
    public Achievements GetUserAchievements(int timeout = 10000) =>
        GetAchievements(timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Runs a navigator search and returns the raw result blocks, blocking the calling thread
    /// until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="SearchRooms"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="category">The navigator view code, for example <c>hotel_view</c> or <c>query</c>.</param>
    /// <param name="filter">The filter text; empty means no filter.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The navigator result.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public NavigatorSearchResult GetNav(
        string category,
        string filter = "",
        int timeout = 10000) =>
        SearchRooms(category, filter, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Runs a navigator search and returns the flattened room list as a query, blocking the
    /// calling thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="SearchRoomQuery"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="category">The navigator view code.</param>
    /// <param name="filter">The filter text; empty means no filter.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>A query over the rooms found.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public RoomDataQuery SearchNav(
        string category,
        string filter = "",
        int timeout = 10000) =>
        SearchRoomQuery(category, filter, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Runs a free text navigator search, blocking the calling thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Runs the <c>query</c> view with the given text, which also accepts the navigator prefixes
    /// such as <c>owner:</c>, <c>roomname:</c>, <c>tag:</c> and <c>group:</c>.
    /// </remarks>
    /// <param name="query">The search text.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>A query over the rooms found.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public RoomDataQuery QueryNav(string query, int timeout = 10000) =>
        SearchRoomQuery("query", query, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Searches the navigator for rooms whose name contains the given text, blocking the calling
    /// thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="SearchRoomsByName"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="room_name">The room name or fragment, compared case-insensitively.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The rooms found.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public IReadOnlyList<RoomData> SearchNavByName(
        string room_name,
        int timeout = 10000) =>
        SearchRoomsByName(room_name, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Searches the navigator for rooms owned by a user, blocking the calling thread until the
    /// reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="SearchRoomsByOwner"/>, which keeps only exact owner name
    /// matches. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="owner_name">The owner's name, compared case-insensitively.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The rooms found.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public IReadOnlyList<RoomData> SearchNavByOwner(
        string owner_name,
        int timeout = 10000) =>
        SearchRoomsByOwner(owner_name, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Searches the navigator for rooms carrying a tag, blocking the calling thread until the
    /// reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="SearchRoomsByTag"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="tag">The tag, without a leading <c>#</c>.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The rooms found.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public IReadOnlyList<RoomData> SearchNavByTag(
        string tag,
        int timeout = 10000) =>
        SearchRoomsByTag(tag, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Searches the navigator for rooms that belong to a group, blocking the calling thread until
    /// the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="SearchRoomsByGroup"/>, which keeps only rooms whose group name
    /// contains the text. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="group_name">The group's name or fragment, compared case-insensitively.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The rooms found.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching result arrived in time.</exception>
    public IReadOnlyList<RoomData> SearchNavByGroup(
        string group_name,
        int timeout = 10000) =>
        SearchRoomsByGroup(group_name, timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Gets the catalog's page tree, blocking the calling thread until it is available.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetCatalogIndex"/>, so a cached copy younger than
    /// <see cref="Qx.Game.CatalogManager.DefaultMaxAge"/> is returned without a request. Prefer
    /// the awaitable native method.
    /// </remarks>
    /// <param name="type">
    /// The catalog to read: <c>NORMAL</c> for the shop, <c>BUILDERS_CLUB</c> for the Builders Club
    /// catalog.
    /// </param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The catalog index.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching index arrived in time.</exception>
    public CatalogIndex GetCatalog(
        string type = "NORMAL",
        int timeout = 10000) =>
        GetCatalogIndex(type, timeout).GetAwaiter().GetResult();

    /// <summary>Gets the Builders Club catalog index, blocking the calling thread until it is available.</summary>
    /// <remarks>
    /// Same as <see cref="GetCatalog"/> with <c>BUILDERS_CLUB</c>, including the use of the catalog cache.
    /// </remarks>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The Builders Club catalog index.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching index arrived in time.</exception>
    public CatalogIndex GetBcCatalog(int timeout = 10000) =>
        GetCatalog("BUILDERS_CLUB", timeout);

    /// <summary>Gets one Builders Club catalog page, blocking the calling thread until it is available.</summary>
    /// <remarks>
    /// Blocking form of <see cref="GetCatalogPage"/> with no preselected offer, so a cached copy
    /// younger than <see cref="Qx.Game.CatalogManager.DefaultMaxAge"/> is returned without a
    /// request. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="page_id">The page id from the catalog index.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The page and its offers.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching page arrived in time.</exception>
    public CatalogPage GetBcCatalogPage(int page_id, int timeout = 10000) =>
        GetCatalogPage(page_id, -1, "BUILDERS_CLUB", timeout)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    /// Buys an offer from the catalog.
    /// </summary>
    /// <remarks>
    /// Same as <see cref="PurchaseFromCatalog"/> with the argument order the older API used: count
    /// before extra data. The call does not wait; the purchase result arrives as its own message.
    /// </remarks>
    /// <param name="page_id">The catalog page id.</param>
    /// <param name="offer_id">The offer id on that page.</param>
    /// <param name="count">The number of items to buy.</param>
    /// <param name="extra">
    /// The offer's extra data, for example a chosen color or the text on a personalized item.
    /// </param>
    public void Purchase(
        int page_id,
        int offer_id,
        int count = 1,
        string extra = "") =>
        PurchaseFromCatalog(page_id, offer_id, extra, count);

    /// <summary>
    /// Requests the local user's own marketplace offers, blocking the calling thread until the
    /// reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetMyMarketplaceOffers(int)"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The own offer list.</returns>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no reply arrived in time.</exception>
    public MarketplaceOwnOfferPage GetUserMarketplaceOffers(int timeout = 10000) =>
        GetMyMarketplaceOffers(timeout).GetAwaiter().GetResult();

    /// <summary>
    /// Requests the marketplace price statistics for one furni kind, blocking the calling thread
    /// until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetMarketplaceStats(int, int, int)"/> that takes the item type
    /// as an enum. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="type">
    /// The item type: <c>Floor</c> maps to marketplace category 1, <c>Wall</c> to 2. Limited
    /// editions cannot be expressed here.
    /// </param>
    /// <param name="kind">The furni type id.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The price statistics.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> is neither floor nor wall.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching stats arrived in time.</exception>
    public MarketplaceItemStatsSnapshot GetMarketplaceInfo(
        ItemType type,
        int kind,
        int timeout = 10000) =>
        GetMarketplaceStats(type switch
        {
            ItemType.Floor => 1,
            ItemType.Wall => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported item type.")
        }, kind, timeout)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    /// Requests the marketplace price statistics for a room item's kind, blocking the calling
    /// thread until the reply arrives.
    /// </summary>
    /// <param name="item">The room item; its type and kind are used.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The price statistics for that furni kind.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the item is neither a floor nor a wall item.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching stats arrived in time.</exception>
    public MarketplaceItemStatsSnapshot GetMarketplaceInfo(
        Furni item,
        int timeout = 10000)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetMarketplaceInfo(item.Type, item.Kind, timeout);
    }

    /// <summary>
    /// Requests the marketplace price statistics for an inventory item's kind, blocking the
    /// calling thread until the reply arrives.
    /// </summary>
    /// <param name="item">The inventory item; its type and kind are used.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The price statistics for that furni kind.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the item is neither a floor nor a wall item.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching stats arrived in time.</exception>
    public MarketplaceItemStatsSnapshot GetMarketplaceInfo(
        InventoryItem item,
        int timeout = 10000)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetMarketplaceInfo(item.Type, item.Kind, timeout);
    }

    /// <summary>
    /// Requests the marketplace price statistics for a furniture definition, blocking the calling
    /// thread until the reply arrives.
    /// </summary>
    /// <param name="item">The furniture definition; its type and kind are used.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The price statistics for that furni kind.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the definition is neither a floor nor a wall item.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching stats arrived in time.</exception>
    public MarketplaceItemStatsSnapshot GetMarketplaceInfo(
        FurniInfo item,
        int timeout = 10000)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetMarketplaceInfo(item.Type, item.Kind, timeout);
    }

    /// <summary>
    /// Uses a room item, picking the floor or wall message from the item's runtime type.
    /// </summary>
    /// <param name="item">The item to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the item is neither a floor nor a wall item.</exception>
    public void UseFurni(Furni item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is FloorItem floor_item)
            UseFloorItem(floor_item.Id);
        else if (item is WallItem wall_item)
            UseWallItem(wall_item.Id);
        else
            throw new ArgumentException("Unsupported furniture type.", nameof(item));
    }

    /// <summary>Uses a floor item.</summary>
    /// <remarks>Alias for <see cref="UseFloorItem(Id, int)"/> with the default state.</remarks>
    /// <param name="item">The item; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void UseFloorItem(FloorItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        UseFloorItem(item.Id);
    }

    /// <summary>Uses a wall item.</summary>
    /// <remarks>Alias for <see cref="UseWallItem(Id, int)"/> with the default state.</remarks>
    /// <param name="item">The item; only its id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void UseWallItem(WallItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        UseWallItem(item.Id);
    }

    /// <summary>
    /// Switches a room item to a specific state instead of cycling it, picking the floor or wall
    /// message from the item's runtime type.
    /// </summary>
    /// <param name="item">The item to switch.</param>
    /// <param name="state">The state to request. Its meaning is furniture-specific.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the item is neither a floor nor a wall item.</exception>
    public void ToggleFurni(Furni item, int state)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is FloorItem floor_item)
            ToggleFloorItem(floor_item.Id, state);
        else if (item is WallItem wall_item)
            ToggleWallItem(wall_item.Id, state);
        else
            throw new ArgumentException("Unsupported furniture type.", nameof(item));
    }

    /// <summary>
    /// Switches a floor item to a specific state.
    /// </summary>
    /// <remarks>
    /// Same as <see cref="UseFloorItem(Id, int)"/> with an explicit state rather than the default 0.
    /// </remarks>
    /// <param name="item_id">The floor item id.</param>
    /// <param name="state">The state to request.</param>
    public void ToggleFloorItem(Id item_id, int state) =>
        UseFloorItem(item_id, state);

    /// <summary>
    /// Switches a wall item to a specific state.
    /// </summary>
    /// <remarks>
    /// Same as <see cref="UseWallItem(Id, int)"/> with an explicit state rather than the default 0.
    /// </remarks>
    /// <param name="item_id">The wall item id.</param>
    /// <param name="state">The state to request.</param>
    public void ToggleWallItem(Id item_id, int state) =>
        UseWallItem(item_id, state);

    /// <summary>Walks to a tile.</summary>
    /// <remarks>Alias for <see cref="Walk(int, int)"/>.</remarks>
    /// <param name="location">The target tile.</param>
    public void Move(Point location) => Move(location.X, location.Y);

    /// <summary>Walks to a tile.</summary>
    /// <remarks>Alias for <see cref="Walk(int, int)"/>.</remarks>
    /// <param name="location">The target tile.</param>
    public void Walk(Point location) => Walk(location.X, location.Y);

    /// <summary>Walks to a tile.</summary>
    /// <remarks>Alias for <see cref="Walk(int, int)"/>.</remarks>
    /// <param name="location">The target tile.</param>
    public void WalkTo(Point location) => WalkTo(location.X, location.Y);

    /// <summary>Turns the avatar to face a tile without moving.</summary>
    /// <remarks>Alias for <see cref="LookTo(int, int)"/>.</remarks>
    /// <param name="location">The tile to face.</param>
    public void LookTo(Point location) => LookTo(location.X, location.Y);

    /// <summary>Turns the avatar to face a tile without moving.</summary>
    /// <remarks>Alias for <see cref="LookTo(int, int)"/>.</remarks>
    /// <param name="location">The tile to face.</param>
    public void FaceTo(Point location) => FaceTo(location.X, location.Y);

    /// <summary>
    /// Turns the avatar to one of the eight compass directions.
    /// </summary>
    /// <remarks>
    /// The hotel has no "face direction" message, so the call aims at a far-off tile in that
    /// direction with <see cref="LookTo(int, int)"/> and lets the server work the facing out.
    /// </remarks>
    /// <param name="direction">
    /// 0 north, 1 north-east, 2 east, 3 south-east, 4 south, 5 south-west, 6 west, 7 north-west.
    /// Values outside 0-7 wrap, including negative ones.
    /// </param>
    public void Turn(int direction) => LookTo(DirectionTarget(direction));

    /// <summary>Turns the avatar to one of the eight compass directions.</summary>
    /// <param name="direction">The compass direction.</param>
    public void Turn(Directions direction) => Turn((int)direction);

    /// <summary>Places a floor item from the inventory into the room.</summary>
    /// <param name="item">The inventory item to place.</param>
    /// <param name="location">The target tile.</param>
    /// <param name="direction">The item's rotation, 0-7 in the same compass order as avatars.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the inventory item is not a floor item.</exception>
    public void Place(InventoryItem item, Point location, int direction = 0)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsFloorItem)
            throw new InvalidOperationException("The inventory item is not a floor item.");
        PlaceFloorItem(item.ItemId, location, direction);
    }

    /// <summary>Places a wall item from the inventory onto a wall.</summary>
    /// <param name="item">The inventory item to place.</param>
    /// <param name="location">The wall position.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the inventory item is not a wall item.</exception>
    public void Place(InventoryItem item, WallLocation location)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsWallItem)
            throw new InvalidOperationException("The inventory item is not a wall item.");
        PlaceWallItem(item.ItemId, location);
    }

    /// <summary>Places a floor item from the inventory at a tile.</summary>
    /// <param name="item_id">The inventory item id.</param>
    /// <param name="location">The target tile.</param>
    /// <param name="direction">The item's rotation, 0-7.</param>
    public void PlaceFloorItem(Id item_id, Point location, int direction = 0) =>
        PlaceFloorItem(item_id, location.X, location.Y, direction);

    /// <summary>Places a wall item from the inventory at a wall position.</summary>
    /// <param name="item_id">The inventory item id.</param>
    /// <param name="location">The wall position, formatted into the hotel's wall-location string.</param>
    public void PlaceWallItem(Id item_id, WallLocation location) =>
        PlaceWallItem(item_id, location.ToString());

    /// <summary>Moves a floor item already in the room to another tile and rotation.</summary>
    /// <param name="item">The item to move; only its id is used.</param>
    /// <param name="location">The target tile.</param>
    /// <param name="direction">The item's rotation, 0-7.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void Move(FloorItem item, Point location, int direction = 0)
    {
        ArgumentNullException.ThrowIfNull(item);
        MoveFloorItem(item.Id, location, direction);
    }

    /// <summary>Moves a wall item already in the room to another wall position.</summary>
    /// <param name="item">The item to move; only its id is used.</param>
    /// <param name="location">The target wall position.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void Move(WallItem item, WallLocation location)
    {
        ArgumentNullException.ThrowIfNull(item);
        MoveWallItem(item.Id, location);
    }

    /// <summary>Moves a floor item in the room to another tile and rotation.</summary>
    /// <param name="item_id">The floor item id.</param>
    /// <param name="location">The target tile.</param>
    /// <param name="direction">The item's rotation, 0-7.</param>
    public void MoveFloorItem(Id item_id, Point location, int direction = 0) =>
        MoveFloorItem(item_id, location.X, location.Y, direction);

    /// <summary>Moves a wall item in the room to another wall position.</summary>
    /// <param name="item_id">The wall item id.</param>
    /// <param name="location">The target wall position.</param>
    public void MoveWallItem(Id item_id, WallLocation location) =>
        MoveWallItem(item_id, location.ToString());

    /// <summary>
    /// Picks a room item up into the inventory, picking the floor or wall message from the item's
    /// runtime type.
    /// </summary>
    /// <param name="item">The item to pick up.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the item is neither a floor nor a wall item.</exception>
    public void Pickup(Furni item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is FloorItem floor_item)
            PickupFurni(floor_item);
        else if (item is WallItem wall_item)
            PickupFurni(wall_item);
        else
            throw new ArgumentException("Unsupported furniture type.", nameof(item));
    }

    /// <summary>Picks a floor item up into the inventory, by id.</summary>
    /// <param name="item_id">The floor item id.</param>
    public void PickupFloorItem(Id item_id) =>
        PickupFurni(new FloorItem { Id = item_id });

    /// <summary>Picks a wall item up into the inventory, by id.</summary>
    /// <param name="item_id">The wall item id.</param>
    public void PickupWallItem(Id item_id) =>
        PickupFurni(new WallItem { Id = item_id });

    /// <summary>
    /// Places a blank sticky note on a wall.
    /// </summary>
    /// <remarks>Alias for <see cref="PlacePostIt"/>.</remarks>
    /// <param name="item_id">The inventory item id of the sticky pad.</param>
    /// <param name="location">The wall position.</param>
    public void PlaceSticky(Id item_id, WallLocation location) =>
        PlacePostIt(item_id, location.ToString());

    /// <summary>Places a blank sticky note from the inventory on a wall.</summary>
    /// <param name="item">The inventory item; it must be in the sticky-note category (5).</param>
    /// <param name="location">The wall position.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the inventory item is not a sticky note.</exception>
    public void PlaceSticky(InventoryItem item, WallLocation location)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Category != 5)
            throw new InvalidOperationException("The inventory item is not a sticky note.");
        PlaceSticky(item.ItemId, location);
    }

    /// <summary>
    /// Places a sticky note and writes it in one step.
    /// </summary>
    /// <remarks>Alias for <see cref="AddPostIt"/>.</remarks>
    /// <param name="item_id">The inventory item id of the sticky pad.</param>
    /// <param name="location">The wall position.</param>
    /// <param name="color">The note's background color.</param>
    /// <param name="text">The note's text.</param>
    public void PlaceStickyWithPole(
        Id item_id,
        WallLocation location,
        string color,
        string text) =>
        AddPostIt(item_id, location.ToString(), color, text);

    /// <summary>Places a sticky note from the inventory and writes it in one step.</summary>
    /// <param name="item">The inventory item; only its id is used.</param>
    /// <param name="location">The wall position.</param>
    /// <param name="color">The note's background color.</param>
    /// <param name="text">The note's text.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void PlaceStickyWithPole(
        InventoryItem item,
        WallLocation location,
        string color,
        string text)
    {
        ArgumentNullException.ThrowIfNull(item);
        PlaceStickyWithPole(item.ItemId, location, color, text);
    }

    /// <summary>
    /// Requests a sticky note's color and text, blocking the calling thread until the reply arrives.
    /// </summary>
    /// <remarks>
    /// Blocking form of <see cref="GetSticky(Id, int)"/>. Prefer the awaitable native method.
    /// </remarks>
    /// <param name="item">The wall item holding the note; only its id is used.</param>
    /// <param name="timeout">The total time budget in milliseconds.</param>
    /// <returns>The note's contents.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="Qx.Game.RequestTimeoutException">Thrown when no matching item data arrived in time.</exception>
    public Sticky GetSticky(WallItem item, int timeout = 10000)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetSticky(item.Id, timeout).GetAwaiter().GetResult();
    }

    private static TradeParticipantView? TradeParticipantOf(
        TradeEpochView trade,
        Id user_id) =>
        trade.FirstParticipant.UserId == user_id
            ? trade.FirstParticipant
            : trade.SecondParticipant.UserId == user_id
                ? trade.SecondParticipant
                : null;

    private TradeParticipantView? TradePartnerOf(TradeEpochView trade) =>
        trade.FirstParticipant.UserId == UserId
            ? trade.SecondParticipant
            : trade.SecondParticipant.UserId == UserId
                ? trade.FirstParticipant
                : null;

    private static TradeOfferView? TradeOfferOf(TradeEpochView trade, Id user_id) =>
        trade.FirstOffer?.UserId == user_id
            ? trade.FirstOffer
            : trade.SecondOffer?.UserId == user_id
                ? trade.SecondOffer
                : null;

    /// <summary>
    /// Gets whether the local user is the side that opened the trade rather than the side that
    /// was invited.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> when no trade is open.
    /// </remarks>
    public bool IsTrader =>
        Trade.Active?.FirstParticipant.UserId == UserId;

    /// <summary>
    /// Gets whether the local user has accepted the current offer.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> when no trade is open. Accepting is reset whenever either side
    /// changes their offer.
    /// </remarks>
    public bool HasAcceptedTrade
    {
        get
        {
            TradeEpochView? trade = Trade.Active;
            return trade is not null &&
                TradeParticipantOf(trade, UserId)?.Accepted == true;
        }
    }

    /// <summary>
    /// The other participant's id, whichever wire slot they occupy.
    /// </summary>
    private Id? TradePartnerId
    {
        get
        {
            TradeEpochView? trade = Trade.Active;
            return trade is null ? null : TradePartnerOf(trade)?.UserId;
        }
    }

    /// <summary>
    /// Gets whether the trading partner has accepted the current offer.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> when no trade is open.
    /// </remarks>
    public bool HasPartnerAcceptedTrade
    {
        get
        {
            TradeEpochView? trade = Trade.Active;
            return trade is not null && TradePartnerOf(trade)?.Accepted == true;
        }
    }

    /// <summary>
    /// Gets the user on the other side of the trade, looked up in the room.
    /// </summary>
    /// <value>
    /// The partner, or <see langword="null"/> when no trade is open or that user is no longer in
    /// the room.
    /// </value>
    public User? TradePartner =>
        TradePartnerId is Id partner_id
            ? Room.AvatarById(partner_id) as User
            : null;

    /// <summary>
    /// Gets the local user's side of the trade: the items offered and the credit amount.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when no trade is open or until the server has sent the first item
    /// list.
    /// </remarks>
    public TradeOfferView? OwnTradeOffer
    {
        get
        {
            TradeEpochView? trade = Trade.Active;
            return trade is null ? null : TradeOfferOf(trade, UserId);
        }
    }

    /// <summary>
    /// Gets the trading partner's side of the trade.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when no trade is open or until the server has sent the first item
    /// list.
    /// </remarks>
    public TradeOfferView? PartnerTradeOffer
    {
        get
        {
            TradeEpochView? trade = Trade.Active;
            TradeParticipantView? partner = trade is null ? null : TradePartnerOf(trade);
            return trade is null || partner is null
                ? null
                : TradeOfferOf(trade, partner.UserId);
        }
    }

    /// <summary>Adds one inventory item to the trade.</summary>
    /// <remarks>Alias for <see cref="OfferTradeItem"/>.</remarks>
    /// <param name="item_id">The inventory item id.</param>
    public void Offer(Id item_id) =>
        OfferTradeItem(item_id);

    /// <summary>Adds one inventory item to the trade.</summary>
    /// <param name="item">The inventory item; only its item id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void Offer(InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Offer(item.ItemId);
    }

    /// <summary>Adds several inventory items to the trade in one message.</summary>
    /// <param name="item_ids">The inventory item ids.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item_ids"/> is <see langword="null"/>.</exception>
    public void Offer(IEnumerable<Id> item_ids)
    {
        ArgumentNullException.ThrowIfNull(item_ids);
        OfferTradeItems(item_ids.Select(item_id => (long)item_id).ToArray());
    }

    /// <summary>Adds several inventory items to the trade, skipping null entries.</summary>
    /// <param name="items">The inventory items.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is <see langword="null"/>.</exception>
    public void Offer(IEnumerable<InventoryItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Offer(items
            .Where(item => item is not null)
            .Select(item => item.ItemId));
    }

    /// <summary>
    /// Takes one item back off the trade.
    /// </summary>
    /// <remarks>Alias for <see cref="RemoveTradeItem"/>.</remarks>
    /// <param name="item_id">The inventory item id.</param>
    public void CancelOffer(Id item_id) =>
        RemoveTradeItem(item_id);

    /// <summary>Takes one item back off the trade.</summary>
    /// <param name="item">The inventory item; only its item id is used.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    public void CancelOffer(InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        CancelOffer(item.ItemId);
    }

    /// <summary>
    /// Blocks the calling thread for an interval, waking early if the script is stopped.
    /// </summary>
    /// <remarks>
    /// Alias for <see cref="Sleep(TimeSpan)"/>. Unlike <see cref="Delay(int)"/>, this overload
    /// blocks and returns nothing to await.
    /// </remarks>
    /// <param name="duration">The time to sleep.</param>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped while sleeping.</exception>
    public void Delay(TimeSpan duration) =>
        Sleep(duration);

    /// <summary>
    /// Asynchronously waits for an interval, observing the script's stop token.
    /// </summary>
    /// <param name="duration">The time to wait.</param>
    /// <returns>A task that completes after the interval.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped while waiting.</exception>
    public Task DelayAsync(TimeSpan duration) =>
        Task.Delay(duration, Ct);

    /// <summary>
    /// Gets a non-negative pseudo-random integer from the shared thread-safe generator.
    /// </summary>
    /// <remarks>
    /// Not suitable for anything security-sensitive.
    /// </remarks>
    /// <returns>A random value from 0 up to but not including <see cref="int.MaxValue"/>.</returns>
    public int Rand() => Random.Shared.Next();

    /// <summary>Fills a buffer with pseudo-random bytes.</summary>
    /// <remarks>The bytes are not cryptographically secure.</remarks>
    /// <param name="buffer">The buffer to fill; every byte is overwritten.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is <see langword="null"/>.</exception>
    public void Rand(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        Random.Shared.NextBytes(buffer);
    }

    /// <summary>
    /// Stores a value in the process-wide global store only if the key is not taken.
    /// </summary>
    /// <remarks>
    /// Several script runs can race to initialize shared state without overwriting each other.
    /// The store is the one read by <see cref="GetGlobal(string)"/>.
    /// </remarks>
    /// <param name="key">The key. Compared case-sensitively.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>
    /// <see langword="true"/> when this call stored the value, <see langword="false"/> when the key
    /// already existed and nothing changed.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public bool InitGlobal(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        lock (_global_sync)
        {
            if (_globals.ContainsKey(key))
                return false;
            _globals[key] = value;
            return true;
        }
    }

    /// <summary>
    /// Stores a lazily built value in the process-wide global store only if the key is not taken.
    /// </summary>
    /// <remarks>
    /// The factory runs only when the key is free, under the store's lock, so an expensive
    /// initialization is skipped on the losing side of a race.
    /// </remarks>
    /// <param name="key">The key. Compared case-sensitively.</param>
    /// <param name="value_factory">The factory that builds the value; it must not return <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when this call stored the value, <see langword="false"/> when the key
    /// already existed and the factory was never called.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is <see langword="null"/>, empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value_factory"/> is <see langword="null"/>, or it returned <see langword="null"/>.
    /// </exception>
    public bool InitGlobal(string key, Func<object> value_factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value_factory);
        lock (_global_sync)
        {
            if (_globals.ContainsKey(key))
                return false;
            object value = value_factory();
            ArgumentNullException.ThrowIfNull(value);
            _globals[key] = value;
            return true;
        }
    }

    /// <summary>
    /// Gets the straight-line distance between two tiles, in tiles.
    /// </summary>
    /// <remarks>
    /// Avatars walk diagonally, so the result is not the number of steps between them.
    /// </remarks>
    /// <param name="first">The first tile.</param>
    /// <param name="second">The second tile.</param>
    /// <returns>The Euclidean distance.</returns>
    public static double Distance(Point first, Point second) =>
        Distance(first.X, first.Y, second.X, second.Y);

    /// <summary>
    /// Gets the furniture definition behind a room item.
    /// </summary>
    /// <remarks>Alias for <see cref="FurniOf(Furni)"/>.</remarks>
    /// <param name="item">The room item.</param>
    /// <returns>
    /// The definition, or <see langword="null"/> when the furniture data has not been loaded or has
    /// no entry for that kind.
    /// </returns>
    public FurniInfo? GetFurniInfo(Furni item) => FurniOf(item);

    /// <summary>
    /// Gets the localized display name of a room item.
    /// </summary>
    /// <remarks>Alias for <see cref="FurniName(Furni)"/>.</remarks>
    /// <param name="item">The room item.</param>
    /// <returns>
    /// The item's name, or <c>"#"</c> followed by the numeric kind when the furniture data has not
    /// been loaded or the kind is unknown.
    /// </returns>
    public string GetFurniName(Furni item) => FurniName(item);

    /// <summary>
    /// Looks up a badge's localized name in the external texts, under the key
    /// <c>badge_name_&lt;code&gt;</c>.
    /// </summary>
    /// <param name="code">The badge code.</param>
    /// <param name="name">Receives the name, or <see langword="null"/> when the key is absent.</param>
    /// <returns><see langword="true"/> when the text table has that key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public bool TryGetBadgeName(string code, out string? name) =>
        TryGetText($"badge_name_{code}", out name);

    /// <summary>Gets a badge's localized name.</summary>
    /// <param name="code">The badge code.</param>
    /// <returns>The name, or <see langword="null"/> when the text table has no entry for it.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public string? GetBadgeName(string code) =>
        TryGetBadgeName(code, out string? name) ? name : null;

    /// <summary>
    /// Looks up a badge's localized description under the key <c>badge_desc_&lt;code&gt;</c>.
    /// </summary>
    /// <param name="code">The badge code.</param>
    /// <param name="description">Receives the description, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> when the text table has that key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public bool TryGetBadgeDescription(string code, out string? description) =>
        TryGetText($"badge_desc_{code}", out description);

    /// <summary>Gets a badge's localized description.</summary>
    /// <param name="code">The badge code.</param>
    /// <returns>The description, or <see langword="null"/> when absent.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public string? GetBadgeDescription(string code) =>
        TryGetBadgeDescription(code, out string? description)
            ? description
            : null;

    /// <summary>
    /// Looks up an avatar effect's localized name under the key <c>fx_&lt;id&gt;</c>.
    /// </summary>
    /// <param name="id">The effect id.</param>
    /// <param name="name">Receives the name, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> when the text table has that key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public bool TryGetEffectName(int id, out string? name) =>
        TryGetText($"fx_{id}", out name);

    /// <summary>Gets an avatar effect's localized name.</summary>
    /// <param name="id">The effect id.</param>
    /// <returns>The name, or <see langword="null"/> when absent.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public string? GetEffectName(int id) =>
        TryGetEffectName(id, out string? name) ? name : null;

    /// <summary>
    /// Looks up an avatar effect's localized description under the key <c>fx_&lt;id&gt;_desc</c>.
    /// </summary>
    /// <param name="id">The effect id.</param>
    /// <param name="description">Receives the description, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> when the text table has that key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public bool TryGetEffectDescription(int id, out string? description) =>
        TryGetText($"fx_{id}_desc", out description);

    /// <summary>Gets an avatar effect's localized description.</summary>
    /// <param name="id">The effect id.</param>
    /// <returns>The description, or <see langword="null"/> when absent.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public string? GetEffectDescription(int id) =>
        TryGetEffectDescription(id, out string? description)
            ? description
            : null;

    /// <summary>
    /// Looks up a hand item's localized name under the key <c>handitem&lt;id&gt;</c>.
    /// </summary>
    /// <param name="id">The hand item id.</param>
    /// <param name="name">Receives the name, or <see langword="null"/> when absent.</param>
    /// <returns><see langword="true"/> when the text table has that key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public bool TryGetHandItemName(int id, out string? name) =>
        TryGetText($"handitem{id}", out name);

    /// <summary>Gets a hand item's localized name.</summary>
    /// <param name="id">The hand item id.</param>
    /// <returns>The name, or <see langword="null"/> when absent.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public string? GetHandItemName(int id) =>
        TryGetHandItemName(id, out string? name) ? name : null;

    /// <summary>
    /// Gets every hand item id whose localized name matches the given name, compared
    /// case-insensitively.
    /// </summary>
    /// <remarks>
    /// The reverse of <see cref="GetHandItemName"/>. Several ids can share one name, which is why
    /// the result is a sequence. Argument checks and the text lookup are deferred: both exceptions
    /// are thrown when the sequence is first enumerated, not when the method is called.
    /// </remarks>
    /// <param name="name">The hand item name to look for.</param>
    /// <returns>
    /// The matching ids, produced lazily by scanning the whole external text table on enumeration.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the external texts have not been loaded.</exception>
    public IEnumerable<int> GetHandItemIds(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach ((string key, string value) in Texts)
        {
            if (!value.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                !key.StartsWith("handitem", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(
                    key.AsSpan("handitem".Length),
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int id))
            {
                continue;
            }
            yield return id;
        }
    }

    private bool TryGetText(string key, out string? value)
    {
        value = Texts[key];
        return value is not null;
    }

    /// <summary>
    /// Turns a compass direction into a tile far enough away that the avatar ends up facing that
    /// way, which is how a facing change is expressed without a dedicated message.
    /// </summary>
    private static Point DirectionTarget(int direction)
    {
        int normalized = ((direction % 8) + 8) % 8;
        return normalized switch
        {
            0 => new Point(-1000, -10000),
            1 => new Point(1000, -10000),
            2 => new Point(10000, -1000),
            3 => new Point(10000, 1000),
            4 => new Point(1000, 10000),
            5 => new Point(-1000, 10000),
            6 => new Point(-10000, 1000),
            _ => new Point(-10000, -1000)
        };
    }
}
