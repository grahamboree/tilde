using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class Console {
	#region Fields.
	public delegate void onChangeCallback(string logText);

	/// Enable/disable the console via some setting in another in-game menu.
	public bool enabled = true;
	public string log { get; private set; }

	public event onChangeCallback Changed;
	
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
		log += "\n" + message;
		Changed(log);
	}

	public void RunCommand(string command) {
		OutputStringToConsole("> " + command);
		SilentlyRunCommand(command);
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

		string logMessage = "<color=#" + outputColor + ">" + message + "</color>";
		OutputStringToConsole(logMessage);
	}
	#endregion
}
