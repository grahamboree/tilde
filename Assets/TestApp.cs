using UnityEngine;
using System.Collections;
using System.Linq;

public class TestApp : MonoBehaviour {
	void Start () {
	}

	[ConsoleCommand("marco help text")]
	public static string marco() {
		return "polo";
	}

	[ConsoleCommand]
	public static string first(string[] args) {
		if (args.Length == 0) {
			throw new System.Exception("At least one parameter must be specified to First");
		}
		return args[0];
	}

	[ConsoleCommand]
	public static void fail() {
		throw new System.Exception("fail");
	}

	[ConsoleCommand("at", "at docs")]
	public static string atTest(string[] options) {
		return "at body";
	}

	[ConsoleCommand("notmarco", "")]
	public static string Marco() {
		return "polo";
	}

	[ConsoleCommand]
	public static string Butts() {
		return "butts";
	}

	[ConsoleCommand]
	public static string Warning() {
		Debug.Log("This is what a log message from Unity looks like");
		Debug.LogWarning("This is what a warning from Unity looks like");
		Debug.LogError("This is what an error from Unity looks like");
		return "";
	}
}
