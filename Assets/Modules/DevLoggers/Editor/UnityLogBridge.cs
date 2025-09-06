using Modules.DevLoggers;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Modules.DevloggerEditor
{
	[InitializeOnLoad]
	public static class UnityLogBridge
	{
		static UnityLogBridge()
		{
			// Subscribe when Editor loads
			Application.logMessageReceived += HandleUnityLog;
			EditorApplication.quitting += OnEditorQuit;
		}

		private static void OnEditorQuit()
		{
			// Unsubscribe to avoid dangling references
			Application.logMessageReceived -= HandleUnityLog;
			EditorApplication.quitting -= OnEditorQuit;
		}

		private static void HandleUnityLog(string logString, string stackTrace, LogType type)
		{
			string tag = "Unity";

			InfoDevLog info = new InfoDevLog(logString, tag, type, 
				ExtractFilePath(stackTrace), ExtractLineNumber(stackTrace));

			DevLogger.AddToStored(info);
		}

		private static string ExtractFilePath(string stackTrace)
		{
			if (string.IsNullOrEmpty(stackTrace)) return "";
			int idx = stackTrace.IndexOf(".cs");
			if (idx < 0) return "";
			int start = stackTrace.LastIndexOf(" ", idx) + 1;
			return stackTrace.Substring(start, idx + 3 - start);
		}

		private static int ExtractLineNumber(string stackTrace)
		{
			if (string.IsNullOrEmpty(stackTrace)) return 0;
			int idx = stackTrace.IndexOf(".cs");
			if (idx < 0) return 0;
			int colon = stackTrace.IndexOf(":", idx);
			if (colon < 0) return 0;
			string numStr = new string(stackTrace.Skip(colon + 1).TakeWhile(char.IsDigit).ToArray());
			return int.TryParse(numStr, out int line) ? line : 0;
		}
	}
}