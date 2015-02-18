using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class Console : MonoBehaviour {
	#region Private fields.
	public GameObject consoleWindow;
	public Text consoleText;
	public Scrollbar scrollbar;
	public InputField commandInput;
	public Button submitButton;

	private string log = "";
	
	// Command history.
	private List<string> history = new List<string>();
	private int currentHistoryOffset = 0;
	
	public bool Visible {
		get {
			return consoleWindow != null && consoleWindow.gameObject.activeSelf;
		}
	}

	// TODO add Color to-hex conversion extension method so these can be color selectors exposed in the editor.
	public string logMessageColor = "586e75";
	//public Color logMessageColor = new Color(88.0f/256.0f, 110.0f/256.0f, 117.0f/256.0f);
	public string warningMessageColor = "b58900";
	//public Color warningMessageColor = new Color(181.0f/256.0f, 137.0f/256.0f, 0);
	public string errorMessageColor = "dc322f";
	//public Color errorMessageColor = new Color(220.0f/256.0f, 50.0f/256.0f, 47.0f/256.0f);
	#endregion
	
	#region MonoBehaviour
	void Awake() {
		// Listen for Debug.Log calls.
		Application.RegisterLogCallback(Log);
		if (Visible) {
			UpdateSubmitButton();
		}
	}
	
	void Update() {
		//var enableConsole = (OtterGameManager.Instance.ParamManager.GetParamVal("enableConsole") != null);
		var enableConsole = true;
		
		// Show or hide the console window if the tilde key was pressed.
		bool bTildePressed = enableConsole && Input.GetKeyDown(KeyCode.BackQuote);
		bTildePressed |= (Input.GetKey(KeyCode.LeftShift) &&
			Input.GetKey(KeyCode.RightShift) &&
			Input.GetKeyDown(KeyCode.C));
		
		if (bTildePressed) {
			consoleWindow.gameObject.SetActive(!consoleWindow.activeSelf);
		}
		
		if (Visible && commandInput.isFocused) {
			if (Input.GetKeyDown(KeyCode.Return)) {
				SubmitText();
			}
			
			if (Input.GetKeyDown(KeyCode.UpArrow) && currentHistoryOffset < history.Count) {
				currentHistoryOffset++;
				commandInput.text = history[history.Count - currentHistoryOffset];
			}
			
			if (Input.GetKeyDown(KeyCode.DownArrow) && currentHistoryOffset > 0) {
				currentHistoryOffset--;
				if (currentHistoryOffset == 0) {
					commandInput.text = "";
				} else {
					commandInput.text = history[history.Count - currentHistoryOffset];
				}
			}
		}

		// TODO TEST
		if (Input.GetKeyDown(KeyCode.A)) {
			Debug.Log("TEST");
			Debug.LogWarning("WARNING");
			Debug.LogError("ERROR");
		}
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
			RunCommand(strippedText);
		}
		commandInput.Select();
		commandInput.ActivateInputField();
	}
	#endregion
	
	#region Private helper functions
	void OutputStringToConsole(string message) {
		log += "\n" + message;
		consoleText.text = log;
		scrollbar.value = 0;
	}
	
	void Log(string message, string stackTrace, LogType type) {
		string outputColor = errorMessageColor;
		if (type == LogType.Warning) {
			outputColor = warningMessageColor;
		}
		if (type == LogType.Log) {
			outputColor = logMessageColor;
		}

		string logMessage = "<color=#" + outputColor + ">" + message + "</color>";
		OutputStringToConsole(logMessage);
	}
	
	void RunCommand(string command) {
		OutputStringToConsole("> " + command);
		try {
			// TODO This is where the command should be parsed and run.
		} catch (System.Exception e) {
			OutputStringToConsole(e.StackTrace);
		}
		history.Add(command);
	}
	#endregion
}
