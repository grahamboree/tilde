using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class Console {
	#region Fields.
	/// Enable/disable the console via some setting in another in-game menu.
	public bool enabled = true;

	// Command history.
	public ConsoleHistory history = new ConsoleHistory();

	// Console output.
	public delegate void onChangeCallback(string logText);
	public event onChangeCallback Changed;
	StringBuilder logContent = new StringBuilder();
	
	string logMessageColor = "586e75";
	string warningMessageColor = "b58900";
	string errorMessageColor = "dc322f";
	#endregion
	
	#region Singleton
	public static Console instance { get { return _instance; } }
	private static Console _instance = new Console();
	private Console() {
		// Listen for Debug.Log calls.
		Application.RegisterLogCallback(Log);
	}
	#endregion
	
	#region Public Methods.
	public void OutputStringToConsole(string message) {
		logContent.Append("\n");
		logContent.Append(message);
		Changed(logContent.ToString());
	}

	public void RunCommand(string command) {
		logContent.Append("\n> ");
		logContent.Append(command);
		Changed(logContent.ToString());
		history.AddCommandToHistory(command);
		SilentlyRunCommand(command);
		Changed(logContent.ToString());
	}

	public void SilentlyRunCommand(string command) {
		// TODO parse and run the command here.
	}

	public void RegisterCommandCallback() {
	}

	public void RegisterCommand() {
	}
	#endregion
	
	#region Private helper functions
	void Log(string message, string stackTrace, LogType type) {
		string outputColor = errorMessageColor;
		if (type == LogType.Warning) {
			outputColor = warningMessageColor;
		}
		if (type == LogType.Log) {
			outputColor = logMessageColor;
		}

		logContent.Append("\n<color=#");
		logContent.Append(outputColor);
		logContent.Append(">");
		logContent.Append(message);
		logContent.Append("</color>");
		Changed(logContent.ToString());
	}
	#endregion
}
