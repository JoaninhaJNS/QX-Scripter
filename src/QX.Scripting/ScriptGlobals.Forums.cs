using Qx.Game;
using Qx.Game.Application;
using Qx.Model.Forums;
using Qx.Model.Messages.Incoming;
using Qx.Model.Messages.Outgoing;
using ForumThreadData = Qx.Model.Forums.ForumThread;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Gets the whole cached forum state as one immutable snapshot.
    /// </summary>
    /// <remarks>
    /// The snapshot holds the forum, thread and message page caches, the per-id forum, thread and
    /// message maps, and the unread forum count. Reading again yields a new object once anything
    /// changed; a snapshot already held never changes underneath the caller. The state is cleared
    /// when the hotel connection closes.
    /// </remarks>
    public ForumSnapshot ForumState => Forums.Snapshot;

    /// <summary>
    /// Gets every forum seen so far, keyed by group id.
    /// </summary>
    /// <remarks>
    /// It is filled from forum directory pages and from single forum detail replies, and is empty
    /// until one of those arrives.
    /// </remarks>
    public IReadOnlyDictionary<Id, ForumSummary> KnownForums =>
        ForumState.KnownForums;

    /// <summary>
    /// Gets the full details of every forum whose detail reply has been seen, keyed by group id.
    /// </summary>
    /// <remarks>
    /// Details add the viewer's read, post and moderate permissions and the staff flag on top of
    /// the summary.
    /// </remarks>
    public IReadOnlyDictionary<Id, ForumDetails> ForumDetails =>
        ForumState.ForumDetails;

    /// <summary>
    /// Gets every thread seen so far, keyed by group id and thread id.
    /// </summary>
    /// <remarks>
    /// Threads arrive both from thread list pages and from single thread create and update
    /// replies.
    /// </remarks>
    public IReadOnlyDictionary<ForumThreadKey, ForumThreadData> ForumThreads =>
        ForumState.KnownThreads;

    /// <summary>
    /// Gets every forum post seen so far, keyed by group id, thread id and post id.
    /// </summary>
    /// <remarks>
    /// Posts arrive both from message list pages and from single post create and update replies.
    /// </remarks>
    public IReadOnlyDictionary<ForumMessageKey, ForumPost> ForumMessages =>
        ForumState.KnownMessages;

    /// <summary>
    /// Gets the number of forums that hold unread messages, or <see langword="null"/> until the
    /// server has sent the count.
    /// </summary>
    public int? UnreadForumsCount => ForumState.UnreadForumsCount;

    /// <summary>Finds a cached forum summary.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <returns>The summary, or <see langword="null"/> when this forum has not been seen.</returns>
    public ForumSummary? FindForum(Id group_id) =>
        Forums.FindForum(group_id);

    /// <summary>Finds the cached details of one forum, including the viewer's permissions.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <returns>
    /// The details, or <see langword="null"/> when no detail reply for this forum has arrived.
    /// </returns>
    public ForumDetails? FindForumDetails(Id group_id) =>
        Forums.FindDetails(group_id);

    /// <summary>Finds one cached thread.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread id.</param>
    /// <returns>The thread, or <see langword="null"/> when it has not been seen.</returns>
    public ForumThreadData? FindForumThread(
        Id group_id,
        Id thread_id) =>
        Forums.FindThread(group_id, thread_id);

    /// <summary>Finds one cached forum post.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread the post belongs to.</param>
    /// <param name="message_id">The post id.</param>
    /// <returns>The post, or <see langword="null"/> when it has not been seen.</returns>
    public ForumPost? FindForumMessage(
        Id group_id,
        Id thread_id,
        Id message_id) =>
        Forums.FindMessage(group_id, thread_id, message_id);

    /// <summary>
    /// Asks for one forum's details and permissions.
    /// </summary>
    /// <remarks>
    /// It returns immediately; the answer lands in <see cref="ForumDetails"/> and runs the
    /// <see cref="OnForumDetailsChanged(Action{Qx.Model.Forums.ForumDetails})"/> handlers.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void RequestForumStats(Id group_id) =>
        Application.Invoke<ForumDetailsRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumDetailsRequest,
            new ForumDetailsRequest(group_id));

    /// <summary>
    /// Asks for a page of the forum directory.
    /// </summary>
    /// <remarks>
    /// It returns immediately; the page lands in the forum cache and runs the
    /// <see cref="OnForumsListed(Action{ForumsList})"/> handlers.
    /// </remarks>
    /// <param name="list_code">
    /// The directory to list: <c>Active</c> (0), <c>Popular</c> (1) or <c>MyForums</c> (2).
    /// </param>
    /// <param name="start_index">The zero-based index of the first entry on the page.</param>
    /// <param name="max_count">The number of entries to return; the game client's own page size is 20.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start_index"/> is negative, or <paramref name="max_count"/> is zero or
    /// negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void RequestForums(
        ForumListCode list_code,
        int start_index = 0,
        int max_count = 20) =>
        Application.Invoke<ForumListRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumsListRequest,
            new ForumListRequest(list_code, start_index, max_count));

    /// <summary>
    /// Asks for a page of threads in one forum.
    /// </summary>
    /// <remarks>
    /// It returns immediately; the page lands in the thread cache and runs the
    /// <see cref="OnForumThreadsListed(Action{Qx.Model.Messages.Incoming.ForumThreads})"/> handlers.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="start_index">The zero-based index of the first thread on the page.</param>
    /// <param name="max_count">The number of threads to return; the game client's own page size is 20.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start_index"/> is negative, or <paramref name="max_count"/> is zero or
    /// negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void RequestForumThreads(
        Id group_id,
        int start_index = 0,
        int max_count = 20) =>
        Application.Invoke<ForumThreadsRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumThreadsRequest,
            new ForumThreadsRequest(group_id, start_index, max_count));

    /// <summary>
    /// Asks for a page of posts in one thread.
    /// </summary>
    /// <remarks>
    /// It returns immediately; the page lands in the message cache and runs the
    /// <see cref="OnForumMessagesListed(Action{ThreadMessages})"/> handlers.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread to read.</param>
    /// <param name="start_index">The zero-based index of the first post on the page.</param>
    /// <param name="max_count">The number of posts to return; the game client's own page size is 20.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start_index"/> is negative, or <paramref name="max_count"/> is zero or
    /// negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void RequestForumMessages(
        Id group_id,
        Id thread_id,
        int start_index = 0,
        int max_count = 20) =>
        Application.Invoke<ForumMessagesRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumMessagesRequest,
            new ForumMessagesRequest(
                group_id,
                thread_id,
                start_index,
                max_count));

    /// <summary>
    /// Asks for one thread's header row on its own, without its posts.
    /// </summary>
    /// <remarks>
    /// It returns immediately; the thread lands in the thread cache and runs the
    /// <see cref="OnForumThreadChanged(Action{Id, ForumThreadData})"/> handlers.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread id.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void RequestForumThread(
        Id group_id,
        Id thread_id) =>
        Application.Invoke<ForumThreadRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumThreadRequest,
            new ForumThreadRequest(group_id, thread_id));

    /// <summary>
    /// Asks how many forums hold unread messages.
    /// </summary>
    /// <remarks>
    /// It returns immediately; the number lands in <see cref="UnreadForumsCount"/> and runs the
    /// <see cref="OnUnreadForumsCountChanged(Action{int})"/> handlers.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void RequestUnreadForumsCount() =>
        Application.Invoke<ForumUnreadRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumsUnreadRequest,
            new ForumUnreadRequest());

    /// <summary>
    /// Starts a new thread.
    /// </summary>
    /// <remarks>
    /// It sends a post with a thread id of 0, which is how the client signals "create" rather
    /// than "reply".
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="subject">The thread title; the game client requires at least 10 characters.</param>
    /// <param name="message_text">The first post's body; the game client requires at least 10 characters.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="subject"/> or <paramref name="message_text"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void CreateForumThread(
        Id group_id,
        string subject,
        string message_text) =>
        Application.Invoke<ForumPostActionRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumsPost,
            new ForumPostActionRequest(group_id, 0, subject, message_text));

    /// <summary>Posts a reply into an existing thread, with an empty subject.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread to reply to.</param>
    /// <param name="message_text">The reply body.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message_text"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void ReplyToForumThread(
        Id group_id,
        Id thread_id,
        string message_text) =>
        Application.Invoke<ForumPostActionRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumsPost,
            new ForumPostActionRequest(group_id, thread_id, "", message_text));

    /// <summary>
    /// Sends the raw post message that thread creation and replying both wrap.
    /// </summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread to post into, or 0 to start a new thread.</param>
    /// <param name="subject">The subject; only meaningful when starting a thread.</param>
    /// <param name="message_text">The post body.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="subject"/> or <paramref name="message_text"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void PostForumMessage(
        Id group_id,
        Id thread_id,
        string subject,
        string message_text) =>
        Application.Invoke<ForumPostActionRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumsPost,
            new ForumPostActionRequest(group_id, thread_id, subject, message_text));

    /// <summary>Hides or restores a whole thread as a forum admin or as hotel staff.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread to moderate.</param>
    /// <param name="state">
    /// The new moderation state on the client's scale: 0 default, 1 restored by admin, 10 hidden
    /// by admin, 20 permanently hidden by staff. The Flash client sends 10 when the viewer only
    /// holds forum moderate rights, 20 when the viewer is staff, and 1 to undelete.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void ModerateForumThread(
        Id group_id,
        Id thread_id,
        int state) =>
        Application.Invoke<ForumThreadModerationRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumThreadModerate,
            new ForumThreadModerationRequest(group_id, thread_id, state));

    /// <summary>Hides or restores a single post as a forum admin or as hotel staff.</summary>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread the post belongs to.</param>
    /// <param name="message_id">The post to moderate.</param>
    /// <param name="state">
    /// The new moderation state, on the same scale as thread moderation: 0 default, 1 restored,
    /// 10 hidden by admin, 20 permanently hidden by staff.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void ModerateForumMessage(
        Id group_id,
        Id thread_id,
        Id message_id,
        int state) =>
        Application.Invoke<ForumMessageModerationRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumMessageModerate,
            new ForumMessageModerationRequest(group_id, thread_id, message_id, state));

    /// <summary>
    /// Rewrites a forum's four permission levels at once.
    /// </summary>
    /// <remarks>
    /// All four travel in one message, so read the current values out of the forum details first
    /// when only one of them should change. The ordering rules below come from the Flash settings
    /// dialog and are not enforced here; the server decides what it accepts.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="read_level">The level that may read the forum. The dialog offers levels 0 to 3, least to most restrictive.</param>
    /// <param name="post_message_level">The level that may reply; the dialog keeps it at or above <paramref name="read_level"/>.</param>
    /// <param name="post_thread_level">The level that may start threads; the dialog keeps it at or above <paramref name="post_message_level"/>.</param>
    /// <param name="moderate_level">The level that may moderate; the dialog offers only 2 and 3 here.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void UpdateForumSettings(
        Id group_id,
        int read_level,
        int post_message_level,
        int post_thread_level,
        int moderate_level) =>
        Application.Invoke<ForumSettingsUpdateRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumSettingsUpdate,
            new ForumSettingsUpdateRequest(
                group_id,
                read_level,
                post_message_level,
                post_thread_level,
                moderate_level));

    /// <summary>
    /// Marks forums as read up to a given post.
    /// </summary>
    /// <remarks>
    /// Several markers travel in one message.
    /// </remarks>
    /// <param name="markers">
    /// The markers, one per forum: the group id, the last post id that was read, and whether the
    /// whole forum should be treated as read.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="markers"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when there are more than 65535 markers.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void UpdateForumReadMarkers(
        params ForumReadMarker[] markers) =>
        Application.Invoke<ForumReadMarkersUpdateRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumReadMarkersUpdate,
            new ForumReadMarkersUpdateRequest(markers));

    /// <summary>
    /// Sets a thread's sticky and locked flags.
    /// </summary>
    /// <remarks>
    /// Both travel together, so pass the current value for whichever flag should stay as it is.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread to change.</param>
    /// <param name="is_sticky"><see langword="true"/> to pin the thread to the top of the list; otherwise, <see langword="false"/>.</param>
    /// <param name="is_locked"><see langword="true"/> to reject further replies; otherwise, <see langword="false"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void UpdateForumThread(
        Id group_id,
        Id thread_id,
        bool is_sticky,
        bool is_locked) =>
        Application.Invoke<ForumThreadUpdateRequest, ForumDispatchResult>(
            ApplicationMemberIds.ForumThreadUpdate,
            new ForumThreadUpdateRequest(group_id, thread_id, is_sticky, is_locked));

    /// <summary>
    /// Reports a forum thread to the moderators with a call for help.
    /// </summary>
    /// <remarks>
    /// The report is sent without waiting for an answer.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread to report.</param>
    /// <param name="category_id">The id of the report category.</param>
    /// <param name="report">The report text.</param>
    /// <param name="first_context">The first context value sent with the report.</param>
    /// <param name="second_context">The second context value sent with the report.</param>
    /// <exception cref="ArgumentNullException">Thrown when a text parameter is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void ReportForumThread(
    Id group_id,
    Id thread_id,
    int category_id,
    string report,
    string first_context = "",
    string second_context = "") =>
    Application.Invoke<ForumThreadReportRequest, ForumDispatchResult>(
        ApplicationMemberIds.ForumThreadReport,
        new ForumThreadReportRequest(
            group_id,
            thread_id,
            category_id,
            report,
            first_context,
            second_context));

    /// <summary>
    /// Reports a single forum post to the moderators with a call for help.
    /// </summary>
    /// <remarks>
    /// The report is sent without waiting for an answer.
    /// </remarks>
    /// <param name="group_id">The group that owns the forum.</param>
    /// <param name="thread_id">The thread the post belongs to.</param>
    /// <param name="message_id">The post to report.</param>
    /// <param name="category_id">The id of the report category.</param>
    /// <param name="report">The report text.</param>
    /// <param name="first_context">The first context value sent with the report.</param>
    /// <param name="second_context">The second context value sent with the report.</param>
    /// <exception cref="ArgumentNullException">Thrown when a text parameter is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void ReportForumMessage(
    Id group_id,
    Id thread_id,
    Id message_id,
    int category_id,
    string report,
    string first_context = "",
    string second_context = "") =>
    Application.Invoke<ForumMessageReportRequest, ForumDispatchResult>(
        ApplicationMemberIds.ForumMessageReport,
        new ForumMessageReportRequest(
            group_id,
            thread_id,
            message_id,
            category_id,
            report,
            first_context,
            second_context));
}
