using UnityEngine;
using System.Collections;
using System.Linq;

public static class BuiltinCommands {
	[ConsoleCommand(name:"res", docs:"List supported fullscreen resolutions on this device")]
	public static string SupportedResolutions() {
		return string.Join("\n", Screen.resolutions.Select(x => x.width + "x" + x.height).ToArray());
	}

	[ConsoleCommand(docs: "Load a scene with the given name.")]
	public static void loadLevel(string[] options) {
		if (options.Length == 0) {
			throw new System.Exception("You must specify a scene to load with 'loadlevel'.");
		}
		Application.LoadLevel(options[0]);
	}
}
