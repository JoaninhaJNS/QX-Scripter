using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests a page of an all time game leaderboard.</summary>
/// <remarks>Sent as the Flash <c>Game2GetTotalLeaderboard</c>, <c>Game2GetFriendsLeaderboard</c> or <c>Game2GetTotalGroupLeaderboard</c> message, which share this layout.</remarks>
/// <param name="GameTypeId">The id of the game type the board ranks.</param>
/// <param name="StartRank">The first rank to return. The initial request sends -1, later pages send a rank from the stored board.</param>
/// <param name="Direction">The paging direction, 0 or 1. The next page and the initial request send 0, the previous page sends 1.</param>
/// <param name="ViewSize">The view size, read from the <c>games.highscores.viewSize</c> game data variable and 8 by default.</param>
/// <param name="WindowSize">The window size, read from the <c>games.highscores.windowSize</c> game data variable and 50 by default.</param>
public sealed record LeaderboardRequest(
    int GameTypeId,
    int StartRank,
    int Direction,
    int ViewSize,
    int WindowSize) : IParserComposer<LeaderboardRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static LeaderboardRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static LeaderboardRequest ParseFlash(in PacketReader p)
    {
        LeaderboardWire.RequireRemaining(in p, sizeof(int) * 5, 0, nameof(LeaderboardRequest));
        var value = new LeaderboardRequest(
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt());
        LeaderboardWire.RequireEmpty(in p, nameof(LeaderboardRequest));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(LeaderboardRequest value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        p.WriteInt(value.GameTypeId);
        p.WriteInt(value.StartRank);
        p.WriteInt(value.Direction);
        p.WriteInt(value.ViewSize);
        p.WriteInt(value.WindowSize);
    }
}

/// <summary>Requests a page of a weekly game leaderboard.</summary>
/// <remarks>Sent as the Flash <c>Game2GetWeeklyLeaderboard</c>, <c>Game2GetWeeklyFriendsLeaderboard</c> or <c>Game2GetWeeklyGroupLeaderboard</c> message, which share this layout.</remarks>
/// <param name="GameTypeId">The id of the game type the board ranks.</param>
/// <param name="WeekOffset">The number of weeks back from the current week, where 0 is the current week.</param>
/// <param name="StartRank">The first rank to return. The initial request sends -1, later pages send a rank from the stored board.</param>
/// <param name="Direction">The paging direction, 0 or 1. The next page and the initial request send 0, the previous page sends 1.</param>
/// <param name="ViewSize">The view size, read from the <c>games.highscores.viewSize</c> game data variable and 8 by default.</param>
/// <param name="WindowSize">The window size, read from the <c>games.highscores.windowSize</c> game data variable and 50 by default.</param>
public sealed record WeeklyLeaderboardRequest(
    int GameTypeId,
    int WeekOffset,
    int StartRank,
    int Direction,
    int ViewSize,
    int WindowSize) : IParserComposer<WeeklyLeaderboardRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static WeeklyLeaderboardRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static WeeklyLeaderboardRequest ParseFlash(in PacketReader p)
    {
        LeaderboardWire.RequireRemaining(
            in p,
            sizeof(int) * 6,
            0,
            nameof(WeeklyLeaderboardRequest));
        var value = new WeeklyLeaderboardRequest(
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt());
        LeaderboardWire.RequireEmpty(in p, nameof(WeeklyLeaderboardRequest));
        return value;
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(WeeklyLeaderboardRequest value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        p.WriteInt(value.GameTypeId);
        p.WriteInt(value.WeekOffset);
        p.WriteInt(value.StartRank);
        p.WriteInt(value.Direction);
        p.WriteInt(value.ViewSize);
        p.WriteInt(value.WindowSize);
    }
}
