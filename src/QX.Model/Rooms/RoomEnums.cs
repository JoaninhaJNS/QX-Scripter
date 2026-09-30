namespace Qx.Model;

/// <summary>Specifies who is let into a room.</summary>
/// <remarks>
/// Sent in <c>RoomSettingsData</c>, <c>GetGuestRoomResult</c> and <c>RoomData</c>. Flash names the
/// values through <c>getDoorModeLocalizationKey</c>:
/// <c>navigator.door.mode.{open,closed,password,invisible,noobs_only}</c>.
/// </remarks>
public enum RoomDoorMode
{
    /// <summary>0: anyone walks straight in.</summary>
    /// <remarks>Localization key <c>navigator.door.mode.open</c>.</remarks>
    Open = 0,
    /// <summary>1: visitors must ring the bell and be let in by someone inside.</summary>
    /// <remarks>
    /// Localization key <c>navigator.door.mode.closed</c>, the <c>doormode_doorbell</c> radio
    /// button.
    /// </remarks>
    Doorbell = 1,
    /// <summary>2: visitors must type the room password.</summary>
    /// <remarks>Localization key <c>navigator.door.mode.password</c>.</remarks>
    Password = 2,
    /// <summary>3: the room is hidden from the navigator and can only be reached by a direct link.</summary>
    /// <remarks>Localization key <c>navigator.door.mode.invisible</c>.</remarks>
    Invisible = 3,
    /// <summary>4: only new accounts may enter.</summary>
    /// <remarks>
    /// Localization key <c>navigator.door.mode.noobs_only</c>, and the value behind
    /// <c>RoomSession.isNoobRoom</c>.
    /// </remarks>
    NewUsersOnly = 4
}

/// <summary>Specifies who may trade inside a room.</summary>
/// <remarks>
/// Named by <c>com.sulake.habbo.session.enum.RoomTradingLevelEnum</c> (<c>NO_TRADING</c>,
/// <c>ROOM_CONTROLLER_REQUIRED</c>, <c>FREE_TRADING</c>).
/// </remarks>
public enum RoomTradeMode
{
    /// <summary>0: trading is switched off in this room.</summary>
    /// <remarks><c>NO_TRADING</c>, localization key <c>trading.mode.not.allowed</c>.</remarks>
    Disabled = 0,
    /// <summary>1: at least one side of the trade must hold room rights.</summary>
    /// <remarks><c>ROOM_CONTROLLER_REQUIRED</c>, localization key <c>trading.mode.controller</c>.</remarks>
    RightsHolders = 1,
    /// <summary>2: any two visitors may trade.</summary>
    /// <remarks><c>FREE_TRADING</c>, localization key <c>trading.mode.free</c>.</remarks>
    Everyone = 2
}

/// <summary>Specifies who may mute, kick or ban in a room.</summary>
/// <remarks>
/// Flash offers <c>[0,1]</c> for mute and ban and <c>[0,1,2]</c> for kick in a normal room,
/// extended with <c>[4,5]</c> once the room belongs to a group.
/// </remarks>
public enum RoomModerationPermission
{
    /// <summary>0: only the room owner may perform the action.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.moderation.none</c>.</remarks>
    OwnerOnly = 0,
    /// <summary>1: the owner and anyone holding room rights may perform the action.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.moderation.rights</c>.</remarks>
    RightsHolders = 1,
    /// <summary>2: every visitor may perform the action.</summary>
    /// <remarks>
    /// Localization key <c>navigator.roomsettings.moderation.all</c>. The hotel offers this for
    /// kick only.
    /// </remarks>
    Everyone = 2,
    /// <summary>4: the owner and the group's administrators may perform the action.</summary>
    /// <remarks>
    /// Localization key <c>navigator.roomsettings.moderation.group_admins</c>. Group rooms only.
    /// </remarks>
    GroupAdmins = 4,
    /// <summary>
    /// 5: the owner, the group's administrators and anyone holding room rights may perform the
    /// action.
    /// </summary>
    /// <remarks>
    /// Localization key <c>navigator.roomsettings.moderation.group_admins_and_rights</c>. Group
    /// rooms only.
    /// </remarks>
    GroupAdminsAndRightsHolders = 5
}

/// <summary>Specifies how thick the room's walls and floor slabs are drawn.</summary>
/// <remarks>
/// The Flash room settings drop menus map selection index <c>0..3</c> onto the wire value
/// <c>index-2</c>, and the client scales the geometry by 2 raised to that value.
/// </remarks>
public enum RoomThickness
{
    /// <summary>-2: quarter thickness.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.wall_thickness.thinnest</c>.</remarks>
    Thinnest = -2,
    /// <summary>-1: half thickness.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.wall_thickness.thin</c>.</remarks>
    Thin = -1,
    /// <summary>0: the default thickness.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.wall_thickness.normal</c>.</remarks>
    Normal = 0,
    /// <summary>1: double thickness.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.wall_thickness.thick</c>.</remarks>
    Thick = 1
}

/// <summary>
/// Specifies how chat bubbles are laid out in the room, the <c>mode</c> field of the Flash chat
/// settings object.
/// </summary>
public enum RoomChatFlowMode
{
    /// <summary>0: bubbles float above each speaker and drift upwards.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.mode.free.flow</c>.</remarks>
    FreeFlow = 0,
    /// <summary>1: bubbles stack as a scrolling transcript instead of floating.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.mode.line.by.line</c>.</remarks>
    LineByLine = 1
}

/// <summary>Specifies how wide chat bubbles may grow before wrapping.</summary>
/// <remarks>
/// <c>ChatBubbleWidth.accordingToRoomChatSetting</c> resolves the values to <c>2000</c>,
/// <c>350</c> and <c>240</c> pixels.
/// </remarks>
public enum RoomChatBubbleWidth
{
    /// <summary>0: effectively unlimited, 2000 pixels.</summary>
    /// <remarks>
    /// Localization key <c>navigator.roomsettings.chat.bubbles.width.wide</c>, client constant
    /// <c>ChatBubbleWidth.WIDE</c>.
    /// </remarks>
    Wide = 0,
    /// <summary>1: 350 pixels.</summary>
    /// <remarks>
    /// Localization key <c>navigator.roomsettings.chat.bubbles.width.normal</c>, client constant
    /// <c>ChatBubbleWidth.NORMAL</c>.
    /// </remarks>
    Normal = 1,
    /// <summary>2: 240 pixels, wrapping soonest.</summary>
    /// <remarks>
    /// Localization key <c>navigator.roomsettings.chat.bubbles.width.thin</c>, client constant
    /// <c>ChatBubbleWidth.THIN</c>.
    /// </remarks>
    Thin = 2
}

/// <summary>Specifies how long a chat bubble stays on screen.</summary>
/// <remarks>
/// <c>ChatFlowStage.refreshSettings</c> turns the value into a bubble lifetime of <c>3000</c>,
/// <c>6000</c> or <c>12000</c> milliseconds.
/// </remarks>
public enum RoomChatScrollSpeed
{
    /// <summary>0: bubbles disappear after 3000 milliseconds.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.speed.fast</c>.</remarks>
    Fast = 0,
    /// <summary>1: bubbles disappear after 6000 milliseconds.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.speed.normal</c>.</remarks>
    Normal = 1,
    /// <summary>2: bubbles disappear after 12000 milliseconds.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.speed.slow</c>.</remarks>
    Slow = 2
}

/// <summary>Specifies how aggressively the room silences repeated or rapid chat.</summary>
/// <remarks>
/// This is the <c>chat_flood_sensitivity</c> drop menu, whose selection index is written straight
/// to the wire.
/// </remarks>
public enum RoomChatFloodSensitivity
{
    /// <summary>0: the filter trips soonest.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.flood.strict</c>.</remarks>
    Strict = 0,
    /// <summary>1: the hotel default.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.flood.normal</c>.</remarks>
    Normal = 1,
    /// <summary>2: the filter tolerates the most chat.</summary>
    /// <remarks>Localization key <c>navigator.roomsettings.chat.flood.loose</c>.</remarks>
    Loose = 2
}
