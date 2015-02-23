using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

public class QuakeConsole : MonoBehaviour {
	public GameObject consoleWindow;
	public Text consoleText;
	public Scrollbar scrollbar;
	public InputField commandInput;
	public Button submitButton;

	// Whether or not pressing tilde will cause the console to animate to hidden or animate to shown.
	bool shown = false;

	// Cached reference to the Console singleton instance.
	Console console;
	
	public bool Visible {
		get {
			return consoleWindow != null && consoleWindow.gameObject.activeSelf;
		}
	}
	
	#region MonoBehaviour
	void Awake() {
		console = Console.instance;
		console.Changed += UpdateLogContent;
		
		if (Visible) {
			UpdateSubmitButton();
		}
	}
	
	void Update() {
		if (!console.enabled) {
			return;
		}
		
		// Show or hide the console window if the tilde key was pressed.
		if (Input.GetKeyDown(KeyCode.BackQuote)) {
			StopAllCoroutines();
			StartCoroutine(shown ? Hide() : Show());
			shown = !shown;
		}
		
		if (Visible && commandInput.isFocused) {
			if (Input.GetKeyDown(KeyCode.Return)) {
				SubmitText();
			} else if (Input.GetKeyDown(KeyCode.UpArrow)) {
				string previous = console.history.TryGetPreviousCommand();
				if (previous != null) {
					commandInput.text = previous;
				}
			} else if (Input.GetKeyDown(KeyCode.DownArrow)) {
				string next = console.history.TryGetNextCommand();
				if (next != null) {
					commandInput.text = next;
				}
			}
		}
	}
	
	void OnDestroy() {
		console.Changed -= UpdateLogContent;
	}
	#endregion

	#region Coroutines
	void SetConsoleY(float y) {
		RectTransform consoleWindowRectTransform = (consoleWindow.transform as RectTransform);
		Vector2 pos = consoleWindowRectTransform.anchoredPosition;
		pos.y = y;
		consoleWindowRectTransform.anchoredPosition = pos;
	}

	IEnumerator Show() {
		consoleWindow.SetActive(true);
		float startTime = Time.time;
		float currentPosition = (consoleWindow.transform as RectTransform).anchoredPosition.y;
		while (currentPosition > -246) {
			currentPosition = Mathf.Lerp(currentPosition, -247, Time.time - startTime);
			SetConsoleY(currentPosition);
			yield return null;
		}
		SetConsoleY(-247);
	}

	IEnumerator Hide() {
		float startTime = Time.time;
		float currentPosition = (consoleWindow.transform as RectTransform).anchoredPosition.y;
		while (currentPosition < 249) {
			currentPosition = Mathf.Lerp(currentPosition, 250, Time.time - startTime);

			SetConsoleY(currentPosition);
			yield return null;
		}
		SetConsoleY(250);

		consoleWindow.SetActive(false);
	}
	#endregion

	#region UI Events.
	public void UpdateSubmitButton() {
		string strippedText = Regex.Replace(commandInput.text, @"\s", "");
		submitButton.interactable = (strippedText != "");
	}
	
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
