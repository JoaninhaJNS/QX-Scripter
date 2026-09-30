using Qx.Game;
using Qx.Game.Snapshots;
using Qx.Model;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

/// <content>
/// The current room's decoration, layout, chat rules and the local user's authority in it.
/// <para>
/// Everything here is read from the room tracker and reflects what the server pushed while
/// entering and staying in the room. Nothing is requested: a member stays <see langword="null"/>
/// until the packet that carries it has arrived, and every value is reset on leaving the room.
/// </para>
/// <para>
/// The plain properties read the live tracker; the snapshot properties take a consistent copy
/// under the tracker's lock and are the safe choice when several related values have to agree.
/// </para>
/// </content>
public partial class ScriptGlobals
{
    /// <summary>
    /// Gets the extra room flags the server sends alongside the room data, or
    /// <see langword="null"/> until the room result has arrived.
    /// </summary>
    /// <remarks>
    /// The flags cover forward on enter, staff pick, group membership, room mute, the moderation
    /// permission levels, whether the local user may mute, and the chat settings.
    /// </remarks>
    public RoomResultDetails? RoomDetails => Room.Details;

    /// <summary>
    /// Gets the room's door tile, or <see langword="null"/> while it has not arrived.
    /// </summary>
    /// <remarks>
    /// The door tile is where avatars appear on entering and which way they face.
    /// </remarks>
    public RoomEntryTile? RoomEntryTile => Room.EntryTile;

    /// <summary>
    /// Gets every room property keyed exactly as the hotel sends it, for example <c>floor</c>,
    /// <c>wallpaper</c>, <c>landscape</c> and <c>landscapeanim</c>.
    /// </summary>
    /// <remarks>Every read returns a copy taken under the room lock, not a live view.</remarks>
    public IReadOnlyDictionary<string, string> RoomProperties => Room.Properties;

    /// <summary>
    /// Gets the <c>floor</c> property, which is the floor pattern identifier, or
    /// <see langword="null"/> when the room has not set one.
    /// </summary>
    public string? RoomFloor => Room.FloorProperty;

    /// <summary>
    /// Gets the <c>wallpaper</c> property, which is the wall pattern identifier, or
    /// <see langword="null"/> when the room has not set one.
    /// </summary>
    public string? RoomWallpaper => Room.WallpaperProperty;

    /// <summary>
    /// Gets the <c>landscape</c> property, which is the window backdrop identifier, or
    /// <see langword="null"/> when the room has not set one.
    /// </summary>
    public string? RoomLandscape => Room.LandscapeProperty;

    /// <summary>
    /// Gets the <c>landscapeanim</c> property, which is the animated backdrop identifier, or
    /// <see langword="null"/> when the room has not set one.
    /// </summary>
    public string? RoomAnimatedLandscape => Room.AnimatedLandscapeProperty;

    /// <summary>
    /// Gets whether the walls are hidden and how thick the walls and floor are drawn, or
    /// <see langword="null"/> before the visualization settings arrive.
    /// </summary>
    public RoomVisualizationSettings? RoomVisualization => Room.VisualizationSettings;

    /// <summary>
    /// Gets the room's chat rules, or <see langword="null"/> before the room result or a chat
    /// settings message arrives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules cover bubble flow mode, bubble width, scroll speed, hearing distance in tiles and
    /// flood filter strength. They are taken from the room result and from the chat settings
    /// message, whichever arrived last.
    /// </para>
    /// <para>
    /// One Flash wire layout carries only the flood filter setting; on such a build the other
    /// fields hold their defaults rather than the room's real values.
    /// </para>
    /// </remarks>
    public RoomChatSettings? RoomChatSettings => Room.ChatSettings;

    /// <summary>
    /// Gets the controller level the server granted the local user in this room, or
    /// <see langword="null"/> while it is still unknown.
    /// </summary>
    /// <remarks>
    /// The client's scale is 0 not a controller, 1 room controller (rights), 2 group member,
    /// 3 group admin, 4 room owner, 5 moderator. A revoke of the local user's rights sets it to 0.
    /// </remarks>
    public int? RoomRightsLevel => Room.RightsLevel;

    /// <summary>
    /// Gets whether the local user's rights in the room are settled.
    /// </summary>
    /// <remarks>
    /// They are settled once the local user is known to own the room or a controller level has
    /// arrived. While this is <see langword="false"/>, a rights check of <see langword="false"/>
    /// only means "not confirmed yet".
    /// </remarks>
    public bool RoomRightsAreKnown => Room.RightsAreKnown;

    /// <summary>
    /// Gets whether the local user owns the room or holds a controller level above 0.
    /// </summary>
    /// <remarks>This is the check to make before attempting anything that needs rights.</remarks>
    public bool HasRoomRights => Room.HasRights;

    /// <summary>
    /// Gets whether the local user entered as a spectator, or <see langword="null"/> while unknown.
    /// </summary>
    /// <remarks>Spectators cannot act in the room.</remarks>
    public bool? IsRoomSpectating => Room.IsSpectating;

    /// <summary>
    /// Gets who may mute, kick and ban in this room as the three permission levels the server
    /// sent, or <see langword="null"/> before the room result arrives.
    /// </summary>
    public RoomModerationSettings? RoomModeration => Room.Details?.Moderation;

    /// <summary>
    /// Gets whether the local user may mute others in this room, or <see langword="null"/> while
    /// the room details have not been loaded.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> is deliberately different from a loaded <see langword="false"/>.
    /// </remarks>
    public bool? CanMuteInRoom => Room.DetailsAreLoaded ? Room.Details?.CanMute : null;

    /// <summary>
    /// Gets the room's decoration and layout captured in one consistent snapshot.
    /// </summary>
    /// <remarks>
    /// The snapshot holds the door tile, every room property, the four well known decoration
    /// properties, the visualization settings and the chat settings. Every read returns a new
    /// immutable copy taken under the room lock.
    /// </remarks>
    public RoomEnvironmentSnapshot RoomEnvironment =>
        Room.Capture(SnapshotFactory.RoomEnvironment);

    /// <summary>
    /// Gets what the local user is permitted to do in this room, captured in one consistent snapshot.
    /// </summary>
    /// <remarks>
    /// The snapshot holds ownership, controller level, whether rights are known, the effective
    /// rights flag, spectator status, room mute state, mute permission and the moderation levels.
    /// Every read returns a new immutable copy taken under the room lock.
    /// </remarks>
    public RoomAuthoritySnapshot RoomAuthority =>
        Room.Capture(SnapshotFactory.RoomAuthority);

    /// <summary>
    /// Gets the room detail flags as an immutable snapshot, or <see langword="null"/> when the
    /// room result has not arrived.
    /// </summary>
    /// <remarks>Every read returns a new copy taken under the room lock.</remarks>
    public RoomResultDetailsSnapshot? RoomDetailsSnapshot =>
        Room.Capture(current => current.Details is { } details
            ? SnapshotFactory.From(details)
            : null);

    /// <summary>
    /// Registers a handler that runs when the room's detail flags arrive or change.
    /// </summary>
    /// <remarks>
    /// This happens on entering a room and whenever the server sends the room result again.
    /// </remarks>
    /// <param name="handler">The handler to call with the details.</param>
    /// <returns>
    /// A handle that removes the handler when disposed. The subscription is also torn down when
    /// the script stops, so the handle only has to be kept to unsubscribe earlier.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomDetailsUpdated(Action<RoomResultDetails> handler)
        => Subscribe(
            handler,
            value => Room.DetailsUpdated += value,
            value => Room.DetailsUpdated -= value);

    /// <summary>Registers a handler that runs when the room's door tile is set or moved.</summary>
    /// <param name="handler">The handler to call with the door tile position and direction.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomEntryTileUpdated(Action<RoomEntryTile> handler)
        => Subscribe(
            handler,
            value => Room.EntryTileUpdated += value,
            value => Room.EntryTileUpdated -= value);

    /// <summary>
    /// Registers a handler that runs for each room property the server sets or changes.
    /// </summary>
    /// <remarks>
    /// This covers floor, wallpaper, landscape and anything else the hotel keys into the property map.
    /// </remarks>
    /// <param name="handler">The handler to call with the property key and its new value.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomPropertyUpdated(Action<FlatProperty> handler)
        => Subscribe(
            handler,
            value => Room.PropertyUpdated += value,
            value => Room.PropertyUpdated -= value);

    /// <summary>Registers a handler that runs when the wall hiding or the wall and floor thickness settings change.</summary>
    /// <param name="handler">The handler to call with the new visualization settings.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomVisualizationUpdated(Action<RoomVisualizationSettings> handler)
        => Subscribe(
            handler,
            value => Room.VisualizationSettingsUpdated += value,
            value => Room.VisualizationSettingsUpdated -= value);

    /// <summary>Registers a handler that runs when the room's chat rules arrive or change.</summary>
    /// <param name="handler">The handler to call with the new chat settings.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomChatSettingsUpdated(Action<RoomChatSettings> handler)
        => Subscribe(
            handler,
            value => Room.ChatSettingsUpdated += value,
            value => Room.ChatSettingsUpdated -= value);

    /// <summary>
    /// Registers a handler that runs whenever the local user's ownership, controller level or
    /// spectator status in the room changes.
    /// </summary>
    /// <param name="handler">
    /// The handler to call with the whole new authority state rather than just the field that moved.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomAuthorityChanged(Action<RoomAuthorityState> handler)
        => Subscribe(
            handler,
            value => Room.AuthorityChanged += value,
            value => Room.AuthorityChanged -= value);

    /// <summary>
    /// Registers a handler that runs when the local user's controller level changes, including the
    /// first time it becomes known.
    /// </summary>
    /// <param name="handler">
    /// The handler to call with the previous level and then the new one; either may be
    /// <see langword="null"/> for "unknown". The scale is 0 not a controller, 1 rights, 2 group
    /// member, 3 group admin, 4 owner, 5 moderator.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomRightsLevelChanged(Action<int?, int?> handler)
        => Subscribe(
            handler,
            value => Room.RightsLevelChanged += value,
            value => Room.RightsLevelChanged -= value);

    /// <summary>Registers a handler that runs when the local user's spectator status changes.</summary>
    /// <param name="handler">
    /// The handler to call with the previous value and then the new one; either may be
    /// <see langword="null"/> for "unknown".
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnRoomSpectatingChanged(Action<bool?, bool?> handler)
        => Subscribe(
            handler,
            value => Room.SpectatingChanged += value,
            value => Room.SpectatingChanged -= value);
}
