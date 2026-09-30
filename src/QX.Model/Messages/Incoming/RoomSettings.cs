using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents the <c>RoomSettingsData</c> message, received with the editable settings of a room.</summary>
/// <remarks>
/// The hotel sends this only for rooms the user may edit. <see cref="AllowPets"/>,
/// <see cref="AllowFoodConsume"/>, <see cref="AllowWalkThrough"/> and <see cref="HideWalls"/> are sent
/// as integers, and only 1 reads as <see langword="true"/>.
/// </remarks>
public sealed record RoomSettings : IParserComposer<RoomSettings>
{
    private const int FlashFixedTailBytes = 57;

    private IReadOnlyList<string> _tags = Array.AsReadOnly(Array.Empty<string>());
    private IReadOnlyList<Id> _nft_group_ids = Array.AsReadOnly(Array.Empty<Id>());

    /// <summary>Gets the ID of the room.</summary>
    public Id RoomId { get; init; }
    /// <summary>Gets the room name.</summary>
    public string Name { get; init; } = "";
    /// <summary>Gets the room description.</summary>
    public string Description { get; init; } = "";
    /// <summary>Gets who may enter the room.</summary>
    public RoomDoorMode DoorMode { get; init; }
    /// <summary>Gets the ID of the navigator category the room is listed in.</summary>
    public int CategoryId { get; init; }
    /// <summary>Gets the maximum number of visitors.</summary>
    public int MaximumVisitors { get; init; }
    /// <summary>Gets the highest visitor limit the hotel allows for the room.</summary>
    public int MaximumVisitorsLimit { get; init; }
    /// <summary>Gets the lowest visitor limit the hotel allows for the room.</summary>
    /// <remarks>The Flash message does not carry it, so it is 0 for parsed settings and is not composed.</remarks>
    public int MaximumVisitorsLowerLimit { get; init; }
    /// <summary>Gets the room's search tags.</summary>
    public IReadOnlyList<string> Tags
    {
        get => _tags;
        init => _tags = Freeze(value, nameof(Tags));
    }
    /// <summary>Gets who may trade in the room.</summary>
    public RoomTradeMode TradeMode { get; init; }
    /// <summary>Gets whether pets are allowed in the room.</summary>
    public bool AllowPets { get; init; }
    /// <summary>Gets whether pets may eat food in the room.</summary>
    public bool AllowFoodConsume { get; init; }
    /// <summary>Gets whether avatars can walk through each other.</summary>
    public bool AllowWalkThrough { get; init; }
    /// <summary>Gets whether the room walls are hidden.</summary>
    public bool HideWalls { get; init; }
    /// <summary>Gets the wall thickness.</summary>
    public RoomThickness WallThickness { get; init; }
    /// <summary>Gets the floor thickness.</summary>
    public RoomThickness FloorThickness { get; init; }
    /// <summary>Gets how strictly the room silences repeated or rapid chat.</summary>
    public RoomChatFloodSensitivity ChatFloodSensitivity { get; init; }
    /// <summary>Gets whether avatars leave the room when they step on the door tile.</summary>
    public bool LeaveOnDoorTile { get; init; }
    /// <summary>Gets whether idle avatars fall asleep.</summary>
    public bool IdleSleepEnabled { get; init; }
    /// <summary>Gets the idle time in seconds before an avatar falls asleep.</summary>
    public int IdleSleepTimeoutSeconds { get; init; }
    /// <summary>Gets whether idle avatars are kicked from the room.</summary>
    public bool IdleAutokickEnabled { get; init; }
    /// <summary>Gets the idle time in seconds before an avatar is kicked.</summary>
    public int IdleAutokickTimeoutSeconds { get; init; }
    /// <summary>Gets whether every pet in the room is muted.</summary>
    public bool MuteAllPets { get; init; }
    /// <summary>Gets whether the hotel reports the room as hidden by Builders Club.</summary>
    public bool HiddenByBc { get; init; }
    /// <summary>Gets whether the room belongs to a group.</summary>
    /// <remarks>The Flash message does not carry it, so it is <see langword="false"/> for parsed settings and is not composed.</remarks>
    public bool IsGroupRoom { get; init; }
    /// <summary>Gets the group rights policy code.</summary>
    /// <remarks>The Flash message does not carry it, so it is 0 for parsed settings and is not composed.</remarks>
    public int GroupRightsPolicy { get; init; }
    /// <summary>Gets whether the room requires a Builders Club membership.</summary>
    /// <remarks>The Flash message does not carry it, so it is <see langword="false"/> for parsed settings and is not composed.</remarks>
    public bool RequiresBuildersClub { get; init; }
    /// <summary>Gets the NFT group IDs of the room.</summary>
    /// <remarks>The Flash message does not carry them, so the list is empty for parsed settings and is not composed.</remarks>
    public IReadOnlyList<Id> NftGroupIds
    {
        get => _nft_group_ids;
        init => _nft_group_ids = Freeze(value, nameof(NftGroupIds));
    }
    /// <summary>Gets whether the room is a HabboX demo room.</summary>
    /// <remarks>The Flash message does not carry it, so it is <see langword="false"/> for parsed settings and is not composed.</remarks>
    public bool IsHabboXDemoRoom { get; init; }
    /// <summary>Gets who may mute users in the room.</summary>
    public RoomModerationPermission WhoCanMute { get; init; }
    /// <summary>Gets who may kick users from the room.</summary>
    public RoomModerationPermission WhoCanKick { get; init; }
    /// <summary>Gets who may ban users from the room.</summary>
    public RoomModerationPermission WhoCanBan { get; init; }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomSettings Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomSettings ParseFlash(in PacketReader p)
    {
        Id room_id = p.ReadInt();
        string name = p.ReadString();
        string description = p.ReadString();
        RoomDoorMode door_mode = (RoomDoorMode)p.ReadInt();
        int category_id = p.ReadInt();
        int maximum_visitors = p.ReadInt();
        int maximum_visitors_limit = p.ReadInt();
        int tag_count = p.ReadInt();
        if (tag_count < 0)
            throw new InvalidDataException($"{nameof(RoomSettings)} has a negative tag count.");
        long minimum_remaining = FlashFixedTailBytes + (long)tag_count * 2;
        if (p.Available < minimum_remaining)
            throw new InvalidDataException($"{nameof(RoomSettings)} tag count exceeds the remaining payload.");

        var tags = new string[tag_count];
        for (int i = 0; i < tags.Length; i++)
            tags[i] = p.ReadString();

        RoomTradeMode trade_mode = (RoomTradeMode)p.ReadInt();
        bool allow_pets = p.ReadInt() == 1;
        bool allow_food = p.ReadInt() == 1;
        bool allow_walk = p.ReadInt() == 1;
        bool hide_walls = p.ReadInt() == 1;
        RoomThickness wall_thickness = (RoomThickness)p.ReadInt();
        RoomThickness floor_thickness = (RoomThickness)p.ReadInt();
        RoomChatFloodSensitivity chat_flood = (RoomChatFloodSensitivity)p.ReadInt();
        bool leave_on_door_tile = p.ReadBool();
        bool idle_sleep = p.ReadBool();
        int idle_sleep_timeout = p.ReadInt();
        bool idle_autokick = p.ReadBool();
        int idle_autokick_timeout = p.ReadInt();
        bool mute_all_pets = p.ReadBool();
        RoomModerationPermission who_can_mute = (RoomModerationPermission)p.ReadInt();
        RoomModerationPermission who_can_kick = (RoomModerationPermission)p.ReadInt();
        RoomModerationPermission who_can_ban = (RoomModerationPermission)p.ReadInt();
        bool hidden_by_bc = p.ReadBool();
        RequireEmpty(in p);

        return new RoomSettings
        {
            RoomId = room_id,
            Name = name,
            Description = description,
            DoorMode = door_mode,
            CategoryId = category_id,
            MaximumVisitors = maximum_visitors,
            MaximumVisitorsLimit = maximum_visitors_limit,
            Tags = tags,
            TradeMode = trade_mode,
            AllowPets = allow_pets,
            AllowFoodConsume = allow_food,
            AllowWalkThrough = allow_walk,
            HideWalls = hide_walls,
            WallThickness = wall_thickness,
            FloorThickness = floor_thickness,
            ChatFloodSensitivity = chat_flood,
            LeaveOnDoorTile = leave_on_door_tile,
            IdleSleepEnabled = idle_sleep,
            IdleSleepTimeoutSeconds = idle_sleep_timeout,
            IdleAutokickEnabled = idle_autokick,
            IdleAutokickTimeoutSeconds = idle_autokick_timeout,
            MuteAllPets = mute_all_pets,
            WhoCanMute = who_can_mute,
            WhoCanKick = who_can_kick,
            WhoCanBan = who_can_ban,
            HiddenByBc = hidden_by_bc
        };
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomSettings value, in PacketWriter p)
    {
        int room_id = checked((int)(long)value.RoomId);
        string[] tags = [.. value.Tags];
        RequireString(value.Name, nameof(Name), in p);
        RequireString(value.Description, nameof(Description), in p);
        foreach (string tag in tags)
            RequireString(tag, nameof(Tags), in p);

        p.WriteInt(room_id);
        p.WriteString(value.Name);
        p.WriteString(value.Description);
        p.WriteInt((int)value.DoorMode);
        p.WriteInt(value.CategoryId);
        p.WriteInt(value.MaximumVisitors);
        p.WriteInt(value.MaximumVisitorsLimit);
        p.WriteInt(tags.Length);
        foreach (string tag in tags)
            p.WriteString(tag);
        p.WriteInt((int)value.TradeMode);
        p.WriteInt(value.AllowPets ? 1 : 0);
        p.WriteInt(value.AllowFoodConsume ? 1 : 0);
        p.WriteInt(value.AllowWalkThrough ? 1 : 0);
        p.WriteInt(value.HideWalls ? 1 : 0);
        p.WriteInt((int)value.WallThickness);
        p.WriteInt((int)value.FloorThickness);
        p.WriteInt((int)value.ChatFloodSensitivity);
        p.WriteBool(value.LeaveOnDoorTile);
        p.WriteBool(value.IdleSleepEnabled);
        p.WriteInt(value.IdleSleepTimeoutSeconds);
        p.WriteBool(value.IdleAutokickEnabled);
        p.WriteInt(value.IdleAutokickTimeoutSeconds);
        p.WriteBool(value.MuteAllPets);
        p.WriteInt((int)value.WhoCanMute);
        p.WriteInt((int)value.WhoCanKick);
        p.WriteInt((int)value.WhoCanBan);
        p.WriteBool(value.HiddenByBc);
    }

    private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T> values, string name)
    {
        ArgumentNullException.ThrowIfNull(values, name);
        return Array.AsReadOnly(values.ToArray());
    }

    private static void RequireString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        int length = p.Encoding.GetByteCount(value);
        if (length > ushort.MaxValue)
            throw new ArgumentException($"{name} exceeds the wire string limit.", name);
    }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException($"{nameof(RoomSettings)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Represents the <c>RoomSettingsSaved</c> message, received when the settings of a room were saved.</summary>
/// <param name="RoomId">The ID of the room.</param>
public sealed record RoomSettingsSaved(Id RoomId) : IParserComposer<RoomSettingsSaved>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomSettingsSaved Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomSettingsSaved ParseFlash(in PacketReader p)
    {
        Id room_id = p.ReadInt();
        RequireEmpty(in p);
        return new RoomSettingsSaved(room_id);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomSettingsSaved value, in PacketWriter p)
    {
        int room_id = checked((int)(long)value.RoomId);
        p.WriteInt(room_id);
    }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException($"{nameof(RoomSettingsSaved)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Represents the <c>RoomSettingsError</c> message, received when the hotel refuses a request for the settings of a room.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="ErrorCode">The error code sent by the hotel.</param>
public sealed record RoomSettingsError(Id RoomId, int ErrorCode) : IParserComposer<RoomSettingsError>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomSettingsError Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomSettingsError ParseFlash(in PacketReader p)
    {
        Id room_id = p.ReadInt();
        int error_code = p.ReadInt();
        RequireEmpty(in p);
        return new RoomSettingsError(room_id, error_code);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomSettingsError value, in PacketWriter p)
    {
        int room_id = checked((int)(long)value.RoomId);
        p.WriteInt(room_id);
        p.WriteInt(value.ErrorCode);
    }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException($"{nameof(RoomSettingsError)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Represents the <c>RoomSettingsSaveError</c> message, received when the hotel rejects saving the settings of a room.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="ErrorCode">The error code sent by the hotel.</param>
/// <param name="Info">Additional information about the error sent by the hotel.</param>
public sealed record RoomSettingsSaveError(Id RoomId, int ErrorCode, string Info)
    : IParserComposer<RoomSettingsSaveError>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RoomSettingsSaveError Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RoomSettingsSaveError ParseFlash(in PacketReader p)
    {
        Id room_id = p.ReadInt();
        int error_code = p.ReadInt();
        string info = p.ReadString();
        RequireEmpty(in p);
        return new RoomSettingsSaveError(room_id, error_code, info);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RoomSettingsSaveError value, in PacketWriter p)
    {
        int room_id = checked((int)(long)value.RoomId);
        RequireString(value.Info, in p);
        p.WriteInt(room_id);
        p.WriteInt(value.ErrorCode);
        p.WriteString(value.Info);
    }

    private static void RequireString(string info, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(info, nameof(Info));
        if (p.Encoding.GetByteCount(info) > ushort.MaxValue)
            throw new ArgumentException("Info exceeds the wire string limit.", nameof(Info));
    }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException($"{nameof(RoomSettingsSaveError)} contains {p.Available} unexpected bytes.");
    }
}
