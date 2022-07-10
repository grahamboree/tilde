using System.Collections.Generic;
using System.Linq;

namespace Tilde {
	public class Autocompleter {
		public Autocompleter(IEnumerable<string> options) {
			this.options = options;
		}

		public string Complete(string partialSelection) {
			if (partialSelection.StartsWith(partial) && currentCompletionOffset > 0) {
				partialSelection = partial;
			} else {
				partial = partialSelection;
				currentCompletionOffset = 0;
			}
			var matches = options.Where(x => x.StartsWith(partialSelection)).ToList();
			int count = matches.Count;
			if (!matches.Contains(partialSelection)) {
				count++;
			}
			currentCompletionOffset %= count;
			string result = matches
				.Skip(currentCompletionOffset)
				.FirstOrDefault();
			currentCompletionOffset++;
			return result ?? partialSelection;
		}

		public void ResetCurrentState() {
			currentCompletionOffset = 0;
			partial = "";
		}

		//////////////////////////////////////////////////

		/// The original string we're completing.
		string partial = "";

		int currentCompletionOffset;
		readonly IEnumerable<string> options;
	}
}
