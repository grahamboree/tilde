using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

namespace Tilde {
	public class WindowedConsole : MonoBehaviour {
		public Console console;
		public GameObject consoleWindow;
		public Text consoleText;
		public InputField commandInput;

		bool Visible => consoleWindow != null && consoleWindow.gameObject.activeSelf;

		#region MonoBehaviour
		void Awake() {
			console.Changed.AddListener(UpdateLogContent);
		}

		void Update() {
			// Show or hide the console window if the tilde key was pressed.
			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				bool visible = !consoleWindow.activeSelf;
				consoleWindow.gameObject.SetActive(visible);
				if (visible) {
					UpdateLogContent(console.Content);
					commandInput.ActivateInputField();
					commandInput.Select();
				}
				commandInput.text = commandInput.text.TrimEnd('`');
			}

			if (Visible && commandInput.isFocused) {
				if (Input.GetKeyDown(KeyCode.Return)) {
					SubmitText();
				} else if (Input.GetKeyDown(KeyCode.UpArrow)) {
					string previous = console.TryGetPreviousCommand();
					if (previous != null) {
						commandInput.text = previous;
						commandInput.MoveTextEnd(false);
					}
				} else if (Input.GetKeyDown(KeyCode.DownArrow)) {
					string next = console.TryGetNextCommand();
					if (next != null) {
						commandInput.text = next;
						commandInput.MoveTextEnd(false);
					}
				} else if (Input.GetKeyDown(KeyCode.Tab)) {
					// Autocomplete
					string partialCommand = commandInput.text.Replace("\t", "");
					commandInput.text = partialCommand;
					partialCommand = partialCommand.TrimStart();

					if (partialCommand.Trim() != "") {
						string result = console.Autocomplete(partialCommand);
						if (result != null && result != partialCommand) {
							commandInput.text = result;
							commandInput.MoveTextEnd(false);
						}
					}
				} else if (Input.anyKeyDown && Input.inputString != "") {
					console.Completer.ResetCurrentState();
				}
			}

			if (Visible && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S)) {
				console.SaveToFile("tilde_console_dump.txt");
			}

			// Run any bound commands triggered this frame.
			if (!commandInput.isFocused) {
				foreach (var boundCommand in console.KeyBindings.Bindings) {
					if (Input.GetKeyDown(boundCommand.Key)) {
						console.RunCommand(boundCommand.Value);
					}
				}
			}
		}

		void OnDestroy() {
			console.Changed.RemoveListener(UpdateLogContent);
        }
		#endregion

		#region UI Events.
		public void SubmitText() {
			// Remove newlines... the UI Input Field has to be set to a multiline input field for submission to work 
			// correctly, so when you hit enter it adds newline characters before Update() can call this function.  Remove 
			// them to get the raw command.
			console.RunCommand(Regex.Replace(commandInput.text, @"\n", ""));
			
			// Clear and re-select the input field.
			commandInput.text = "";
			commandInput.Select();
			commandInput.ActivateInputField();
		}
		#endregion

		#region Event callbacks.
		void UpdateLogContent(string log) {
			consoleText.text = log;
		}
		#endregion
	}
}
