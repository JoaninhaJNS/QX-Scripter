using Qx.Messages;
using Qx.Model.Forums;
using Qx.Model.Messages.Incoming;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Represents a request for the details of a group forum.</summary>
/// <remarks>The server answers with <see cref="ForumData"/>.</remarks>
/// <param name="GroupId">The id of the group that owns the forum.</param>
public sealed record GetForumStats(Id GroupId) : IParserComposer<GetForumStats>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetForumStats Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetForumStats ParseFlash(in PacketReader p) =>
        new(ForumRequestProtocol.ReadFlashGroupId(in p));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetForumStats value, in PacketWriter p) =>
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
}

/// <summary>Represents a request for a page of threads in a group forum, with the same layout as <see cref="GetForumThreads"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="StartIndex">The zero based index of the first thread on the page.</param>
/// <param name="MaxCount">The maximum number of threads to return.</param>
public sealed record GetThreads(
    Id GroupId,
    int StartIndex,
    int MaxCount) : IParserComposer<GetThreads>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetThreads Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetThreads ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetThreads value, in PacketWriter p)
    {
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        p.WriteInt(value.StartIndex);
        p.WriteInt(value.MaxCount);
    }
}

/// <summary>Represents a request for a page of messages in a forum thread, with the same layout as <see cref="GetForumThreadMessages"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread to read.</param>
/// <param name="StartIndex">The zero based index of the first message on the page.</param>
/// <param name="MaxCount">The maximum number of messages to return.</param>
public sealed record GetMessages(
    Id GroupId,
    Id ThreadId,
    int StartIndex,
    int MaxCount) : IParserComposer<GetMessages>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetMessages Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetMessages ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetMessages value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        p.WriteInt(value.StartIndex);
        p.WriteInt(value.MaxCount);
    }
}

/// <summary>Represents a request for a single forum thread, with the same layout as <see cref="GetForumThread"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread id.</param>
public sealed record GetThread(
    Id GroupId,
    Id ThreadId) : IParserComposer<GetThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetThread ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetThread value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
    }
}

/// <summary>Represents a request for a page of the forum directory.</summary>
/// <remarks>The server answers with <see cref="ForumsList"/>.</remarks>
/// <param name="ListCode">The directory list to read.</param>
/// <param name="StartIndex">The zero based index of the first forum on the page.</param>
/// <param name="MaxCount">The maximum number of forums to return.</param>
public sealed record GetForumsList(
    ForumListCode ListCode,
    int StartIndex,
    int MaxCount) : IParserComposer<GetForumsList>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetForumsList Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetForumsList ParseFlash(in PacketReader p) =>
        new(
            (ForumListCode)p.ReadInt(),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetForumsList value, in PacketWriter p)
    {
        p.WriteInt((int)value.ListCode);
        p.WriteInt(value.StartIndex);
        p.WriteInt(value.MaxCount);
    }
}

/// <summary>Represents a request to change the permission levels of a group forum.</summary>
/// <remarks>All four levels are sent together. Levels run from 0, the least restrictive, to 3.</remarks>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ReadLevel">The permission level required to read the forum.</param>
/// <param name="PostMessageLevel">The permission level required to reply to threads.</param>
/// <param name="PostThreadLevel">The permission level required to start threads.</param>
/// <param name="ModerateLevel">The permission level required to moderate the forum.</param>
public sealed record UpdateForumSettings(
    Id GroupId,
    int ReadLevel,
    int PostMessageLevel,
    int PostThreadLevel,
    int ModerateLevel) : IParserComposer<UpdateForumSettings>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateForumSettings Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static UpdateForumSettings ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateForumSettings value, in PacketWriter p)
    {
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        p.WriteInt(value.ReadLevel);
        p.WriteInt(value.PostMessageLevel);
        p.WriteInt(value.PostThreadLevel);
        p.WriteInt(value.ModerateLevel);
    }
}

/// <summary>Represents a request for the number of forums with unread messages.</summary>
public sealed record GetUnreadForumsCount : IParserComposer<GetUnreadForumsCount>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetUnreadForumsCount Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetUnreadForumsCount ParseFlash(in PacketReader p) => new();

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetUnreadForumsCount value, in PacketWriter p) { }
}

/// <summary>Represents a request to hide or restore a forum thread, with the same layout as <see cref="ModerateForumThread"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread to moderate.</param>
/// <param name="State">The new moderation state: 0 default, 1 restored, 10 hidden by a forum admin, 20 hidden by staff.</param>
public sealed record ModerateThread(
    Id GroupId,
    Id ThreadId,
    int State) : IParserComposer<ModerateThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ModerateThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static ModerateThread ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ModerateThread value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        p.WriteInt(value.State);
    }
}

/// <summary>Represents a request to hide or restore a forum message, with the same layout as <see cref="ModerateForumMessage"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread the message belongs to.</param>
/// <param name="MessageId">The message to moderate.</param>
/// <param name="State">The new moderation state: 0 default, 1 restored, 10 hidden by a forum admin, 20 hidden by staff.</param>
public sealed record ModerateMessage(
    Id GroupId,
    Id ThreadId,
    Id MessageId,
    int State) : IParserComposer<ModerateMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ModerateMessage Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static ModerateMessage ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ModerateMessage value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId, value.MessageId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        ForumRequestProtocol.WriteIntId(in p, value.MessageId, "message");
        p.WriteInt(value.State);
    }
}

/// <summary>Represents a call for help that reports a forum thread to the moderators.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The reported thread.</param>
/// <param name="CategoryId">The id of the call for help category.</param>
/// <param name="Report">The report text.</param>
/// <param name="FirstContext">The first context string sent with the report.</param>
/// <param name="SecondContext">The second context string sent with the report.</param>
public sealed record CallForHelpFromForumThread(
    Id GroupId,
    Id ThreadId,
    int CategoryId,
    string Report,
    string FirstContext,
    string SecondContext) : IParserComposer<CallForHelpFromForumThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CallForHelpFromForumThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static CallForHelpFromForumThread ParseFlash(in PacketReader p)
    {
        return new CallForHelpFromForumThread(
            ForumProtocol.ReadFlashId(in p),
            ForumProtocol.ReadFlashId(in p),
            p.ReadInt(),
            p.ReadString(),
            p.ReadString(),
            p.ReadString());
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CallForHelpFromForumThread value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.PrepareStrings(in p, value.Report, value.FirstContext, value.SecondContext);
        ForumProtocol.WriteFlashId(in p, value.GroupId);
        ForumProtocol.WriteFlashId(in p, value.ThreadId);
        p.WriteInt(value.CategoryId);
        p.WriteString(value.Report);
        p.WriteString(value.FirstContext);
        p.WriteString(value.SecondContext);
    }
}

/// <summary>Represents a call for help that reports a forum message to the moderators.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread the message belongs to.</param>
/// <param name="MessageId">The reported message.</param>
/// <param name="CategoryId">The id of the call for help category.</param>
/// <param name="Report">The report text.</param>
/// <param name="FirstContext">The first context string sent with the report.</param>
/// <param name="SecondContext">The second context string sent with the report.</param>
public sealed record CallForHelpFromForumMessage(
    Id GroupId,
    Id ThreadId,
    Id MessageId,
    int CategoryId,
    string Report,
    string FirstContext,
    string SecondContext) : IParserComposer<CallForHelpFromForumMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CallForHelpFromForumMessage Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static CallForHelpFromForumMessage ParseFlash(in PacketReader p)
    {
        return new CallForHelpFromForumMessage(
            ForumProtocol.ReadFlashId(in p),
            ForumProtocol.ReadFlashId(in p),
            ForumProtocol.ReadFlashId(in p),
            p.ReadInt(),
            p.ReadString(),
            p.ReadString(),
            p.ReadString());
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CallForHelpFromForumMessage value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId, value.MessageId);
        ForumRequestProtocol.PrepareStrings(in p, value.Report, value.FirstContext, value.SecondContext);
        ForumProtocol.WriteFlashId(in p, value.GroupId);
        ForumProtocol.WriteFlashId(in p, value.ThreadId);
        ForumProtocol.WriteFlashId(in p, value.MessageId);
        p.WriteInt(value.CategoryId);
        p.WriteString(value.Report);
        p.WriteString(value.FirstContext);
        p.WriteString(value.SecondContext);
    }
}

/// <summary>Represents one forum entry in a read marker update.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="LastReadMessageId">The id of the last message that was read.</param>
/// <param name="MarkAsRead">Whether the whole forum should be treated as read.</param>
public readonly record struct ForumReadMarker(
    Id GroupId,
    Id LastReadMessageId,
    bool MarkAsRead) : IParserComposer<ForumReadMarker>
{
    /// <summary>Gets whether the whole forum should be treated as read, the same value as <see cref="MarkAsRead"/>.</summary>
    public bool MarkEntireForumRead => MarkAsRead;

    /// <summary>Parses a single read marker from a packet that holds nothing else.</summary>
    /// <param name="p">The packet reader.</param>
    public static ForumReadMarker Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    internal static ForumReadMarker ParseWire(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static ForumReadMarker ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadBool());

    /// <summary>Composes the read marker into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    internal static void Prepare(ForumReadMarker value, ClientType client)
    {
        ForumProtocol.RequireFlashId(value.GroupId, "forum group");
        ForumProtocol.RequireFlashId(value.LastReadMessageId, "last-read message");
    }

    internal static void ComposeWire(ForumReadMarker value, in PacketWriter p) =>
        FlashWire.Compose(value, in p, ComposeFlash);

    private static void ComposeFlash(ForumReadMarker value, in PacketWriter p)
    {
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.LastReadMessageId, "last-read message");
        p.WriteBool(value.MarkAsRead);
    }
}

/// <summary>Represents a request to update forum read markers, with the same layout as <see cref="UpdateForumReadMarkers"/>.</summary>
/// <param name="Markers">The markers, one per forum. The list is copied and may hold at most 65535 entries.</param>
public sealed record UpdateForumReadMarker(
    IReadOnlyList<ForumReadMarker> Markers) : IParserComposer<UpdateForumReadMarker>
{
    private IReadOnlyList<ForumReadMarker> markers =
        ForumProtocol.FreezeValues(Markers, nameof(Markers));

    /// <summary>Gets the markers, as a read only copy.</summary>
    public IReadOnlyList<ForumReadMarker> Markers
    {
        get => markers;
        init => markers = ForumProtocol.FreezeValues(value, nameof(Markers));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateForumReadMarker Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static UpdateForumReadMarker ParseFlash(in PacketReader p)
    {
        int count = ForumRequestProtocol.ReadFlashMarkerCount(in p);
        var markers = new ForumReadMarker[count];
        for (int index = 0; index < count; index++)
            markers[index] = ForumReadMarker.ParseWire(in p);
        return new UpdateForumReadMarker(markers);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateForumReadMarker value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        int count = ForumProtocol.RequireCount(value.Markers.Count, nameof(Markers));
        for (int index = 0; index < count; index++)
            ForumReadMarker.Prepare(value.Markers[index], p.Client);
        ForumRequestProtocol.WriteFlashMarkerCount(in p, count);
        for (int index = 0; index < count; index++)
            ForumReadMarker.ComposeWire(value.Markers[index], in p);
    }
}

/// <summary>Represents a request for a page of threads in a group forum.</summary>
/// <remarks>The server answers with <see cref="ForumThreads"/>.</remarks>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="StartIndex">The zero based index of the first thread on the page.</param>
/// <param name="Amount">The maximum number of threads to return.</param>
public sealed record GetForumThreads(
    Id GroupId,
    int StartIndex,
    int Amount) : IParserComposer<GetForumThreads>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetForumThreads Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetForumThreads ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetForumThreads value, in PacketWriter p)
    {
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        p.WriteInt(value.StartIndex);
        p.WriteInt(value.Amount);
    }
}

/// <summary>Represents a request for a page of messages in a forum thread.</summary>
/// <remarks>The server answers with <see cref="ThreadMessages"/>.</remarks>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread to read.</param>
/// <param name="StartIndex">The zero based index of the first message on the page.</param>
/// <param name="Amount">The maximum number of messages to return.</param>
public sealed record GetForumThreadMessages(
    Id GroupId,
    Id ThreadId,
    int StartIndex,
    int Amount) : IParserComposer<GetForumThreadMessages>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetForumThreadMessages Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetForumThreadMessages ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetForumThreadMessages value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        p.WriteInt(value.StartIndex);
        p.WriteInt(value.Amount);
    }
}

/// <summary>Represents a request for a single forum thread without its messages.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread id.</param>
public sealed record GetForumThread(
    Id GroupId,
    Id ThreadId) : IParserComposer<GetForumThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static GetForumThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static GetForumThread ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p));

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(GetForumThread value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
    }
}

/// <summary>Represents a request to post in a group forum, with the same layout as the outgoing form of <see cref="PostMessage"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread to reply to, or 0 to start a new thread.</param>
/// <param name="Subject">The subject of a new thread.</param>
/// <param name="MessageText">The text to post.</param>
public sealed record PostForumMessage(
    Id GroupId,
    Id ThreadId,
    string Subject,
    string MessageText) : IParserComposer<PostForumMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static PostForumMessage Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static PostForumMessage ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadString(),
            p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(PostForumMessage value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.PrepareStrings(in p, value.Subject, value.MessageText);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        p.WriteString(value.Subject);
        p.WriteString(value.MessageText);
    }
}

/// <summary>Represents a request to hide or restore a forum thread.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread to moderate.</param>
/// <param name="State">The new moderation state: 0 default, 1 restored, 10 hidden by a forum admin, 20 hidden by staff.</param>
public sealed record ModerateForumThread(
    Id GroupId,
    Id ThreadId,
    int State) : IParserComposer<ModerateForumThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ModerateForumThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static ModerateForumThread ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ModerateForumThread value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        p.WriteInt(value.State);
    }
}

/// <summary>Represents a request to hide or restore a forum message.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread the message belongs to.</param>
/// <param name="MessageId">The message to moderate.</param>
/// <param name="State">The new moderation state: 0 default, 1 restored, 10 hidden by a forum admin, 20 hidden by staff.</param>
public sealed record ModerateForumMessage(
    Id GroupId,
    Id ThreadId,
    Id MessageId,
    int State) : IParserComposer<ModerateForumMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static ModerateForumMessage Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static ModerateForumMessage ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ModerateForumMessage value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId, value.MessageId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        ForumRequestProtocol.WriteIntId(in p, value.MessageId, "message");
        p.WriteInt(value.State);
    }
}

/// <summary>Represents a request to change a forum thread's sticky and locked flags, with the same layout as the outgoing form of <see cref="UpdateThread"/>.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread to change.</param>
/// <param name="IsSticky">Whether the thread is pinned to the top of the thread list.</param>
/// <param name="IsLocked">Whether the thread rejects further replies.</param>
public sealed record UpdateForumThread(
    Id GroupId,
    Id ThreadId,
    bool IsSticky,
    bool IsLocked) : IParserComposer<UpdateForumThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateForumThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static UpdateForumThread ParseFlash(in PacketReader p) =>
        new(
            ForumRequestProtocol.ReadFlashGroupId(in p),
            ForumRequestProtocol.ReadIntId(in p),
            p.ReadBool(),
            p.ReadBool());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateForumThread value, in PacketWriter p)
    {
        ForumRequestProtocol.PrepareIds(p.Client, value.GroupId, value.ThreadId);
        ForumRequestProtocol.WriteFlashGroupId(in p, value.GroupId);
        ForumRequestProtocol.WriteIntId(in p, value.ThreadId, "thread");
        p.WriteBool(value.IsSticky);
        p.WriteBool(value.IsLocked);
    }
}

/// <summary>Represents a request to mark group forums as read up to a given message.</summary>
/// <param name="Markers">The markers, one per forum. The list is copied and may hold at most 65535 entries.</param>
public sealed record UpdateForumReadMarkers(
    IReadOnlyList<ForumReadMarker> Markers) : IParserComposer<UpdateForumReadMarkers>
{
    private IReadOnlyList<ForumReadMarker> markers =
        ForumProtocol.FreezeValues(Markers, nameof(Markers));

    /// <summary>Gets the markers, as a read only copy.</summary>
    public IReadOnlyList<ForumReadMarker> Markers
    {
        get => markers;
        init => markers = ForumProtocol.FreezeValues(value, nameof(Markers));
    }

    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UpdateForumReadMarkers Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static UpdateForumReadMarkers ParseFlash(in PacketReader p)
    {
        int count = ForumRequestProtocol.ReadFlashMarkerCount(in p);
        var markers = new ForumReadMarker[count];
        for (int index = 0; index < count; index++)
            markers[index] = ForumReadMarker.ParseWire(in p);
        return new UpdateForumReadMarkers(markers);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UpdateForumReadMarkers value, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value);
        int count = ForumProtocol.RequireCount(value.Markers.Count, nameof(Markers));
        for (int index = 0; index < count; index++)
            ForumReadMarker.Prepare(value.Markers[index], p.Client);
        ForumRequestProtocol.WriteFlashMarkerCount(in p, count);
        for (int index = 0; index < count; index++)
            ForumReadMarker.ComposeWire(value.Markers[index], in p);
    }
}

/// <summary>Represents a report of a forum thread that no supported client can send.</summary>
/// <remarks>
/// The Flash client reports threads with <see cref="CallForHelpFromForumThread"/>, so parsing and
/// composing always throw <see cref="UnsupportedClientException"/>.
/// </remarks>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The reported thread.</param>
/// <param name="CategoryId">The id of the report category.</param>
/// <param name="Report">The report text.</param>
public sealed record ReportForumThread(
    Id GroupId,
    Id ThreadId,
    int CategoryId,
    string Report) : IParserComposer<ReportForumThread>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    /// <exception cref="UnsupportedClientException">Thrown on every call, since no supported client has the message.</exception>
    public static ReportForumThread Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static ReportForumThread ParseFlash(in PacketReader p) =>
        throw new UnsupportedClientException(p.Client);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    /// <exception cref="UnsupportedClientException">Thrown on every call, since no supported client has the message.</exception>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ReportForumThread value, in PacketWriter p) =>
        throw new UnsupportedClientException(p.Client);
}

/// <summary>Represents a report of a forum message that no supported client can send.</summary>
/// <remarks>
/// The Flash client reports messages with <see cref="CallForHelpFromForumMessage"/>, so parsing and
/// composing always throw <see cref="UnsupportedClientException"/>.
/// </remarks>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The thread the message belongs to.</param>
/// <param name="MessageId">The reported message.</param>
/// <param name="CategoryId">The id of the report category.</param>
/// <param name="Report">The report text.</param>
public sealed record ReportForumMessage(
    Id GroupId,
    Id ThreadId,
    Id MessageId,
    int CategoryId,
    string Report) : IParserComposer<ReportForumMessage>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    /// <exception cref="UnsupportedClientException">Thrown on every call, since no supported client has the message.</exception>
    public static ReportForumMessage Parse(in PacketReader p) =>
        ForumRequestProtocol.ParseRoot(in p, ParseFlash);

    private static ReportForumMessage ParseFlash(in PacketReader p) =>
        throw new UnsupportedClientException(p.Client);

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    /// <exception cref="UnsupportedClientException">Thrown on every call, since no supported client has the message.</exception>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(ReportForumMessage value, in PacketWriter p) =>
        throw new UnsupportedClientException(p.Client);
}
