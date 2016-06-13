using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tilde {
	public class Autocompleter {
		/// The original string we're completing.
		private string partial = "";

		private int currentCompletionOffset = 0;
		private IEnumerable<string> options;

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
			var matches = options.Where(x => x.StartsWith(partialSelection));
			int count = matches.Count();
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
	}
}
