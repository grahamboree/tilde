using System;

namespace Tilde {
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public class ConsoleCommandAttribute : Attribute {
		public readonly string CommandName;
		public readonly string Docstring;

		public ConsoleCommandAttribute(string name = null, string docs = "") {
			CommandName = name;
			Docstring = docs;
		}
	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public class CompletionAttribute : Attribute {
		public readonly int ArgIndex;
		public readonly string[] Options;

		public CompletionAttribute(int argIndex, params string[] options) {
			ArgIndex = argIndex;
			Options = options;
		}
	}
}
