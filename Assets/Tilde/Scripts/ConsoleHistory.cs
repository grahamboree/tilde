using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tilde {
public class ConsoleHistory {
	private List<string> history = new List<string>();
	private int currentHistoryOffset = 0;

	public string TryGetPreviousCommand() {
		if (currentHistoryOffset < history.Count) {
			currentHistoryOffset++;
			return history[history.Count - currentHistoryOffset];
		}
		return null;
	}

	public string TryGetNextCommand() {
		if (currentHistoryOffset > 0) {
			currentHistoryOffset--;
			if (currentHistoryOffset == 0) {
				return "";
			} else {
				return history[history.Count - currentHistoryOffset];
			}
		}
		return null;
	}

	public void AddCommandToHistory(string commandString) {
		history.Add(commandString);
		currentHistoryOffset = 0;
	}
}
}
