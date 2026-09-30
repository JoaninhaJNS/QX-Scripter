# Command line

The command line edition runs the QX Scripter runtime without a window. It connects to G-Earth the
same way the desktop app does.

```
QX [shell] [-p <port>] [-q] [--script <file>] [--headless] [--packets]
```

| Option | Description |
| --- | --- |
| `-p <port>` | Connects to the G-Earth instance on this port. Without it, local ports are searched. |
| `--script <file>` | Runs a script once after connecting. |
| `--headless` | Runs only the host and the startup script, without a shell. |
| `--packets` | Logs packets in the shell. |
| `-q` | Turns packet logging off in every mode. |

## Shell

An interactive terminal opens the shell. It keeps one connection and one room cache for every
command.

| Command | Description |
| --- | --- |
| `run <file>` | Runs a script file. |
| `rerun` | Runs the last file again. |
| `eval <code>` | Runs one line of code. |
| `check <file>` | Compiles a script without running it. |
| `scripts` | Lists the scripts in the library. |
| `status` | Shows the G-Earth connection, the hotel session and the room. |
| `stop` | Stops the running script. |
| `wait` | Waits until the running script ends. |
| `exit`, `quit` | Closes the shell. |

## Automation

`QX app` takes JSON commands for other programs.

```
QX app list
QX app describe <id>
QX app invoke <id> <json>
QX app watch <id>
QX app session
```

`list` prints every application member, `describe` one of them with its parameters, `invoke` calls
one and `watch` streams an event. Each of these starts a new runtime.

`app session` keeps one runtime and reads one JSON request per line. Responses and script events carry
the request id:

```json
{"id":"1","method":"status"}
{"id":"2","method":"run_script","file":"my-script.csx"}
{"id":"3","method":"run_code","code":"Log(RoomId);"}
{"id":"4","method":"compile_check","code":"Log(RoomId);"}
{"id":"5","method":"cancel_request","request_id":"2"}
{"id":"6","method":"close"}
```

Other methods: `health`, `list`, `describe`, `invoke`, `scripts`, `subscribe` and `unsubscribe`.

## Panels

The command line has no panels. A script that declares `//@ui:required` is refused, and
`Ui.Confirm` and `Ui.Prompt` answer `false` and `null` at once.
