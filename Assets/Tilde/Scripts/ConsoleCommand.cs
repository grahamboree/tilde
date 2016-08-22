using System;


namespace Tilde {
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class ConsoleCommand : Attribute {
		public string CommandName;
		public string Docstring;

		public ConsoleCommand(string name = null, string docs = null) {
			CommandName = name;
			Docstring = docs;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public class Completion : Attribute {
		public int ArgIndex;
		public string[] Options;

		public Completion(int argIndex, params string[] options) {
			ArgIndex = argIndex;
			Options = options;
		}
	}
}
