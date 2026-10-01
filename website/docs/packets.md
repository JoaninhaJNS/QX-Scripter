# Packets

Scripts can send, intercept, block and wait for packets. Messages are named with the Flash client's
message names, such as `MoveAvatar` or `ObjectUpdate`.

## Send a packet

`SendToServer` and `SendToClient` send a message by name with its values in order:

```csharp
SendToServer("MoveAvatar", 5, 8);
SendToServer("ClickFurni", 874336973, 0);
```

`Out["Name"]` and `In["Name"]` resolve a name to its header for the current session:

```csharp
Send(Out["MoveAvatar"], 5, 8);
```

## Intercept a packet

`OnOut` and `OnIn` run a handler for every matching packet. The handler receives an
<xref:Qx.Interception.Intercept>:

```csharp
OnOut("MoveAvatar", e =>
{
    var reader = e.Packet.Reader();
    Log($"walking to {reader.ReadInt()}, {reader.ReadInt()}");
});
```

## Intercept a parsed message

The generic overloads parse the packet into a message type:

```csharp
OnOut<WalkRequest>("MoveAvatar", walk => Log($"walking to {walk.X}, {walk.Y}"));
OnIn<FloorItemUpdate>("ObjectUpdate", update => Log($"item {update.Item.Id} updated"));
```

## Block a packet

```csharp
OnOut<WalkRequest>("MoveAvatar", (walk, e) =>
{
    e.Block();
    Log($"blocked a walk to {walk.X}, {walk.Y}");
});
```

## Wait for a packet

`ReceiveAsync` waits for the next matching packet and returns it. A parsed overload returns the
message:

```csharp
Walk(5, 8);
IPacket update = await ReceiveAsync("UserUpdate", timeoutMs: 5000);
UserUpdate parsed = await ReceiveAsync<UserUpdate>("UserUpdate");
```

`ReceiveAsync` and `Receive` watch both directions, so pass the message name only. A name with an
`in:` or `out:` prefix throws an `ArgumentException`.

The returned packet is a copy that belongs to the script.
