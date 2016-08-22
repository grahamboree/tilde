using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Tilde {
	public static class BuiltinCommands {
		[ConsoleCommand(name: "res", docs: "List supported fullscreen resolutions on this device")]
		public static string SupportedResolutions() {
			return string.Join("\n", Screen.resolutions.Select(x => x.width + "x" + x.height).ToArray());
		}

		[ConsoleCommand(docs: "Exit the game.")]
		public static void exit() {
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		[ConsoleCommand(docs: "Load a scene with the given name.")]
		public static void loadLevel(string[] options) {
			if (options.Length == 0) {
				throw new System.Exception("You must specify the name of a scene to load with 'loadlevel'.");
			}
			SceneManager.LoadScene(options[0]);
		}

		[ConsoleCommand]
		[Completion(1, "one", "two", "three")]
		public static string testFail(string[] args) {
			return string.Join("\n", args);
		}
	}
}
