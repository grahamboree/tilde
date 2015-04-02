using UnityEngine;
using System.Collections;
using System.Linq;

namespace Tilde {
public static class BuiltinCommands {
	[ConsoleCommand(name:"res", docs:"List supported fullscreen resolutions on this device")]
	public static string SupportedResolutions() {
		return string.Join("\n", Screen.resolutions.Select(x => x.width + "x" + x.height).ToArray());
	}

	[ConsoleCommand(docs: "Exit the game.")]
	public static void exit() {
		Application.Quit();
	}

	[ConsoleCommand(docs: "Load a scene with the given name.")]
	public static void loadLevel(string[] options) {
		if (options.Length == 0) {
			throw new System.Exception("You must specify a scene to load with 'loadlevel'.");
		}
		Application.LoadLevel(options[0]);
	}

	[ConsoleCommand(docs: "Syntax: 'bind <key> <command>' Bind a console command to a key.")]
	public static void bind(string[] args) {
		if (args.Length < 2) {
			Debug.LogError("You must specify a key and a command as arguments to 'bind'.");
			return;
		}

		KeyCode key = KeyCodeFromString(args[0]);
		string command = string.Join(" ", args.Skip(1).ToArray());
		Console.instance.boundCommands[key] = command;
	}

	[ConsoleCommand(docs: "Syntax: 'unbind <key>' Unbind a console comand from a key.")]
	public static void unbind(string[] args) {
		if (args.Length != 1) {
			Debug.LogError("Command 'unbind' only takes 1 argument.");
			return;
		}

		KeyCode key = KeyCodeFromString(args[0]);
		Console.instance.boundCommands.Remove(key);
	}

	private static KeyCode KeyCodeFromString(string keyString) {
		if (keyString.Length == 1) {
			keyString = keyString.ToUpper();
		}
		
		KeyCode key = KeyCode.None;
		try {
			key = (KeyCode)System.Enum.Parse(typeof(KeyCode), keyString);
		} catch(System.ArgumentException) {
			Debug.LogError("Key '" + keyString + "' does not specify a key code.");
		}
		return key;
	}
}
}
