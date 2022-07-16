<p align="center">
    <a href="#-quickstart">
      <img src="/Docs~/tilde_title.png">
    </a>
</p>

<p align="center">
    A remote developer console for Unity
</p>

---
[![](https://img.shields.io/github/license/grahamboree/tilde.svg)](https://github.com/grahamboree/tilde/blob/master/LICENSE.txt)

[[HTML5 DEMO]](https://grahamboree.github.io/tilde/)

# ⚡ Quickstart
1. In the package manager, select "Add package from git URL..." and add tilde using the url `https://github.com/grahamboree/tilde`
2. Add the `Prefabs/Tilde` prefab to your scene.
2. Add either the `Prefabs/Drawer Console` or `Prefabs/Windowed Console` prefab to your scene.
3. Start the game and press the tilde/backtick key to open the console.
4. Run `help` to list available commands.
5. Open `localhost:55055` in a web browser to access the remote console.

# 📸 Screenshots
<p align="center">
<img src="/Docs~/drawer.gif" alt="Drawer console prefab"> <img src="/Docs~/windowed.gif" alt="Windowed console prefab">
</p>

# ✨ Features
* Two styles of console prefabs: window (similar to popular MOBAs) and drawer (similar to classic FPS's).
* Remotely execute commands with a web-based console served by an embedded web server.
* Automatic command registration via function annotation.
* Automatic command history.  Cycle through previous commands with the up and down arrow keys.
* Tab autocomplete.
* Auto-generated command names, or specify them explicitly.
* Displays all Unity log, warning, error and exception messages colored as you'd expect.
* Bind keys to run commands with the `bind` and `unbind` commands.
* Supports commands with any number of arguments.
* `help` commmand and basic commmand documentation system.

# 📲 Remote Console
Tilde also provides an embedded web server and web-based console for executing commands remotely.  The default port is `55055`, but it can be specified in the `Tilde Web Console Server` component.  This enables you to remotely execute commands on mobile devices or consoles where bringing up a keyboard and typing a command is difficult.

❗️It's highly recommended that you disable the remote console in shipping versions of your game. To do so, simply remove the `Tilde Web Console Server` component.

# 🏗 Adding Commands
Console commands are public, static functions annotated with `[ConsoleCommand]`.  Annotated command functions optionally take an array of strings containing the arguments to that command, and also can optionally return a string to be printed to the console as output.
```cs
// Takes no arguments, no output
[ConsoleCommand] public static void Command1() { ... }

// Takes no arguments, prints some output
[ConsoleCommand] public static string Command2() { ... }

// Can take some arguments, no output
[ConsoleCommand] public static void Command3(string[] args) { ... }

// Can take some arguments, prints some output
[ConsoleCommand] public static string Command4(string[] args) { ... }
```

## Command Name
The name of a console command is generated from the annotated function's name. You can override this by specifying the `name` argument to the `ConsoleCommand` attribute.
```cs
// Registers the command "damageThePlayer"
[ConsoleCommand()]
public static void damageThePlayer(string[] args) { ... }

// Registeres the command "heal"
[ConsoleCommand(name: "heal")]
public static void HealThePlayer(string[] args) { ... }
```

## Doc String
Console commands can also have a documentation (doc) string associated with them to provide a brief explination of what the command does and how to use it. To add a doc string to a command, specify the `docs` argument to the `ConsoleCommand` attribute.

```cs
[ConsoleCommand(name: "heal", docs: "heal the player by the specified amount")]
public static void HealThePlayer(string[] args) { ... }
```

The `help` command shows all the registered commands along with the first line of their doc strings. `help <command>` shows the full command doc string for a specific command.

## Arguments
Commands can optionally consume any number of positional arguments. Arguments are split by whitespace and passed as an array of strings to the command function. The command then processes each argument string as it sees fit.
