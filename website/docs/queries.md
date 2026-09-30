# Queries

Queries filter and sort game state. Every query method returns a new query, so calls chain, and a
query is also a normal collection that works with LINQ.

| Method | Returns |
| --- | --- |
| `QueryAvatars()` | <xref:Qx.Scripting.AvatarQuery> |
| `QueryFloorItems()` | <xref:Qx.Scripting.FloorItemQuery> |
| `QueryWallItems()` | <xref:Qx.Scripting.WallItemQuery> |
| `QueryInventoryItems()` | <xref:Qx.Scripting.InventoryItemQuery> |
| `QueryInventoryPets()` | <xref:Qx.Scripting.InventoryPetQuery> |
| `QueryFriends()` | <xref:Qx.Scripting.FriendQuery> |
| `QueryAchievements()` | <xref:Qx.Scripting.AchievementQuery> |
| `QueryGuildMembers(members)` | <xref:Qx.Scripting.GuildMemberQuery> |
| `QueryCurrentRoom()`, `QueryRooms(rooms)` | <xref:Qx.Scripting.RoomDataQuery> |

## Filter furni

```csharp
var chairs = QueryFloorItems()
    .Named("chair")
    .Inside(new Area(1, 1, 5, 5))
    .OrderByDistanceTo(new Point(3, 3))
    .ToArray();
```

## Find the nearest

```csharp
FloorItem? dice = QueryFloorItems()
    .OfIdentifier("edice")
    .NearestTo(SelfAvatar!.XY);
```

## Filter avatars

```csharp
foreach (var avatar in QueryAvatars().WithinDistance(SelfAvatar!.XY, 3))
    Log(avatar.Name);
```

## Search rooms

`SearchRoomQuery` runs a navigator search and returns the rooms as a query. The code `"query"`
takes free text and the navigator prefixes `owner:`, `roomname:`, `tag:` and `group:`:

```csharp
var rooms = await SearchRoomQuery("query", "tag:maze");
foreach (var room in rooms.Take(5))
    Log($"{room.Name} by {room.OwnerName}");
```
