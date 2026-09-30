using ForumThreadData = Qx.Model.Forums.ForumThread;
using Qx.Model.Forums;
using Qx.Game.Protocol;
using Qx.Interception;
using Qx.Model.Messages.Incoming;
using Qx.Model.Messages.Outgoing;
using System.Collections.ObjectModel;

namespace Qx.Game;

/// <summary>Represents the key of a cached page of the forum list.</summary>
/// <param name="ListCode">The forum list the page belongs to.</param>
/// <param name="StartIndex">The index of the first forum on the page.</param>
public readonly record struct ForumListPageKey(
    ForumListCode ListCode,
    int StartIndex);

/// <summary>Represents the key of a cached page of threads in a forum.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="StartIndex">The index of the first thread on the page.</param>
public readonly record struct ForumThreadPageKey(
    Id GroupId,
    int StartIndex);

/// <summary>Represents the key of a cached page of messages in a forum thread.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The id of the thread.</param>
/// <param name="StartIndex">The index of the first message on the page.</param>
public readonly record struct ForumMessagePageKey(
    Id GroupId,
    Id ThreadId,
    int StartIndex);

/// <summary>Represents the key of a cached forum thread.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The id of the thread.</param>
public readonly record struct ForumThreadKey(
    Id GroupId,
    Id ThreadId);

/// <summary>Represents the key of a cached forum message.</summary>
/// <param name="GroupId">The id of the group that owns the forum.</param>
/// <param name="ThreadId">The id of the thread that contains the message.</param>
/// <param name="MessageId">The id of the message.</param>
public readonly record struct ForumMessageKey(
    Id GroupId,
    Id ThreadId,
    Id MessageId);

/// <summary>Represents an immutable snapshot of the forum data received in the current session.</summary>
/// <param name="ForumPages">The received pages of the forum list, by list and start index.</param>
/// <param name="KnownForums">The forum summaries by group id, collected from forum list pages and forum details.</param>
/// <param name="ForumDetails">The received forum details by group id.</param>
/// <param name="ThreadPages">The received pages of forum threads, by group id and start index.</param>
/// <param name="KnownThreads">
/// The forum threads by group and thread id, collected from thread pages and from created and updated threads.
/// </param>
/// <param name="MessagePages">The received pages of thread messages, by group id, thread id and start index.</param>
/// <param name="KnownMessages">
/// The forum messages by group, thread and message id, collected from message pages and from created and
/// updated messages.
/// </param>
/// <param name="UnreadForumsCount">
/// The number of forums with unread messages, or <see langword="null"/> if the server has not sent it.
/// </param>
public sealed record ForumSnapshot(
    IReadOnlyDictionary<ForumListPageKey, ForumsList> ForumPages,
    IReadOnlyDictionary<Id, ForumSummary> KnownForums,
    IReadOnlyDictionary<Id, ForumDetails> ForumDetails,
    IReadOnlyDictionary<ForumThreadPageKey, ForumThreads> ThreadPages,
    IReadOnlyDictionary<ForumThreadKey, ForumThreadData> KnownThreads,
    IReadOnlyDictionary<ForumMessagePageKey, ThreadMessages> MessagePages,
    IReadOnlyDictionary<ForumMessageKey, ForumPost> KnownMessages,
    int? UnreadForumsCount)
{
    /// <summary>Gets a snapshot that contains no forum data.</summary>
    public static ForumSnapshot Empty { get; } = new(
        EmptyMap<ForumListPageKey, ForumsList>(),
        EmptyMap<Id, ForumSummary>(),
        EmptyMap<Id, ForumDetails>(),
        EmptyMap<ForumThreadPageKey, ForumThreads>(),
        EmptyMap<ForumThreadKey, ForumThreadData>(),
        EmptyMap<ForumMessagePageKey, ThreadMessages>(),
        EmptyMap<ForumMessageKey, ForumPost>(),
        null);

    /// <summary>Gets the summary of a forum.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <returns>The forum summary, or <see langword="null"/> if it has not been received.</returns>
    public ForumSummary? FindForum(Id group_id) =>
        KnownForums.GetValueOrDefault(group_id);

    /// <summary>Gets the details of a forum.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <returns>The forum details, or <see langword="null"/> if they have not been received.</returns>
    public ForumDetails? FindDetails(Id group_id) =>
        ForumDetails.GetValueOrDefault(group_id);

    /// <summary>Gets a forum thread.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <returns>The thread, or <see langword="null"/> if it has not been received.</returns>
    public ForumThreadData? FindThread(Id group_id, Id thread_id) =>
        KnownThreads.GetValueOrDefault(new ForumThreadKey(group_id, thread_id));

    /// <summary>Gets a forum message.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread that contains the message.</param>
    /// <param name="message_id">The id of the message.</param>
    /// <returns>The message, or <see langword="null"/> if it has not been received.</returns>
    public ForumPost? FindMessage(
        Id group_id,
        Id thread_id,
        Id message_id) =>
        KnownMessages.GetValueOrDefault(
            new ForumMessageKey(group_id, thread_id, message_id));

    /// <summary>Gets a page of the forum list.</summary>
    /// <param name="list_code">The forum list.</param>
    /// <param name="start_index">The index of the first forum on the page.</param>
    /// <returns>The page, or <see langword="null"/> if it has not been received.</returns>
    public ForumsList? FindForumPage(
        ForumListCode list_code,
        int start_index = 0) =>
        ForumPages.GetValueOrDefault(
            new ForumListPageKey(list_code, start_index));

    /// <summary>Gets a page of threads in a forum.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="start_index">The index of the first thread on the page.</param>
    /// <returns>The page, or <see langword="null"/> if it has not been received.</returns>
    public ForumThreads? FindThreadPage(
        Id group_id,
        int start_index = 0) =>
        ThreadPages.GetValueOrDefault(
            new ForumThreadPageKey(group_id, start_index));

    /// <summary>Gets a page of messages in a forum thread.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <param name="start_index">The index of the first message on the page.</param>
    /// <returns>The page, or <see langword="null"/> if it has not been received.</returns>
    public ThreadMessages? FindMessagePage(
        Id group_id,
        Id thread_id,
        int start_index = 0) =>
        MessagePages.GetValueOrDefault(
            new ForumMessagePageKey(group_id, thread_id, start_index));

    private static IReadOnlyDictionary<TKey, TValue>
        EmptyMap<TKey, TValue>() where TKey : notnull =>
        new ReadOnlyDictionary<TKey, TValue>(
            new Dictionary<TKey, TValue>());
}

/// <summary>Manages the group forum data received in the current session.</summary>
/// <remarks>
/// <para>All members are safe to call from any thread.</para>
/// <para>
/// Every received forum message is merged into a new immutable <see cref="ForumSnapshot"/>. After each
/// change the specific event is raised first, then <see cref="SnapshotChanged"/>. Delivery stops when a
/// newer snapshot is published while listeners are running. The state is cleared when the hotel
/// connection closes.
/// </para>
/// </remarks>
public sealed class ForumManager : GameStateManager
{
    private readonly ManagerStateGate _state = new();
    private readonly Dictionary<ForumListPageKey, ForumsList> _forum_pages = [];
    private readonly Dictionary<Id, ForumSummary> _forums = [];
    private readonly Dictionary<Id, ForumDetails> _details = [];
    private readonly Dictionary<ForumThreadPageKey, ForumThreads> _thread_pages = [];
    private readonly Dictionary<ForumThreadKey, ForumThreadData> _threads = [];
    private readonly Dictionary<ForumMessagePageKey, ThreadMessages> _message_pages = [];
    private readonly Dictionary<ForumMessageKey, ForumPost> _messages = [];
    private ForumSnapshot _snapshot = ForumSnapshot.Empty;
    private int? _unread_forums_count;

    /// <summary>Gets the current snapshot of the forum data.</summary>
    public ForumSnapshot Snapshot => Volatile.Read(ref _snapshot);
    internal long SessionGeneration => CurrentStateGeneration;
    internal Session? Session => CurrentSession;

    /// <summary>Occurs when the forum data changes.</summary>
    /// <remarks>
    /// The argument is the new value of <see cref="Snapshot"/>. Raised after the event that describes
    /// the specific change, including <see cref="ResetCompleted"/>.
    /// </remarks>
    public event Action<ForumSnapshot>? SnapshotChanged;
    /// <summary>Occurs when the server sends the details of a forum.</summary>
    /// <remarks>The argument is the received details. The forum summary they contain is also stored.</remarks>
    public event Action<ForumDetails>? DetailsChanged;
    /// <summary>Occurs when the server sends a page of the forum list.</summary>
    /// <remarks>The argument is the received page.</remarks>
    public event Action<ForumsList>? ForumPageReceived;
    /// <summary>Occurs when the server sends a page of threads in a forum.</summary>
    /// <remarks>The argument is the received page.</remarks>
    public event Action<ForumThreads>? ThreadPageReceived;
    /// <summary>Occurs when the server sends a page of messages in a forum thread.</summary>
    /// <remarks>The argument is the received page.</remarks>
    public event Action<ThreadMessages>? MessagePageReceived;
    /// <summary>Occurs when the server reports a created or updated forum thread.</summary>
    /// <remarks>The arguments are the id of the group that owns the forum and the thread.</remarks>
    public event Action<Id, ForumThreadData>? ThreadChanged;
    /// <summary>Occurs when the server reports a created or updated forum message.</summary>
    /// <remarks>The arguments are the group id, the thread id and the message.</remarks>
    public event Action<Id, Id, ForumPost>? MessageChanged;
    /// <summary>Occurs when the server sends the number of forums with unread messages.</summary>
    /// <remarks>The argument is the received count.</remarks>
    public event Action<int>? UnreadForumsCountChanged;
    /// <summary>Occurs when the forum data is cleared after the hotel connection closes.</summary>
    public event Action? ResetCompleted;

    /// <inheritdoc/>
    protected override void OnAttach()
    {
        OnIncoming(
            MessageContracts.Forums.Stats,
            (message, generation) =>
                StoreDetails(message.Data, generation));

        OnIncoming(
            MessageContracts.Forums.List,
            StoreForumPage);

        OnIncoming(
            MessageContracts.Forums.Threads,
            StoreThreadPage);

        OnIncoming(
            MessageContracts.Forums.Messages,
            StoreMessagePage);

        OnIncoming(
            MessageContracts.Forums.ThreadCreated,
            (message, generation) =>
                StoreThread(
                    message.GroupId,
                    message.Thread,
                    generation));

        OnIncoming(
            MessageContracts.Forums.MessageCreated,
            (message, generation) =>
                StoreMessage(
                    message.GroupId,
                    message.ThreadId,
                    message.Message ??
                        throw new InvalidDataException(
                            "Incoming PostMessage did not contain a forum post."),
                    generation));

        OnIncoming(
            MessageContracts.Forums.ThreadUpdated,
            (message, generation) =>
                StoreThread(
                    message.GroupId,
                    message.Thread ??
                        throw new InvalidDataException(
                            "Incoming UpdateThread did not contain a forum thread."),
                    generation));

        OnIncoming(
            MessageContracts.Forums.MessageUpdated,
            (message, generation) =>
                StoreMessage(
                    message.GroupId,
                    message.ThreadId,
                    message.Message,
                    generation));

        OnIncoming(
            MessageContracts.Forums.UnreadCount,
            StoreUnreadForumsCount);
    }

    /// <summary>Gets the summary of a forum from the current snapshot.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <returns>The forum summary, or <see langword="null"/> if it has not been received.</returns>
    public ForumSummary? FindForum(Id group_id) =>
        Snapshot.FindForum(group_id);

    /// <summary>Gets the details of a forum from the current snapshot.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <returns>The forum details, or <see langword="null"/> if they have not been received.</returns>
    public ForumDetails? FindDetails(Id group_id) =>
        Snapshot.FindDetails(group_id);

    /// <summary>Gets a forum thread from the current snapshot.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <returns>The thread, or <see langword="null"/> if it has not been received.</returns>
    public ForumThreadData? FindThread(Id group_id, Id thread_id) =>
        Snapshot.FindThread(group_id, thread_id);

    /// <summary>Gets a forum message from the current snapshot.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread that contains the message.</param>
    /// <param name="message_id">The id of the message.</param>
    /// <returns>The message, or <see langword="null"/> if it has not been received.</returns>
    public ForumPost? FindMessage(
        Id group_id,
        Id thread_id,
        Id message_id) =>
        Snapshot.FindMessage(group_id, thread_id, message_id);

    /// <summary>Requests the details of a forum from the server.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <remarks>
    /// The request is sent without waiting for a response. The response raises
    /// <see cref="DetailsChanged"/>.
    /// </remarks>
    public void RequestStats(Id group_id) =>
        SendMessage(
            MessageContracts.Forums.StatsRequest,
            new GetForumStats(group_id));

    /// <summary>Requests a page of the forum list from the server.</summary>
    /// <param name="list_code">The forum list to request.</param>
    /// <param name="start_index">The index of the first forum on the page.</param>
    /// <param name="max_count">The maximum number of forums on the page.</param>
    /// <remarks>
    /// The request is sent without waiting for a response. The response raises
    /// <see cref="ForumPageReceived"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start_index"/> is negative or <paramref name="max_count"/> is zero or negative.
    /// </exception>
    public void RequestForums(
        ForumListCode list_code,
        int start_index = 0,
        int max_count = 20)
    {
        ValidatePage(start_index, max_count);
        SendMessage(
            MessageContracts.Forums.ListRequest,
            new GetForumsList(list_code, start_index, max_count));
    }

    /// <summary>Requests a page of threads in a forum from the server.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="start_index">The index of the first thread on the page.</param>
    /// <param name="max_count">The maximum number of threads on the page.</param>
    /// <remarks>
    /// The request is sent without waiting for a response. The response raises
    /// <see cref="ThreadPageReceived"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start_index"/> is negative or <paramref name="max_count"/> is zero or negative.
    /// </exception>
    public void RequestThreads(
        Id group_id,
        int start_index = 0,
        int max_count = 20)
    {
        ValidatePage(start_index, max_count);
        SendMessage(
            MessageContracts.Forums.ThreadsRequest,
            new GetForumThreads(group_id, start_index, max_count));
    }

    /// <summary>Requests a page of messages in a forum thread from the server.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <param name="start_index">The index of the first message on the page.</param>
    /// <param name="max_count">The maximum number of messages on the page.</param>
    /// <remarks>
    /// The request is sent without waiting for a response. The response raises
    /// <see cref="MessagePageReceived"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start_index"/> is negative or <paramref name="max_count"/> is zero or negative.
    /// </exception>
    public void RequestMessages(
        Id group_id,
        Id thread_id,
        int start_index = 0,
        int max_count = 20)
    {
        ValidatePage(start_index, max_count);
        SendMessage(
            MessageContracts.Forums.MessagesRequest,
            new GetForumThreadMessages(
                group_id,
                thread_id,
                start_index,
                max_count));
    }

    /// <summary>Requests a forum thread from the server.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <remarks>
    /// The request is sent without waiting for a response. The response raises
    /// <see cref="ThreadChanged"/>.
    /// </remarks>
    public void RequestThread(Id group_id, Id thread_id) =>
        SendMessage(
            MessageContracts.Forums.ThreadRequest,
            new GetForumThread(group_id, thread_id));

    /// <summary>Requests the number of forums with unread messages from the server.</summary>
    /// <remarks>
    /// The request is sent without waiting for a response. The response raises
    /// <see cref="UnreadForumsCountChanged"/>.
    /// </remarks>
    public void RequestUnreadForumsCount() =>
        SendMessage(
            MessageContracts.Forums.UnreadCountRequest,
            new GetUnreadForumsCount());

    /// <summary>Posts a message to a forum, creating a new thread when <paramref name="thread_id"/> is 0.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread to reply to, or 0 to create a new thread.</param>
    /// <param name="subject">The subject of a new thread, or an empty string for a reply.</param>
    /// <param name="message_text">The text of the message.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="subject"/> or <paramref name="message_text"/> is <see langword="null"/>.
    /// </exception>
    public void Post(
        Id group_id,
        Id thread_id,
        string subject,
        string message_text)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(message_text);
        SendMessage(
            MessageContracts.Forums.Post,
            new PostMessage(
                group_id,
                thread_id,
                subject,
                message_text));
    }

    /// <summary>Creates a new thread in a forum.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="subject">The subject of the thread.</param>
    /// <param name="message_text">The text of the first message.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="subject"/> or <paramref name="message_text"/> is <see langword="null"/>.
    /// </exception>
    public void CreateThread(
        Id group_id,
        string subject,
        string message_text) =>
        Post(group_id, 0, subject, message_text);

    /// <summary>Replies to a forum thread.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <param name="message_text">The text of the reply.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message_text"/> is <see langword="null"/>.</exception>
    public void Reply(
        Id group_id,
        Id thread_id,
        string message_text) =>
        Post(group_id, thread_id, "", message_text);

    /// <summary>Sets the moderation state of a forum thread.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <param name="state">
    /// The moderation state to set, using the values of <see cref="Qx.Model.Forums.ForumThread.State"/>.
    /// </param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    public void ModerateThread(
        Id group_id,
        Id thread_id,
        int state) =>
        SendMessage(
            MessageContracts.Forums.ThreadModerate,
            new ModerateForumThread(group_id, thread_id, state));

    /// <summary>Sets the moderation state of a forum message.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread that contains the message.</param>
    /// <param name="message_id">The id of the message.</param>
    /// <param name="state">The moderation state to set, using the values of <see cref="ForumPost.State"/>.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    public void ModerateMessage(
        Id group_id,
        Id thread_id,
        Id message_id,
        int state) =>
        SendMessage(
            MessageContracts.Forums.MessageModerate,
            new ModerateForumMessage(
                group_id,
                thread_id,
                message_id,
                state));

    /// <summary>Updates the permission levels of a forum.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="read_level">The permission level required to read the forum.</param>
    /// <param name="post_message_level">The permission level required to reply to threads.</param>
    /// <param name="post_thread_level">The permission level required to create threads.</param>
    /// <param name="moderate_level">The permission level required to moderate the forum.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    public void UpdateSettings(
        Id group_id,
        int read_level,
        int post_message_level,
        int post_thread_level,
        int moderate_level) =>
        SendMessage(
            MessageContracts.Forums.SettingsUpdate,
            new UpdateForumSettings(
                group_id,
                read_level,
                post_message_level,
                post_thread_level,
                moderate_level));

    /// <summary>Updates the read markers of one or more forums.</summary>
    /// <param name="markers">The read markers to send. The list is copied before sending.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="markers"/> is <see langword="null"/>.</exception>
    public void UpdateReadMarkers(
        IReadOnlyList<ForumReadMarker> markers)
    {
        ArgumentNullException.ThrowIfNull(markers);
        ForumReadMarker[] snapshot = markers.ToArray();
        SendMessage(
            MessageContracts.Forums.ReadMarkersUpdate,
            new UpdateForumReadMarkers(snapshot));
    }

    /// <summary>Sets whether a forum thread is sticky and whether it is locked.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <param name="is_sticky">Whether the thread is pinned to the top of the forum.</param>
    /// <param name="is_locked">Whether the thread is closed to new replies.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    public void UpdateThread(
        Id group_id,
        Id thread_id,
        bool is_sticky,
        bool is_locked) =>
        SendMessage(
            MessageContracts.Forums.ThreadUpdate,
            new UpdateThread(
                group_id,
                thread_id,
                is_sticky,
                is_locked));

    /// <summary>Reports a forum thread to the moderators.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread.</param>
    /// <param name="category_id">The id of the report category.</param>
    /// <param name="report">The text of the report.</param>
    /// <param name="first_context">The first context string of the Flash report message.</param>
    /// <param name="second_context">The second context string of the Flash report message.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a string argument is <see langword="null"/>.</exception>
    public void ReportThread(
        Id group_id,
        Id thread_id,
        int category_id,
        string report,
        string first_context = "",
        string second_context = "")
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(first_context);
        ArgumentNullException.ThrowIfNull(second_context);
        SendMessage(
            MessageContracts.Forums.ThreadReport,
            new CallForHelpFromForumThread(
                group_id,
                thread_id,
                category_id,
                report,
                first_context,
                second_context));
    }

    /// <summary>Reports a forum message to the moderators.</summary>
    /// <param name="group_id">The id of the group that owns the forum.</param>
    /// <param name="thread_id">The id of the thread that contains the message.</param>
    /// <param name="message_id">The id of the message.</param>
    /// <param name="category_id">The id of the report category.</param>
    /// <param name="report">The text of the report.</param>
    /// <param name="first_context">The first context string of the Flash report message.</param>
    /// <param name="second_context">The second context string of the Flash report message.</param>
    /// <remarks>The request is sent without waiting for a response.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a string argument is <see langword="null"/>.</exception>
    public void ReportMessage(
        Id group_id,
        Id thread_id,
        Id message_id,
        int category_id,
        string report,
        string first_context = "",
        string second_context = "")
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(first_context);
        ArgumentNullException.ThrowIfNull(second_context);
        SendMessage(
            MessageContracts.Forums.MessageReport,
            new CallForHelpFromForumMessage(
                group_id,
                thread_id,
                message_id,
                category_id,
                report,
                first_context,
                second_context));
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Reset(
            CurrentStateGeneration,
            () =>
            {
                _forum_pages.Clear();
                _forums.Clear();
                _details.Clear();
                _thread_pages.Clear();
                _threads.Clear();
                _message_pages.Clear();
                _messages.Clear();
                _unread_forums_count = null;
                snapshot = PublishSnapshot();
            },
            () => Publish(snapshot, ResetCompleted, listener => listener()));
    }

    private void StoreDetails(
        ForumDetails details,
        long generation)
    {
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _details[details.GroupId] = details;
                _forums[details.GroupId] = details.Summary;
                snapshot = PublishSnapshot();
            },
            () => Publish(snapshot, DetailsChanged, listener => listener(details)));
    }

    private void StoreForumPage(
        ForumsList message,
        long generation)
    {
        ForumsList page = Freeze(message);
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _forum_pages[
                    new ForumListPageKey(
                        page.ListCode,
                        page.StartIndex)] = page;
                foreach (ForumSummary forum in page.Forums)
                    _forums[forum.GroupId] = forum;
                snapshot = PublishSnapshot();
            },
            () => Publish(snapshot, ForumPageReceived, listener => listener(page)));
    }

    private void StoreThreadPage(
        ForumThreads message,
        long generation)
    {
        ForumThreads page = Freeze(message);
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _thread_pages[
                    new ForumThreadPageKey(
                        page.GroupId,
                        page.StartIndex)] = page;
                foreach (ForumThreadData thread in page.Threads)
                    _threads[
                        new ForumThreadKey(
                            page.GroupId,
                            thread.ThreadId)] = thread;
                snapshot = PublishSnapshot();
            },
            () => Publish(snapshot, ThreadPageReceived, listener => listener(page)));
    }

    private void StoreMessagePage(
        ThreadMessages message,
        long generation)
    {
        ThreadMessages page = Freeze(message);
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _message_pages[
                    new ForumMessagePageKey(
                        page.GroupId,
                        page.ThreadId,
                        page.StartIndex)] = page;
                foreach (ForumPost post in page.Messages)
                    _messages[
                        new ForumMessageKey(
                            page.GroupId,
                            page.ThreadId,
                            post.MessageId)] = post;
                snapshot = PublishSnapshot();
            },
            () => Publish(snapshot, MessagePageReceived, listener => listener(page)));
    }

    private void StoreThread(
        Id group_id,
        ForumThreadData thread,
        long generation)
    {
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _threads[
                    new ForumThreadKey(
                        group_id,
                        thread.ThreadId)] = thread;
                snapshot = PublishSnapshot();
            },
            () => Publish(
                snapshot,
                ThreadChanged,
                listener => listener(group_id, thread)));
    }

    private void StoreMessage(
        Id group_id,
        Id thread_id,
        ForumPost message,
        long generation)
    {
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _messages[
                    new ForumMessageKey(
                        group_id,
                        thread_id,
                        message.MessageId)] = message;
                snapshot = PublishSnapshot();
            },
            () => Publish(
                snapshot,
                MessageChanged,
                listener => listener(group_id, thread_id, message)));
    }

    private void StoreUnreadForumsCount(
        UnreadForumsCount message,
        long generation)
    {
        ForumSnapshot snapshot = ForumSnapshot.Empty;
        _state.Commit(
            generation,
            () =>
            {
                _unread_forums_count = message.Count;
                snapshot = PublishSnapshot();
            },
            () => Publish(
                snapshot,
                UnreadForumsCountChanged,
                listener => listener(message.Count)));
    }

    private ForumSnapshot PublishSnapshot()
    {
        var snapshot = new ForumSnapshot(
            ReadOnly(_forum_pages),
            ReadOnly(_forums),
            ReadOnly(_details),
            ReadOnly(_thread_pages),
            ReadOnly(_threads),
            ReadOnly(_message_pages),
            ReadOnly(_messages),
            _unread_forums_count);
        Volatile.Write(ref _snapshot, snapshot);
        return snapshot;
    }

    private void Publish<TDelegate>(
        ForumSnapshot snapshot,
        TDelegate? legacy,
        Action<TDelegate> invoke)
        where TDelegate : Delegate
    {
        List<Exception>? failures = null;
        PublishListeners(snapshot, legacy, invoke, ref failures);
        if (ReferenceEquals(Snapshot, snapshot))
        {
            PublishListeners(
                snapshot,
                SnapshotChanged,
                listener => listener(snapshot),
                ref failures);
        }
        if (failures is { Count: 1 })
            throw failures[0];
        if (failures is { Count: > 1 })
            throw new AggregateException(failures);
    }

    private void PublishListeners<TDelegate>(
        ForumSnapshot snapshot,
        TDelegate? listeners,
        Action<TDelegate> invoke,
        ref List<Exception>? failures)
        where TDelegate : Delegate
    {
        if (listeners is null)
            return;
        foreach (TDelegate listener in listeners.GetInvocationList().Cast<TDelegate>())
        {
            if (!ReferenceEquals(Snapshot, snapshot))
                return;
            try
            {
                invoke(listener);
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
            }
        }
    }

    private static void ValidatePage(
        int start_index,
        int max_count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start_index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(max_count);
    }

    private static ForumsList Freeze(ForumsList message) =>
        message with
        {
            Forums = Array.AsReadOnly(message.Forums.ToArray())
        };

    private static ForumThreads Freeze(ForumThreads message) =>
        message with
        {
            Threads = Array.AsReadOnly(message.Threads.ToArray())
        };

    private static ThreadMessages Freeze(ThreadMessages message) =>
        message with
        {
            Messages = Array.AsReadOnly(message.Messages.ToArray())
        };

    private static IReadOnlyDictionary<TKey, TValue> ReadOnly<TKey, TValue>(
        Dictionary<TKey, TValue> values) where TKey : notnull =>
        new ReadOnlyDictionary<TKey, TValue>(
            new Dictionary<TKey, TValue>(values));
}
