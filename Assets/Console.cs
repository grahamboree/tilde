using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommand : Attribute {
	public string commandName;
	public string docstring;

	public ConsoleCommand() {
	}

	public ConsoleCommand(string docstring) {
		this.docstring = docstring;
	}

	public ConsoleCommand(string commandName, string docstring) {
		this.commandName = commandName;
		this.docstring = docstring;
	}
}

public class Console {
	#region Fields.
	// Command history.
	public ConsoleHistory history = new ConsoleHistory();

	// Console output.
	public delegate void onChangeCallback(string logText);
	public event onChangeCallback Changed;

	// Registering console commands
	public delegate string commandAction(params string[] args);
	public delegate string simpleCommandAction();
	public delegate void silentCommandAction(string[] args);
	public delegate void simpleSilentCommandAction();
	private class CommandEntry {
		public string docs;
		public commandAction action;
	}
	Dictionary<string, CommandEntry> commandMap = new Dictionary<string, CommandEntry>();

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
		commandMap["help"] = new CommandEntry(){docs = "View available commands as well as their documentation.", action = Help};
		commandMap["res"] = new CommandEntry(){docs = "List supported fullscreen resolutions on this device", action = _ => SupportedResolutions()};
		FindCommands();
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
		string[] splitCommand = commandString.Split(' ');
		string commandName = splitCommand[0];
		CommandEntry command = null;
		if (commandMap.TryGetValue(commandName, out command)) {
			try {
				LogMessage(command.action(splitCommand.Skip(1).ToArray()));
			} catch (Exception e) {
				LogError(e.Message);
			}
		} else {
			LogError("Unknown console command: " + commandName);
		}
	}

	public string Autocomplete(string partialCommand) {
		return commandMap.Keys.FirstOrDefault(x => x.StartsWith(partialCommand));
	}

	public void SaveToFile(string filePath) {
		var lines = Regex.Replace(logContent.ToString(), "<.*?>", string.Empty).Split('\n');
		System.IO.File.WriteAllLines(filePath, lines);
	}
	#endregion

	#region Built-in commands
	string Help(string[] options) {
		if (options.Length == 0) {
			string result = "Available commands:\n";
			string[] commands = commandMap.Keys.ToArray();
			Array.Sort(commands);
			return result + String.Join("\n", commands.Select(x => "\t" + x).ToArray());
		}
		
		CommandEntry command = null;
		if (commandMap.TryGetValue(options[0], out command)) {
			return command.docs;
		}

		LogError("Command not found: " + options[0]);
		return "";
	}
	
	string SupportedResolutions() {
		return string.Join("\n", Screen.resolutions.Select(x => x.width + "x" + x.height).ToArray());
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

	void FindCommands() {
		foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
			foreach(Type type in assembly.GetTypes()) {
				foreach(MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
					ConsoleCommand[] attrs = method.GetCustomAttributes(typeof(ConsoleCommand), true) as ConsoleCommand[];
					if (attrs.Length == 0)
						continue;

					commandAction action = Delegate.CreateDelegate(typeof(commandAction), method, false) as commandAction;
					if (action == null) {
						simpleCommandAction simpleAction = Delegate.CreateDelegate(typeof(simpleCommandAction), method, false) as simpleCommandAction;
						if (simpleAction != null) {
							action = _ => simpleAction();
						} else {
							silentCommandAction silentAction = Delegate.CreateDelegate(typeof(silentCommandAction),  method, false) as silentCommandAction;
							if (silentAction != null) {
								action = args => { silentAction(args); return ""; };
							} else {
								simpleSilentCommandAction simpleSilentAction = Delegate.CreateDelegate(typeof(simpleSilentCommandAction),  method, false) as simpleSilentCommandAction;
								action = args => { simpleSilentAction(); return ""; };
							}
						}
					}
					
					if (action == null) {
						Debug.LogError(string.Format(
							"Method {0}.{1} is the wrong type.  It must take either no argumets, or just an array " + 
							"of strings, and its return type must be string or void.", type, method.Name));
						continue;
					}

					foreach(ConsoleCommand cmd in attrs) {
						if (string.IsNullOrEmpty(cmd.commandName)) {
							cmd.commandName = method.Name;
						}

						commandMap[cmd.commandName] = new CommandEntry(){docs = cmd.docstring ?? "", action = action };
					}
				}
			}
		}
	}
	#endregion
}
