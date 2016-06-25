using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;

namespace Tilde {
	public class DrawerConsole : MonoBehaviour {
		#region Fields.
		// Config.
		public const float height = 500.0f;

		public Console console;

		// GUI elements.
		public GameObject consoleWindow;
		public Text consoleText;
		public Scrollbar scrollbar;
		public InputField commandInput;

		// Whether or not pressing tilde will cause the console to animate to hidden or animate to shown.
		bool shown = false;

		bool Visible {
			get {
				return consoleWindow != null && consoleWindow.gameObject.activeSelf;
			}
		}
		#endregion

		#region MonoBehaviour
		void Awake() {
			(consoleWindow.transform as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			SetConsoleY(height);

			console.Changed += UpdateLogContent;
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
					SubmitText();
				} else if (Input.GetKeyDown(KeyCode.UpArrow)) {
					string previous = console.history.TryGetPreviousCommand();
					if (previous != null) {
						commandInput.text = previous;
						commandInput.MoveTextEnd(false);
					}
				} else if (Input.GetKeyDown(KeyCode.DownArrow)) {
					string next = console.history.TryGetNextCommand();
					if (next != null) {
						commandInput.text = next;
						commandInput.MoveTextEnd(false);
					}
				} else if (Input.GetKeyDown(KeyCode.Tab)) {
					// Autocomplete
					string partialCommand = commandInput.text.Trim();
					if (partialCommand != "") {
						string result = console.Autocomplete(partialCommand);
						if (result != null) {
							commandInput.text = result;
							commandInput.MoveTextEnd(false);
						}
					}
				} else if (Input.anyKeyDown && Input.inputString != ""){
					console.completer.ResetCurrentState();
				}
			}

			if (Visible && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S)) {
				console.SaveToFile("tilde_console_dump.txt");
			}

			// Run any bound commands triggered this frame.
			if (!commandInput.isFocused) {
				foreach (var boundCommand in console.boundCommands) {
					if (Input.GetKeyDown(boundCommand.Key)) {
						console.RunCommand(boundCommand.Value);
					}
				}
			}
		}

		void OnDestroy() {
			console.Changed -= UpdateLogContent;
		}
		#endregion

		#region Coroutines
		IEnumerator Show() {
			consoleWindow.SetActive(true);
			commandInput.ActivateInputField();
			commandInput.Select();
			float startTime = Time.time;
			float currentPosition = (consoleWindow.transform as RectTransform).anchoredPosition.y;
			while (currentPosition > -height + 4) {
				currentPosition = Mathf.Lerp(currentPosition, -height, Time.time - startTime);
				SetConsoleY(currentPosition);
				yield return null;
			}
			SetConsoleY(-height);
		}

		IEnumerator Hide() {
			float startTime = Time.time;
			float currentPosition = (consoleWindow.transform as RectTransform).anchoredPosition.y;
			while (currentPosition < height - 4) {
				currentPosition = Mathf.Lerp(currentPosition, height, Time.time - startTime);

				SetConsoleY(currentPosition);
				yield return null;
			}
			SetConsoleY(height);

			consoleWindow.SetActive(false);
		}
		#endregion

		#region Private methods.
		void SetConsoleY(float y) {
			RectTransform consoleWindowRectTransform = (consoleWindow.transform as RectTransform);
			Vector2 pos = consoleWindowRectTransform.anchoredPosition;
			pos.y = y;
			consoleWindowRectTransform.anchoredPosition = pos;
		}
		#endregion

		#region UI Events.
		public void SubmitText() {
			// Remove newlines... the UI Input Field has to be set to a multiline input field for submission to work 
			// correctly, so when you hit enter it adds newline characters before Update() can call this function.  Remove 
			// them to get the raw command.
			string strippedText = Regex.Replace(commandInput.text, @"\n", "");
			if (strippedText != "") {
				console.RunCommand(strippedText);
			}

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
