﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
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

	// Registering console commands
	public delegate string commandAction(params string[] args);
	Dictionary<string, commandAction> commandMap = new Dictionary<string, commandAction>();

	// Log scrollback.
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

		commandMap["marco"] = _ => "polo";
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

	public void SilentlyRunCommand(string commandString) {
		// TODO parse and run the command here.
		string[] splitCommand = commandString.Split(' ');
		string commandName = splitCommand[0];
		commandAction command = null;
		if (commandMap.TryGetValue(commandName, out command)) {
			try {
				LogMessage(command(splitCommand.Skip(1).ToArray()));
			} catch (Exception e) {
				LogError(e.Message);
			}
		} else {
			LogError("Unknown console command: " + commandName);
		}
	}

	public void RegisterCommand(string commandName, commandAction action) {
		commandMap[commandName] = action;
	}
	#endregion
	
	#region Private helper functions
	void Log(string message, string stackTrace, LogType type) {
		switch (type) {
		case LogType.Assert:
		case LogType.Error:
		case LogType.Exception:
			LogError(message);
			break;
		case LogType.Warning:
			LogWarning(message);
			break;
		case LogType.Log:
			LogMessage(message);
			break;
		}
	}
	
	void LogMessage(string message) {
		OutputFormatted(message ?? "", logMessageColor);
	}

	void LogWarning(string warning) {
		OutputFormatted(warning ?? "", warningMessageColor);
	}

	void LogError(string error) {
		OutputFormatted(error ?? "", errorMessageColor);
	}

	void OutputFormatted(string message, string color) {
		logContent.Append("\n<color=#");
		logContent.Append(color);
		logContent.Append(">");
		logContent.Append(message);
		logContent.Append("</color>");
		Changed(logContent.ToString());
	}
	#endregion
}
