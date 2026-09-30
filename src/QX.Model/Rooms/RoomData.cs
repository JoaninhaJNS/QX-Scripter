using Qx.Messages;

namespace Qx.Model;

/// <summary>Represents the navigator record of a room, as shown in room listings and on the room info card.</summary>
public sealed class RoomData : IParserComposer<RoomData>
{
    /// <summary>Gets or sets the room identifier.</summary>
    public Id Id { get; set; }
    /// <summary>Gets or sets the room name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets the identifier of the room's owner.</summary>
    public Id OwnerId { get; set; }
    /// <summary>Gets or sets the name of the room's owner.</summary>
    public string OwnerName { get; set; } = "";
    /// <summary>Gets or sets who may enter the room.</summary>
    public RoomDoorMode DoorMode { get; set; }
    /// <summary>Gets or sets how many users are in the room.</summary>
    public int UserCount { get; set; }
    /// <summary>Gets or sets the room's user capacity.</summary>
    public int MaxUserCount { get; set; }
    /// <summary>Gets or sets the room description.</summary>
    public string Description { get; set; } = "";
    /// <summary>Gets or sets who may trade in the room.</summary>
    public RoomTradeMode TradeMode { get; set; }
    /// <summary>Gets or sets the room's like count.</summary>
    public int Score { get; set; }
    /// <summary>Gets or sets the room's position in the hotel ranking.</summary>
    public int Ranking { get; set; }
    /// <summary>Gets or sets the navigator category identifier.</summary>
    /// <remarks>The numbering is defined per hotel by the navigator configuration.</remarks>
    public int Category { get; set; }
    /// <summary>Gets or sets the room's search tags.</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the official room picture reference, or <see langword="null"/> for an ordinary room.</summary>
    public string? OfficialRoomPicRef { get; set; }

    /// <summary>Gets or sets whether the room belongs to a group.</summary>
    /// <remarks>The group properties are only meaningful when this is <see langword="true"/>.</remarks>
    public bool HasGroup { get; set; }
    /// <summary>Gets or sets the identifier of the group that owns the room.</summary>
    public Id GroupId { get; set; }
    /// <summary>Gets or sets the name of the group that owns the room.</summary>
    public string GroupName { get; set; } = "";
    /// <summary>Gets or sets the badge code of the group that owns the room.</summary>
    public string GroupBadge { get; set; } = "";

    /// <summary>Gets or sets whether a room event is running.</summary>
    /// <remarks>The event properties are only meaningful when this is <see langword="true"/>.</remarks>
    public bool HasEvent { get; set; }
    /// <summary>Gets or sets the running event's title.</summary>
    public string EventName { get; set; } = "";
    /// <summary>Gets or sets the running event's description.</summary>
    public string EventDescription { get; set; } = "";
    /// <summary>Gets or sets the minutes left before the event ends.</summary>
    public int EventMinutesRemaining { get; set; }

    /// <summary>Gets or sets whether the navigator shows the owner's name.</summary>
    public bool ShowOwner { get; set; }
    /// <summary>Gets or sets whether visitors may bring pets into the room.</summary>
    public bool AllowPets { get; set; }
    /// <summary>Gets or sets whether the client shows an entry advertisement for the room.</summary>
    public bool DisplayRoomEntryAd { get; set; }

    /// <summary>Initializes a new instance of the <see cref="RoomData"/> class.</summary>
    public RoomData() { }

    private RoomData(in PacketReader p)
    {
        Id = p.ReadId();
        Name = p.ReadString();
        OwnerId = p.ReadId();
        OwnerName = p.ReadString();
        DoorMode = (RoomDoorMode)p.ReadInt();
        UserCount = p.ReadInt();
        MaxUserCount = p.ReadInt();
        Description = p.ReadString();
        TradeMode = (RoomTradeMode)p.ReadInt();
        Score = p.ReadInt();
        Ranking = p.ReadInt();
        Category = p.ReadInt();

        int tagCount = p.ReadLength();
        var tags = new string[tagCount];
        for (int i = 0; i < tagCount; i++)
            tags[i] = p.ReadString();
        Tags = tags;

        int flags = p.ReadInt();
        if ((flags & 1) != 0)
            OfficialRoomPicRef = p.ReadString();
        if ((flags & 2) != 0)
        {
            HasGroup = true;
            GroupId = p.ReadId();
            GroupName = p.ReadString();
            GroupBadge = p.ReadString();
        }
        if ((flags & 4) != 0)
        {
            HasEvent = true;
            EventName = p.ReadString();
            EventDescription = p.ReadString();
            EventMinutesRemaining = p.ReadInt();
        }
        ShowOwner = (flags & 8) != 0;
        AllowPets = (flags & 16) != 0;
        DisplayRoomEntryAd = (flags & 32) != 0;
    }

    /// <summary>Reads a room record from a packet.</summary>
    /// <remarks>
    /// A flags integer after the tags decides which optional parts follow: the official picture,
    /// the group and the event. It also carries <see cref="ShowOwner"/>, <see cref="AllowPets"/> and
    /// <see cref="DisplayRoomEntryAd"/>.
    /// </remarks>
    /// <param name="p">The packet to read from.</param>
    public static RoomData Parse(in PacketReader p) => new(in p);

    /// <summary>Writes the room record to a packet, deriving the flags integer from the properties.</summary>
    /// <param name="p">The packet to write to.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteId(Id);
        p.WriteString(Name);
        p.WriteId(OwnerId);
        p.WriteString(OwnerName);
        p.WriteInt((int)DoorMode);
        p.WriteInt(UserCount);
        p.WriteInt(MaxUserCount);
        p.WriteString(Description);
        p.WriteInt((int)TradeMode);
        p.WriteInt(Score);
        p.WriteInt(Ranking);
        p.WriteInt(Category);

        p.WriteLength((Length)Tags.Count);
        foreach (string tag in Tags)
            p.WriteString(tag);

        int flags = 0;
        if (OfficialRoomPicRef is not null) flags |= 1;
        if (HasGroup) flags |= 2;
        if (HasEvent) flags |= 4;
        if (ShowOwner) flags |= 8;
        if (AllowPets) flags |= 16;
        if (DisplayRoomEntryAd) flags |= 32;
        p.WriteInt(flags);

        if (OfficialRoomPicRef is not null)
            p.WriteString(OfficialRoomPicRef);
        if (HasGroup)
        {
            p.WriteId(GroupId);
            p.WriteString(GroupName);
            p.WriteString(GroupBadge);
        }
        if (HasEvent)
        {
            p.WriteString(EventName);
            p.WriteString(EventDescription);
            p.WriteInt(EventMinutesRemaining);
        }
    }

    /// <summary>Returns the room name and identifier.</summary>
    /// <returns>A string in the form <c>Name (Id)</c>.</returns>
    public override string ToString() => $"{Name} ({Id})";
}
