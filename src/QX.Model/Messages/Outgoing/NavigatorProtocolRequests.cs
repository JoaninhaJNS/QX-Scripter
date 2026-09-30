using Qx.Messages;

namespace Qx.Model.Messages.Outgoing;

/// <summary>Requests the navigator's initial data, such as its views, saved searches and collapsed categories.</summary>
/// <remarks>Sent as the Flash <c>NewNavigatorInit</c> message, which carries no fields.</remarks>
public sealed record NavigatorMetadataRequest : IParserComposer<NavigatorMetadataRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorMetadataRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorMetadataRequest ParseFlash(in PacketReader p)
    {
        RequireEmpty(in p);
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorMetadataRequest value, in PacketWriter p) { }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException(
                $"{nameof(NavigatorMetadataRequest)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Requests the room categories that rooms can be filed under.</summary>
/// <remarks>Sent as the Flash <c>GetUserFlatCats</c> message, which carries no fields.</remarks>
public sealed record FlatCategoriesRequest : IParserComposer<FlatCategoriesRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static FlatCategoriesRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static FlatCategoriesRequest ParseFlash(in PacketReader p)
    {
        RequireEmpty(in p);
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(FlatCategoriesRequest value, in PacketWriter p) { }

    private static void RequireEmpty(in PacketReader p)
    {
        if (p.Available != 0)
            throw new InvalidDataException(
                $"{nameof(FlatCategoriesRequest)} contains {p.Available} unexpected bytes.");
    }
}

/// <summary>Requests a fixed navigator room list that takes no search input.</summary>
/// <remarks>Sent as one of the Flash <c>MyRoomsSearch</c>, <c>MyFavouriteRoomsSearch</c>, <c>MyRoomRightsSearch</c>, <c>MyRoomHistorySearch</c>, <c>MyFrequentRoomHistorySearch</c>, <c>MyFriendsRoomsSearch</c>, <c>RoomsWhereMyFriendsAreSearch</c> or <c>MyGuildBasesSearch</c> messages, which carry no fields.</remarks>
public sealed record NavigatorEmptySearchRequest : IParserComposer<NavigatorEmptySearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorEmptySearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorEmptySearchRequest ParseFlash(in PacketReader p)
    {
        NavigatorProtocolWire.RequireEmpty(in p, nameof(NavigatorEmptySearchRequest));
        return new();
    }

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorEmptySearchRequest value, in PacketWriter p) { }
}

/// <summary>Requests the results of a navigator view search.</summary>
/// <remarks>Sent as the Flash <c>NewNavigatorSearch</c> message. Composing throws when a string is <see langword="null"/> or exceeds 65535 bytes.</remarks>
/// <param name="SearchCode">The code of the view or category to search, such as <c>hotel_view</c>.</param>
/// <param name="Filter">The filter text in the navigator's prefix syntax, such as <c>owner:name</c>, or an empty string for no filter.</param>
public sealed record NavigatorViewSearchRequest(string SearchCode, string Filter)
    : IParserComposer<NavigatorViewSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorViewSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorViewSearchRequest ParseFlash(in PacketReader p) =>
        new(p.ReadString(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorViewSearchRequest value, in PacketWriter p)
    {
        NavigatorProtocolWire.ValidateString(value.SearchCode, nameof(SearchCode), in p);
        NavigatorProtocolWire.ValidateString(value.Filter, nameof(Filter), in p);
        p.WriteString(value.SearchCode);
        p.WriteString(value.Filter);
    }
}

/// <summary>Requests rooms that match a search text.</summary>
/// <remarks>Sent as the Flash <c>RoomTextSearch</c> message. Composing throws when a string is <see langword="null"/> or exceeds 65535 bytes.</remarks>
/// <param name="Text">The search text, optionally with a field prefix such as <c>owner:</c>, <c>roomname:</c>, <c>tag:</c> or <c>group:</c>.</param>
public sealed record NavigatorTextSearchRequest(string Text)
    : IParserComposer<NavigatorTextSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorTextSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorTextSearchRequest ParseFlash(in PacketReader p) =>
        new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorTextSearchRequest value, in PacketWriter p)
    {
        NavigatorProtocolWire.ValidateString(value.Text, nameof(Text), in p);
        p.WriteString(value.Text);
    }
}

/// <summary>Requests the popular rooms, optionally narrowed to one tag.</summary>
/// <remarks>Sent as the Flash <c>PopularRoomsSearch</c> message. Composing throws when a string is <see langword="null"/> or exceeds 65535 bytes.</remarks>
/// <param name="Tag">The room tag, or an empty string for the most popular rooms overall.</param>
/// <param name="AdIndex">The promoted room slot sent with the request, -1 or greater.</param>
public sealed record NavigatorTagSearchRequest(string Tag, int AdIndex)
    : IParserComposer<NavigatorTagSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorTagSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorTagSearchRequest ParseFlash(in PacketReader p) =>
        new(p.ReadString(), p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorTagSearchRequest value, in PacketWriter p)
    {
        NavigatorProtocolWire.ValidateString(value.Tag, nameof(Tag), in p);
        p.WriteString(value.Tag);
        p.WriteInt(value.AdIndex);
    }
}

/// <summary>Requests the highest scoring rooms or the group base rooms.</summary>
/// <remarks>Sent as the Flash <c>RoomsWithHighestScoreSearch</c> or <c>GuildBaseSearch</c> message, which share this layout.</remarks>
/// <param name="AdIndex">The promoted room slot sent with the request, -1 or greater.</param>
public sealed record NavigatorAdSearchRequest(int AdIndex)
    : IParserComposer<NavigatorAdSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static NavigatorAdSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static NavigatorAdSearchRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(NavigatorAdSearchRequest value, in PacketWriter p) =>
        p.WriteInt(value.AdIndex);
}

/// <summary>Sent when the user saves a navigator search.</summary>
/// <remarks>Sent as the Flash <c>NavigatorAddSavedSearch</c> message. Composing throws when a string is <see langword="null"/> or exceeds 65535 bytes.</remarks>
/// <param name="SearchCode">The code of the view the search belongs to, such as <c>hotel_view</c>.</param>
/// <param name="Filter">The filter text in the navigator's prefix syntax, or an empty string for no filter.</param>
public sealed record AddSavedSearchRequest(string SearchCode, string Filter)
    : IParserComposer<AddSavedSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AddSavedSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AddSavedSearchRequest ParseFlash(in PacketReader p) =>
        new(p.ReadString(), p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AddSavedSearchRequest value, in PacketWriter p)
    {
        ValidateStrings(value, in p);
        p.WriteString(value.SearchCode);
        p.WriteString(value.Filter);
    }

    private static void ValidateStrings(AddSavedSearchRequest value, in PacketWriter p)
    {
        ValidateString(value.SearchCode, nameof(SearchCode), in p);
        ValidateString(value.Filter, nameof(Filter), in p);
    }

    private static void ValidateString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        int length = p.Encoding.GetByteCount(value);
        if (length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"String byte length ({length}) exceeds {ushort.MaxValue}.",
                name);
        }
    }
}

/// <summary>Sent when the user deletes a saved navigator search.</summary>
/// <remarks>Sent as the Flash <c>NavigatorDeleteSavedSearch</c> message.</remarks>
/// <param name="SavedSearchId">The id of the saved search.</param>
public sealed record DeleteSavedSearchRequest(int SavedSearchId)
    : IParserComposer<DeleteSavedSearchRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DeleteSavedSearchRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DeleteSavedSearchRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DeleteSavedSearchRequest value, in PacketWriter p) =>
        p.WriteInt(value.SavedSearchId);
}

/// <summary>Sent when the user collapses a navigator category.</summary>
/// <remarks>Sent as the Flash <c>NavigatorAddCollapsedCategory</c> message.</remarks>
/// <param name="SearchCode">The code of the category.</param>
public sealed record AddCollapsedCategoryRequest(string SearchCode)
    : IParserComposer<AddCollapsedCategoryRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static AddCollapsedCategoryRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static AddCollapsedCategoryRequest ParseFlash(in PacketReader p) =>
        new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(AddCollapsedCategoryRequest value, in PacketWriter p) =>
        p.WriteString(value.SearchCode);
}

/// <summary>Sent when the user expands a collapsed navigator category.</summary>
/// <remarks>Sent as the Flash <c>NavigatorRemoveCollapsedCategory</c> message.</remarks>
/// <param name="SearchCode">The code of the category.</param>
public sealed record RemoveCollapsedCategoryRequest(string SearchCode)
    : IParserComposer<RemoveCollapsedCategoryRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static RemoveCollapsedCategoryRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static RemoveCollapsedCategoryRequest ParseFlash(in PacketReader p) =>
        new(p.ReadString());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(RemoveCollapsedCategoryRequest value, in PacketWriter p) =>
        p.WriteString(value.SearchCode);
}

/// <summary>Sent when the user sets the home room.</summary>
/// <remarks>Sent as the Flash <c>UpdateHomeRoom</c> message.</remarks>
/// <param name="RoomId">The id of the room, written as a 32 bit integer.</param>
public sealed record SetHomeRoomRequest(Id RoomId)
    : IParserComposer<SetHomeRoomRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static SetHomeRoomRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static SetHomeRoomRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(SetHomeRoomRequest value, in PacketWriter p)
    {
        int room_id = checked((int)value.RoomId);
        p.WriteInt(room_id);
    }
}

/// <summary>Sent when the user creates a room.</summary>
/// <remarks>Sent as the Flash <c>CreateFlat</c> message. Composing throws when a string is <see langword="null"/> or exceeds 65535 bytes.</remarks>
/// <param name="Name">The name of the room.</param>
/// <param name="Description">The description of the room.</param>
/// <param name="Model">The name of the floor plan model.</param>
/// <param name="Category">The id of the room category.</param>
/// <param name="MaximumVisitors">The maximum number of visitors.</param>
/// <param name="TradeMode">The trading mode of the room, as a <see cref="RoomTradeMode"/> value.</param>
public sealed record CreateRoomRequest(
    string Name,
    string Description,
    string Model,
    int Category,
    int MaximumVisitors,
    int TradeMode) : IParserComposer<CreateRoomRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static CreateRoomRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static CreateRoomRequest ParseFlash(in PacketReader p) =>
        new(
            p.ReadString(),
            p.ReadString(),
            p.ReadString(),
            p.ReadInt(),
            p.ReadInt(),
            p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(CreateRoomRequest value, in PacketWriter p)
    {
        ValidateStrings(value, in p);
        p.WriteString(value.Name);
        p.WriteString(value.Description);
        p.WriteString(value.Model);
        p.WriteInt(value.Category);
        p.WriteInt(value.MaximumVisitors);
        p.WriteInt(value.TradeMode);
    }

    private static void ValidateStrings(CreateRoomRequest value, in PacketWriter p)
    {
        ValidateString(value.Name, nameof(Name), in p);
        ValidateString(value.Description, nameof(Description), in p);
        ValidateString(value.Model, nameof(Model), in p);
    }

    private static void ValidateString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        int length = p.Encoding.GetByteCount(value);
        if (length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"String byte length ({length}) exceeds {ushort.MaxValue}.",
                name);
        }
    }
}

/// <summary>Sent when the user deletes an owned room.</summary>
/// <remarks>Sent as the Flash <c>DeleteRoom</c> message.</remarks>
/// <param name="RoomId">The id of the room, written as a 32 bit integer.</param>
public sealed record DeleteRoomRequest(Id RoomId)
    : IParserComposer<DeleteRoomRequest>
{
    /// <summary>Parses the message from a packet.</summary>
    /// <param name="p">The packet reader.</param>
    public static DeleteRoomRequest Parse(in PacketReader p) =>
        FlashWire.Parse(in p, ParseFlash);

    private static DeleteRoomRequest ParseFlash(in PacketReader p) =>
        new(p.ReadInt());

    /// <summary>Composes the message into a packet.</summary>
    /// <param name="p">The packet writer.</param>
    public void Compose(in PacketWriter p) =>
        FlashWire.Compose(this, in p, ComposeFlash);

    private static void ComposeFlash(DeleteRoomRequest value, in PacketWriter p)
    {
        int room_id = checked((int)value.RoomId);
        p.WriteInt(room_id);
    }
}

internal static class NavigatorProtocolWire
{
    public static void RequireEmpty(in PacketReader p, string message_name)
    {
        if (p.Available != 0)
        {
            throw new InvalidDataException(
                $"{message_name} contains {p.Available} unexpected bytes.");
        }
    }

    public static void ValidateString(string value, string name, in PacketWriter p)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        int length = p.Encoding.GetByteCount(value);
        if (length > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"String byte length ({length}) exceeds {ushort.MaxValue}.",
                name);
        }
    }
}
