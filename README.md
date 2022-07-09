<p align="center">
    <a href="#-quickstart">
      <img src="/Docs/tilde_title.png">
    </a>
</p>

<p align="center">
    A remote developer console for Unity
</p>

<p align="center">
<img src="/Docs/drawer.gif"> <img src="/Docs/windowed.gif">
</p>

# ⚡ Quickstart
1. Add the `Tilde Console` and optionally the `Tilde Web Console Server` components to a game object in your scene.
2. Add either the `Drawer Console` or `Windowed Console` prefab and set the `Console` reference on it.
3. Start the game and press the tilde/backtick key to open the console.
4. Run `help` to list available commands.
5. Open `localhost:55055` in a web browser to remotely execute commands.

# ✨ Features
* Two styles of console prefabs: window (similar to popular MOBAs) and drawer (similar to classic FPS's).
* Remotely execute commands with a web-based console served by an embedded server.
* Automatic command registration via function annotation.
* Automatic command history.  Cycle through previous commands with the up and down arrow keys.
* Tab autocomplete.
* Auto-generate command names, or specify them explicitly
* Displays all Unity log, warning, error and exception messages colored as you'd expect.
* Bind keys to run commands with the `bind` and `unbind` commands.
* Supports custom commands with any number of arguments.
