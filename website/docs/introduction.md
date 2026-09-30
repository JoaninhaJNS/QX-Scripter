# Introduction

QX Scripter is a G-Earth extension for the Flash client. It keeps a live copy of the game state of
the connected session and runs C# scripts against it.

## Parts

### Desktop app

The desktop app is the editor. It has a script library, tabs with code completion, a panel view for
scripts that declare a user interface, and an output console for every tab.

### Command line

The command line runs the same runtime without a window. It can run a script file, keep an
interactive shell open, or take JSON requests from another program. See [Command line](cli.md).

### MCP server

The MCP server lets an AI client read the game state, write and run scripts and edit the open tabs.
See [MCP](mcp.md).

## Scripts

A script is a C# script file (`.csx`) with top-level statements. Every public member of
<xref:Qx.Scripting.ScriptGlobals> is in scope, so a script reads and acts on the game directly:

```csharp
Log($"{Users.Count()} users in room {RoomId}");
Talk("hello");
```

Scripts are saved in the script library:

| System | Folder |
| --- | --- |
| Windows | `%APPDATA%\QX Scripter\scripts` |
| macOS | `~/Library/Application Support/QX Scripter/scripts` |
| Linux | `$XDG_CONFIG_HOME/QX Scripter/scripts`, or `~/.config/QX Scripter/scripts` |

See [Getting started](getting-started.md) to run a first script.
