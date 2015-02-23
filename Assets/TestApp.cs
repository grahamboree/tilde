﻿using UnityEngine;
using System.Collections;
using System.Linq;

public class TestApp : MonoBehaviour {
	void Start () {
		Console.instance.RegisterCommand("marco", _ => "polo");
		Console.instance.RegisterCommand("first", First);
		Console.instance.RegisterCommand("fail", Fail);
		Console.instance.RegisterCommand("res", SupportedResolutions);
	}
	
	string First(string[] args) {
		if (args.Length == 0) {
			throw new System.Exception("At least one parameter must be specified to First");
		}
		return args[0];
	}
	
	string Fail(string[] args) {
		throw new System.Exception("fail");
	}

	string SupportedResolutions(string[] args) {
		return string.Join("\n", Screen.resolutions.Select(x => x.width + "x" + x.height).ToArray());
	}
}
