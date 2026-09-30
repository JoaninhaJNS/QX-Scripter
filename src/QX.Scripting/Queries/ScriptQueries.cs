using Qx.Game;
using Qx.Game.Application;
using Qx.Model;
using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

/// <summary>
/// Provides snapshot queries over the current game state.
/// </summary>
/// <remarks>
/// Each property reads the state at the moment it is accessed and returns a new query. The
/// <c>From</c> methods wrap other sequences and attach the current furni data where the query uses it.
/// </remarks>
public sealed class ScriptQueries
{
    private readonly GameState _game;
    private readonly IApplicationRuntime _application;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptQueries"/> class.
    /// </summary>
    /// <param name="game">The game state to read.</param>
    /// <param name="application">The application runtime that the inventory queries read from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> or <paramref name="application"/> is <see langword="null"/>.</exception>
    public ScriptQueries(GameState game, IApplicationRuntime application)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(application);
        _game = game;
        _application = application;
    }

    /// <summary>
    /// Gets a query over copies of the users, pets and bots in the current room.
    /// </summary>
    public AvatarQuery Avatars =>
        _game.Room.Capture(room => new AvatarQuery(room.Avatars));

    /// <summary>
    /// Gets a query over copies of the floor items in the current room, with the current furni data attached.
    /// </summary>
    public FloorItemQuery FloorItems =>
        _game.Room.Capture(room => new FloorItemQuery(room.FloorItems, _game.GameData.Furni));

    /// <summary>
    /// Gets a query over copies of the wall items in the current room, with the current furni data attached.
    /// </summary>
    public WallItemQuery WallItems =>
        _game.Room.Capture(room => new WallItemQuery(room.WallItems, _game.GameData.Furni));

    /// <summary>
    /// Gets a query over the furni in the inventory, with the current furni data attached.
    /// </summary>
    /// <remarks>
    /// The query is empty until the inventory has been loaded.
    /// </remarks>
    public InventoryItemQuery InventoryItems =>
        new(ScriptGlobals.ReadInventoryItems(_application), _game.GameData.Furni);

    /// <summary>
    /// Gets a query over the pets in the inventory.
    /// </summary>
    /// <remarks>
    /// The query is empty until the pet inventory has been loaded.
    /// </remarks>
    public InventoryPetQuery InventoryPets =>
        new(ScriptGlobals.ReadInventoryPetModels(_application));

    /// <summary>
    /// Gets a query over the badges the user owns.
    /// </summary>
    public BadgeQuery OwnedBadges =>
        new(_game.Badges.OwnedBadges);

    /// <summary>
    /// Gets a query over the friend list.
    /// </summary>
    public FriendQuery Friends =>
        new(_game.Friends.Friends);

    /// <summary>
    /// Gets a query over the data of the current room.
    /// </summary>
    /// <remarks>
    /// The query holds a single room, or is empty when no room data is available.
    /// </remarks>
    public RoomDataQuery CurrentRoom =>
        new(_game.Room.Data is { } room ? [room] : []);

    /// <summary>
    /// Gets a query over the user's achievements.
    /// </summary>
    public AchievementQuery Achievements =>
        new(_game.Achievements.All);

    /// <summary>
    /// Creates an avatar query over copies of the specified avatars.
    /// </summary>
    /// <param name="avatars">The avatars to query.</param>
    /// <returns>A new query over the avatars.</returns>
    public AvatarQuery From(IEnumerable<Avatar> avatars) =>
        new(avatars);

    /// <summary>
    /// Creates a floor item query over copies of the specified items, with the current furni data attached.
    /// </summary>
    /// <param name="items">The floor items to query.</param>
    /// <returns>A new query over the items.</returns>
    public FloorItemQuery From(IEnumerable<FloorItem> items) =>
        new(items, _game.GameData.Furni);

    /// <summary>
    /// Creates a wall item query over copies of the specified items, with the current furni data attached.
    /// </summary>
    /// <param name="items">The wall items to query.</param>
    /// <returns>A new query over the items.</returns>
    public WallItemQuery From(IEnumerable<WallItem> items) =>
        new(items, _game.GameData.Furni);

    /// <summary>
    /// Creates an inventory item query over the specified items, with the current furni data attached.
    /// </summary>
    /// <param name="items">The inventory items to query.</param>
    /// <returns>A new query over the items.</returns>
    public InventoryItemQuery From(IEnumerable<InventoryItem> items) =>
        new(items, _game.GameData.Furni);

    /// <summary>
    /// Creates an inventory pet query over the specified pets.
    /// </summary>
    /// <param name="pets">The inventory pets to query.</param>
    /// <returns>A new query over the pets.</returns>
    public InventoryPetQuery From(IEnumerable<InventoryPet> pets) =>
        new(pets);

    /// <summary>
    /// Creates a badge query over the specified owned badges.
    /// </summary>
    /// <param name="badges">The owned badges to query.</param>
    /// <returns>A new query over the badges.</returns>
    public BadgeQuery From(IEnumerable<OwnedBadge> badges) =>
        new(badges);

    /// <summary>
    /// Creates a selected badge query over the specified badges.
    /// </summary>
    /// <param name="badges">The selected badges to query.</param>
    /// <returns>A new query over the badges.</returns>
    public SelectedBadgeQuery From(IEnumerable<SelectedBadge> badges) =>
        new(badges);

    /// <summary>
    /// Creates a friend query over the specified friends.
    /// </summary>
    /// <param name="friends">The friends to query.</param>
    /// <returns>A new query over the friends.</returns>
    public FriendQuery From(IEnumerable<Friend> friends) =>
        new(friends);

    /// <summary>
    /// Creates a room data query over the specified rooms.
    /// </summary>
    /// <param name="rooms">The room data to query.</param>
    /// <returns>A new query over the rooms.</returns>
    public RoomDataQuery From(IEnumerable<RoomData> rooms) =>
        new(rooms);

    /// <summary>
    /// Creates an achievement query over the specified achievements.
    /// </summary>
    /// <param name="achievements">The achievements to query.</param>
    /// <returns>A new query over the achievements.</returns>
    public AchievementQuery From(IEnumerable<Achievement> achievements) =>
        new(achievements);
}

/// <summary>
/// Provides extension methods that create queries from game model sequences.
/// </summary>
public static class GameQueryExtensions
{
    /// <summary>
    /// Creates an avatar query over copies of the avatars.
    /// </summary>
    /// <param name="avatars">The avatars to query.</param>
    /// <returns>A new query over the avatars.</returns>
    public static AvatarQuery Query(this IEnumerable<Avatar> avatars) =>
        new(avatars);

    /// <summary>
    /// Creates a floor item query over copies of the items, with the furni data of the specified game state attached.
    /// </summary>
    /// <param name="items">The floor items to query.</param>
    /// <param name="game">The game state that supplies the furni data.</param>
    /// <returns>A new query over the items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> or <paramref name="game"/> is <see langword="null"/>.</exception>
    public static FloorItemQuery Query(this IEnumerable<FloorItem> items, GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return new FloorItemQuery(items, game.GameData.Furni);
    }

    /// <summary>
    /// Creates a wall item query over copies of the items, with the furni data of the specified game state attached.
    /// </summary>
    /// <param name="items">The wall items to query.</param>
    /// <param name="game">The game state that supplies the furni data.</param>
    /// <returns>A new query over the items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> or <paramref name="game"/> is <see langword="null"/>.</exception>
    public static WallItemQuery Query(this IEnumerable<WallItem> items, GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return new WallItemQuery(items, game.GameData.Furni);
    }

    /// <summary>
    /// Creates an inventory item query over the items, with the furni data of the specified game state attached.
    /// </summary>
    /// <param name="items">The inventory items to query.</param>
    /// <param name="game">The game state that supplies the furni data.</param>
    /// <returns>A new query over the items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> or <paramref name="game"/> is <see langword="null"/>.</exception>
    public static InventoryItemQuery Query(this IEnumerable<InventoryItem> items, GameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        return new InventoryItemQuery(items, game.GameData.Furni);
    }

    /// <summary>
    /// Creates an inventory pet query over the pets.
    /// </summary>
    /// <param name="pets">The inventory pets to query.</param>
    /// <returns>A new query over the pets.</returns>
    public static InventoryPetQuery Query(this IEnumerable<InventoryPet> pets) =>
        new(pets);

    /// <summary>
    /// Creates a badge query over the owned badges.
    /// </summary>
    /// <param name="badges">The owned badges to query.</param>
    /// <returns>A new query over the badges.</returns>
    public static BadgeQuery Query(this IEnumerable<OwnedBadge> badges) =>
        new(badges);

    /// <summary>
    /// Creates a selected badge query over the badges.
    /// </summary>
    /// <param name="badges">The selected badges to query.</param>
    /// <returns>A new query over the badges.</returns>
    public static SelectedBadgeQuery Query(this IEnumerable<SelectedBadge> badges) =>
        new(badges);

    /// <summary>
    /// Creates a friend query over the friends.
    /// </summary>
    /// <param name="friends">The friends to query.</param>
    /// <returns>A new query over the friends.</returns>
    public static FriendQuery Query(this IEnumerable<Friend> friends) =>
        new(friends);

    /// <summary>
    /// Creates a room data query over the rooms.
    /// </summary>
    /// <param name="rooms">The room data to query.</param>
    /// <returns>A new query over the rooms.</returns>
    public static RoomDataQuery Query(this IEnumerable<RoomData> rooms) =>
        new(rooms);

    /// <summary>
    /// Creates an achievement query over the achievements.
    /// </summary>
    /// <param name="achievements">The achievements to query.</param>
    /// <returns>A new query over the achievements.</returns>
    public static AchievementQuery Query(this IEnumerable<Achievement> achievements) =>
        new(achievements);
}
