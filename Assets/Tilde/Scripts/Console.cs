using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Tilde {

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleCommand : Attribute {
	public string commandName;
	public string docstring;
	
	public ConsoleCommand(string name = null, string docs = null) {
		this.commandName = name;
		this.docstring = docs;
	}
}

public class Console {
	#region Fields.
		const string startingText = @"
  ___  _   __         ___       __            ___  _    
 /   \/ \ /\ \__  __ /\_ \     /\ \          /   \/ \   
/\_/\__// \ \  _\/\_\\//\ \    \_\ \     __ /\_/\__//   
\//\/__/   \ \ \/\/\ \ \ \ \   / _  \  / __`\//\/__/    
            \ \ \_\ \ \ \_\ \_/\ \/\ \/\  __/           
             \ \__\\ \_\/\____\ \___,_\ \____\          
              \/__/ \/_/\/____/\/__,_ /\/____/          
                                                        
To view available commands, type 'help'";

	/// The complete console command execution history.
	public ConsoleHistory history = new ConsoleHistory();

	/// Callback type for Console.Changed events.
	public delegate void onChangeCallback(string logText);

	/// Occurs when the log contents have changed, most often occurring when a command is executed.
	public event onChangeCallback Changed;

	// Registering console commands
	private delegate string commandAction(params string[] args);
	private delegate string simpleCommandAction();
	private delegate void silentCommandAction(string[] args);
	private delegate void simpleSilentCommandAction();

	private class CommandEntry {
		public string docs;
		public commandAction action;
	}

	private Dictionary<string, CommandEntry> commandMap = new Dictionary<string, CommandEntry>();
	public Dictionary<KeyCode, string> boundCommands = new Dictionary<KeyCode, string>();

	// Log scrollback.
	private StringBuilder logContent = new StringBuilder();

	/// The full console log string.
	public string Content { get { return logContent.ToString(); } }

	// Log styling.
	private const string logMessageColor = "586e75";
	private const string warningMessageColor = "b58900";
	private const string errorMessageColor = "dc322f";
	#endregion
	
	#region Singleton
	public static Console instance { get { return _instance; } }
	private static Console _instance = new Console();
	private Console() {
		// Listen for Debug.Log calls.
		Application.logMessageReceived += Log;
		commandMap["help"] = new CommandEntry(){docs = "View available commands as well as their documentation.", action = Help};
		FindCommands();
		logContent.Append(startingText);
	}
	#endregion

	#region Public Methods.
	/// <summary>
	/// Print a string to the console window.  Appears as if it was command output.
	/// </summary>
	/// <param name="message">The text to print.</param>
	public void OutputStringToConsole(string message) {
		logContent.Append("\n");
		logContent.Append(message);
		Changed(logContent.ToString());
	}

	/// <summary>
	/// Run a console command.  This is used by the text input UI to parse and execute commands.
	/// </summary>
	/// <param name="command">The complete command string, including any arguments.</param>
	public void RunCommand(string command) {
		logContent.Append("\n> ");
		logContent.Append(command);
		// Inform the UI that the console text has changed so it can redraw, 
		// in case the command takes a while to execute.
		Changed(logContent.ToString());
		history.AddCommandToHistory(command);
		logContent.Append("\n");
		logContent.Append(SilentlyRunCommand(command));
		Changed(logContent.ToString());
	}

	/// <summary>
	/// Execute a console command, but do not display output in the console window.
	/// </summary>
	/// <param name="commandString">The complete command string, including any arguments.</param>
	public string SilentlyRunCommand(string commandString) {
		string[] splitCommand = commandString.Split(' ');
		string commandName = splitCommand[0];
		CommandEntry command = null;
		if (commandMap.TryGetValue(commandName, out command)) {
			try {
				return command.action(splitCommand.Skip(1).ToArray());
			} catch (Exception e) {
				LogError(e.Message);
			}
		} else {
			LogError("Unknown console command: " + commandName);
		}
		return "";
	}
	
	/// <summary>
	/// Attempt to autocomplete a partial command.
	/// </summary>
	/// <param name="partialCommand">The full command name if a match is found.</param>
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
			int maxCommandLength = commands.Select(x => x.Length).Max();
			foreach (string c in commands) {
				result += "\n  " + c + new string(' ', (maxCommandLength - c.Length) + 3);
				string docstring = commandMap[c].docs;
				int docstringLength = 80; // starting line length
				docstringLength -= 2; // Leftmost indent
				docstringLength -= maxCommandLength; // command name length
				docstringLength -= 3; // rightmost padding between command and docstring.
				docstringLength = Mathf.Max(docstringLength, 0);

				bool addElipsis = docstringLength < docstring.Length;
				if (addElipsis) {
					docstringLength -= 3; // elipsis
				}

				result += new string(docstring.Take(docstringLength).ToArray());
				if (addElipsis) {
					result += "...";
				}
			}
			return result;
		}
		
		CommandEntry command = null;
		if (commandMap.TryGetValue(options[0], out command)) {
			return command.docs;
		}

		LogError("Command not found: " + options[0]);
		return "";
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
}
