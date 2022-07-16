using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Tilde {
	public enum LogLineType {
		Normal,
		UnityLog,
		Warning,
		Error
	}
	
	public class TildeConsole : MonoBehaviour {
		#region Types
		/// Callback type for Console.Changed events.
		public class OnChangeCallback : UnityEvent<string> {}

		// Registering commands
		delegate string CommandAction(params string[] args);
		delegate void SilentCommandAction(params string[] args);
		delegate string SimpleCommandAction();
		delegate void SimpleSilentCommandAction();

		class RegisteredCommand {
			public string Docs;
			public CommandAction Action;
			public Autocompleter[] Completers;
		}

		class UnknownCommandException : Exception {
			public UnknownCommandException(string command) : base("Unknown command: " + command) { }
		}
		#endregion
		
		//////////////////////////////////////////////////

		[SerializeField] bool showUnityLogMessages = true;
		[SerializeField] bool caseInsensitiveMatching = true;

		//////////////////////////////////////////////////

		/// The complete console command execution history.
		public readonly List<string> History = new();

		/// The Console command autocompleter.
		public Autocompleter Completer;

		public readonly BoundCommands KeyBindings = new();

		/// Occurs when the log contents have changed, most often occurring when a command is executed.
		public readonly OnChangeCallback Changed = new();

		/// The full console log string.
		public string Content => Scrollback.ToUIString();
		
		/// The full console log string formatted for the remote web console.
		public string RemoteContent => Scrollback.ToRemoteString();
		
		//////////////////////////////////////////////////

		/// <summary>
		/// Print a string to the console window.  Appears as if it was command output.
		/// </summary>
		/// <param name="message">The text to print.</param>
		public void OutputStringToConsole(string message) {
			AppendOutput(LogLineType.Normal, message);
		}

		/// <summary>
		/// Run a console command.  This is used by the text input UI to parse and execute commands.
		/// </summary>
		/// <param name="command">The complete command string, including any arguments.</param>
		public void RunCommand(string command) {
			AppendOutput(LogLineType.Normal, "> " + command);
			
			if (string.IsNullOrEmpty(command)) {
				return;
			}
			
			History.Add(command);

			try {
				AppendOutput(LogLineType.Normal, SilentlyRunCommand(command));
			} catch (Exception e) {
				AppendOutput(LogLineType.Error, e.Message);
			}
		}

		/// <summary>
		/// Execute a console command, but do not display output in the console window.
		/// </summary>
		/// <param name="commandString">The complete command string, including any arguments.</param>
		/// <exception cref="UnknownCommandException">Thrown when the command is not found</exception>
		public string SilentlyRunCommand(string commandString) {
			string[] splitCommand = commandString.Split(' ');
			string commandName = splitCommand[0];
			if (!TryGetCommand(commandName, out var command)) {
				throw new UnknownCommandException(commandName);
			}
			return command.Action(splitCommand.Skip(1).ToArray());
		}

		/// <summary>
		/// Attempt to autocomplete a partial command.
		/// </summary>
		/// <param name="partialCommand">The full command name if a match is found.</param>
		public string Autocomplete(string partialCommand) {
			if (partialCommand.EndsWith("\t")) {
				// Remove the \t
				partialCommand = partialCommand.Substring(0, partialCommand.Length - 1);
			}
			string[] parameters = partialCommand.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);

			if (parameters.Length == 1 && !partialCommand.EndsWith(" ")) {
				return Completer.Complete(partialCommand);
			}

			if (registeredCommands.ContainsKey(parameters[0])) {
				int lastIndex = parameters.Length - 1;
				if (partialCommand.EndsWith(" ")) {
					lastIndex++;
				}

				var completers = registeredCommands[parameters[0]].Completers;
				if (completers.Length > lastIndex && completers[lastIndex] != null) {
					string lastParam = lastIndex < parameters.Length ? parameters[lastIndex] : "";
					string completion = completers[lastIndex].Complete(lastParam);
					if (lastParam == "") {
						return partialCommand + completion;
					}
					return partialCommand[..^lastParam.Length] + completion;
				}
			}
			return partialCommand;
		}

		public void SaveToFile(string filePath) {
			string contents = Regex.Replace(Content, "<color.*?>", string.Empty);
			System.IO.File.WriteAllText(filePath, contents);
		}
		
		public string GetCommandHistory(int offset) {
			return offset > 0 && offset <= History.Count ? History[^offset] : null;
		}

		//////////////////////////////////////////////////

		// Font face: slightly modified "Larry 3D" found here: https://patorjk.com/software/taag/#p=display&f=Larry%203D&t=~tilde~
		const string STARTING_TEXT = @"
  ___  _   __         ___       __            ___  _
 /   \/ \ /\ \__  __ /\_ \     /\ \          /   \/ \
/\_/\__// \ \  _\/\_\\//\ \    \_\ \     __ /\_/\__//
\//\/__/   \ \ \/\/\ \ \ \ \   / _  \  / __`\//\/__/
            \ \ \_\ \ \ \_\ \_/\ \/\ \/\  __/
             \ \__\\ \_\/\____\ \___,_\ \____\
              \/__/ \/_/\/____/\/__,_ /\/____/

To view available commands, type 'help'";
		
		static readonly Dictionary<string, RegisteredCommand> registeredCommands = new();
		Scrollback Scrollback = new();
		
		//////////////////////////////////////////////////

		#region MonoBehavior
		void Awake() {
			AppendOutput(LogLineType.Normal, STARTING_TEXT);
			
			// Listen for Debug.Log calls.
			Application.logMessageReceived += OnUnityLogMessage;
			
			RegisterCommands();
			
			// Add a few special commands
			registeredCommands["help"] = new RegisteredCommand {
				Docs = "View available commands as well as their documentation.",
				Action = Help
			};
			registeredCommands["bind"] = new RegisteredCommand {
				Docs = "Syntax: 'bind <key> <command>' Bind a console command to a key.",
				Action = KeyBindings.Bind
			};
			registeredCommands["unbind"] = new RegisteredCommand {
				Docs = "Syntax: 'unbind <key>' Unbind a console command from a key.",
				Action = KeyBindings.Unbind
			};
			
			Completer = new Autocompleter(registeredCommands.Keys);
		}
		
		void OnDestroy() {
			Application.logMessageReceived -= OnUnityLogMessage;
		}
		#endregion
		
		/// <summary>
		/// Find the command with the given name.  Obeys case sensitivity setting when doing comparisons.
		/// </summary>
		/// <param name="commandName">The name to match</param>
		/// <param name="command">The matching registered command or null if none was found</param>
		/// <returns>True if <paramref name="commandName"/> matches a registered command (and <paramref name="command"/> is not null).  False otherwise.</returns>
		bool TryGetCommand(string commandName, out RegisteredCommand command) {
			foreach ((string registeredName, var registeredCommand) in registeredCommands) {
				if (commandName.Equals(registeredName, caseInsensitiveMatching ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) {
					command = registeredCommand;
					return true;
				}
			}
			command = null;
			return false;
		}

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
				int maxCommandLength = registeredCommands.Keys.Select(x => x.Length).Max();
				int docsColumnPadding = COMMAND_INDENT + maxCommandLength + COMMAND_DOCSTRING_PADDING;

				helpText.AppendLine("Available commands:");
				foreach (var commandEntry in registeredCommands.OrderBy(x => x.Key)) {
					string command = commandEntry.Key;
					string docstring = commandEntry.Value.Docs;

					// Add the command name and enough spaces to align the docstrings.
					helpText.AppendLine();
					helpText.Append(new string(' ', COMMAND_INDENT));
					helpText.Append(command);
					helpText.Append(new string(' ', maxCommandLength - command.Length + COMMAND_DOCSTRING_PADDING));

					// Add the docstring, wrapping and aligning to the column if necessary.
					int docsColumnSize = LINE_WIDTH - COMMAND_INDENT - maxCommandLength - COMMAND_DOCSTRING_PADDING;
					if (docsColumnSize <= 0) {
						continue;
					}
					bool padOutNewLine = false;
					while (docsColumnSize < docstring.Length) {
						if (padOutNewLine) {
							helpText.AppendLine();
							helpText.Append(new string(' ', docsColumnPadding));
						}
						helpText.Append(docstring[..docsColumnSize]);
						docstring = docstring[docsColumnSize..];
						padOutNewLine = true;
					}

					if (string.IsNullOrEmpty(docstring)) {
						continue;
					}
					if (padOutNewLine) {
						helpText.AppendLine();
						helpText.Append(new string(' ', docsColumnPadding));
					}
					helpText.Append(docstring);
				}
			} else {
				// Show help for a specific command.
				if (registeredCommands.TryGetValue(options[0], out var command)) {
					helpText.Append(command.Docs);
				} else {
					AppendOutput(LogLineType.Error, "Command not found: " + options[0]);
				}
			}
			return helpText.ToString();
		}

		/// Event handler for Unity's LogMessageReceived event 
		void OnUnityLogMessage(string message, string stackTrace, LogType type) {
			if (!showUnityLogMessages || string.IsNullOrEmpty(message)) {
				return;
			}
			
			switch (type) {
				case LogType.Assert:
				case LogType.Error:
				case LogType.Exception:
					AppendOutput(LogLineType.Error, message);
					break;
				case LogType.Warning:
					AppendOutput(LogLineType.Warning, message);
					break;
				case LogType.Log:
					AppendOutput(LogLineType.Normal, message);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		void AppendOutput(LogLineType type, string message) {
			Scrollback.Append(message, type);
			Changed.Invoke(Content);
		}

		/// Finds and registers all annotated functions in all assemblies.
		static void RegisterCommands() {
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				// Try to skip system assemblies
				if (assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("mscorlib") || assembly.FullName.StartsWith("Unity") || assembly.FullName.StartsWith("Mono")) {
					continue;
				}

				foreach (var type in assembly.GetTypes()) {
					foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)) {
						// Only find annotated static methods.
						var commandAttribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
						if (commandAttribute == null) {
							continue;
						}
						if (!method.IsStatic) {
							Debug.LogError($"[Tilde] Method {type}.{method.Name} must be static to be registered as a console command.");
							continue;
						}

						// Determine if it's a valid signature and distill it to a common type
						CommandAction action;
						if (Delegate.CreateDelegate(typeof(CommandAction), method, false) is CommandAction commandAction) {
							action = commandAction;
						} else if (Delegate.CreateDelegate(typeof(SimpleCommandAction), method, false) is SimpleCommandAction simpleAction) {
							action = _ => simpleAction();
						} else if (Delegate.CreateDelegate(typeof(SilentCommandAction), method, false) is SilentCommandAction silentAction) {
							action = args => { silentAction(args); return string.Empty; };
						} else if (Delegate.CreateDelegate(typeof(SimpleSilentCommandAction), method, false) is SimpleSilentCommandAction simpleSilentAction) {
							action = _ => { simpleSilentAction(); return string.Empty; };
						} else {
							Debug.LogError($"[Tilde] Method {type}.{method.Name} is annotated as a command but does not have a valid signature.");
							continue;
						}

						// Find and record any completion attributes
						Autocompleter[] autocompleters = null;
						if (method.GetCustomAttributes(typeof(CompletionAttribute), true) is CompletionAttribute[] completionAttributes) {
							int maxArgIndex = -1;
							foreach (var completion in completionAttributes) {
								maxArgIndex = Math.Max(completion.ArgIndex, maxArgIndex);
							}
							
							if (maxArgIndex >= 0) {
								autocompleters = new Autocompleter[maxArgIndex + 1];
								foreach (var completion in completionAttributes) {
									autocompleters[completion.ArgIndex] = new Autocompleter(completion.Options);
								}
							}
						}

						string commandName = !string.IsNullOrEmpty(commandAttribute.CommandName) ? commandAttribute.CommandName : method.Name;
						registeredCommands[commandName] = new RegisteredCommand {
							Docs = commandAttribute.Docstring,
							Action = action,
							Completers = autocompleters
						};
					}
				}
			}
		}
	}

	public class BoundCommands {
		public readonly Dictionary<KeyCode, string> Bindings = new();

		public string Bind(string[] args) {
			if (args.Length < 2) {
				Debug.LogError("You must specify a key and a command as arguments to 'Bind'.");
				return "";
			}

			var key = KeyCodeFromString(args[0]);
			if (key == KeyCode.None) {
				return string.Empty;
			}
			string command = string.Join(" ", args.Skip(1).ToArray());
			Bindings[key] = command;
			return string.Empty;
		}

		public string Unbind(string[] args) {
			if (args.Length != 1) {
				Debug.LogError("Command 'Unbind' only takes 1 argument.");
				return "";
			}

			var key = KeyCodeFromString(args[0]);
			if (key != KeyCode.None) {
				Bindings.Remove(key);
			}
			return "";
		}
		
		//////////////////////////////////////////////////

		static KeyCode KeyCodeFromString(string keyString) {
			if (keyString.Length == 1) {
				keyString = keyString.ToUpper();
			}

			var key = KeyCode.None;
			try {
				key = (KeyCode)Enum.Parse(typeof(KeyCode), keyString);
			} catch (ArgumentException) {
				Debug.LogError("Key '" + keyString + "' does not specify a key code.");
			}
			return key;
		}
	}
}
