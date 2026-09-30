using System.Globalization;
using System.Text;
using Qx;
using Qx.Messages;

namespace Qx.Model;

/// <summary>Specifies the posture an avatar's status update puts it in.</summary>
public enum AvatarStance
{
    /// <summary>Standing, when the status has neither a <c>sit</c> nor a <c>lay</c> fragment.</summary>
    Stand,
    /// <summary>Sitting, when the status has a <c>sit</c> fragment.</summary>
    Sit,
    /// <summary>Lying down, when the status has a <c>lay</c> fragment.</summary>
    Lay
}

/// <summary>Represents one avatar's entry in a room status update.</summary>
/// <remarks>
/// The status string is split into fragments such as <c>mv</c>, <c>sit</c>, <c>lay</c>,
/// <c>flatctrl</c>, <c>trd</c> and <c>sign</c>, and the properties below decode the common ones.
/// </remarks>
public sealed class AvatarStatus : IParserComposer<AvatarStatus>
{
    private readonly Dictionary<string, string[]> _fragments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the room index of the avatar the update is for.</summary>
    public int Index { get; set; }
    /// <summary>Gets or sets the tile the avatar is on, including its height.</summary>
    public Tile Location { get; set; }
    /// <summary>Gets or sets the direction the avatar's head faces, from 0 (north) to 7, clockwise.</summary>
    public int HeadDirection { get; set; }
    /// <summary>Gets or sets the direction the avatar's body faces, from 0 (north) to 7, clockwise.</summary>
    public int Direction { get; set; }
    /// <summary>Gets or sets the jumping power value of the entry.</summary>
    /// <remarks>Read from the integer that follows <see cref="Direction"/> in the Flash status entry.</remarks>
    public int JumpingPower { get; set; }
    /// <summary>Gets or sets the target identifier of the entry.</summary>
    /// <remarks>
    /// Packets do not carry it and <see cref="Compose"/> does not write it; it is only set
    /// through <see cref="StatusId"/> or by assignment.
    /// </remarks>
    public int TargetId { get; set; }
    /// <summary>
    /// Gets or sets the status identifier, which is <see cref="TargetId"/> when it is not 0 and
    /// <see cref="JumpingPower"/> otherwise.
    /// </summary>
    /// <remarks>Setting it assigns the value to both <see cref="JumpingPower"/> and <see cref="TargetId"/>.</remarks>
    public int StatusId
    {
        get => TargetId != 0 ? TargetId : JumpingPower;
        set
        {
            JumpingPower = value;
            TargetId = value;
        }
    }

    /// <summary>Gets the status fragments, keyed case-insensitively by name, with their space-separated arguments.</summary>
    public IReadOnlyDictionary<string, string[]> Fragments => _fragments;

    /// <summary>Gets the avatar's posture, taken from the <c>sit</c> and <c>lay</c> fragments.</summary>
    public AvatarStance Stance =>
        _fragments.ContainsKey("sit") ? AvatarStance.Sit :
        _fragments.ContainsKey("lay") ? AvatarStance.Lay :
        AvatarStance.Stand;

    /// <summary>Gets whether the status has a <c>flatctrl</c> fragment, which marks a user with room rights.</summary>
    public bool IsController => _fragments.ContainsKey("flatctrl");

    /// <summary>
    /// Gets the controller level from the <c>flatctrl</c> fragment, or 0 when the fragment is
    /// missing or has no numeric argument.
    /// </summary>
    /// <remarks>
    /// The client's scale is 0 not a controller, 1 room rights, 2 group member, 3 group admin,
    /// 4 room owner and 5 moderator.
    /// </remarks>
    public int RightsLevel =>
        _fragments.TryGetValue("flatctrl", out string[]? args) && args.Length > 0 && int.TryParse(args[0], out int level)
            ? level
            : 0;

    /// <summary>Gets whether the status has a <c>trd</c> fragment, which marks an avatar that is trading.</summary>
    public bool IsTrading => _fragments.ContainsKey("trd");

    /// <summary>Gets the controller level, the same value as <see cref="RightsLevel"/>.</summary>
    public int ControlLevel => RightsLevel;

    /// <summary>Gets whether the <c>sit</c> fragment's second argument is <c>1</c>, which marks sitting on the floor.</summary>
    public bool SittingOnFloor =>
        _fragments.TryGetValue("sit", out string[]? arguments) &&
        arguments.Length > 1 &&
        arguments[1] == "1";

    /// <summary>Gets the height offset of the <c>sit</c> or <c>lay</c> fragment, in tile units.</summary>
    /// <remarks>
    /// <see langword="null"/> when the avatar stands or when the fragment carries no finite number.
    /// </remarks>
    public double? ActionHeight
    {
        get
        {
            string key = Stance switch
            {
                AvatarStance.Sit => "sit",
                AvatarStance.Lay => "lay",
                _ => ""
            };
            if (key.Length == 0 ||
                !_fragments.TryGetValue(key, out string[]? arguments) ||
                arguments.Length == 0 ||
                !double.TryParse(
                    arguments[0],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double height))
            {
                return null;
            }
            return double.IsFinite(height) ? height : null;
        }
    }

    /// <summary>
    /// Gets the sign from the <c>sign</c> fragment, or 0 when the fragment is missing or has no
    /// numeric argument.
    /// </summary>
    public int Sign =>
        _fragments.TryGetValue("sign", out string[]? args) && args.Length > 0 && int.TryParse(args[0], out int sign)
            ? sign
            : 0;

    /// <summary>
    /// Gets the tile from the <c>mv</c> fragment that the avatar is stepping onto, or
    /// <see langword="null"/> when the status has no valid <c>mv</c> fragment.
    /// </summary>
    public Tile? MovingTo =>
        _fragments.TryGetValue("mv", out string[]? args) && args.Length > 0 && Tile.TryParseString(args[0], out Tile tile)
            ? tile
            : null;

    /// <summary>Initializes a new instance of the <see cref="AvatarStatus"/> class with no fragments.</summary>
    public AvatarStatus() { }

    private AvatarStatus(in PacketReader p)
    {
        Index = p.ReadInt();
        Location = p.Parse<Tile>();
        HeadDirection = p.ReadInt();
        Direction = p.ReadInt();
        JumpingPower = p.ReadInt();
        ParseStatus(p.ReadString());
    }

    /// <summary>Reads a status entry from a packet.</summary>
    /// <param name="p">The packet to read from.</param>
    public static AvatarStatus Parse(in PacketReader p) => new(in p);

    /// <summary>Writes the status entry to a packet, rebuilding the status string from <see cref="Fragments"/>.</summary>
    /// <param name="p">The packet to write to.</param>
    public void Compose(in PacketWriter p)
    {
        p.WriteInt(Index);
        {
            p.Compose(Location);
        }
        p.WriteInt(HeadDirection);
        p.WriteInt(Direction);
        p.WriteInt(JumpingPower);
        p.WriteString(CompileStatus());
    }

    private void ParseStatus(string status)
    {
        _fragments.Clear();
        foreach (string part in status.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            int space = part.IndexOf(' ');
            if (space > 0)
                _fragments[part[..space]] = part[(space + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            else
                _fragments[part] = [];
        }
    }

    /// <summary>Builds the status string from <see cref="Fragments"/>.</summary>
    /// <returns>
    /// The fragments in the form <c>/key arg arg/key/</c>, or <c>/</c> when there are none.
    /// </returns>
    public string CompileStatus()
    {
        var sb = new StringBuilder();
        foreach ((string key, string[] args) in _fragments)
        {
            sb.Append('/').Append(key);
            foreach (string arg in args)
                sb.Append(' ').Append(arg);
        }
        return sb.Append('/').ToString();
    }

    internal AvatarStatus Snapshot()
    {
        var snapshot = new AvatarStatus
        {
            Index = Index,
            Location = Location,
            HeadDirection = HeadDirection,
            Direction = Direction,
            JumpingPower = JumpingPower,
            TargetId = TargetId
        };
        foreach ((string key, string[] args) in _fragments)
            snapshot._fragments[key] = [.. args];
        return snapshot;
    }

    /// <summary>Returns the status string built from <see cref="Fragments"/>.</summary>
    /// <returns>The result of <see cref="CompileStatus"/>.</returns>
    public override string ToString() => CompileStatus();
}
