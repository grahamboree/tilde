using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Tilde {
	[CreateAssetMenu(fileName = "Console", menuName = "Tilde/Console", order = 1)]
	public class Console : ScriptableObject {
		#region types
		/// Callback type for Console.Changed events.
		public delegate void onChangeCallback(string logText);

		// Registering commands
		delegate string commandAction(params string[] args);
		delegate string simpleCommandAction();
		delegate void silentCommandAction(string[] args);
		delegate void simpleSilentCommandAction();

		class CommandEntry {
			public string docs;
			public commandAction action;
		}
		#endregion

		#region Public fields
		public bool ShowUnityLogMessages = true;

		[Header("Output Styling")]
		public Color LogColor = new Color(88.0f / 255.0f, 110.0f / 255.0f, 117.0f / 255.0f);
		public Color WarningColor = new Color(181.0f / 255.0f, 137.0f / 255.0f, 0);
		public Color ErrorColor = new Color(220.0f / 255.0f, 50.0f / 255.0f, 47.0f / 255.0f);

		/// The complete console command execution history.
		public ConsoleHistory history = new ConsoleHistory();

		/// The Console command autocompleter.
		public Autocompleter completer;
			
		public BoundCommands keyBindings = new BoundCommands();

		/// Occurs when the log contents have changed, most often occurring when a command is executed.
		public event onChangeCallback Changed;

		/// The full console log string.
		public string Content { get { return logContent.ToString(); } }
		#endregion

		#region Private fields
		const string startingText = @"
  ___  _   __         ___       __            ___  _    
 /   \/ \ /\ \__  __ /\_ \     /\ \          /   \/ \   
/\_/\__// \ \  _\/\_\\//\ \    \_\ \     __ /\_/\__//   
\//\/__/   \ \ \/\/\ \ \ \ \   / _  \  / __`\//\/__/    
            \ \ \_\ \ \ \_\ \_/\ \/\ \/\  __/           
             \ \__\\ \_\/\____\ \___,_\ \____\          
              \/__/ \/_/\/____/\/__,_ /\/____/          
                                                        
To view available commands, type 'help'";
		
		static Dictionary<string, CommandEntry> commandMap = new Dictionary<string, CommandEntry>();

		// Log scrollback.
		StringBuilder logContent = new StringBuilder();
		#endregion

		#region ScriptableObject
		void OnEnable() {
			// Listen for Debug.Log calls.
			Application.logMessageReceived += Log;

			// Add a few special commands
			commandMap["help"] = new CommandEntry() {
				docs = "View available commands as well as their documentation.",
				action = Help
			};
			commandMap["bind"] = new CommandEntry() {
				docs = "Syntax: 'bind <key> <command>' Bind a console command to a key.",
				action = keyBindings.bind
			};
			commandMap["unbind"] = new CommandEntry() {
				docs = "Syntax: 'unbind <key>' Unbind a console comand from a key.",
				action = keyBindings.unbind
			};

			FindCommands(true);
			completer = new Autocompleter(commandMap.Keys);

			logContent.Append(startingText);
		}
		#endregion

#if UNITY_EDITOR
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded() {
			FindCommands();
		}
#endif

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
					OutputFormatted(e.Message, ErrorColor);
				}
			} else {
				OutputFormatted("Unknown command: " + commandName, ErrorColor);
			}
			return "";
		}

		/// <summary>
		/// Attempt to autocomplete a partial command.
		/// </summary>
		/// <param name="partialCommand">The full command name if a match is found.</param>
		public string Autocomplete(string partialCommand) {
			return completer.Complete(partialCommand);
		}

		public void SaveToFile(string filePath) {
			var lines = Regex.Replace(logContent.ToString(), "<.*?>", string.Empty).Split('\n');
			System.IO.File.WriteAllLines(filePath, lines);
		}
		#endregion

		#region Built-in commands
		/// <summary>
		/// Special command for listing command help
		/// </summary>
		/// <param name="options">Any extra options passed to help.</param>
		string Help(string[] options) {
			const int LINE_WIDTH = 80;
			const int COMMAND_INDENT = 2;
			const int COMMAND_DOCSTRING_PADDING = 3;

			var helpText = new StringBuilder();

			if (options.Length == 0) {
				// Show help for everything.
				int maxCommandLength = commandMap.Keys.Select(x => x.Length).Max();
				int docsColumnPadding = COMMAND_INDENT + maxCommandLength + COMMAND_DOCSTRING_PADDING;

				helpText.AppendLine("Available commands:");
				foreach (var commandEntry in commandMap.OrderBy(x => x.Key)) {
					string command = commandEntry.Key;
					string docstring = commandEntry.Value.docs;

					// Add the command name and enough spaces to align the docstrings.
					helpText.AppendLine();
					helpText.Append(new string(' ', COMMAND_INDENT));
					helpText.Append(command);
					helpText.Append(new string(' ', (maxCommandLength - command.Length) + COMMAND_DOCSTRING_PADDING));

					// Add the docstring, wrapping and aligning to the column if necessary.
					int docsColumnSize = LINE_WIDTH - COMMAND_INDENT - maxCommandLength - COMMAND_DOCSTRING_PADDING;
					if (docsColumnSize > 0) {
						bool padOutNewLine = false;
						while (docsColumnSize < docstring.Length) {
							if (padOutNewLine) {
								helpText.AppendLine();
								helpText.Append(new string(' ', docsColumnPadding));
							}
							helpText.Append(docstring.Substring(0, docsColumnSize));
							docstring = docstring.Substring(docsColumnSize);
							padOutNewLine = true;
						}

						if (!string.IsNullOrEmpty(docstring)) {
							if (padOutNewLine) {
								helpText.AppendLine();
								helpText.Append(new string(' ', docsColumnPadding));
							}
							helpText.Append(docstring);
						}
					}
				}
			} else {
				// Show help for a specific command.
				CommandEntry command = null;
				if (commandMap.TryGetValue(options[0], out command)) {
					helpText.Append(command.docs);
				} else {
					OutputFormatted("Command not found: " + options[0], ErrorColor);
				}
			}
			return helpText.ToString();
		}
		#endregion

		#region Private helper functions
		void Log(string message, string stackTrace, LogType type) {
			if (ShowUnityLogMessages && !string.IsNullOrEmpty(message)) {
				switch (type) {
				case LogType.Assert:
				case LogType.Error:
				case LogType.Exception:
					OutputFormatted(message, ErrorColor);
					break;
				case LogType.Warning:
					OutputFormatted(message, WarningColor);
					break;
				case LogType.Log:
					OutputFormatted(message, LogColor);
					break;
				}
			}
		}

		void OutputFormatted(string message, Color color) {
			logContent.Append("\n<color=");
			logContent.Append(String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}",
				Mathf.RoundToInt(color.r * 255),
				Mathf.RoundToInt(color.g * 255),
				Mathf.RoundToInt(color.b * 255),
				Mathf.RoundToInt(color.a * 255)));
			logContent.Append(">");
			logContent.Append(message);
			logContent.Append("</color>");
			Changed(logContent.ToString());
		}

		static void FindCommands(bool silently = false) {
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				// Skip non-user assemblies.
				if (!assembly.GetName().Name.StartsWith("Assembly")) {
					continue;
				}

				foreach (Type type in assembly.GetTypes()) {
					foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
						ConsoleCommand[] attrs = method.GetCustomAttributes(typeof(ConsoleCommand), true) as ConsoleCommand[];
						if (attrs.Length == 0) {
							continue;
						}

						if (!method.IsStatic) {
							if (!silently) {
								Debug.LogError(string.Format(
									"Tilde: Method {0}.{1} must be static to be registered as a console command.",
									type, method.Name));
							}
							continue;
						}

						commandAction action = Delegate.CreateDelegate(typeof(commandAction), method, false) as commandAction;
						if (action == null) {
							simpleCommandAction simpleAction = Delegate.CreateDelegate(typeof(simpleCommandAction), method, false) as simpleCommandAction;
							if (simpleAction != null) {
								action = _ => simpleAction();
							} else {
								silentCommandAction silentAction = Delegate.CreateDelegate(typeof(silentCommandAction), method, false) as silentCommandAction;
								if (silentAction != null) {
									action = args => { silentAction(args); return ""; };
								} else {
									simpleSilentCommandAction simpleSilentAction = Delegate.CreateDelegate(typeof(simpleSilentCommandAction), method, false) as simpleSilentCommandAction;
									if (simpleSilentAction != null) {
										action = args => { simpleSilentAction(); return ""; };
									}
								}
							}
						}

						if (action == null) {
							if (!silently) {
								Debug.LogError(string.Format(
									"Tilde: Method {0}.{1} is the wrong type.  It must take either no argumets, or just " +
									"an array of strings, and its return type must be string or void.", type, method.Name));
							}
							continue;
						}

						foreach (ConsoleCommand cmd in attrs) {
							if (string.IsNullOrEmpty(cmd.commandName)) {
								cmd.commandName = method.Name;
							}

							commandMap[cmd.commandName] = new CommandEntry() { docs = cmd.docstring ?? "", action = action };
						}
					}
				}
			}
		}
		#endregion
	}

	public class BoundCommands {
		public Dictionary<KeyCode, string> bindings = new Dictionary<KeyCode, string>();

		public string bind(string[] args) {
			if (args.Length < 2) {
				Debug.LogError("You must specify a key and a command as arguments to 'bind'.");
				return "";
			}

			KeyCode key = KeyCodeFromString(args[0]);
			if (key != KeyCode.None) {
				string command = string.Join(" ", args.Skip(1).ToArray());
				bindings[key] = command;
			}
			return "";
		}
			
		public string unbind(string[] args) {
			if (args.Length != 1) {
				Debug.LogError("Command 'unbind' only takes 1 argument.");
				return "";
			}

			KeyCode key = KeyCodeFromString(args[0]);
			if (key != KeyCode.None) {
				bindings.Remove(key);
			}
			return "";
		}

		private static KeyCode KeyCodeFromString(string keyString) {
			if (keyString.Length == 1) {
				keyString = keyString.ToUpper();
			}

			KeyCode key = KeyCode.None;
			try {
				key = (KeyCode)System.Enum.Parse(typeof(KeyCode), keyString);
			} catch (System.ArgumentException) {
				Debug.LogError("Key '" + keyString + "' does not specify a key code.");
			}
			return key;
		}
	}
}