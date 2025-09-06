using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;

namespace Modules.DevLoggers
{
	public static class DevLogger
	{
		public static Dictionary<string, List<InfoDevLog>> Stored
		= new Dictionary<string, List<InfoDevLog>>();

		public static System.Action<InfoDevLog> OnNew;

		public static void AddToStored(InfoDevLog log)
		{
			if (!Stored.ContainsKey(log.Tag))
				Stored[log.Tag] = new List<InfoDevLog>();

			Stored[log.Tag].Add(log);
			OnNew?.Invoke(log);
		}

		public static void Log(string message,string tag = "General", 
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
		{
			AddToStored(new InfoDevLog(message, tag, LogType.Log, file, line));
		}

		public static void Warning(string message,	string tag = "General",
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
		{
			AddToStored(new InfoDevLog(message, tag, LogType.Warning, file, line));
		}

		public static void Error(string message, string tag = "General",
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
		{
			AddToStored(new InfoDevLog(message, tag, LogType.Error, file, line));
		}
	}

	[Serializable]
	public class InfoDevLog
	{
		public string Message;
		public string Tag;
		public LogType Type;
		public string FilePath;
		public int LineNumber;
		public int Frame;
		public System.DateTime Time;

		public InfoDevLog(string msg, string tag, LogType type, string file, int line)
		{
			Message = msg;
			Tag = tag;
			Type = type;
			FilePath = file;
			LineNumber = line;
			Frame = UnityEngine.Time.frameCount;
			Time = System.DateTime.Now;
		}
	}
}