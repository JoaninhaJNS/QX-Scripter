# Writing scripts

A script is C# with top-level statements. The members of <xref:Qx.Scripting.ScriptGlobals> are in
scope without a prefix, and these namespaces are imported:

`System`, `System.Collections.Generic`, `System.Collections.Concurrent`, `System.Globalization`,
`System.Linq`, `System.Text`, `System.Text.RegularExpressions`, `System.Threading`,
`System.Threading.Tasks`, `Qx`, `Qx.Messages`, `Qx.Interception`, `Qx.Game`, `Qx.Game.Snapshots`,
`Qx.Model`, `Qx.Model.Bots`, `Qx.Model.Crafting`, `Qx.Model.Figures`, `Qx.Model.Forums`,
`Qx.Model.Marketplace`, `Qx.Model.Messages.Incoming`, `Qx.Model.Messages.Outgoing`,
`Qx.Model.Polls`, `Qx.Model.Quests`, `Qx.Model.Subscriptions`, `Qx.Model.Wired`, `Qx.Platform` and
`Qx.Scripting`.

## Output

`Log` writes a line to the tab's output console. `Status` does the same.

```csharp
Log("started");
Log(RoomId);
```

## Waiting

`Delay` waits without blocking and stops waiting when the script is stopped.

```csharp
Talk("one");
await Delay(1000);
Talk("two");
```

## Loops

`Run` is `true` until the script is stopped. A loop that checks it ends cleanly:

```csharp
while (Run)
{
    Talk("still here");
    await Delay(5000);
}
```

Every `while`, `for`, `foreach` and `do` loop in a script also checks for a stop on each pass, so a
loop that forgets `Run` still ends when **Stop** is pressed.

## Ending a script

A script ends after its last statement. `Finish()` ends it early and counts as a normal end.

```csharp
if (SelfAvatar is null)
    Finish();
```

`await Wait()` keeps a script alive until it is stopped, for scripts that only react to events.

## Cancellation

`Ct` is the script's cancellation token. `Delay`, `Wait` and the request methods use it and throw
<xref:System.OperationCanceledException> when the script is stopped. Put cleanup in `finally`:

```csharp
try
{
    while (Run)
    {
        UseFurni(QueryFloorItems().Named("dice").First());
        await Delay(2000);
    }
}
finally
{
    Log("stopped");
}
```

## Background work

`RunTask` starts work that runs next to the script. An exception in it faults the script, and it is
canceled when the script stops.

```csharp
RunTask(async () =>
{
    while (Run)
    {
        Log($"{Users.Count()} users");
        await Delay(10000);
    }
});
```

## Shared code

`#load` compiles another script into this one. See [Shared code](shared-code.md).
