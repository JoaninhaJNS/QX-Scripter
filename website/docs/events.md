# Events

Event methods take a handler. Most return an <xref:System.IDisposable>; dispose it to remove the
handler. Every handler is removed when the script stops, so a script usually keeps none of them.

An exception thrown in a handler stops the script and shows up in its output as an error.

## Room

```csharp
OnEnteredRoom(() => Log($"entered room {RoomId}"));
OnLeftRoom(() => Log("left the room"));
OnRoomReady(() => Log("room loaded"));
```

## Avatars

```csharp
OnAvatarAdded(avatar => Log($"{avatar.Name} entered"));
OnAvatarRemoved(avatar => Log($"{avatar.Name} left"));
OnChat((avatar, chat) => Log($"{avatar?.Name}: {chat.Message}"));
```

`OnAvatarDanceChanged`, `OnAvatarEffectChanged`, `OnAvatarHandItemChanged`,
`OnAvatarIdleChanged` and `OnAvatarTypingChanged` report status changes.

## Furni

```csharp
OnFloorItemAdded(item => Log($"placed {FurniName(item)} at {item.Location}"));
OnFloorItemUpdated(item => Log($"{item.Id} is now in state {item.State}"));
OnFloorItemRemoved(id => Log($"removed {id}"));
OnWallItemAdded(item => Log($"hung {FurniName(item)}"));
```

`OnFloorItemAdded` and `OnWallItemAdded` report furni placed while the script runs. The furni a room
loads with arrive in one batch through `OnFloorItemsLoaded` and `OnWallItemsLoaded`. The avatars a room
loads with do call `OnAvatarAdded`, once for each.

## Everything else

There are events for the inventory, friends, private messages, trades, achievements, quests,
forums, the marketplace, crafting, polls and Wired. They all start with `On`; the
<xref:Qx.Scripting.ScriptGlobals> page lists every one.

```csharp
OnPrivateMessage((sender, text) => Log($"{sender}: {text}"));
OnFriendRequest(request => Log($"friend request from {request.RequesterName}"));
OnTradeOpened(() => Log("trade opened"));
```

## Remove a handler

```csharp
IDisposable chat = OnChat(chat => Log(chat.Message));
await Delay(60000);
chat.Dispose();
```

## Packets

`OnIn` and `OnOut` intercept raw or parsed packets. See [Packets](packets.md).
