using Qx;
using Qx.Messages;

namespace Qx.Model;

/// <summary>Represents an avatar in the room: a user, a pet or a bot.</summary>
public abstract class Avatar : IParserComposer<Avatar>
{
    /// <summary>Gets the kind of avatar.</summary>
    public AvatarType Type { get; }
    /// <summary>Gets the avatar's own identifier.</summary>
    /// <remarks>
    /// This is a user identifier for users, a pet identifier for pets and a bot identifier for
    /// bots. Room packets address the avatar by <see cref="Index"/> instead.
    /// </remarks>
    public Id Id { get; }
    /// <summary>Gets the room index that room packets use to address the avatar.</summary>
    /// <remarks>The index is only valid within the current room session.</remarks>
    public int Index { get; }

    /// <summary>Gets or sets the avatar's name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Gets or sets the avatar's motto.</summary>
    public string Motto { get; set; } = "";
    /// <summary>Gets or sets the avatar's figure string.</summary>
    /// <remarks>For a pet this is the pet's appearance string.</remarks>
    public string Figure { get; set; } = "";

    /// <summary>Gets or sets the tile the avatar stands on, including its height.</summary>
    public Tile Location { get; set; }
    /// <summary>Gets or sets the direction the avatar's body faces, from 0 (north) to 7, clockwise.</summary>
    public int Direction { get; set; }
    /// <summary>Gets or sets the direction the avatar's head faces, from 0 (north) to 7, clockwise.</summary>
    public int HeadDirection { get; set; }

    /// <summary>Gets the x coordinate of <see cref="Location"/>.</summary>
    public int X => Location.X;
    /// <summary>Gets the y coordinate of <see cref="Location"/>.</summary>
    public int Y => Location.Y;
    /// <summary>Gets the x and y coordinates of <see cref="Location"/>.</summary>
    public Point XY => Location.XY;

    /// <summary>
    /// Gets or sets the latest status update received for the avatar, or <see langword="null"/>
    /// before the first one arrives.
    /// </summary>
    public AvatarStatus? CurrentUpdate { get; set; }

    /// <summary>
    /// Gets the tile the avatar is stepping onto, or <see langword="null"/> when it stands still.
    /// </summary>
    /// <remarks>
    /// Taken from the latest status update and cleared when a roller or wired moves the avatar, so
    /// it never points at a walk the avatar was pulled out of.
    /// </remarks>
    public Tile? MovingTo { get; internal set; }

    /// <summary>Gets whether the avatar is walking, which is when <see cref="MovingTo"/> is set.</summary>
    public bool IsMoving => MovingTo is not null;

    /// <summary>Gets whether the avatar stands still on a tile.</summary>
    /// <remarks>
    /// This holds when the latest status update put the avatar on the tile without a next step
    /// and nothing has moved it since.
    /// </remarks>
    /// <param name="tile">The tile to check.</param>
    public bool IsSettledAt(Point tile) =>
        XY == tile && MovingTo is null && CurrentUpdate is { MovingTo: null } status && status.Location.XY == tile;

    /// <summary>Gets whether the avatar stands still on a tile.</summary>
    /// <remarks>
    /// This holds when the latest status update put the avatar on the tile without a next step
    /// and nothing has moved it since.
    /// </remarks>
    /// <param name="x">The tile x coordinate.</param>
    /// <param name="y">The tile y coordinate.</param>
    public bool IsSettledAt(int x, int y) => IsSettledAt(new Point(x, y));
    /// <summary>Gets or sets the dance the avatar is doing, or 0 when it is not dancing.</summary>
    /// <remarks>The values match <see cref="Dances"/>.</remarks>
    public int Dance { get; set; }
    /// <summary>Gets or sets the avatar effect currently applied, or 0 when none is.</summary>
    public int Effect { get; set; }
    /// <summary>Gets or sets the hand item the avatar carries, or 0 when its hands are empty.</summary>
    public int HandItem { get; set; }
    /// <summary>Gets or sets whether the hotel reports the avatar as idle.</summary>
    public bool IsIdle { get; set; }
    /// <summary>Gets or sets whether the avatar shows the typing indicator.</summary>
    public bool IsTyping { get; set; }
    /// <summary>Gets whether the avatar is no longer in the room.</summary>
    /// <remarks>
    /// Set when the avatar leaves, is replaced by a newer copy or the room session ends, so a kept
    /// reference can tell that it is no longer live.
    /// </remarks>
    public bool IsRemoved { get; internal set; }

    /// <summary>Initializes a new instance of the <see cref="Avatar"/> class.</summary>
    /// <param name="type">The kind of avatar.</param>
    /// <param name="id">The avatar's own identifier.</param>
    /// <param name="index">The avatar's room index.</param>
    protected Avatar(AvatarType type, Id id, int index)
    {
        Type = type;
        Id = id;
        Index = index;
    }

    /// <summary>Writes the avatar to a packet in the room users layout.</summary>
    /// <param name="p">The packet to write to.</param>
    public virtual void Compose(in PacketWriter p)
    {
        p.WriteId(Id);
        p.WriteString(Name);
        p.WriteString(Motto);
        p.WriteString(Figure);
        p.WriteInt(Index);
        {
            p.Compose(Location);
        }
        p.WriteInt(Direction);
        p.WriteInt((int)Type);
    }

    /// <summary>Reads an avatar from a packet in the room users layout.</summary>
    /// <remarks>
    /// Returns a <see cref="User"/>, <see cref="Pet"/> or <see cref="Bot"/> depending on the avatar
    /// type in the packet.
    /// </remarks>
    /// <param name="p">The packet to read from.</param>
    /// <returns>The parsed avatar.</returns>
    /// <exception cref="Exception">Thrown when the avatar type is not recognized.</exception>
    public static Avatar Parse(in PacketReader p)
    {
        Id id = p.ReadId();
        string name = p.ReadString();
        string motto = p.ReadString();
        string figure = p.ReadString();
        int index = p.ReadInt();
        Tile location = p.Parse<Tile>();
        int direction = p.ReadInt();
        var type = (AvatarType)p.ReadInt();

        Avatar avatar = type switch
        {
            AvatarType.User => new User(id, index, in p),
            AvatarType.Pet => new Pet(id, index, in p),
            AvatarType.PublicBot or AvatarType.PrivateBot => new Bot(type, id, index, in p),
            _ => throw new Exception($"Unknown avatar type: {type}.")
        };

        avatar.Name = name;
        avatar.Motto = motto;
        avatar.Figure = figure;
        avatar.Location = location;
        avatar.Direction = direction;
        avatar.HeadDirection = direction;
        return avatar;
    }

    /// <summary>Returns the avatar's name.</summary>
    /// <returns>The value of <see cref="Name"/>.</returns>
    public override string ToString() => Name;
}

/// <summary>Represents a user avatar in the room.</summary>
public sealed class User : Avatar
{
    /// <summary>Gets or sets the user's gender.</summary>
    public Gender Gender { get; set; } = Gender.Unisex;
    /// <summary>Gets or sets the identifier of the user's favorite group, or -1 when the user displays none.</summary>
    public Id GroupId { get; set; } = -1;
    /// <summary>Gets or sets the favorite group membership status as sent with the room user.</summary>
    public int GroupStatus { get; set; }
    /// <summary>Gets or sets the name of the user's favorite group, empty when the user displays none.</summary>
    public string GroupName { get; set; } = "";
    /// <summary>Gets or sets the secondary figure string the hotel sends with the room user, empty when none.</summary>
    public string FigureExtra { get; set; } = "";
    /// <summary>Gets or sets the user's achievement score.</summary>
    public int AchievementScore { get; set; }
    /// <summary>Gets or sets whether the hotel flags the user as staff.</summary>
    public bool IsStaff { get; set; }
    /// <summary>Gets or sets the user's badge code.</summary>
    /// <remarks>The room users packet does not carry it, so it stays empty unless assigned.</remarks>
    public string BadgeCode { get; set; } = "";
    /// <summary>Gets or sets the favorite group's badge code.</summary>
    /// <remarks>The room users packet does not carry it, so it stays empty unless assigned.</remarks>
    public string GroupBadge { get; set; } = "";
    /// <summary>Gets or sets the favorite group's badge parts as a flat list of integers.</summary>
    /// <remarks>The room users packet does not carry it, so it stays empty unless assigned.</remarks>
    public List<int> GroupPayload { get; set; } = [];
    /// <summary>Gets or sets the badge rank the hotel sends with the user, or -1 when none was sent.</summary>
    public int BadgeRank { get; set; } = -1;

    /// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="index">The user's room index.</param>
    public User(Id id, int index) : base(AvatarType.User, id, index) { }

    internal User(Id id, int index, in PacketReader p) : this(id, index)
    {
        Gender = Genders.Parse(p.ReadString());
        GroupId = p.ReadId();
        GroupStatus = p.ReadInt();
        GroupName = p.ReadString();
        FigureExtra = p.ReadString();
        AchievementScore = p.ReadInt();
        IsStaff = p.ReadBool();
        BadgeRank = p.ReadInt();
    }

    /// <inheritdoc/>
    public override void Compose(in PacketWriter p)
    {
        base.Compose(in p);
        p.WriteString(Gender.ToClientString().ToLowerInvariant());
        p.WriteId(GroupId);
        p.WriteInt(GroupStatus);
        p.WriteString(GroupName);
        p.WriteString(FigureExtra);
        p.WriteInt(AchievementScore);
        p.WriteBool(IsStaff);
        {
            p.WriteInt(BadgeRank);
        }
    }
}

/// <summary>Represents a pet avatar in the room.</summary>
public sealed class Pet : Avatar
{
    /// <summary>Gets or sets the pet type, which identifies the kind of animal.</summary>
    /// <remarks>
    /// This is not the breed variant within the type, which only comes from
    /// <see cref="PetInfo.BreedId"/>. Type 16 is the monsterplant.
    /// </remarks>
    public int PetType { get; set; }
    /// <summary>Gets or sets the pet type, the same value as <see cref="PetType"/>.</summary>
    /// <remarks>Despite its name this is not the breed variant; see <see cref="PetInfo.BreedId"/> for that.</remarks>
    public int Breed
    {
        get => PetType;
        set => PetType = value;
    }
    /// <summary>Gets or sets the identifier of the pet's owner, or -1 when none was sent.</summary>
    public Id OwnerId { get; set; } = -1;
    /// <summary>Gets or sets the name of the pet's owner.</summary>
    public string OwnerName { get; set; } = "";
    /// <summary>Gets or sets the pet's rarity tier as sent by the hotel.</summary>
    public int RarityLevel { get; set; }
    /// <summary>Gets or sets whether the pet wears a saddle.</summary>
    public bool HasSaddle { get; set; }
    /// <summary>Gets or sets whether a user is riding the pet.</summary>
    public bool IsRiding { get; set; }
    /// <summary>Gets or sets whether the pet may be bred right now.</summary>
    public bool CanBreed { get; set; }
    /// <summary>Gets or sets whether the pet may be harvested right now.</summary>
    public bool CanHarvest { get; set; }
    /// <summary>Gets or sets whether the pet is dead and may be revived.</summary>
    public bool CanRevive { get; set; }
    /// <summary>Gets or sets whether the local user may breed the pet.</summary>
    public bool HasBreedingPermission { get; set; }
    /// <summary>Gets or sets the pet's level.</summary>
    public int Level { get; set; }
    /// <summary>Gets or sets the pet's posture string as sent by the hotel, for example <c>ded</c>.</summary>
    public string Posture { get; set; } = "";

    /// <summary>Initializes a new instance of the <see cref="Pet"/> class.</summary>
    /// <param name="id">The pet identifier.</param>
    /// <param name="index">The pet's room index.</param>
    public Pet(Id id, int index) : base(AvatarType.Pet, id, index) { }

    internal Pet(Id id, int index, in PacketReader p) : this(id, index)
    {
        PetType = p.ReadInt();
        OwnerId = p.ReadId();
        OwnerName = p.ReadString();
        RarityLevel = p.ReadInt();
        HasSaddle = p.ReadBool();
        IsRiding = p.ReadBool();
        CanBreed = p.ReadBool();
        CanHarvest = p.ReadBool();
        CanRevive = p.ReadBool();
        HasBreedingPermission = p.ReadBool();
        Level = p.ReadInt();
        Posture = p.ReadString();
    }

    /// <inheritdoc/>
    public override void Compose(in PacketWriter p)
    {
        base.Compose(in p);
        p.WriteInt(PetType);
        p.WriteId(OwnerId);
        p.WriteString(OwnerName);
        p.WriteInt(RarityLevel);
        p.WriteBool(HasSaddle);
        p.WriteBool(IsRiding);
        p.WriteBool(CanBreed);
        p.WriteBool(CanHarvest);
        p.WriteBool(CanRevive);
        p.WriteBool(HasBreedingPermission);
        p.WriteInt(Level);
        p.WriteString(Posture);
    }
}

/// <summary>Represents a bot avatar in the room.</summary>
/// <remarks>
/// The hotel only sends the gender, owner and skills for private bots. On a public bot the owner
/// is -1, the owner name empty and the skill list empty.
/// </remarks>
public sealed class Bot : Avatar
{
    /// <summary>Gets whether this is a hotel-owned public bot.</summary>
    public bool IsPublicBot => Type == AvatarType.PublicBot;
    /// <summary>Gets whether this is a user-owned private bot.</summary>
    public bool IsPrivateBot => Type == AvatarType.PrivateBot;

    /// <summary>Gets or sets the bot's gender.</summary>
    public Gender Gender { get; set; } = Gender.Unisex;
    /// <summary>Gets or sets the identifier of the bot's owner, or -1 for a public bot.</summary>
    public Id OwnerId { get; set; } = -1;
    /// <summary>Gets or sets the name of the bot's owner, empty for a public bot.</summary>
    public string OwnerName { get; set; } = "";
    /// <summary>Gets or sets the bot's skill identifiers as sent by the hotel, empty for a public bot.</summary>
    public List<short> Skills { get; set; } = [];

    /// <summary>Initializes a new instance of the <see cref="Bot"/> class.</summary>
    /// <param name="type">The bot kind, <see cref="AvatarType.PublicBot"/> or <see cref="AvatarType.PrivateBot"/>.</param>
    /// <param name="id">The bot identifier.</param>
    /// <param name="index">The bot's room index.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not a bot type.</exception>
    public Bot(AvatarType type, Id id, int index) : base(type, id, index)
    {
        if (type is not (AvatarType.PublicBot or AvatarType.PrivateBot))
            throw new ArgumentException($"Invalid avatar type for Bot: {type}.");
    }

    internal Bot(AvatarType type, Id id, int index, in PacketReader p) : this(type, id, index)
    {
        if (type is AvatarType.PrivateBot)
        {
            Gender = Genders.Parse(p.ReadString());
            OwnerId = p.ReadId();
            OwnerName = p.ReadString();
            Skills = [.. p.ReadShortArray()];
        }
    }

    /// <inheritdoc/>
    public override void Compose(in PacketWriter p)
    {
        base.Compose(in p);
        if (Type is AvatarType.PrivateBot)
        {
            p.WriteString(Gender.ToClientString().ToLowerInvariant());
            p.WriteId(OwnerId);
            p.WriteString(OwnerName);
            p.WriteShortArray(Skills);
        }
    }
}
