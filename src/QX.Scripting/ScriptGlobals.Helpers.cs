using Qx.Game;
using Qx.Game.Application;
using Qx.Messages;
using Qx.Model;
using Qx.Protocol;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    private static readonly ConcurrentDictionary<string, object?> _globals = new();
    private static readonly object _global_sync = new();

    /// <summary>
    /// Finds a user in the current room by account id.
    /// </summary>
    /// <param name="id">The account id of the user.</param>
    /// <returns>
    /// The user, or <see langword="null"/> when nobody with that id is in the room. Bots and
    /// pets are never returned even when their id matches.
    /// </returns>
    public User? GetUser(Id id) => Room.AvatarById(id) as User;

    /// <summary>
    /// Finds a user in the current room by name, case-insensitively.
    /// </summary>
    /// <param name="name">The name of the user.</param>
    /// <returns>The user, or <see langword="null"/> when nobody in the room matches.</returns>
    public User? GetUser(string name) => Room.UserByName(name);

    /// <summary>
    /// Finds a pet in the current room by name, case-insensitively.
    /// </summary>
    /// <param name="name">The name of the pet.</param>
    /// <returns>The pet, or <see langword="null"/> when no pet in the room matches.</returns>
    public Pet? GetPet(string name) =>
        Pets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Finds a bot in the current room by name, case-insensitively.
    /// </summary>
    /// <param name="name">The name of the bot.</param>
    /// <returns>The bot, or <see langword="null"/> when no bot in the room matches.</returns>
    public Bot? GetBot(string name) =>
        Bots.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Gets whether a trade window is currently open.</summary>
    public bool IsTrading => Trade.Active is not null;

    /// <summary>
    /// Gets the state of the room session: outside a room, entering, ready, or leaving.
    /// </summary>
    public RoomSessionState RoomState => Room.State;

    /// <summary>
    /// Gets whether the room session is ready, which is the case once the server has reported
    /// the room ready and confirmed the entry.
    /// </summary>
    /// <remarks>
    /// Prefer it over <see cref="InRoom"/> before reading room contents. It does not wait for
    /// the avatar, furni, floor plan or heightmap messages, which arrive separately.
    /// </remarks>
    public bool IsRoomReady => Room.IsReady;

    /// <summary>
    /// Gets whether the furni inventory has been fully received.
    /// </summary>
    /// <remarks>
    /// While it is <see langword="false"/>, <see cref="InventoryItems"/> is empty or holds stale
    /// items from an earlier load.
    /// </remarks>
    public bool IsInventoryLoaded => ReadInventoryState().Furni.Loaded;

    /// <summary>
    /// Gets whether the cached furni inventory holds items that a completed load has not
    /// confirmed.
    /// </summary>
    /// <remarks>
    /// It is <see langword="true"/> after the server invalidates the inventory and while a
    /// reload runs over existing items. The old items stay readable until the reload completes,
    /// while <see cref="IsInventoryLoaded"/> is <see langword="false"/>.
    /// </remarks>
    public bool IsInventoryStale => ReadInventoryState().Furni.Stale;

    /// <summary>Gets whether the pet inventory has been fully received.</summary>
    public bool IsPetInventoryLoaded => ReadInventoryState().Pets.Loaded;

    /// <summary>
    /// Gets whether the cached pet inventory holds pets that a completed load has not confirmed.
    /// </summary>
    /// <remarks>
    /// It is <see langword="true"/> after the server invalidates the pet inventory and while a
    /// reload runs over existing pets.
    /// </remarks>
    public bool IsPetInventoryStale => ReadInventoryState().Pets.Stale;

    /// <summary>
    /// Gets whether the complete friend list has been received.
    /// </summary>
    /// <remarks>
    /// While it is <see langword="false"/>, <see cref="Friends"/> and
    /// <see cref="IsFriend(string)"/> are not authoritative.
    /// </remarks>
    public bool IsFriendsLoaded => Game.Friends.IsLoaded;

    /// <summary>
    /// Gets whether the cached friend list holds friends that a completed load has not
    /// confirmed.
    /// </summary>
    /// <remarks>
    /// It is <see langword="true"/> while a new load runs over an existing list, after a load was
    /// abandoned, or when the hotel reported friend changes before the list was loaded.
    /// </remarks>
    public bool IsFriendsStale => Game.Friends.IsStale;

    /// <summary>
    /// Gets whether the achievement list has been received.
    /// </summary>
    /// <remarks>
    /// It is what makes an empty <see cref="Achievements"/> meaningful.
    /// </remarks>
    public bool IsAchievementsLoaded => Game.Achievements.IsLoaded;

    /// <summary>
    /// Gets the achievement category the server marked as the default one for the achievement UI.
    /// </summary>
    /// <remarks>Empty until the achievement list has been received.</remarks>
    public string AchievementDefaultCategory => Game.Achievements.DefaultCategory;

    /// <summary>
    /// Finds a pet in the pet inventory by id.
    /// </summary>
    /// <param name="id">The id of the pet.</param>
    /// <returns>
    /// The pet, or <see langword="null"/> when it is not in the cached inventory, which is also
    /// the answer while the pet inventory has never been loaded.
    /// </returns>
    public InventoryPet? GetInventoryPet(Id id) =>
        InventoryApplicationPages.ReadPets(
                Application,
                pet_id: id,
                cancellation_token: Ct)
            .Pets
            .Select(LegacyInventoryPet)
            .FirstOrDefault();

    /// <summary>
    /// Finds a pet in the pet inventory by name, case-insensitively.
    /// </summary>
    /// <param name="name">The name of the pet.</param>
    /// <returns>
    /// The pet, or <see langword="null"/> when no pet in the cached inventory matches, including
    /// when the pet inventory has never been loaded.
    /// </returns>
    public InventoryPet? GetInventoryPet(string name) =>
        InventoryApplicationPages.ReadPets(
                Application,
                name: name,
                cancellation_token: Ct)
            .Pets
            .Select(LegacyInventoryPet)
            .FirstOrDefault();

    /// <summary>
    /// Gets the room's static floor plan, which tells which tiles exist and at what stack height.
    /// </summary>
    /// <remarks>
    /// Available early in room entry. <see langword="null"/> outside a room or before it has
    /// arrived.
    /// </remarks>
    public FloorPlan? FloorPlan => Room.FloorPlan;

    /// <summary>
    /// Gets the room's heightmap, which unlike <see cref="FloorPlan"/> also reflects furni
    /// currently blocking a tile.
    /// </summary>
    /// <remarks><see langword="null"/> outside a room or before it has arrived.</remarks>
    public Heightmap? Heightmap => Room.Heightmap;

    /// <summary>
    /// Gets the floor plan stack height of a tile.
    /// </summary>
    /// <param name="x">The tile x coordinate.</param>
    /// <param name="y">The tile y coordinate.</param>
    /// <returns>
    /// The height, or -1 when the tile is a hole, is outside the room, or the floor plan has not
    /// arrived yet.
    /// </returns>
    public int TileHeight(int x, int y) => Room.FloorPlan?.HeightAt(x, y) ?? -1;

    /// <summary>
    /// Gets whether a tile is part of the room's floor, ignoring anything standing on it.
    /// </summary>
    /// <remarks>
    /// Uses the heightmap when available and falls back to the floor plan.
    /// </remarks>
    /// <param name="x">The tile x coordinate.</param>
    /// <param name="y">The tile y coordinate.</param>
    /// <returns>
    /// <see langword="true"/> when the tile is floor; <see langword="false"/> for holes,
    /// out-of-bounds tiles and when neither map has loaded.
    /// </returns>
    public bool IsOpenTile(int x, int y) => Room.Heightmap is { } map
        ? map.TileAt(x, y).IsFloor
        : Room.FloorPlan?.IsOpen(x, y) ?? false;

    /// <summary>
    /// Gets whether a tile can currently be stepped on: it is floor, the heightmap does not mark
    /// it blocked by furni, and no avatar is standing on it.
    /// </summary>
    /// <remarks>
    /// Without a heightmap it degrades to <see cref="IsOpenTile"/> plus the avatar check, so
    /// blocking furni is not accounted for.
    /// </remarks>
    /// <param name="x">The tile x coordinate.</param>
    /// <param name="y">The tile y coordinate.</param>
    /// <returns><see langword="true"/> when the tile is free; otherwise, <see langword="false"/>.</returns>
    public bool IsWalkable(int x, int y)
    {
        if (AvatarAt(x, y) is not null)
            return false;
        if (Room.Heightmap is { } map)
            return map.TileAt(x, y).IsFree;
        return IsOpenTile(x, y);
    }

    /// <summary>
    /// Gets the furni inventory, requesting it from the server and waiting for the full load if
    /// it is not already there.
    /// </summary>
    /// <remarks>
    /// A loaded inventory that is not stale returns immediately without touching the network;
    /// a missing or stale inventory is requested again.
    /// </remarks>
    /// <param name="timeout_ms">The timeout for the load, in milliseconds.</param>
    /// <param name="cancellation_token">
    /// An extra token to cancel on, combined with the script's own. Leave unset to use only the
    /// script's.
    /// </param>
    /// <returns>A snapshot of every inventory item.</returns>
    /// <exception cref="TimeoutException">Thrown when the inventory did not finish loading in time.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no hotel session is active, or the connection closed or the session changed during the load.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped, or the supplied token fired.</exception>
    public async Task<IReadOnlyCollection<InventoryItem>> EnsureInventoryLoaded(
        int timeout_ms = 10000,
        CancellationToken cancellation_token = default)
    {
        if (cancellation_token == default)
            return await LoadInventory(timeout_ms, Ct).ConfigureAwait(false);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(Ct, cancellation_token);
        return await LoadInventory(timeout_ms, linked.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the pet inventory, requesting it and waiting for the full load if needed.
    /// </summary>
    /// <remarks>
    /// A loaded pet inventory that is not stale returns immediately without touching the
    /// network; a missing or stale one is requested again.
    /// </remarks>
    /// <param name="timeout_ms">The timeout for the load, in milliseconds.</param>
    /// <param name="cancellation_token">An extra token to cancel on, combined with the script's own.</param>
    /// <returns>A snapshot of every inventory pet.</returns>
    /// <exception cref="TimeoutException">Thrown when the pet inventory did not finish loading in time.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no hotel session is active, or the connection closed or the session changed during the load.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped, or the supplied token fired.</exception>
    public async Task<IReadOnlyCollection<InventoryPet>> EnsurePetInventoryLoaded(
        int timeout_ms = 10000,
        CancellationToken cancellation_token = default)
    {
        if (cancellation_token == default)
            return await LoadPetInventory(timeout_ms, Ct).ConfigureAwait(false);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(Ct, cancellation_token);
        return await LoadPetInventory(timeout_ms, linked.Token).ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<InventoryItem>> LoadInventory(
        int timeout_ms,
        CancellationToken cancellation_token)
    {
        InventoryFurniPage first = await Application
            .InvokeAsync<InventoryFurniPageRequest, InventoryFurniPage>(
                ApplicationMemberIds.InventoryFurniList,
                new InventoryFurniPageRequest(Limit: 500),
                cancellation_token)
            .ConfigureAwait(false);
        if (!first.Loaded || first.Stale || first.RecoveryPending)
        {
            first = await Application
                .InvokeAsync<InventoryFurniRefreshRequest, InventoryFurniPage>(
                    ApplicationMemberIds.InventoryFurniRefresh,
                    new InventoryFurniRefreshRequest(
                        Limit: 500,
                        TimeoutMilliseconds: timeout_ms),
                    cancellation_token)
                .ConfigureAwait(false);
        }
        InventoryFurniPage inventory = await InventoryApplicationPages.CompleteFurniAsync(
            Application,
            first,
            cancellation_token: cancellation_token).ConfigureAwait(false);
        return Array.AsReadOnly(inventory.Items.Select(LegacyInventoryItem).ToArray());
    }

    private async Task<IReadOnlyCollection<InventoryPet>> LoadPetInventory(
        int timeout_ms,
        CancellationToken cancellation_token)
    {
        InventoryPetPage first = await Application
            .InvokeAsync<InventoryPetPageRequest, InventoryPetPage>(
                ApplicationMemberIds.InventoryPetsList,
                new InventoryPetPageRequest(Limit: 500),
                cancellation_token)
            .ConfigureAwait(false);
        if (!first.Loaded || first.Stale || first.RecoveryPending)
        {
            first = await Application
                .InvokeAsync<InventoryPetRefreshRequest, InventoryPetPage>(
                    ApplicationMemberIds.InventoryPetsRefresh,
                    new InventoryPetRefreshRequest(
                        Limit: 500,
                        TimeoutMilliseconds: timeout_ms),
                    cancellation_token)
                .ConfigureAwait(false);
        }
        InventoryPetPage inventory = await InventoryApplicationPages.CompletePetsAsync(
            Application,
            first,
            cancellation_token: cancellation_token).ConfigureAwait(false);
        return Array.AsReadOnly(inventory.Pets.Select(LegacyInventoryPet).ToArray());
    }

    /// <summary>
    /// Gets the friend list, requesting it and waiting for the full load if needed.
    /// </summary>
    /// <remarks>
    /// Call it before relying on <see cref="IsFriend(string)"/> or <see cref="FindFriend"/>. A
    /// loaded list returns without touching the network.
    /// </remarks>
    /// <param name="timeout_ms">The timeout for the load, in milliseconds. Must be positive.</param>
    /// <param name="cancellation_token">An extra token to cancel on, combined with the script's own.</param>
    /// <returns>A snapshot of every friend.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout_ms"/> is zero or negative.</exception>
    /// <exception cref="TimeoutException">Thrown when the friend list did not finish loading in time.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the session changed or the list kept changing while it was being read.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown when the script was stopped, or the supplied token fired.</exception>
    public async Task<IReadOnlyCollection<Friend>> EnsureFriendsLoaded(
        int timeout_ms = 10000,
        CancellationToken cancellation_token = default)
    {
        if (cancellation_token == default)
            return await LoadFriends(timeout_ms, Ct);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(Ct, cancellation_token);
        return await LoadFriends(timeout_ms, linked.Token);
    }

    /// <summary>Writes a line to the script output.</summary>
    /// <remarks>Alias of <see cref="Log"/>.</remarks>
    /// <param name="message">The value to write; <see langword="null"/> writes an empty line.</param>
    public void Status(object? message) => Log(message);

    /// <summary>
    /// Ends the script immediately and successfully, by throwing
    /// <see cref="ScriptFinishedException"/>.
    /// </summary>
    /// <remarks>
    /// The host treats that as a normal finish, but a <c>catch (Exception)</c> in the script will
    /// swallow it.
    /// </remarks>
    /// <exception cref="ScriptFinishedException">Thrown on every call.</exception>
    public void Finish() => throw new ScriptFinishedException();

    /// <summary>
    /// Stores a value in the process-wide store that outlives a single script run and is shared
    /// by every script and tab.
    /// </summary>
    /// <remarks>Use it to pass state between runs.</remarks>
    /// <param name="key">The key, compared case-sensitively.</param>
    /// <param name="value">The value; <see langword="null"/> is stored as a real null entry.</param>
    public void SetGlobal(string key, object? value)
    {
        lock (_global_sync)
            _globals[key] = value;
    }

    /// <summary>
    /// Reads a value from the shared store.
    /// </summary>
    /// <param name="key">The key, compared case-sensitively.</param>
    /// <returns>
    /// The stored value, or <see langword="null"/> when the key is absent, which is
    /// indistinguishable from a stored null.
    /// </returns>
    public object? GetGlobal(string key) => _globals.GetValueOrDefault(key);

    /// <summary>
    /// Reads a value from the shared store and casts it.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="key">The key, compared case-sensitively.</param>
    /// <returns>
    /// The value, or <c>default</c> when the key is absent or the stored value is of another
    /// type. A type mismatch is not reported.
    /// </returns>
    public T? GetGlobal<T>(string key) => _globals.TryGetValue(key, out object? value) && value is T typed ? typed : default;

    /// <summary>
    /// Serializes a value to JSON with the default options: no indentation, property names kept
    /// exactly as declared.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON text, or <c>"null"</c> for <see langword="null"/>.</returns>
    public static string ToJson(object? value) => JsonSerializer.Serialize(value);

    /// <summary>
    /// Deserializes JSON into <typeparamref name="T"/> with the default options.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="json">The JSON text.</param>
    /// <returns>The deserialized value, or <c>default</c> when the JSON is the literal <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or does not fit <typeparamref name="T"/>.</exception>
    public static T? FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json);

    /// <summary>
    /// Enters a room with no password.
    /// </summary>
    /// <remarks>
    /// Fire-and-forget: the room may still refuse entry (locked door, ban, full room). Subscribe
    /// to <see cref="OnRoomReady"/> to know when the entry succeeded.
    /// </remarks>
    /// <param name="roomId">The room id.</param>
    /// <exception cref="InvalidOperationException">Thrown when no hotel session is active.</exception>
    public void EnterRoom(Id roomId) => EnterRoom(roomId, "");

    /// <summary>
    /// Enters a room, supplying a door password.
    /// </summary>
    /// <param name="room_id">The room id.</param>
    /// <param name="password">The door password; empty for rooms that need none.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no hotel session is active.</exception>
    /// <remarks>
    /// Fire-and-forget. A wrong password or a refused entry is not reported by an exception
    /// here; subscribe to <see cref="OnRoomReady"/> to know when the entry succeeded.
    /// </remarks>
    public void EnterRoom(Id room_id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        Application.Invoke<RoomEnterRequest, RoomLifecycleDispatchResult>(
            ApplicationMemberIds.RoomEnter,
            new RoomEnterRequest(room_id, password),
            Ct);
    }

    /// <summary>
    /// Gets the straight-line (Euclidean) distance between two tiles, in tiles.
    /// </summary>
    /// <remarks>
    /// Avatars walk diagonally, so it is not the number of steps between them.
    /// </remarks>
    /// <param name="x1">The x coordinate of the first tile.</param>
    /// <param name="y1">The y coordinate of the first tile.</param>
    /// <param name="x2">The x coordinate of the second tile.</param>
    /// <param name="y2">The y coordinate of the second tile.</param>
    public static double Distance(int x1, int y1, int x2, int y2)
    {
        double dx = (double)x1 - x2;
        double dy = (double)y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Gets the straight-line distance between two tiles, in tiles, ignoring height.</summary>
    /// <param name="a">The first tile.</param>
    /// <param name="b">The second tile.</param>
    public static double Distance(Tile a, Tile b) => Distance(a.X, a.Y, b.X, b.Y);

    /// <summary>
    /// Gets the straight-line distance between two avatars' current tiles, in tiles.
    /// </summary>
    /// <remarks>Height is ignored.</remarks>
    /// <param name="a">The first avatar.</param>
    /// <param name="b">The second avatar.</param>
    public double Distance(Avatar a, Avatar b) => Distance(a.X, a.Y, b.X, b.Y);
}
