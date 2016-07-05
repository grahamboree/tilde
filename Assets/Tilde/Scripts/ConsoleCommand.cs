using System;


namespace Tilde {
	[AttributeUsage(AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
	public class ConsoleCommand : Attribute {
		public string commandName;
		public string docstring;

		public ConsoleCommand(string name = null, string docs = null) {
			commandName = name;
			docstring = docs;
		}
	}
}
