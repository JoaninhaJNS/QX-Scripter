using Qx;
using Qx.Game;
using Qx.Game.Application;
using Qx.Game.Protocol;
using Qx.Game.Snapshots;
using Qx.Interception;
using Qx.Messages;
using Qx.Model;
using Qx.Model.Messages.Incoming;
using Qx.Model.Messages.Outgoing;
using Qx.Protocol;

namespace Qx.Scripting;

/// <summary>
/// Represents the globals object that every QX script runs against.
/// </summary>
/// <remarks>
/// <para>
/// All public members are in scope unqualified inside a <c>.csx</c> script, so
/// <c>Say("hi")</c> and <c>Room.RoomId</c> work without any receiver.
/// </para>
/// <para>
/// <b>State properties</b> such as <see cref="Users"/>, <see cref="FloorItems"/> or
/// <see cref="Credits"/> read whatever the interceptor has observed on the wire so far.
/// Nothing on this class polls the server on its own: if the game client never asked for a
/// piece of state, the corresponding property stays empty or zero rather than blocking. The
/// <c>Is...Loaded</c> flags distinguish "empty" from "not yet received", and the
/// <c>Ensure...Loaded</c> / <c>Get...</c> methods are the ones that actually go to the server.
/// Collections are snapshots taken per read, while the objects inside them are live and keep
/// updating.
/// </para>
/// <para>
/// <b>Events.</b> Every <c>On...</c> method returns an <see cref="IDisposable"/> handle.
/// Disposing it unsubscribes that one handler; all handles are disposed automatically when the
/// script stops, so a script that runs to completion never has to unsubscribe. Discarding the
/// handle does not unsubscribe. Handlers run on the interceptor's dispatch thread in packet
/// order and must not block for long. An exception thrown by a handler does not stop the other
/// handlers of the same event; it is reported as a script error, which stops the run in the QX
/// host. Callbacks that carry a before/after pair always pass the subject first, then the
/// previous value, then the new one.
/// </para>
/// <para>
/// <b>Requests</b> (<c>Get...</c>, <c>Search...</c>) send a message and await the matching reply.
/// Their <c>timeoutMs</c> is milliseconds and, for requests that retry, a total budget across the
/// automatic retry. Whether the reply is also delivered to the game client depends on the
/// request and is stated on each member. Every call goes to the server again. They throw
/// <see cref="Qx.Game.RequestTimeoutException"/> on timeout,
/// <see cref="Qx.Game.RequestDisconnectedException"/> when the connection drops while waiting,
/// <see cref="OperationCanceledException"/> when the script is stopped, and
/// <see cref="NotSupportedException"/> where the active client cannot express the request.
/// </para>
/// <para>
/// <b>Actions</b> (<see cref="Walk(int, int)"/>, <see cref="Say"/>, <see cref="PickupFurni(FloorItem, bool)"/>
/// and the rest) are fire-and-forget: they compose one packet and return. They never confirm
/// success, and the server silently ignores requests that fail on rights, flood limits or a
/// missing target. Observe the matching event instead. Actions do check their local
/// preconditions first and throw <see cref="InvalidOperationException"/> when one is not met,
/// for example when there is no active hotel session or no ready room.
/// </para>
/// <para>
/// <b>Raw messages.</b> Message names are plain strings resolved against the catalog loaded for
/// the active session; the compile-checked constants live on <see cref="Msg"/> in
/// <c>QX.Protocol</c>. A name that cannot be resolved throws when sending, but binds nothing
/// and stays silent when intercepting.
/// </para>
/// </remarks>
public partial class ScriptGlobals : IDisposable
{
    private readonly Action<string> _log;
    private readonly CancellationToken _cancellationToken;
    private readonly Action<Exception>? _backgroundError;
    private readonly Action? _backgroundFinishedCallback;
    private readonly Qx.Platform.Keyboard _hostKeyboard;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptGlobals"/> class.
    /// </summary>
    /// <remarks>
    /// Instances created through this constructor have no keyboard access, so
    /// <see cref="Keyboard"/> reports the keyboard as unsupported.
    /// </remarks>
    /// <param name="extension">The interceptor the script sends and intercepts messages through.</param>
    /// <param name="game">The shared game state that backs the state properties.</param>
    /// <param name="application">The application runtime that serves state views and commands.</param>
    /// <param name="log">The callback that receives every line written with <see cref="Log(object)"/>.</param>
    /// <param name="cancellationToken">The token that is canceled when the script is stopped.</param>
    /// <param name="backgroundError">The callback that receives exceptions thrown by background tasks, or <see langword="null"/> to write them to the log.</param>
    public ScriptGlobals(
        IInterceptor extension,
        GameState game,
        IApplicationRuntime application,
        Action<string> log,
        CancellationToken cancellationToken,
        Action<Exception>? backgroundError = null)
        : this(
            extension,
            game,
            application,
            log,
            cancellationToken,
            backgroundError,
            null,
            null)
    {
    }

    internal ScriptGlobals(
        IInterceptor extension,
        GameState game,
        IApplicationRuntime application,
        Action<string> log,
        CancellationToken cancellationToken,
        Action<Exception>? backgroundError,
        Action? backgroundFinished,
        Qx.Platform.Keyboard? keyboard)
    {
        Ext = extension;
        _hostKeyboard = keyboard ?? NoKeyboard;
        Game = game;
        Application = application;
        _log = log;
        _cancellationToken = cancellationToken;
        _backgroundError = backgroundError;
        _backgroundFinishedCallback = backgroundFinished;
    }

    /// <summary>
    /// Gets the interceptor the script is attached to.
    /// </summary>
    /// <remarks>
    /// Use it for packet sending and interception that the higher-level helpers on this class do
    /// not cover, and for <see cref="IInterceptor.Messages"/> when a message name has to be
    /// resolved by hand.
    /// </remarks>
    public IInterceptor Ext { get; }

    /// <summary>
    /// Gets the shared game state tracker that backs the state properties on this class.
    /// </summary>
    /// <remarks>
    /// It is owned by the host and survives across script runs, so state observed before the
    /// script started is already present.
    /// </remarks>
    public GameState Game { get; }

    /// <summary>
    /// Gets the application runtime that serves the state views, requests and commands behind
    /// the higher-level helpers.
    /// </summary>
    /// <remarks>
    /// Members are addressed by their string id, as listed in
    /// <see cref="IApplicationRuntime.Members"/>.
    /// </remarks>
    public IApplicationRuntime Application { get; }

    private ProfileStateView ReadProfileState() =>
        Application.Invoke<ProfileStateRequest, ProfileStateView>(
            ApplicationMemberIds.ProfileState,
            new ProfileStateRequest(),
            Ct);

    private static UserData? LegacyProfile(ProfileIdentitySnapshot? identity) =>
        identity is null
            ? null
            : new UserData
            {
                Id = identity.Id,
                Name = identity.Name,
                Figure = identity.Figure,
                Gender = identity.Gender,
                Motto = identity.Motto,
                RealName = identity.RealName,
                DirectMail = identity.DirectMail,
                RespectTotal = identity.RespectTotal,
                RespectLeft = identity.RespectLeft,
                PetRespectLeft = identity.PetRespectLeft,
                StreamPublishingAllowed = identity.StreamPublishingAllowed,
                LastAccessDate = identity.LastAccessDate,
                IsNameChangeable = identity.IsNameChangeable,
                IsSafetyLocked = identity.IsSafetyLocked,
                IsTradeLocked = identity.IsTradeLocked,
                NameColor = identity.NameColor,
                RespectReplenishesLeft = identity.RespectReplenishesLeft,
                MaxRespectPerDay = identity.MaxRespectPerDay,
                TrailingFields = identity.TrailingFields
            };

    private InventoryStateView ReadInventoryState() =>
        Application.Invoke<InventoryStateRequest, InventoryStateView>(
            ApplicationMemberIds.InventoryState,
            new InventoryStateRequest(),
            Ct);

    private TradeStateView ReadTradeState() =>
        Application.Invoke<TradeStateRequest, TradeStateView>(
            ApplicationMemberIds.TradeState,
            new TradeStateRequest(),
            Ct);

    private void SendTradeCommand(string member_id)
    {
        TradeStateView trade = ReadTradeState();
        Application.Invoke<TradeCommandRequest, TradeDispatchResult>(
            member_id,
            new TradeCommandRequest(
                trade.SessionGeneration,
                trade.Revision,
                trade.LatestEpoch),
            Ct);
    }

    internal static IReadOnlyList<InventoryItem> ReadInventoryItems(
        IApplicationRuntime application,
        CancellationToken cancellation_token = default) =>
        Array.AsReadOnly(
            InventoryApplicationPages.ReadFurni(
                    application,
                    cancellation_token: cancellation_token)
                .Items
                .Select(LegacyInventoryItem)
                .ToArray());

    internal static IReadOnlyList<InventoryPet> ReadInventoryPetModels(
        IApplicationRuntime application,
        CancellationToken cancellation_token = default) =>
        Array.AsReadOnly(
            InventoryApplicationPages.ReadPets(
                    application,
                    cancellation_token: cancellation_token)
                .Pets
                .Select(LegacyInventoryPet)
                .ToArray());

    internal static InventoryItem LegacyInventoryItem(InventoryItemSnapshot snapshot)
    {
        if (!Enum.TryParse(snapshot.Type, false, out ItemType item_type) ||
            item_type is not (ItemType.Floor or ItemType.Wall))
        {
            throw new InvalidDataException($"Unsupported inventory item type '{snapshot.Type}'.");
        }

        return new InventoryItem
        {
            ItemId = snapshot.ItemId,
            Type = item_type,
            Id = snapshot.Id,
            Kind = snapshot.Kind,
            Category = snapshot.Category,
            Data = LegacyItemData(snapshot.Data),
            IsRecyclable = snapshot.IsRecyclable,
            IsTradeable = snapshot.IsTradeable,
            IsGroupable = snapshot.IsGroupable,
            IsSellable = snapshot.IsSellable,
            SecondsToExpiration = snapshot.SecondsToExpiration,
            HasRentPeriodStarted = snapshot.HasRentPeriodStarted,
            RoomId = snapshot.RoomId,
            SlotId = snapshot.SlotId,
            Extra = snapshot.Extra
        };
    }

    internal static InventoryPet LegacyInventoryPet(InventoryPetSnapshot snapshot)
    {
        var pet = new InventoryPet
        {
            Id = snapshot.Id,
            Name = snapshot.Name,
            TypeId = snapshot.TypeId,
            PaletteId = snapshot.PaletteId,
            Color = snapshot.Color,
            BreedId = snapshot.BreedId,
            CustomParts = snapshot.CustomParts
                .Select(part => new PetCustomPart(part.LayerId, part.PartId, part.PaletteId))
                .ToArray(),
            Level = snapshot.Level,
            RarityLevel = snapshot.RarityLevel
        };
        if (pet.FigureString != snapshot.FigureString)
        {
            throw new InvalidDataException("The inventory pet snapshot is internally inconsistent.");
        }
        return pet;
    }

    private static ItemData LegacyItemData(ItemDataSnapshot snapshot)
    {
        ItemData data = snapshot.Type switch
        {
            nameof(ItemDataType.Legacy) => new LegacyData(),
            nameof(ItemDataType.Map) => LegacyMapData(snapshot),
            nameof(ItemDataType.StringArray) => LegacyStringArrayData(snapshot),
            nameof(ItemDataType.VoteResult) => new VoteResultData
            {
                Result = snapshot.VoteResult ?? throw MissingItemData(snapshot, nameof(snapshot.VoteResult))
            },
            nameof(ItemDataType.Empty) => new EmptyItemData(),
            nameof(ItemDataType.IntArray) => LegacyIntArrayData(snapshot),
            nameof(ItemDataType.HighScore) => LegacyHighScoreData(snapshot),
            nameof(ItemDataType.CrackableFurni) => new CrackableFurniData
            {
                Hits = snapshot.Hits ?? throw MissingItemData(snapshot, nameof(snapshot.Hits)),
                Target = snapshot.Target ?? throw MissingItemData(snapshot, nameof(snapshot.Target))
            },
            _ => throw new InvalidDataException($"Unsupported inventory item data type '{snapshot.Type}'.")
        };
        data.Flags = (ItemDataFlags)snapshot.Flags;
        data.Value = snapshot.Value;
        data.UniqueSerialNumber = snapshot.UniqueSerialNumber;
        data.UniqueSeriesSize = snapshot.UniqueSeriesSize;
        data.UniqueLimitedData = snapshot.UniqueLimitedData;
        if (data.IsLimitedRare != snapshot.IsLimitedRare || data.State != snapshot.State)
            throw new InvalidDataException("The inventory item data snapshot is internally inconsistent.");
        return data;
    }

    private static MapData LegacyMapData(ItemDataSnapshot snapshot)
    {
        var data = new MapData();
        foreach ((string key, string value) in
                 snapshot.MapEntries ?? throw MissingItemData(snapshot, nameof(snapshot.MapEntries)))
        {
            data.Entries.Add(key, value);
        }
        return data;
    }

    private static StringArrayData LegacyStringArrayData(ItemDataSnapshot snapshot)
    {
        var data = new StringArrayData();
        data.Values.AddRange(
            snapshot.StringValues ?? throw MissingItemData(snapshot, nameof(snapshot.StringValues)));
        return data;
    }

    private static IntArrayData LegacyIntArrayData(ItemDataSnapshot snapshot)
    {
        var data = new IntArrayData();
        data.Values.AddRange(
            snapshot.IntValues ?? throw MissingItemData(snapshot, nameof(snapshot.IntValues)));
        return data;
    }

    private static HighScoreData LegacyHighScoreData(ItemDataSnapshot snapshot)
    {
        var data = new HighScoreData
        {
            ScoreType = snapshot.ScoreType ?? throw MissingItemData(snapshot, nameof(snapshot.ScoreType)),
            ClearType = snapshot.ClearType ?? throw MissingItemData(snapshot, nameof(snapshot.ClearType))
        };
        foreach (HighScoreSnapshot score in
                 snapshot.HighScores ?? throw MissingItemData(snapshot, nameof(snapshot.HighScores)))
        {
            data.Scores.Add(new HighScore
            {
                Score = score.Score,
                Names = [.. score.Names]
            });
        }
        return data;
    }

    private static InvalidDataException MissingItemData(ItemDataSnapshot snapshot, string member) =>
        new($"Inventory item data type '{snapshot.Type}' is missing {member}.");

    /// <summary>
    /// Gets the panel a tab declares with <c>//@ui:</c> directives.
    /// </summary>
    /// <remarks>
    /// It holds the values the user entered, the button that started the run, the click handlers
    /// that keep the script alive after its body returns, and the sinks that write output boxes,
    /// tables, progress bars, status lines and toasts back to it. Outside panel mode every getter
    /// returns its fallback, <see cref="ScriptUi.Clicked"/> is always <see langword="false"/>, the
    /// writers do nothing and <see cref="ScriptUi.Confirm"/> and <see cref="ScriptUi.Prompt"/>
    /// answer at once rather than wait.
    /// </remarks>
    public ScriptUi Ui { get; } = new();

    /// <summary>
    /// Gets the live tracker of the current room session.
    /// </summary>
    /// <remarks>
    /// It covers room identity, entry and exit state, avatars, furni, the floor plan and the
    /// room events. It is present even when the user is outside a room, in which case its
    /// collections are empty and <see cref="RoomManager.RoomId"/> is 0.
    /// </remarks>
    public RoomManager Room => Game.Room;

    /// <summary>
    /// Gets a snapshot of the local user's profile state.
    /// </summary>
    /// <remarks>
    /// Every read builds a new <see cref="ProfileStateView"/> from the application runtime. The
    /// identity is <see langword="null"/> until the user data has been received.
    /// </remarks>
    public ProfileStateView Profile => ReadProfileState();

    /// <summary>Gets the tracker of the badges owned and the badge slots currently equipped.</summary>
    public BadgeInventoryManager BadgeInventory => Game.Badges;

    /// <summary>
    /// Gets a snapshot of the trade state.
    /// </summary>
    /// <remarks>
    /// Every read builds a new <see cref="TradeStateView"/> from the application runtime.
    /// <see cref="TradeStateView.Active"/> is <see langword="null"/> when no trade is open.
    /// </remarks>
    public TradeStateView Trade => ReadTradeState();

    /// <summary>Gets the tracker of quest and campaign state.</summary>
    public QuestManager Quests => Game.Quests;

    /// <summary>
    /// Gets a snapshot of the marketplace state with the first page of up to 100 cached entries.
    /// </summary>
    /// <remarks>
    /// Every read builds a new view. Use <see cref="GetMarketplaceStatePage(int, int)"/> to page
    /// through larger results.
    /// </remarks>
    public MarketplaceStateView Marketplace => GetMarketplaceStatePage();

    /// <summary>Gets the tracker of the crafting (alchemy) session state.</summary>
    public CraftingManager Crafting => Game.Crafting;

    /// <summary>Gets the tracker of received gifts and gift opening results.</summary>
    public GiftManager Gifts => Game.Gifts;

    /// <summary>Gets the tracker of Habbo Club and subscription state for the local user.</summary>
    public SubscriptionManager Subscriptions => Game.Subscriptions;

    /// <summary>
    /// Gets the tracker of group forum state such as thread lists, threads and moderation results.
    /// </summary>
    /// <remarks>Group forums exist only on the Flash client.</remarks>
    public ForumManager Forums => Game.Forums;

    /// <summary>
    /// Gets the cancellation token that is canceled when the script is stopped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pass it to every awaited call that accepts one. Awaiting it, or calling
    /// <see cref="Sleep(int)"/> or <see cref="Delay(int)"/>, throws
    /// <see cref="OperationCanceledException"/> once the script is asked to stop.
    /// </para>
    /// <para>
    /// Inside a script body this resolves to the ambient per-run token; outside a run it falls
    /// back to the token the globals were constructed with.
    /// </para>
    /// </remarks>
    public CancellationToken Ct
    {
        get
        {
            CancellationToken current = ScriptExecutionContext.CancellationToken;
            return current.CanBeCanceled ? current : _cancellationToken;
        }
    }

    internal CancellationToken BaseCancellationToken => _cancellationToken;

    private readonly List<TrackedSubscription> _subscriptions = [];
    private readonly List<Task> _backgroundTasks = [];
    private bool _cleanupHooked;
    private bool _backgroundClosed;
    private bool _disposed;
    private int _backgroundFinished;
    private CancellationTokenRegistration _cleanupRegistration;

    private IDisposable Track(IDisposable subscription)
    {
        CancellationToken cancellation_token = Ct;
        var tracked = new TrackedSubscription(subscription, Untrack);
        lock (_subscriptions)
        {
            if (_disposed)
            {
                tracked.Dispose();
                throw new ObjectDisposedException(nameof(ScriptGlobals));
            }
            if (_backgroundClosed || cancellation_token.IsCancellationRequested)
            {
                tracked.Dispose();
                throw new OperationCanceledException(
                    "Script event subscriptions are closed.",
                    cancellation_token);
            }

            try
            {
                if (!_cleanupHooked)
                {
                    _cleanupHooked = true;
                    _cleanupRegistration = cancellation_token.Register(DisposeSubscriptions);
                }
                if (_backgroundClosed || cancellation_token.IsCancellationRequested)
                    throw new OperationCanceledException(cancellation_token);
                _subscriptions.Add(tracked);
            }
            catch
            {
                tracked.Dispose();
                throw;
            }
        }

        return tracked;
    }

    private void Untrack(TrackedSubscription subscription)
    {
        lock (_subscriptions)
            _subscriptions.Remove(subscription);
    }

    private void DisposeSubscriptions()
    {
        TrackedSubscription[] subscriptions;
        lock (_subscriptions)
        {
            _backgroundClosed = true;
            subscriptions = [.. _subscriptions];
            _subscriptions.Clear();
        }
        foreach (TrackedSubscription subscription in subscriptions)
            subscription.Dispose();
    }

    /// <summary>
    /// Unsubscribes every handler this instance is still holding.
    /// </summary>
    /// <remarks>
    /// The host calls it when the script run ends; a script does not need to call it. After
    /// disposal no new subscriptions or background tasks are accepted.
    /// </remarks>
    public void Dispose()
    {
        lock (_subscriptions)
        {
            if (_disposed)
                return;
            _disposed = true;
            _backgroundClosed = true;
        }
        DisposeSubscriptions();
        _cleanupRegistration.Dispose();
    }

    /// <summary>
    /// Waits for the background work of the script to finish.
    /// </summary>
    /// <remarks>
    /// It covers tasks started by <see cref="RunTask(Action)"/> and event handlers that are still
    /// running. The host calls it during shutdown; scripts rarely need it. Once the script has
    /// been stopped, no new background work is accepted.
    /// </remarks>
    /// <param name="timeoutMs">The time to wait, in milliseconds.</param>
    /// <returns>
    /// <see langword="true"/> when every background task completed (or none was running);
    /// <see langword="false"/> when the timeout elapsed first. It does not throw on timeout.
    /// </returns>
    public async Task<bool> WaitForBackgroundTasksAsync(int timeoutMs = 500)
    {
        Task[] tasks;
        lock (_subscriptions)
        {
            if (Ct.IsCancellationRequested)
                _backgroundClosed = true;
            lock (_backgroundTasks)
                tasks = [.. _backgroundTasks];
        }
        if (tasks.Length == 0)
            return true;

        Task all = Task.WhenAll(tasks);
        if (all.IsCompleted)
        {
            await all.ConfigureAwait(false);
            return true;
        }

        Task completed = await Task.WhenAny(all, Task.Delay(timeoutMs)).ConfigureAwait(false);
        return ReferenceEquals(completed, all);
    }

    private bool TryTrackBackgroundTask(Task task, CancellationToken cancellation_token)
    {
        lock (_subscriptions)
        {
            if (_disposed || _backgroundClosed || cancellation_token.IsCancellationRequested)
                return false;
            lock (_backgroundTasks)
                _backgroundTasks.Add(task);
        }
        _ = RemoveBackgroundTask(task);
        return true;
    }

    private async Task RemoveBackgroundTask(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        finally
        {
            lock (_backgroundTasks)
                _backgroundTasks.Remove(task);
        }
    }

    private void ReportBackgroundFinished()
    {
        if (Interlocked.Exchange(ref _backgroundFinished, 1) == 0)
            _backgroundFinishedCallback?.Invoke();
    }

    /// <summary>
    /// Gets whether the interceptor has an active hotel session.
    /// </summary>
    /// <remarks>
    /// Sending a raw packet while it is <see langword="false"/> throws
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    public bool IsConnected => Ext.IsConnected;

    /// <summary>
    /// Gets the active hotel session, or <see langword="null"/> when there is none.
    /// </summary>
    /// <remarks>
    /// The session carries the host, port, hotel version, client identifier and client type of
    /// the intercepted connection.
    /// </remarks>
    public Session? Session => Ext.Session;

    /// <summary>
    /// Gets the client type that packets are built for.
    /// </summary>
    /// <remarks>
    /// It is the client of the active session, otherwise the active client of the message
    /// catalog, and <see cref="ClientType.Flash"/> when neither is known.
    /// </remarks>
    public ClientType Client => CurrentClient;

    /// <summary>
    /// Gets the local user's account data, or <see langword="null"/> until the server has sent it.
    /// </summary>
    /// <remarks>
    /// It holds the id, name, figure, gender, motto, respect counters and account flags. Every
    /// read builds a new <see cref="UserData"/> from <see cref="Profile"/>, so the returned object
    /// does not change afterwards.
    /// </remarks>
    public UserData? SelfProfile => LegacyProfile(Profile.Identity);

    /// <summary>
    /// Gets the local user's avatar in the current room, or <see langword="null"/> when it is not
    /// in the room.
    /// </summary>
    /// <remarks>
    /// It is also <see langword="null"/> before the avatar list has loaded or before the own user
    /// id is known. The object is live: its position, dance, effect and idle state are updated in
    /// place as packets arrive.
    /// </remarks>
    public User? SelfAvatar => Room.Self as User;

    /// <summary>Gets the local user's account data.</summary>
    /// <remarks>Alias of <see cref="SelfProfile"/>.</remarks>
    public UserData? Self => SelfProfile;

    /// <summary>Gets the local user's avatar in the current room.</summary>
    /// <remarks>Alias of <see cref="SelfAvatar"/>.</remarks>
    public User? Me => SelfAvatar;

    /// <summary>
    /// Gets whether a room session is open.
    /// </summary>
    /// <remarks>
    /// It becomes <see langword="true"/> while entering, so it does not mean that the entry has
    /// completed; use <see cref="IsRoomReady"/> for that. Avatars and furni arrive in separate
    /// messages, so check <see cref="RoomManager.AvatarsAreLoaded"/> and
    /// <see cref="RoomManager.FloorItemsAreLoaded"/> before relying on them.
    /// </remarks>
    public bool InRoom => Room.IsInRoom;

    /// <summary>
    /// Gets how the last room session ended, or <see langword="null"/> when no room has been left
    /// yet.
    /// </summary>
    /// <remarks>
    /// It holds the room id, whether the room had been fully entered, the native reason and the
    /// kick, if any.
    /// </remarks>
    public RoomExitState? LastRoomExit => Room.LastExit;

    /// <summary>Gets whether the last room exit was caused by the local user being kicked.</summary>
    public bool WasKickedFromRoom => Room.WasKicked;

    /// <summary>
    /// Gets the most recent kick observed in the current or a previous room session.
    /// </summary>
    /// <remarks>
    /// It is cleared when a new room session begins, and is <see langword="null"/> when no kick
    /// has been observed.
    /// </remarks>
    public RoomKick? LastRoomKick => Room.LastKick;

    /// <summary>
    /// Gets the kick that caused <see cref="LastRoomExit"/>, or <see langword="null"/> when the
    /// last room exit was not caused by a kick.
    /// </summary>
    public RoomKick? LastRoomExitKick => Room.LastExitKick;

    /// <summary>
    /// Gets every avatar currently in the room, including users, bots and pets.
    /// </summary>
    /// <remarks>
    /// A snapshot is taken on each read, so the sequence does not change while it is enumerated,
    /// but the <see cref="Avatar"/> objects in it are live and keep updating. It is empty when
    /// outside a room or before the avatar list has arrived.
    /// </remarks>
    public IEnumerable<Avatar> Avatars => Room.Avatars;

    /// <summary>
    /// Gets the users in the room, including the local user.
    /// </summary>
    /// <remarks>
    /// It is <see cref="Avatars"/> filtered to <see cref="User"/>. A snapshot is taken on each
    /// read; the elements are live.
    /// </remarks>
    public IEnumerable<User> Users => Room.Avatars.OfType<User>();

    /// <summary>
    /// Gets the pets in the room.
    /// </summary>
    /// <remarks>
    /// It is <see cref="Avatars"/> filtered to <see cref="Pet"/>. A snapshot is taken on each
    /// read; the elements are live.
    /// </remarks>
    public IEnumerable<Pet> Pets => Room.Avatars.OfType<Pet>();

    /// <summary>
    /// Gets the bots in the room.
    /// </summary>
    /// <remarks>
    /// It is <see cref="Avatars"/> filtered to <see cref="Bot"/>. A snapshot is taken on each
    /// read; the elements are live.
    /// </remarks>
    public IEnumerable<Bot> Bots => Room.Avatars.OfType<Bot>();

    /// <summary>
    /// Gets every floor item currently placed in the room.
    /// </summary>
    /// <remarks>
    /// A snapshot is taken on each read; the <see cref="FloorItem"/> objects are live and their
    /// location and state keep updating. It is empty until the server has sent the object list.
    /// Check <see cref="RoomManager.FloorItemsAreLoaded"/> to tell an empty room from one that
    /// has not loaded.
    /// </remarks>
    public IEnumerable<FloorItem> FloorItems => Room.FloorItems;

    /// <summary>
    /// Gets every wall item currently placed in the room.
    /// </summary>
    /// <remarks>
    /// It follows the same rules as <see cref="FloorItems"/>. Check
    /// <see cref="RoomManager.WallItemsAreLoaded"/> to tell an empty room from one that has not
    /// loaded.
    /// </remarks>
    public IEnumerable<WallItem> WallItems => Room.WallItems;

    /// <summary>
    /// Gets the furni currently held in the inventory.
    /// </summary>
    /// <remarks>
    /// It is empty until the inventory has been requested; call
    /// <see cref="EnsureInventoryLoaded"/> first, or check <see cref="IsInventoryLoaded"/>. Every
    /// read builds a new snapshot.
    /// </remarks>
    public IEnumerable<InventoryItem> InventoryItems => ReadInventoryItems(Application, Ct);

    /// <summary>
    /// Gets the pets currently held in the inventory.
    /// </summary>
    /// <remarks>
    /// It is empty until the pet inventory has been requested; call
    /// <see cref="EnsurePetInventoryLoaded"/> first, or check <see cref="IsPetInventoryLoaded"/>.
    /// Every read builds a new snapshot.
    /// </remarks>
    public IEnumerable<InventoryPet> InventoryPets => ReadInventoryPetModels(Application, Ct);

    /// <summary>
    /// Gets the friend list.
    /// </summary>
    /// <remarks>
    /// It is empty until the friend list has been received; call
    /// <see cref="EnsureFriendsLoaded"/> first, or check <see cref="IsFriendsLoaded"/>. A
    /// snapshot is taken on each read.
    /// </remarks>
    public IEnumerable<Friend> Friends => Game.Friends.Friends;

    /// <summary>
    /// Finds a user in the current room by name, ignoring case.
    /// </summary>
    /// <param name="name">The user name to look for.</param>
    /// <returns>The matching user, or <see langword="null"/> when nobody in the room matches.</returns>
    public User? FindUser(string name) => Room.UserByName(name);

    /// <summary>
    /// Gets the first avatar standing on the given tile.
    /// </summary>
    /// <remarks>
    /// Only the tile the avatar occupies is considered, not the tiles a walk animation passes
    /// over.
    /// </remarks>
    /// <param name="x">The tile x coordinate.</param>
    /// <param name="y">The tile y coordinate.</param>
    /// <returns>The avatar, or <see langword="null"/> when no avatar stands on the tile.</returns>
    public Avatar? AvatarAt(int x, int y) => Room.Avatars.FirstOrDefault(a => a.X == x && a.Y == y);

    /// <summary>
    /// Finds an entry in the friend list by name, ignoring case.
    /// </summary>
    /// <param name="name">The friend's name.</param>
    /// <returns>
    /// The friend, or <see langword="null"/> when there is no such friend, which is also the
    /// result when the friend list has not been loaded yet.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public Friend? FindFriend(string name) => Game.Friends.FriendByName(name);

    /// <summary>
    /// Gets whether the named user is in the friend list, ignoring case.
    /// </summary>
    /// <remarks>
    /// It returns <see langword="false"/> when the friend list has not been loaded yet, so call
    /// <see cref="EnsureFriendsLoaded"/> first when the answer must be authoritative.
    /// </remarks>
    /// <param name="name">The user name to look for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    public bool IsFriend(string name) => Game.Friends.IsFriend(name);

    /// <summary>
    /// Writes a line to the script's output log.
    /// </summary>
    /// <remarks>
    /// Values are rendered with <see cref="object.ToString"/>; <see langword="null"/> logs an
    /// empty line.
    /// </remarks>
    /// <param name="message">The value to write.</param>
    public void Log(object? message) => _log(message?.ToString() ?? "");

    /// <summary>
    /// Waits asynchronously for the given number of milliseconds, observing script cancellation.
    /// </summary>
    /// <param name="milliseconds">The time to wait, in milliseconds.</param>
    /// <returns>A task that completes after the delay.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped while waiting.</exception>
    public Task Delay(int milliseconds) => Task.Delay(milliseconds, Ct);

    /// <summary>
    /// Sends a raw packet to the server or the client, depending on the direction of its header.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when the packet was built for a different client than the active session uses.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Send(IPacket packet)
    {

        if (packet.Client is not ClientType.None && packet.Client != CurrentClient)
            throw new NotSupportedException($"A {packet.Client} packet cannot be sent through a {CurrentClient} session.");

        Ext.Send(packet);
    }

    /// <summary>
    /// Sends an outgoing message to the server by its message name.
    /// </summary>
    /// <remarks>
    /// Each value is written in order: <see cref="int"/>, <see cref="string"/>,
    /// <see cref="bool"/>, <see cref="short"/>, <see cref="long"/>, <see cref="byte"/>,
    /// <see cref="float"/>, <see cref="double"/>, <see cref="char"/>, <see cref="Id"/>,
    /// <see cref="Length"/> and <see cref="IComposer"/> values are supported.
    /// </remarks>
    /// <param name="name">The outgoing message name, resolved against the active message catalog.</param>
    /// <param name="values">The values to write into the packet body.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the message name cannot be resolved, or there is no active hotel session.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when a value has an unsupported type.</exception>
    public void SendToServer(string name, params object[] values) => SendNamed(Direction.Out, name, values);

    /// <summary>
    /// Sends an outgoing message to the server by its semantic message key.
    /// </summary>
    /// <remarks>
    /// The key is mapped to the message name of the active client, and the values are written
    /// as in <see cref="SendToServer(string, object[])"/>.
    /// </remarks>
    /// <param name="key">The semantic key of an outgoing message.</param>
    /// <param name="values">The values to write into the packet body.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the key is empty, unknown or not an outgoing message, or there is no active hotel session.
    /// </exception>
    /// <exception cref="NotSupportedException">Thrown when the message does not exist for the active client.</exception>
    public void SendToServer(MessageKey key, params object[] values) => SendNamed(Direction.Out, key, values);

    /// <summary>
    /// Sends an outgoing message to the server, composed from a message model.
    /// </summary>
    /// <typeparam name="T">The message model type.</typeparam>
    /// <param name="key">The semantic key of an outgoing message.</param>
    /// <param name="message">The message model that writes the packet body.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the key is empty, unknown, not an outgoing message, or has no header in the active catalog,
    /// or there is no active hotel session.
    /// </exception>
    public void SendToServer<T>(MessageKey key, T message) where T : IComposer
    {
        if (key.IsEmpty ||
            !Ext.Messages.Registry.TryGet(key, out MessageDescriptor? descriptor) ||
            descriptor.Direction != Direction.Out ||
            !Ext.Messages.TryGetHeader(key, out Header header))
        {
            throw new InvalidOperationException($"Unknown outgoing semantic message '{key.Value}'.");
        }

        using var packet = new Packet(header, CurrentClient)
        {
            Context = new ParserContext(
                Ext.Messages,
                Ext.Messages.GetWireProfile(CurrentClient))
        };
        packet.Writer().Compose(message);
        Send(packet);
    }

    /// <summary>
    /// Sends an incoming message to the game client by its message name, as if the server had
    /// sent it.
    /// </summary>
    /// <remarks>
    /// The values are written as in <see cref="SendToServer(string, object[])"/>.
    /// </remarks>
    /// <param name="name">The incoming message name, resolved against the active message catalog.</param>
    /// <param name="values">The values to write into the packet body.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the message name cannot be resolved, or there is no active hotel session.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when a value has an unsupported type.</exception>
    public void SendToClient(string name, params object[] values) => SendNamed(Direction.In, name, values);

    private void SendToClient<T>(MessageContract<T> contract, T message)
        where T : IParserComposer<T>
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(message);

        ClientType client = CurrentClient;
        if (!contract.Supports(client))
            throw new UnsupportedClientException(client);
        if (!Ext.Messages.TryGetHeader(contract.Key, out Header header))
            throw new InvalidOperationException($"Unknown incoming semantic message '{contract.Key.Value}'.");

        using var packet = new Packet(header, client)
        {
            Context = new ParserContext(
                Ext.Messages,
                Ext.Messages.GetWireProfile(client))
        };
        PacketWriter writer = packet.Writer();
        contract.Compose(message, in writer);
        Ext.Send(packet);
    }

    /// <summary>
    /// Registers a handler that runs for every packet with the given header.
    /// </summary>
    /// <param name="header">The exact header to intercept, which also fixes the direction.</param>
    /// <param name="handler">
    /// The handler to call with each matching intercept. Call <see cref="Intercept.Block"/>
    /// inside it to stop the packet from reaching its destination, or replace
    /// <see cref="Intercept.Packet"/> to rewrite it. An exception thrown by the handler does not
    /// stop the other handlers for the same header; it is reported as a script error.
    /// </param>
    /// <returns>
    /// A handle that removes the handler when disposed. It is also disposed automatically when
    /// the script stops.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnIntercept(Header header, Action<Intercept> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Track(Ext.Intercept(
            header,
            Guarded(handler)));
    }

    /// <summary>
    /// Registers a handler that runs for every packet matching the identifier's client,
    /// direction and message name.
    /// </summary>
    /// <remarks>
    /// While the identifier cannot be resolved against the active message catalog, the handler
    /// is bound to nothing and does not fire. It is resolved again whenever the catalog changes.
    /// </remarks>
    /// <param name="identifier">The client, direction and message name to intercept.</param>
    /// <param name="handler">
    /// The handler to call with each matching intercept. It can block or rewrite the packet.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="identifier"/> has no message name or no direction.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnIntercept(Identifier identifier, Action<Intercept> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Track(Ext.Intercept(identifier, Guarded(handler)));
    }

    /// <summary>
    /// Registers a handler that runs for every incoming packet of the named message.
    /// </summary>
    /// <remarks>
    /// While the name cannot be resolved against the active message catalog, the handler is
    /// bound to nothing and does not fire. It is resolved again whenever the catalog changes.
    /// </remarks>
    /// <param name="name">The incoming message name.</param>
    /// <param name="handler">
    /// The handler to call with each matching intercept. It can block or rewrite the packet.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnIn(string name, Action<Intercept> handler) =>
    Track(InterceptIncomingEvent(name, ClientType.None, handler));

    /// <summary>
    /// Registers a handler that runs for every incoming packet of the named Flash message.
    /// </summary>
    /// <remarks>
    /// The name is resolved for the Flash client only, and the handler does not fire while the
    /// name cannot be resolved against the active message catalog.
    /// </remarks>
    /// <param name="name">The incoming Flash message name.</param>
    /// <param name="handler">
    /// The handler to call with each matching intercept. It can block or rewrite the packet.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnFlashIn(string name, Action<Intercept> handler) =>
    Track(InterceptIncomingEvent(name, ClientType.Flash, handler));

    /// <summary>
    /// Registers a handler that runs for every outgoing packet of the named message.
    /// </summary>
    /// <remarks>
    /// While the name cannot be resolved against the active message catalog, the handler is
    /// bound to nothing and does not fire. It is resolved again whenever the catalog changes.
    /// </remarks>
    /// <param name="name">The outgoing message name.</param>
    /// <param name="handler">
    /// The handler to call with each matching intercept. It can block or rewrite the packet.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnOut(string name, Action<Intercept> handler) =>
    Track(InterceptOutgoingEvent(name, ClientType.None, handler));

    /// <summary>
    /// Registers a handler that runs for every outgoing packet of the named Flash message.
    /// </summary>
    /// <remarks>
    /// The name is resolved for the Flash client only, and the handler does not fire while the
    /// name cannot be resolved against the active message catalog.
    /// </remarks>
    /// <param name="name">The outgoing Flash message name.</param>
    /// <param name="handler">
    /// The handler to call with each matching intercept. It can block or rewrite the packet.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnFlashOut(string name, Action<Intercept> handler) =>
    Track(InterceptOutgoingEvent(name, ClientType.Flash, handler));

    /// <summary>
    /// Registers a handler that runs for every incoming packet of the named message, parsed into
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The packet is copied before parsing, so blocking or rewriting is not possible from this
    /// overload. Use <see cref="OnIn(string, Action{Intercept})"/> or
    /// <see cref="OnIn{T}(string, Action{T, Intercept})"/> for that. A packet that does not
    /// parse cleanly into <typeparamref name="T"/>, or leaves trailing bytes, raises an
    /// <see cref="InvalidOperationException"/> inside the dispatch, which is reported as a script
    /// error.
    /// </remarks>
    /// <typeparam name="T">The message model to parse the packet as.</typeparam>
    /// <param name="name">The incoming message name.</param>
    /// <param name="handler">The handler to call with each parsed message.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    public IDisposable OnIn<T>(string name, Action<T> handler) where T : IParserComposer<T> =>
        Track(InterceptIncomingEvent(
            name,
            ClientType.None,
            intercept => handler(ParseCopy<T>(name, intercept.Packet))));

    /// <summary>
    /// Registers a handler that runs for every incoming packet of the named message, with both
    /// the parsed message and the intercept.
    /// </summary>
    /// <remarks>
    /// The handler can read the typed message and still block the packet with
    /// <see cref="Intercept.Block"/>. The message is parsed from a copy of the packet.
    /// </remarks>
    /// <typeparam name="T">The message model to parse the packet as.</typeparam>
    /// <param name="name">The incoming message name.</param>
    /// <param name="handler">The handler to call with each parsed message and its intercept.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnIn<T>(string name, Action<T, Intercept> handler) where T : IParserComposer<T>
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Track(InterceptIncomingEvent(
            name,
            ClientType.None,
            intercept => handler(ParseCopy<T>(name, intercept.Packet), intercept)));
    }

    private IDisposable OnIn<T>(MessageContract<T> contract, Action<T> handler)
        where T : IParserComposer<T> =>
        Track(Ext.Intercept(
            contract.Key,
            Guarded<Intercept>(intercept => handler(ParseCopy(contract, intercept.Packet)))));

    /// <summary>
    /// Registers a handler that runs for every outgoing packet of the named message, parsed into
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The packet is copied before parsing, so the original cannot be blocked or rewritten from
    /// this overload. A packet that does not parse cleanly into <typeparamref name="T"/>, or
    /// leaves trailing bytes, raises an <see cref="InvalidOperationException"/> inside the
    /// dispatch, which is reported as a script error.
    /// </remarks>
    /// <typeparam name="T">The message model to parse the packet as.</typeparam>
    /// <param name="name">The outgoing message name.</param>
    /// <param name="handler">The handler to call with each parsed message.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    public IDisposable OnOut<T>(string name, Action<T> handler) where T : IParserComposer<T> =>
        Track(InterceptOutgoingEvent(
            name,
            ClientType.None,
            intercept => handler(ParseCopy<T>(name, intercept.Packet))));

    /// <summary>
    /// Registers a handler that runs for every outgoing packet of the named message, with both
    /// the parsed message and the intercept.
    /// </summary>
    /// <remarks>
    /// The handler can read the typed message and still block the packet with
    /// <see cref="Intercept.Block"/>. This is how a click in the room can be turned into a tile
    /// pick instead of a walk.
    /// </remarks>
    /// <typeparam name="T">The message model to parse the packet as.</typeparam>
    /// <param name="name">The outgoing message name.</param>
    /// <param name="handler">The handler to call with each parsed message and its intercept.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnOut<T>(string name, Action<T, Intercept> handler) where T : IParserComposer<T>
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Track(InterceptOutgoingEvent(
            name,
            ClientType.None,
            intercept => handler(ParseCopy<T>(name, intercept.Packet), intercept)));
    }

    /// <summary>
    /// Waits for the next packet with the given message name, in either direction, and parses
    /// it into <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The packet is not blocked and still reaches its destination.
    /// </remarks>
    /// <typeparam name="T">The message model to parse the packet as.</typeparam>
    /// <param name="name">The message name, resolved against both directions.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <returns>The parsed message.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or carries an <c>in:</c>, <c>out:</c> or <c>flash:</c> prefix.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the timeout elapsed, or the script was stopped, before a matching packet arrived.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the packet did not parse cleanly into <typeparamref name="T"/> or left trailing bytes.
    /// </exception>
    public async Task<T> ReceiveAsync<T>(string name, int timeoutMs = 10000) where T : IParserComposer<T>
    {
        using IPacket packet = await ReceiveAsync(name, timeoutMs);
        PacketReader reader = packet.Reader();
        T message = reader.Parse<T>();
        if (reader.Available != 0)
            throw new InvalidOperationException($"Message '{name}' contains {reader.Available} unparsed bytes for model '{typeof(T).Name}'.");
        return message;
    }

    /// <summary>
    /// Registers a handler that runs for every chat message seen in the room.
    /// </summary>
    /// <remarks>
    /// It covers talk, shout and whisper, from users, bots and pets alike. The chat carries the
    /// speaker's room index rather than their name.
    /// </remarks>
    /// <param name="handler">The handler to call with each chat message.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnChat(Action<AvatarChat> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Action<RoomChatEntry> guarded = Guarded<RoomChatEntry>(entry => handler(entry.Chat));
        return Track(Application.Subscribe(
            ApplicationMemberIds.RoomChatReceived,
            guarded));
    }

    /// <summary>
    /// Registers a handler that runs for every chat message in the room, with the speaking avatar
    /// resolved.
    /// </summary>
    /// <param name="handler">
    /// The handler to call with the speaking avatar and the chat. The avatar is
    /// <see langword="null"/> when the chat's room index is not (or no longer) in the avatar
    /// list, or the chat belongs to a previous room session.
    /// </param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is <see langword="null"/>.</exception>
    public IDisposable OnChat(Action<Avatar?, AvatarChat> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Action<RoomChatEntry> guarded = Guarded<RoomChatEntry>(entry =>
            handler(
                Room.Capture(room =>
                    room.Generation == entry.RoomGeneration
                        ? room.AvatarByIndex(entry.SpeakerIndex)
                        : null),
                entry.Chat));
        return Track(Application.Subscribe(
            ApplicationMemberIds.RoomChatReceived,
            guarded));
    }

    /// <summary>
    /// Waits for the next packet with the given message name, in either direction, and returns
    /// a copy of it.
    /// </summary>
    /// <remarks>
    /// The original is not blocked and still reaches its destination.
    /// </remarks>
    /// <param name="name">The message name, resolved against both directions.</param>
    /// <param name="timeoutMs">The timeout in milliseconds.</param>
    /// <returns>
    /// A copy of the packet, positioned at the start. The caller owns it and should dispose it.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or carries an <c>in:</c>, <c>out:</c> or <c>flash:</c> prefix.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the timeout elapsed, or the script was stopped, before a matching packet arrived.
    /// </exception>
    public Task<IPacket> ReceiveAsync(string name, int timeoutMs = 10000) =>
        CaptureAny([name], timeoutMs, false);

    // High-level actions (field orders verified against the decompiled Flash composers).

    /// <summary>
    /// Says a message in the room, audible to everyone nearby.
    /// </summary>
    /// <remarks>
    /// It does not wait for the server to echo the chat back, and gives no indication when the
    /// server drops it for flood control or filtering.
    /// </remarks>
    /// <param name="message">The text to say.</param>
    /// <param name="bubble">
    /// The chat bubble style id; 0 is the account's default bubble. Styles beyond the default
    /// set require the corresponding club or item.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is empty or white space.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session or no ready room.</exception>
    public void Talk(string message, int bubble = 0) =>
        Application.Invoke<RoomChatTalkRequest, RoomChatSendResult>(
            ApplicationMemberIds.RoomChatTalk,
            new RoomChatTalkRequest(message, bubble),
            Ct);

    /// <summary>Says a message in the room.</summary>
    /// <remarks>Alias of <see cref="Talk"/>.</remarks>
    /// <param name="message">The text to say.</param>
    /// <param name="bubble">The chat bubble style id; 0 is the account's default bubble.</param>
    public void Say(string message, int bubble = 0) => Talk(message, bubble);

    /// <summary>
    /// Shouts a message, which reaches the whole room instead of only nearby avatars.
    /// </summary>
    /// <remarks>
    /// It follows the same rules as <see cref="Talk"/>.
    /// </remarks>
    /// <param name="message">The text to shout.</param>
    /// <param name="bubble">The chat bubble style id; 0 is the account's default bubble.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is empty or white space.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session or no ready room.</exception>
    public void Shout(string message, int bubble = 0) =>
        Application.Invoke<RoomChatShoutRequest, RoomChatSendResult>(
            ApplicationMemberIds.RoomChatShout,
            new RoomChatShoutRequest(message, bubble),
            Ct);

    /// <summary>
    /// Whispers a message to one user in the room.
    /// </summary>
    /// <remarks>
    /// It follows the same rules as <see cref="Talk"/>: nothing confirms that the recipient
    /// received it.
    /// </remarks>
    /// <param name="recipient">The name of the user in the current room.</param>
    /// <param name="message">The text to whisper.</param>
    /// <param name="bubble">The chat bubble style id; 0 is the account's default bubble.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="recipient"/> or <paramref name="message"/> is empty or white space.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session or no ready room.</exception>
    public void Whisper(string recipient, string message, int bubble = 0) =>
    Application.Invoke<RoomChatWhisperRequest, RoomChatWhisperResult>(
        ApplicationMemberIds.RoomChatWhisper,
        new RoomChatWhisperRequest(recipient, message, bubble),
        Ct);

    /// <summary>
    /// Requests a walk to the given tile.
    /// </summary>
    /// <remarks>
    /// The server computes the path and may refuse or stop short; no completion is reported.
    /// Subscribe to <see cref="OnAvatarMoved"/> on the own avatar to observe the actual movement.
    /// </remarks>
    /// <param name="x">The target tile x coordinate.</param>
    /// <param name="y">The target tile y coordinate.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Walk(int x, int y) =>
        Application.Invoke<RoomAvatarWalkRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarWalk,
            new RoomAvatarWalkRequest(x, y),
            Ct);

    /// <summary>
    /// Turns the avatar to face the given tile without moving.
    /// </summary>
    /// <remarks>
    /// The server ignores it while the avatar is walking.
    /// </remarks>
    /// <param name="x">The tile x coordinate to face.</param>
    /// <param name="y">The tile y coordinate to face.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void LookTo(int x, int y) =>
        Application.Invoke<RoomAvatarLookRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarLook,
            new RoomAvatarLookRequest(x, y),
            Ct);

    /// <summary>
    /// Starts dancing.
    /// </summary>
    /// <remarks>
    /// The server ignores it while the avatar is sitting or lying, and rejects club-only styles
    /// for accounts without a subscription.
    /// </remarks>
    /// <param name="style">
    /// The dance style. 0 stops dancing; the client's dance menu offers styles 1 to 4, of which
    /// only 1 is available without Habbo Club.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Dance(int style = 1) =>
        Application.Invoke<RoomAvatarDanceRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarDance,
            new RoomAvatarDanceRequest(style),
            Ct);

    /// <summary>Stops dancing.</summary>
    /// <remarks>Equivalent to <c>Dance(0)</c>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void StopDancing() => Dance(0);

    /// <summary>
    /// Plays an avatar expression.
    /// </summary>
    /// <param name="type">
    /// The expression id: 0 clears the current expression (and wakes an idle avatar), 1 wave,
    /// 2 blow a kiss, 3 laugh, 4 cry, 5 go idle, 6 jump, 7 thumbs up.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Expression(int type) =>
        Application.Invoke<RoomAvatarExpressionRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarExpression,
            new RoomAvatarExpressionRequest(type),
            Ct);

    /// <summary>Waves.</summary>
    /// <remarks>Equivalent to <c>Expression(1)</c>.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Wave() => Expression(1);

    /// <summary>
    /// Sits down on the current tile.
    /// </summary>
    /// <remarks>
    /// The server ignores it when the avatar is standing on furni that dictates its own posture.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Sit() =>
        Application.Invoke<RoomAvatarPostureRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarPosture,
            new RoomAvatarPostureRequest(1),
            Ct);

    /// <summary>Stands up from a sitting or lying posture.</summary>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Stand() =>
        Application.Invoke<RoomAvatarPostureRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarPosture,
            new RoomAvatarPostureRequest(0),
            Ct);

    /// <summary>
    /// Uses (clicks) a floor item.
    /// </summary>
    /// <remarks>
    /// No result is reported, and the server ignores it when the item is not interactive or the
    /// user lacks rights.
    /// </remarks>
    /// <param name="id">The item's room id.</param>
    /// <param name="state">
    /// The interaction slot to trigger. 0 is the item's normal click action; multi-state furni
    /// use higher values for their additional actions.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void UseFloorItem(Id id, int state = 0) =>
        Application.Invoke<RoomFloorItemUseRequest, RoomItemDispatchResult>(
            ApplicationMemberIds.RoomItemFloorUse,
            new RoomFloorItemUseRequest(id, state),
            Ct);

    /// <summary>
    /// Uses (clicks) a wall item.
    /// </summary>
    /// <remarks>
    /// It follows the same rules as <see cref="UseFloorItem(Id, int)"/>.
    /// </remarks>
    /// <param name="id">The item's room id.</param>
    /// <param name="state">The interaction slot to trigger; 0 is the normal click action.</param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void UseWallItem(Id id, int state = 0) =>
        Application.Invoke<RoomWallItemUseRequest, RoomItemDispatchResult>(
            ApplicationMemberIds.RoomItemWallUse,
            new RoomWallItemUseRequest(id, state),
            Ct);

    /// <summary>
    /// Moves a floor item that is already placed in the room to a new tile and rotation.
    /// </summary>
    /// <remarks>
    /// It requires room rights; the server drops the request otherwise, and does not report a
    /// rejection when the target tile is occupied.
    /// </remarks>
    /// <param name="id">The item's room id.</param>
    /// <param name="x">The target tile x coordinate.</param>
    /// <param name="y">The target tile y coordinate.</param>
    /// <param name="direction">The rotation, in eighths of a turn (0 to 7).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when a coordinate is negative or <paramref name="direction"/> is outside 0 to 7.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no active hotel session or ready room, or the item is not in the room.
    /// </exception>
    public void MoveFloorItem(Id id, int x, int y, int direction) =>
        Application.Invoke<RoomPlacementFloorMoveRequest, RoomPlacementDispatchReceipt>(
            ApplicationMemberIds.RoomPlacementFloorMove,
            new RoomPlacementFloorMoveRequest(
                id,
                new RoomPlacementFloorPosition(x, y, direction)),
            Ct);

    /// <summary>
    /// Holds up a sign above the avatar for a few seconds.
    /// </summary>
    /// <param name="type">
    /// The sign to show: 0 to 10 are the numbered signs, 11 a heart, 12 a skull, and 13 to 17
    /// the remaining picture signs.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void Sign(int type) =>
        Application.Invoke<RoomAvatarSignRequest, RoomAvatarDispatchResult>(
            ApplicationMemberIds.RoomAvatarSign,
            new RoomAvatarSignRequest(type),
            Ct);

    /// <summary>Requests a walk to the given tile.</summary>
    /// <remarks>Alias of <see cref="Walk(int, int)"/>.</remarks>
    /// <param name="x">The target tile x coordinate.</param>
    /// <param name="y">The target tile y coordinate.</param>
    public void WalkTo(int x, int y) => Walk(x, y);

    /// <summary>Requests a walk to the given tile, as <see cref="Walk(int, int)"/> does.</summary>
    /// <param name="tile">The target tile; its height is ignored.</param>
    public void WalkTo(Tile tile) => Walk(tile.X, tile.Y);

    /// <summary>
    /// Requests a walk toward the tile the given avatar currently occupies.
    /// </summary>
    /// <remarks>
    /// Since that tile is taken, the server normally stops on an adjacent tile.
    /// </remarks>
    /// <param name="avatar">The avatar to walk toward.</param>
    public void WalkTo(Avatar avatar) => Walk(avatar.X, avatar.Y);

    /// <summary>Turns to face the given tile.</summary>
    /// <remarks>Alias of <see cref="LookTo(int, int)"/>.</remarks>
    /// <param name="x">The tile x coordinate to face.</param>
    /// <param name="y">The tile y coordinate to face.</param>
    public void FaceTo(int x, int y) => LookTo(x, y);

    /// <summary>Turns to face the given avatar's current tile.</summary>
    /// <param name="avatar">The avatar to face.</param>
    public void FaceTo(Avatar avatar) => LookTo(avatar.X, avatar.Y);

    /// <summary>
    /// Leaves the current room.
    /// </summary>
    /// <remarks>
    /// Subscribe to <see cref="OnLeftRoom"/> or <see cref="OnRoomExited"/> to know when the room
    /// session has actually ended.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when there is no active hotel session.</exception>
    public void LeaveRoom() =>
        Application.Invoke<RoomLeaveRequest, RoomLifecycleDispatchResult>(
            ApplicationMemberIds.RoomLeave,
            new RoomLeaveRequest(),
            Ct);

    /// <summary>
    /// Asks another user in the room to open a trade.
    /// </summary>
    /// <remarks>
    /// The trade opens only if they accept and the room's trade mode allows it. Subscribe to
    /// <see cref="OnTradeOpened"/> and <see cref="OnTradeOpenFailed"/> for the outcome.
    /// </remarks>
    /// <param name="userIndex">The other user's room index, not their user id.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="userIndex"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no active hotel session or ready room, the index is not another user in the room,
    /// or a trade is already open.
    /// </exception>
    public void OpenTrade(int userIndex) => OpenTrade(userIndex, null);

    private void OpenTrade(int user_index, Id? expected_user_id)
    {
        TradeStateView trade = ReadTradeState();
        Application.Invoke<TradeOpenRequest, TradeDispatchResult>(
            ApplicationMemberIds.TradeOpen,
            new TradeOpenRequest(
                user_index,
                trade.SessionGeneration,
                trade.Revision,
                trade.LatestEpoch,
                trade.RoomGeneration,
                expected_user_id),
            Ct);
    }

    /// <summary>Asks the given user to open a trade, as <see cref="OpenTrade(int)"/> does.</summary>
    /// <param name="user">The user in the current room to trade with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no active hotel session or ready room, the user's room index no longer belongs
    /// to that user, the user is the local user, or a trade is already open.
    /// </exception>
    public void OpenTrade(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        OpenTrade(user.Index, user.Id);
    }

    /// <summary>Adds a single inventory item to the open trade offer.</summary>
    /// <param name="itemId">The inventory item id.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="itemId"/> is 0 or outside the 32-bit range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no trade is open in the offer phase.</exception>
    public void OfferTradeItem(long itemId) => OfferTradeItems(itemId);

    /// <summary>
    /// Adds several inventory items to the open trade offer in one message.
    /// </summary>
    /// <remarks>
    /// Adding items resets both sides' acceptance.
    /// </remarks>
    /// <param name="itemIds">The distinct inventory item ids to offer.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="itemIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the list is empty, or an id is 0 or outside the 32-bit range.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when the list contains the same id twice.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no trade is open in the offer phase.</exception>
    public void OfferTradeItems(params long[] itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        TradeStateView trade = ReadTradeState();
        Application.Invoke<TradeItemsAddRequest, TradeDispatchResult>(
            ApplicationMemberIds.TradeItemsAdd,
            new TradeItemsAddRequest(
                itemIds.Select(item_id => (Id)item_id).ToArray(),
                trade.SessionGeneration,
                trade.Revision,
                trade.LatestEpoch),
            Ct);
    }

    /// <summary>Removes an item from the own trade offer, resetting both sides' acceptance.</summary>
    /// <param name="itemId">The inventory item id to remove from the offer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="itemId"/> is 0 or outside the 32-bit range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no trade is open in the offer phase.</exception>
    public void RemoveTradeItem(Id itemId)
    {
        TradeStateView trade = ReadTradeState();
        Application.Invoke<TradeItemRemoveRequest, TradeDispatchResult>(
            ApplicationMemberIds.TradeItemRemove,
            new TradeItemRemoveRequest(
                itemId,
                trade.SessionGeneration,
                trade.Revision,
                trade.LatestEpoch),
            Ct);
    }

    /// <summary>
    /// Accepts the current trade offer.
    /// </summary>
    /// <remarks>
    /// This is the first of the two confirmation steps; the trade still needs
    /// <see cref="ConfirmTrade"/> from both sides afterwards.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no trade is open in the offer phase, the local user cannot trade, or the required silver
    /// fee has not been reached.
    /// </exception>
    public void AcceptTrade() => SendTradeCommand(ApplicationMemberIds.TradeAccept);

    /// <summary>Withdraws a previous <see cref="AcceptTrade"/>, returning the trade to the offer phase.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no trade is open.</exception>
    public void UnacceptTrade() => SendTradeCommand(ApplicationMemberIds.TradeUnaccept);

    /// <summary>
    /// Confirms the trade in the final phase, after both sides have accepted.
    /// </summary>
    /// <remarks>
    /// The trade completes once both sides have confirmed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no trade is waiting for confirmation, the local user cannot trade, or the required silver
    /// fee has not been reached.
    /// </exception>
    public void ConfirmTrade() => SendTradeCommand(ApplicationMemberIds.TradeConfirm);

    /// <summary>Cancels the trade for both participants.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no trade is open.</exception>
    public void CancelTrade() => SendTradeCommand(ApplicationMemberIds.TradeClose);

    private Packet NewPacket(Direction direction, string name)
    {
        var identifier = new Identifier(ClientType.None, direction, name);
        if (!Ext.Messages.TryGetHeader(identifier, out Header header))
            throw new InvalidOperationException($"Unknown {(direction == Direction.Out ? "outgoing" : "incoming")} message '{name}'.");
        return new Packet(header, CurrentClient);
    }

    private void SendNamed(Direction direction, string name, object[] values, Header? preferred_header = null)
    {

        using Packet packet = NewPacket(direction, name);
        packet.Writer().WriteValues(values);
        Ext.Send(packet);
    }

    private void SendNamed(Direction direction, MessageKey key, object[] values)
    {
        if (key.IsEmpty ||
            !Ext.Messages.Registry.TryGet(key, out MessageDescriptor? descriptor) ||
            descriptor.Direction != direction)
        {
            throw new InvalidOperationException($"Unknown semantic message '{key.Value}'.");
        }

        string name = descriptor.NameFor(CurrentClient) ??
            throw new NotSupportedException($"Message '{key.Value}' is unavailable for {CurrentClient}.");
        SendNamed(direction, name, values);
    }

    private ClientType CurrentClient
    {
        get
        {
            ClientType client = Session?.Client ?? Ext.Messages.ActiveClient;
            return client is ClientType.None ? ClientType.Flash : client;
        }
    }

    private static T ParseCopy<T>(string name, IPacket packet) where T : IParserComposer<T>
    {
        using IPacket copy = packet.Copy();
        copy.Position = 0;
        PacketReader reader = copy.Reader();
        T message = reader.Parse<T>();
        if (reader.Available != 0)
            throw new InvalidOperationException($"Message '{name}' contains {reader.Available} unparsed bytes for model '{typeof(T).Name}'.");
        return message;
    }

    private static T ParseCopy<T>(MessageContract<T> contract, IPacket packet)
        where T : IParserComposer<T>
    {
        using IPacket copy = packet.Copy();
        copy.Position = 0;
        PacketReader reader = copy.Reader();
        T message = contract.Parse(in reader);
        if (reader.Available != 0)
        {
            throw new InvalidOperationException(
                $"Message '{contract.Key}' contains {reader.Available} unparsed bytes for model '{typeof(T).Name}'.");
        }
        return message;
    }

    private IDisposable InterceptIncomingEvent(string name, ClientType requested, Action<Intercept> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var identifier = new Identifier(requested, Direction.In, name);
        return Ext.Intercept(identifier, Guarded(handler));
    }

    private IDisposable InterceptOutgoingEvent(string name, ClientType requested, Action<Intercept> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Ext.Intercept(new Identifier(requested, Direction.Out, name), Guarded(handler));
    }

    private sealed class Unsubscriber(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private sealed class TrackedSubscription(
        IDisposable subscription,
        Action<TrackedSubscription> untrack) : IDisposable
    {
        private IDisposable? _subscription = subscription;

        public void Dispose()
        {
            IDisposable? current = Interlocked.Exchange(ref _subscription, null);
            if (current is null)
                return;
            untrack(this);
            current.Dispose();
        }
    }

}
