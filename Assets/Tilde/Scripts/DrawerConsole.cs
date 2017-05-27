using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;

namespace Tilde {
	public class DrawerConsole : MonoBehaviour {
		[SerializeField] float height = 500.0f;

		[SerializeField] Console console;

		[Header("GUI elements")]
		[SerializeField] GameObject consoleWindow;
		[SerializeField] Text consoleText;
		[SerializeField] InputField commandInput;
		
		//////////////////////////////////////////////////
		
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
		
		//////////////////////////////////////////////////

		// Whether or not pressing tilde will cause the console to animate to hidden or animate to shown.
		bool shown;

		bool Visible { get { return consoleWindow != null && consoleWindow.activeSelf; } }
		
		//////////////////////////////////////////////////

		#region MonoBehaviour
		void Awake() {
			((RectTransform)consoleWindow.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
			SetConsoleY(height);
			console.Changed.AddListener(UpdateLogContent);
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
					string previous = console.History.TryGetPreviousCommand();
					if (previous != null) {
						commandInput.text = previous;
						commandInput.MoveTextEnd(false);
					}
				} else if (Input.GetKeyDown(KeyCode.DownArrow)) {
					string next = console.History.TryGetNextCommand();
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
				} else if (Input.anyKeyDown && Input.inputString != ""){
					console.Completer.ResetCurrentState();
				}
			}

			if (Visible && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S)) {
				console.SaveToFile("tilde_console_dump.txt");
			}

			// Run any bound commands triggered this frame.
			if (!commandInput.isFocused) {
				foreach (var boundCommand in console.KeyBindings.bindings) {
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
			consoleWindow.SetActive(true);
			commandInput.ActivateInputField();
			commandInput.Select();
			float startTime = Time.time;
			float currentPosition = ((RectTransform)consoleWindow.transform).anchoredPosition.y;
			while (currentPosition > -height + 4) {
				currentPosition = Mathf.Lerp(currentPosition, -height, Time.time - startTime);
				SetConsoleY(currentPosition);
				yield return null;
			}
			SetConsoleY(-height);
		}

		IEnumerator Hide() {
			float startTime = Time.time;
			float currentPosition = ((RectTransform)consoleWindow.transform).anchoredPosition.y;
			while (currentPosition < height - 4) {
				currentPosition = Mathf.Lerp(currentPosition, height, Time.time - startTime);

				SetConsoleY(currentPosition);
				yield return null;
			}
			SetConsoleY(height);

			consoleWindow.SetActive(false);
		}

		void SetConsoleY(float y) {
			var consoleWindowRectTransform = (RectTransform)consoleWindow.transform;
			var pos = consoleWindowRectTransform.anchoredPosition;
			pos.y = y;
			consoleWindowRectTransform.anchoredPosition = pos;
		}

		void UpdateLogContent(string log) {
			consoleText.text = log;
		}
	}
}
