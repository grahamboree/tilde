using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class SourceConsole : MonoBehaviour {
	public GameObject consoleWindow;
	public Text consoleText;
	public Scrollbar scrollbar;
	public InputField commandInput;
	public Button submitButton;

	// Command history.
	private List<string> history = new List<string>();
	private int currentHistoryOffset = 0;
	
	public bool Visible {
		get {
			return consoleWindow != null && consoleWindow.gameObject.activeSelf;
		}
	}
	
	#region MonoBehaviour
	void Awake() {
		Console.instance.Changed += UpdateLogContent;

		if (Visible) {
			UpdateSubmitButton();
		}
	}
	
	void Update() {
		if (!Console.instance.enabled) {
			return;
		}

		// Show or hide the console window if the tilde key was pressed.
		if (Input.GetKeyDown(KeyCode.BackQuote)) {
			consoleWindow.gameObject.SetActive(!consoleWindow.activeSelf);
		}
		
		if (Visible && commandInput.isFocused) {
			if (Input.GetKeyDown(KeyCode.Return)) {
				SubmitText();
			} else if (Input.GetKeyDown(KeyCode.UpArrow) && currentHistoryOffset < history.Count) {
				currentHistoryOffset++;
				commandInput.text = history[history.Count - currentHistoryOffset];
			} else if (Input.GetKeyDown(KeyCode.DownArrow) && currentHistoryOffset > 0) {
				currentHistoryOffset--;
				if (currentHistoryOffset == 0) {
					commandInput.text = "";
				} else {
					commandInput.text = history[history.Count - currentHistoryOffset];
				}
			}
		}
	}

	void OnDestroy() {
		Console.instance.Changed -= UpdateLogContent;
	}
	#endregion
	
	#region UI Events.
	public void UpdateSubmitButton() {
		string strippedText = Regex.Replace(commandInput.text, @"\s", "");
		submitButton.interactable = (strippedText != "");
		Text t = submitButton.GetComponentInChildren<Text>();
		Color textColor = t.color;
		textColor.a = submitButton.interactable ? 1 : 0.2f;
		t.color = textColor;
	}
	
	public void SubmitText() {
		// Remove newlines... the UI Input Field has to be set to a multiline input field for submission to work 
		// correctly, so when you hit enter it adds newline characters before Update() can call this function.  Remove 
		// them to get the raw command.
		string strippedText = Regex.Replace(commandInput.text, @"\n", "");
		commandInput.text = "";
		if (strippedText != "") {
			Console.instance.RunCommand(strippedText);
			history.Add(strippedText);
		}
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
