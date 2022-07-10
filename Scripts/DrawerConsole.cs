using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;

namespace Tilde {
	public class DrawerConsole : MonoBehaviour {
		[SerializeField] float height = 500.0f;

		[SerializeField] TildeConsole console;

		[Header("UI elements")]
		[SerializeField] GameObject consoleWindow;
		[SerializeField] Text consoleText;
		[SerializeField] InputField commandInput;

		//////////////////////////////////////////////////

		// Whether or not pressing tilde will cause the console to animate to hidden or animate to shown.
		bool shown;

		bool Visible => consoleWindow != null && consoleWindow.activeSelf;

		RectTransform consoleWindowRectTransform;
		int historyOffset;

		//////////////////////////////////////////////////

		#region MonoBehaviour
		void Awake() {
			consoleWindowRectTransform = consoleWindow.GetComponent<RectTransform>();
			consoleWindowRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			SetConsoleY(height);
			console.Changed.AddListener(UpdateLogContent);
		}

		void OnEnable() {
			UpdateLogContent(console.Content);
		}

		void Update() {
			// Show or hide the console window if the tilde key was pressed.
			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				StopAllCoroutines();
				StartCoroutine(shown ? Hide() : Show());
				shown = !shown;
				commandInput.text = commandInput.text.TrimEnd('`');
			}

			if (Visible && commandInput.isFocused) {
				if (Input.GetKeyDown(KeyCode.Return)) {
					// Remove newlines... the UI Input Field has to be set to a multiline input field for submission to work
					// correctly, so when you hit enter it adds newline characters before Update() can call this function.  Remove
					// them to get the raw command.
					console.RunCommand(Regex.Replace(commandInput.text, @"\n", ""));

					// Reset the history navigation state
					historyOffset = 0;
					
					// Clear and re-select the input field.
					commandInput.text = "";
					commandInput.Select();
					commandInput.ActivateInputField();
				} else if (Input.GetKeyDown(KeyCode.UpArrow)) {
					string previous = console.GetCommandHistory(historyOffset + 1);
					if (previous != null) {
						historyOffset++;
						commandInput.text = previous;
					}
					commandInput.MoveTextEnd(false);
				} else if (Input.GetKeyDown(KeyCode.DownArrow)) {
					string previous = console.GetCommandHistory(historyOffset - 1);
					if (previous != null) {
						historyOffset--;
						commandInput.text = previous;
					}
					commandInput.MoveTextEnd(false);
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
				} else if (Input.anyKeyDown && Input.inputString != ""){
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

		IEnumerator Show() {
			UpdateLogContent(console.Content);
			consoleWindow.SetActive(true);
			commandInput.ActivateInputField();
			commandInput.Select();
			float startTime = Time.time;
			float currentPosition = consoleWindowRectTransform.anchoredPosition.y;
			while (currentPosition > -height + 4) {
				currentPosition = Mathf.Lerp(currentPosition, -height, Time.time - startTime);
				SetConsoleY(currentPosition);
				yield return null;
			}
			SetConsoleY(-height);
		}

		IEnumerator Hide() {
			float startTime = Time.time;
			float currentPosition = consoleWindowRectTransform.anchoredPosition.y;
			while (currentPosition < height - 4) {
				currentPosition = Mathf.Lerp(currentPosition, height, Time.time - startTime);
				SetConsoleY(currentPosition);
				yield return null;
			}
			SetConsoleY(height);

			consoleWindow.SetActive(false);
		}

		void SetConsoleY(float y) {
			var pos = consoleWindowRectTransform.anchoredPosition;
			pos.y = y;
			consoleWindowRectTransform.anchoredPosition = pos;
		}

		void UpdateLogContent(string log) {
			consoleText.text = log;
		}
	}
}
