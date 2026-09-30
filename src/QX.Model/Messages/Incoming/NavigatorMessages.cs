using Qx.Messages;

namespace Qx.Model.Messages.Incoming;

/// <summary>Represents a named search the navigator offers in its left pane.</summary>
/// <remarks>
/// The same shape serves both the quick links under a category and the searches the user saved,
/// which is why the identifier is only meaningful for the saved ones.
/// </remarks>
/// <param name="Id">The saved search's identifier; zero for a quick link.</param>
/// <param name="SearchCode">Which view the search belongs to, for example <c>hotel_view</c>.</param>
/// <param name="Filter">The filter text, in the navigator's own prefix syntax.</param>
/// <param name="Localization">The text key for the label the client shows.</param>
public sealed record NavigatorSearch(
    int Id,
    string SearchCode,
    string Filter,
    string Localization) : IParserComposer<NavigatorSearch>
{
    /// <summary>Parses the search from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorSearch Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorSearch ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadString(), p.ReadString(), p.ReadString());

    /// <summary>Composes the search into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorSearch value, in PacketWriter p)
    {
        p.WriteInt(value.Id);
        p.WriteString(value.SearchCode);
        p.WriteString(value.Filter);
        p.WriteString(value.Localization);
    }
}

/// <summary>Represents a navigator category with the searches offered under it.</summary>
/// <param name="SearchCode">The category's code.</param>
/// <param name="QuickLinks">The searches the hotel offers under it.</param>
public sealed record NavigatorCategory(string SearchCode, IReadOnlyList<NavigatorSearch> QuickLinks)
    : IParserComposer<NavigatorCategory>
{
    /// <summary>Gets the view mode of the category.</summary>
    /// <remarks>The Flash parser does not read it and composing does not write it, so it is 0 for a parsed category.</remarks>
    public int ViewMode { get; init; }

    /// <summary>Parses the category from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorCategory Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorCategory ParseFlash(in PacketReader p)
    {
        string search_code = p.ReadString();
        return ParseLinks(in p, search_code, p.ReadInt());
    }

    private static NavigatorCategory ParseLinks(in PacketReader p, string search_code, int count)
    {
        var links = new NavigatorSearch[count];
        for (int i = 0; i < count; i++)
            links[i] = p.Parse<NavigatorSearch>();
        return new NavigatorCategory(search_code, links);
    }

    /// <summary>Composes the category into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorCategory value, in PacketWriter p)
    {
        p.WriteString(value.SearchCode);
        p.WriteInt(value.QuickLinks.Count);
        foreach (NavigatorSearch link in value.QuickLinks)
            p.Compose(link);
    }
}

/// <summary>Represents the <c>NavigatorMetaData</c> message, received with every navigator category the hotel publishes and the searches under each.</summary>
/// <remarks>
/// This is what makes a search code valid. A search sent with a code the hotel does not list here
/// comes back empty rather than refused, so the categories are worth reading before searching.
/// </remarks>
/// <param name="Categories">The categories.</param>
public sealed record NavigatorMetaData(IReadOnlyList<NavigatorCategory> Categories)
    : IParserComposer<NavigatorMetaData>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorMetaData Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorMetaData ParseFlash(in PacketReader p) =>
        ParseCategories(in p, p.ReadInt());

    private static NavigatorMetaData ParseCategories(in PacketReader p, int count)
    {
        var categories = new NavigatorCategory[count];
        for (int i = 0; i < count; i++)
            categories[i] = p.Parse<NavigatorCategory>();
        return new NavigatorMetaData(categories);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorMetaData value, in PacketWriter p)
    {
        p.WriteInt(value.Categories.Count);
        foreach (NavigatorCategory category in value.Categories)
            p.Compose(category);
    }
}

/// <summary>Represents a room the hotel is promoting, shown as a tile rather than a list row.</summary>
/// <param name="RoomId">The ID of the room.</param>
/// <param name="AreaId">The ID of the promoted area the room belongs to.</param>
/// <param name="Image">The tile's image reference.</param>
/// <param name="Caption">The tile's caption.</param>
public sealed record NavigatorLiftedRoom(int RoomId, int AreaId, string Image, string Caption)
    : IParserComposer<NavigatorLiftedRoom>
{
    /// <summary>Parses the room from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorLiftedRoom Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorLiftedRoom ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt(), p.ReadString(), p.ReadString());

    /// <summary>Composes the room into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorLiftedRoom value, in PacketWriter p)
    {
        p.WriteInt(value.RoomId);
        p.WriteInt(value.AreaId);
        p.WriteString(value.Image);
        p.WriteString(value.Caption);
    }
}

/// <summary>Represents the <c>NavigatorLiftedRooms</c> message, received with the rooms the hotel is currently promoting.</summary>
/// <param name="Rooms">The promoted rooms.</param>
public sealed record NavigatorLiftedRooms(IReadOnlyList<NavigatorLiftedRoom> Rooms)
    : IParserComposer<NavigatorLiftedRooms>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorLiftedRooms Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorLiftedRooms ParseFlash(in PacketReader p) =>
        ParseRooms(in p, p.ReadInt());

    private static NavigatorLiftedRooms ParseRooms(in PacketReader p, int count)
    {
        var rooms = new NavigatorLiftedRoom[count];
        for (int i = 0; i < count; i++)
            rooms[i] = p.Parse<NavigatorLiftedRoom>();
        return new NavigatorLiftedRooms(rooms);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorLiftedRooms value, in PacketWriter p)
    {
        p.WriteInt(value.Rooms.Count);
        foreach (NavigatorLiftedRoom room in value.Rooms)
            p.Compose(room);
    }
}

/// <summary>Represents the <c>NavigatorSavedSearches</c> message, received with the searches the user has saved.</summary>
/// <param name="Searches">The saved searches.</param>
public sealed record NavigatorSavedSearches(IReadOnlyList<NavigatorSearch> Searches)
    : IParserComposer<NavigatorSavedSearches>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorSavedSearches Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorSavedSearches ParseFlash(in PacketReader p) =>
        ParseSearches(in p, p.ReadInt());

    private static NavigatorSavedSearches ParseSearches(in PacketReader p, int count)
    {
        var searches = new NavigatorSearch[count];
        for (int i = 0; i < count; i++)
            searches[i] = p.Parse<NavigatorSearch>();
        return new NavigatorSavedSearches(searches);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorSavedSearches value, in PacketWriter p)
    {
        p.WriteInt(value.Searches.Count);
        foreach (NavigatorSearch search in value.Searches)
            p.Compose(search);
    }
}

/// <summary>Represents the <c>NavigatorSettings</c> message, received with the user's home room and the room the client should enter.</summary>
/// <param name="HomeRoomId">The ID of the user's home room, or 0 when none is set.</param>
/// <param name="RoomIdToEnter">The ID of the room the hotel tells the client to enter.</param>
public sealed record NavigatorSettings(Id HomeRoomId, Id RoomIdToEnter)
    : IParserComposer<NavigatorSettings>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorSettings Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorSettings ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorSettings value, in PacketWriter p)
    {
        p.WriteInt(checked((int)value.HomeRoomId));
        p.WriteInt(checked((int)value.RoomIdToEnter));
    }
}

/// <summary>Represents the <c>NewNavigatorPreferences</c> message, received with how the user has arranged the navigator window.</summary>
/// <param name="WindowX">The x position of the window.</param>
/// <param name="WindowY">The y position of the window.</param>
/// <param name="WindowWidth">The width of the window.</param>
/// <param name="WindowHeight">The height of the window.</param>
/// <param name="LeftPaneHidden">Whether the category pane is collapsed away.</param>
/// <param name="ResultsMode">The mode the results are drawn in, such as a list or thumbnails.</param>
public sealed record NewNavigatorPreferences(
    int WindowX,
    int WindowY,
    int WindowWidth,
    int WindowHeight,
    bool LeftPaneHidden,
    int ResultsMode) : IParserComposer<NewNavigatorPreferences>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NewNavigatorPreferences Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NewNavigatorPreferences ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadInt(), p.ReadInt(), p.ReadInt(), p.ReadBool(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NewNavigatorPreferences value, in PacketWriter p)
    {
        p.WriteInt(value.WindowX);
        p.WriteInt(value.WindowY);
        p.WriteInt(value.WindowWidth);
        p.WriteInt(value.WindowHeight);
        p.WriteBool(value.LeftPaneHidden);
        p.WriteInt(value.ResultsMode);
    }
}

/// <summary>Represents a room category a room owner can file their room under.</summary>
/// <param name="NodeId">The category's identifier, which is what a room's category field holds.</param>
/// <param name="Name">The category's name.</param>
/// <param name="Visible">Whether the category is shown at all.</param>
/// <param name="Automatic">
/// Whether the hotel assigns this category itself. An automatic category cannot be chosen by an
/// owner, so it is not a valid target for a room settings save.
/// </param>
/// <param name="AutomaticCategoryKey">The key behind an automatic category.</param>
/// <param name="GlobalCategoryKey">The hotel-wide key this category maps to.</param>
/// <param name="StaffOnly">Whether only staff may file a room here.</param>
public sealed record FlatCategory(
    int NodeId,
    string Name,
    bool Visible,
    bool Automatic,
    string AutomaticCategoryKey,
    string GlobalCategoryKey,
    bool StaffOnly) : IParserComposer<FlatCategory>
{
    /// <summary>Gets whether a room owner can file a room under this category.</summary>
    /// <remarks>True when the category is visible, not automatic and not staff only.</remarks>
    public bool IsSelectable => Visible && !Automatic && !StaffOnly;

    /// <summary>Parses the category from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FlatCategory Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FlatCategory ParseFlash(in PacketReader p) =>
        new(p.ReadInt(), p.ReadString(), p.ReadBool(), p.ReadBool(),
            p.ReadString(), p.ReadString(), p.ReadBool());

    /// <summary>Composes the category into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FlatCategory value, in PacketWriter p)
    {
        p.WriteInt(value.NodeId);
        p.WriteString(value.Name);
        p.WriteBool(value.Visible);
        p.WriteBool(value.Automatic);
        p.WriteString(value.AutomaticCategoryKey);
        p.WriteString(value.GlobalCategoryKey);
        p.WriteBool(value.StaffOnly);
    }
}

/// <summary>Represents the <c>UserFlatCats</c> message, received with the room categories the hotel publishes.</summary>
/// <param name="Categories">The categories.</param>
public sealed record UserFlatCats(IReadOnlyList<FlatCategory> Categories)
    : IParserComposer<UserFlatCats>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static UserFlatCats Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static UserFlatCats ParseFlash(in PacketReader p) =>
        ParseCategories(in p, p.ReadInt());

    private static UserFlatCats ParseCategories(in PacketReader p, int count)
    {
        var categories = new FlatCategory[count];
        for (int i = 0; i < count; i++)
            categories[i] = p.Parse<FlatCategory>();
        return new UserFlatCats(categories);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(UserFlatCats value, in PacketWriter p)
    {
        p.WriteInt(value.Categories.Count);
        foreach (FlatCategory category in value.Categories)
            p.Compose(category);
    }
}

/// <summary>Represents the <c>CollapsedCategories</c> message, received with the navigator categories the user has collapsed.</summary>
/// <param name="Categories">The collapsed category codes.</param>
public sealed record CollapsedCategories(IReadOnlyList<string> Categories)
    : IParserComposer<CollapsedCategories>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CollapsedCategories Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CollapsedCategories ParseFlash(in PacketReader p) =>
        ParseCategories(in p, p.ReadInt());

    private static CollapsedCategories ParseCategories(in PacketReader p, int count)
    {
        var categories = new string[count];
        for (int i = 0; i < count; i++)
            categories[i] = p.ReadString();
        return new CollapsedCategories(categories);
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CollapsedCategories value, in PacketWriter p)
    {
        p.WriteInt(value.Categories.Count);
        foreach (string category in value.Categories)
            p.WriteString(category);
    }
}
